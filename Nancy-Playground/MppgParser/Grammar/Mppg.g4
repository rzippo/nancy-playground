grammar Mppg;

@parser::members {
    public enum VariableType
    {
        Number,
        Function
    }

    private readonly Dictionary<string, VariableType> _variableTypes = new();
    private static readonly HashSet<string> FunctionExpressionStarters = new()
    {
        "ratency",
        "bucket",
        "affine",
        "step",
        "stair",
        "delay",
        "zero",
        "epsilon",
        "uaf",
        "upp",
        "star",
        "subaddclosure",
        "superaddclosure",
        "hShift",
        "hshift",
        "vShift",
        "vshift",
        "inv",
        "low_inv",
        "up_inv",
        "upclosure",
        "nnupclosure",
        "left-ext",
        "right-ext"
    };
    private static readonly HashSet<string> NumberReturningFunctionStarters = new()
    {
        "hDev",
        "hdev",
        "vDev",
        "vdev"
    };

    public IReadOnlyDictionary<string, VariableType> VariableTypes => _variableTypes;

    public void SetVariableType(string name, VariableType type)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        _variableTypes[name] = type;
    }

    public void SetVariableTypes(IEnumerable<KeyValuePair<string, VariableType>> variableTypes)
    {
        foreach (var pair in variableTypes)
            SetVariableType(pair.Key, pair.Value);
    }

    private bool IsFunctionVariable(string name) =>
        _variableTypes.TryGetValue(name, out var type) && type == VariableType.Function;

    private bool IsNumberVariable(string name) =>
        _variableTypes.TryGetValue(name, out var type) && type == VariableType.Number;

    private bool IsKnownVariable(string name) =>
        _variableTypes.ContainsKey(name);

    private bool IsFunctionSampleStart() =>
            IsFunctionVariable(CurrentToken.Text) && TokenStream.LT(2).Text == "(";

    private bool IsFunctionVariableReferenceAt(int lookaheadIndex)
    {
        var token = TokenStream.LT(lookaheadIndex);
        return token.Type == VARIABLE_NAME
            && IsFunctionVariable(token.Text)
            && TokenStream.LT(lookaheadIndex + 1).Text != "(";
    }

    private bool IsSignedNumberLiteralStart(int lookaheadIndex)
    {
        var text = TokenStream.LT(lookaheadIndex).Text;
        return (text == "+" || text == "-")
            && TokenStream.LT(lookaheadIndex + 1).Type == NUMBER_ABS_LITERAL;
    }

    private bool IsFunctionOperandStart(int lookaheadIndex)
    {
        var token = TokenStream.LT(lookaheadIndex);
        var text = token.Text;

        if (token.Type == TokenConstants.EOF || token.Type == NEW_LINE)
            return false;

        if (text == "(")
            return ExpressionSegmentContainsFunction(lookaheadIndex + 1);

        if (text == "+" || text == "-")
            return IsFunctionOperandStart(lookaheadIndex + 1);

        if (token.Type == VARIABLE_NAME && IsFunctionVariableReferenceAt(lookaheadIndex))
            return true;

        return FunctionExpressionStarters.Contains(text);
    }

    private bool IsFunctionProductExpressionStart(int lookaheadIndex) =>
        IsFunctionOperandStart(lookaheadIndex)
        || IsNumberFunctionOperationStart(lookaheadIndex, "*")
        || IsNumberFunctionOperationStart(lookaheadIndex, "comp");

    private bool IsNumberFunctionOperationStart(int lookaheadIndex, string operation)
    {
        var numberEnd = TryGetNumberEnclosedExpressionEnd(lookaheadIndex);
        return numberEnd > 0
            && TokenStream.LT(numberEnd).Text == operation
            && IsFunctionOperandStart(numberEnd + 1);
    }

    private bool IsNumberEnclosedExpressionStart(int lookaheadIndex)
    {
        var token = TokenStream.LT(lookaheadIndex);
        var text = token.Text;

        if (token.Type == NUMBER_ABS_LITERAL)
            return true;

        if (IsSignedNumberLiteralStart(lookaheadIndex))
            return true;

        if (text == "(")
            return !ExpressionSegmentContainsFunction(lookaheadIndex + 1);

        if (token.Type != VARIABLE_NAME)
            return false;

        return IsNumberVariable(text)
            || IsFunctionVariable(text) && TokenStream.LT(lookaheadIndex + 1).Text == "("
            || NumberReturningFunctionStarters.Contains(text);
    }

    private int TryGetNumberEnclosedExpressionEnd(int lookaheadIndex)
    {
        if (!IsNumberEnclosedExpressionStart(lookaheadIndex))
            return -1;

        var token = TokenStream.LT(lookaheadIndex);
        var text = token.Text;

        if (token.Type == NUMBER_ABS_LITERAL)
            return lookaheadIndex + 1;

        if (IsSignedNumberLiteralStart(lookaheadIndex))
            return lookaheadIndex + 2;

        if (text == "(")
            return FindMatchingRightParenthesis(lookaheadIndex);

        var numberReturningCallEnd = TryGetNumberReturningFunctionCallEnd(lookaheadIndex);
        if (numberReturningCallEnd > 0)
            return numberReturningCallEnd;

        if (token.Type == VARIABLE_NAME && IsNumberVariable(text))
            return lookaheadIndex + 1;

        return -1;
    }

    private int FindMatchingRightParenthesis(int leftParenthesisIndex)
    {
        var depth = 0;

        for (var index = leftParenthesisIndex;; index++)
        {
            var token = TokenStream.LT(index);
            var text = token.Text;

            if (token.Type == TokenConstants.EOF)
                return -1;

            if (text == "(")
                depth++;
            else if (text == ")")
            {
                depth--;
                if (depth == 0)
                    return index + 1;
            }
        }
    }

    private bool ExpressionSegmentContainsFunction(int startIndex)
    {
        return ExpressionSegmentContainsFunctionUntil(startIndex, IsExpressionDelimiter);
    }

    private bool SumOperandContainsFunction(int startIndex)
    {
        return ExpressionSegmentContainsFunctionUntil(startIndex, IsSumExpressionDelimiter);
    }

    private bool ProductOperandContainsFunction(int startIndex)
    {
        return ExpressionSegmentContainsFunctionUntil(startIndex, IsProductExpressionDelimiter);
    }

    // Scans only the current expression segment or operand. 
    // Number-returning calls are skipped so f(x) and hDev(f, g) stay scalar at their call site.
    private bool ExpressionSegmentContainsFunctionUntil(int startIndex, Func<string, bool> isDelimiter)
    {
        var depth = 0;

        for (var index = startIndex;; index++)
        {
            var token = TokenStream.LT(index);
            var text = token.Text;

            if (token.Type == TokenConstants.EOF || token.Type == NEW_LINE)
                return false;

            if (depth == 0 && isDelimiter(text))
                return false;

            var numberReturningCallEnd = TryGetNumberReturningFunctionCallEnd(index);
            if (numberReturningCallEnd > 0)
            {
                index = numberReturningCallEnd - 1;
                continue;
            }

            if (token.Type == VARIABLE_NAME && IsFunctionVariableReferenceAt(index))
                return true;

            if (FunctionExpressionStarters.Contains(text))
                return true;

            if (text == "(")
                depth++;
            else if (text == ")")
            {
                if (depth == 0)
                    return false;

                depth--;
            }
        }
    }

    private bool IsExpressionDelimiter(string text) =>
        text == ","
        || text == ")"
        || text == "="
        || text == "!="
        || text == "<"
        || text == "<="
        || text == ">"
        || text == ">=";

    private bool IsSumExpressionDelimiter(string text) =>
        text == "+"
        || text == "-"
        || text == "/\\"
        || text == "\\/"
        || IsExpressionDelimiter(text);

    private bool IsProductExpressionDelimiter(string text) =>
        text == "comp"
        || text == "*"
        || text == "*_"
        || text == "*^"
        || text == "/"
        || text == "/_"
        || text == "/^"
        || text == "div"
        || IsSumExpressionDelimiter(text);

    private int TryGetNumberReturningFunctionCallEnd(int lookaheadIndex)
    {
        var token = TokenStream.LT(lookaheadIndex);
        var text = token.Text;

        var isNumberReturningFunctionCall =
            token.Type == VARIABLE_NAME && IsFunctionVariable(text)
            || NumberReturningFunctionStarters.Contains(text);

        if (!isNumberReturningFunctionCall || TokenStream.LT(lookaheadIndex + 1).Text != "(")
            return -1;

        return FindMatchingRightParenthesis(lookaheadIndex + 1);
    }

    private void DeclareVariable(string name, ExpressionContext expression)
    {
        var type = TryGetExpressionType(expression);
        if (type is not null)
            SetVariableType(name, type.Value);
    }

    private VariableType? TryGetExpressionType(ExpressionContext expression)
    {
        if (expression.functionExpression() is not null)
            return VariableType.Function;

        if (expression.numberExpression() is not null)
            return VariableType.Number;

        return null;
    }

    // Syntax versioning
    private int _syntaxVersionMajor = 1;
    private int _syntaxVersionMinor = 2;
    private bool _versionDirectiveSeen = false;
    private bool _inPreamble = false;

    public (int Major, int Minor) SyntaxVersion => (_syntaxVersionMajor, _syntaxVersionMinor);

    public void SetSyntaxVersion(int major, int minor)
    {
        _syntaxVersionMajor = major;
        _syntaxVersionMinor = minor;
    }

    private bool IsVersionDirective()
    {
        if (TokenStream.LA(1) != INLINABLE_COMMENT)
            return false;
        return TokenStream.LT(1).Text.StartsWith("#!syntax");
    }

    private void ParseVersionFromShebang(string shebang)
    {
        if (!_inPreamble || _versionDirectiveSeen)
            return;
        _versionDirectiveSeen = true;

        var parts = shebang.Split(new char[]{' ', '\t'}, System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 && parts[2].Contains('.'))
        {
            var versionParts = parts[2].Split('.');
            if (versionParts.Length == 2
                && int.TryParse(versionParts[0], out var major)
                && int.TryParse(versionParts[1], out var minor))
            {
                _syntaxVersionMajor = major;
                _syntaxVersionMinor = minor;
            }
        }
    }

    private bool AllowsPrintExpression()
    {
        return _syntaxVersionMajor > 1 || (_syntaxVersionMajor == 1 && _syntaxVersionMinor >= 1);
    }

    private bool AllowsPlotTikz()
    {
        return _syntaxVersionMajor > 1 || (_syntaxVersionMajor == 1 && _syntaxVersionMinor >= 1);
    }

    private bool AllowsSubaddClosure()
    {
        return _syntaxVersionMajor > 1 || (_syntaxVersionMajor == 1 && _syntaxVersionMinor >= 2);
    }

    private bool AllowsSuperaddClosure()
    {
        return _syntaxVersionMajor > 1 || (_syntaxVersionMajor == 1 && _syntaxVersionMinor >= 2);
    }

}

// lexer rules
NEW_LINE : [\r\n]+;
WHITE_SPACE : [ \t]+ -> skip;

NUMBER_ABS_LITERAL : INTEGER_LITERAL | DECIMAL_NUMBER_ABS_LITERAL | INFINITE_NUMBER_ABS_LITERAL;
INTEGER_LITERAL : [0-9]+;
DECIMAL_NUMBER_ABS_LITERAL : [0-9]+('.'[0-9]+)?;
INFINITE_NUMBER_ABS_LITERAL : 'inf'|'infinity'|'Infinity';

ASSIGN : ':=';
PLUS : '+';
MINUS : '-';
WEDGE : '/\\';
VEE : '\\/';
PROD_SIGN: '*';
DIV_SIGN: '/';
DIV_OP: 'div';
STRING_LITERAL : '"' ~([\r\n"])*? '"';
INLINABLE_COMMENT: ('//'|'%'|'#') [\p{L}\p{Nd}\p{P}\p{S} \t]*;
VARIABLE_NAME : [a-zA-Z_][a-zA-Z_0-9]*;

// parser rules
program : preamble? statementLine (NEW_LINE statementLine)* NEW_LINE? EOF;
preamble 
@init { _inPreamble = true; }
@after { _inPreamble = false; }
    : preambleStatement (NEW_LINE preambleStatement)* NEW_LINE?;
preambleStatement : versionDirective;
versionDirective : {IsVersionDirective()}? c=comment { ParseVersionFromShebang($c.text); };
statementLine: statement inlineComment? ;
statement
    : assignment
    | expressionCommand
    | plotCommand
    | {AllowsPlotTikz()}? plotTikzCommand
    | assertion
    | {AllowsPrintExpression()}? printExpressionCommand
    | versionDirective
    | comment
    | empty;
assignment : name=VARIABLE_NAME ASSIGN value=expression { DeclareVariable($name.text, $value.ctx); } ;
expressionCommand : expression;
expression : {ExpressionSegmentContainsFunction(1)}? functionExpression | numberExpression;
comment
    : INLINABLE_COMMENT
    // less precise that INLINABLE_COMMENT, but could not figure out a better way
    | '>' (~NEW_LINE)*?;
inlineComment: INLINABLE_COMMENT;
empty: ;

// Functions
functionExpression
    : functionSumExpression;

functionSumExpression
    : functionSumStart functionSumSuffix* #functionSumChain
    ;

// The start/suffix split preserves left-to-right folding while letting predicates
// classify mixed scalar/function operands before ANTLR commits to an alternative.
functionSumStart
    : {IsFunctionProductExpressionStart(1)}? functionProductExpression #functionSumFunctionStart
    | {IsNumberEnclosedExpressionStart(1)}? numberEnclosedExpression op=(PLUS|MINUS|WEDGE|VEE) functionProductExpression #functionShiftMinMaxRev
    ;

functionSumSuffix
    : {SumOperandContainsFunction(2)}? op=(PLUS|MINUS|WEDGE|VEE) functionProductExpression #functionSumSubMinMaxSuffix
    | op=(PLUS|MINUS|WEDGE|VEE) numberEnclosedExpression #functionShiftMinMaxSuffix
    ;

functionProductExpression
    : functionProductStart functionProductSuffix* #functionProductChain
    ;

// Product-level predicates distinguish convolution/composition from scalar
// multiplication, division, and sampling forms that share the same tokens.
functionProductStart
    : {IsFunctionOperandStart(1)}? functionUnaryExpression #functionProductFunctionStart
    | {IsNumberEnclosedExpressionStart(1)}? numberEnclosedExpression '*' functionUnaryExpression #functionScalarMulRev
    | {IsNumberEnclosedExpressionStart(1)}? numberEnclosedExpression 'comp' functionUnaryExpression #functionScalarCompositionRev
    ;

functionProductSuffix
    : {ProductOperandContainsFunction(2)}? '*' functionUnaryExpression #functionMinPlusConvolutionSuffix
    | '*' numberEnclosedExpression #functionScalarMulSuffix
    | '*_' functionUnaryExpression #functionMinPlusConvolutionSuffix
    | '*^' functionUnaryExpression #functionMaxPlusConvolutionSuffix
    | {ProductOperandContainsFunction(2)}? '/' functionUnaryExpression #functionMinPlusDeconvolutionSuffix
    | '/' numberEnclosedExpression #functionScalarDivSuffix
    | '/_' functionUnaryExpression #functionMinPlusDeconvolutionSuffix
    | '/^' functionUnaryExpression #functionMaxPlusDeconvolutionSuffix
    | {IsFunctionOperandStart(2)}? 'comp' functionUnaryExpression #functionComposition
    | 'comp' numberEnclosedExpression #functionScalarCompositionSuffix
    ;

functionUnaryExpression
    : PLUS functionUnaryExpression #functionPositive
    | MINUS functionUnaryExpression #functionNegative
    | functionEnclosedExpression #functionEnclosedExpressionExp
    ;

functionEnclosedExpression
    : {ExpressionSegmentContainsFunction(2)}? '(' functionExpression ')' #functionBrackets
    | 'star' '(' functionExpression ')' #functionSubadditiveClosure
    | {AllowsSubaddClosure()}? 'subaddclosure' '(' functionExpression ')' #functionSubadditiveClosure
    | {AllowsSuperaddClosure()}? 'superaddclosure' '(' functionExpression ')' #functionSuperadditiveClosure
    | ('hShift'|'hshift') '(' functionExpression ',' numberExpression ')' #functionHShift
    | ('vShift'|'vshift') '(' functionExpression ',' numberExpression ')' #functionVShift
    | ('inv'|'low_inv') '(' functionExpression ')' #functionLowerPseudoInverse
    | 'up_inv' '(' functionExpression ')' #functionUpperPseudoInverse
    | 'upclosure' '(' functionExpression ')' #functionUpNonDecreasingClosure
    | 'nnupclosure' '(' functionExpression ')' #functionNonNegativeUpNonDecreasingClosure
    | 'left-ext' '(' functionExpression ')' #functionLeftExt
    | 'right-ext' '(' functionExpression ')' #functionRightExt
    | functionConstructor #functionConstructorExp
    | {IsFunctionVariable(CurrentToken.Text)}? VARIABLE_NAME #functionVariableExp
    ;

functionConstructor
    : rateLatency
    | tokenBucket
    | affineFunction
    | stepFunction
    | stairFunction
    | delayFunction
    | zeroFunction
    | epsilonFunction
    | ultimatelyPseudoPeriodicFunction
    | ultimatelyAffineFunction
    ;

rateLatency : 'ratency' '(' numberExpression ',' numberExpression ')';
tokenBucket : 'bucket' '(' numberExpression ',' numberExpression ')';
affineFunction : 'affine' '(' numberExpression ',' numberExpression ')';
stepFunction : 'step' '(' numberExpression ',' numberExpression ')';
stairFunction : 'stair' '(' numberExpression ',' numberExpression ',' numberExpression ')';
delayFunction : 'delay' '(' numberExpression ')';
zeroFunction : 'zero' ;
epsilonFunction : 'epsilon' ;

// Ultimately Affine
ultimatelyAffineFunction: 'uaf' '(' sequence ')';

// Ultimately Pseudo-Periodic
ultimatelyPseudoPeriodicFunction: 'upp' '(' uppTransientPart?  uppPeriodicPart increment? ')';
uppTransientPart: sequence ',';
uppPeriodicPart: 'period' '(' sequence ')';
increment: ',' numberLiteral periodLenght?;
periodLenght: ',' numberLiteral;

// Segments
sequence: element+;
element: point | segment;
point: '[' endpoint ']';
segment
    : segmentLeftOpenRightOpen
    | segmentLeftOpenRightClosed
    | segmentLeftClosedRightOpen
    | segmentLeftClosedRightClosed
    ;
endpoint: '(' numberExpression ',' numberExpression ')';
segmentLeftOpenRightOpen: ']' endpoint numberExpression? endpoint '[';
segmentLeftOpenRightClosed: ']' endpoint numberExpression? endpoint ']';
segmentLeftClosedRightOpen: '[' endpoint numberExpression? endpoint '[';
segmentLeftClosedRightClosed: '[' endpoint numberExpression? endpoint ']';

// Numbers
numberExpression
    : numberReturningfunctionOperation #numberReturningfunctionOperationExp
    | '(' numberExpression ')' #numberBrackets
    | PLUS numberExpression #numberPositive
    | MINUS numberExpression #numberNegative
    | {IsNumberVariable(CurrentToken.Text)}? VARIABLE_NAME #numberVariableExp
    | numberLiteral #numberLiteralExp
    | numberExpression op=(PROD_SIGN|DIV_SIGN|DIV_OP) numberExpression #numberMulDiv
    | numberExpression op=(PLUS|MINUS|WEDGE|VEE) numberExpression #numberSumSubMinMax
    ;

numberEnclosedExpression
    : numberReturningfunctionOperation #encNumberReturningfunctionOperationExp
    | {!(ExpressionSegmentContainsFunction(2))}? '(' numberExpression ')' #encNumberBrackets
    | {IsNumberVariable(CurrentToken.Text)}? VARIABLE_NAME #encNumberVariableExp
    | numberLiteral #encNumberLiteralExp
    ;

numberLiteral: (PLUS|MINUS)? NUMBER_ABS_LITERAL;

// Number-returning function operations
numberReturningfunctionOperation
    : functionValueAt
    | functionLeftLimitAt
    | functionRightLimitAt
    | functionHorizontalDeviation
    | functionVerticalDeviation;
functionValueAt: {IsFunctionSampleStart()}? functionName '(' numberExpression ')';
functionLeftLimitAt: {IsFunctionSampleStart()}? functionName '(' numberExpression '~'? MINUS ')';
functionRightLimitAt: {IsFunctionSampleStart()}? functionName '(' numberExpression '~'? PLUS ')';
functionHorizontalDeviation : ('hDev'|'hdev') '(' functionExpression ',' functionExpression ')';
functionVerticalDeviation : ('vDev'|'vdev') '(' functionExpression ',' functionExpression ')';

// Plots
plotCommand: 'plot' '(' plotArg (',' plotArg)* ')';
plotTikzCommand: 'plotTikz' '(' plotArg (',' plotArg)* ')';
plotArg: functionName | plotOption;
functionName: {IsFunctionVariable(CurrentToken.Text)}? VARIABLE_NAME;
plotOption
    : 'main' '=' string
    | 'title' '=' string
    | 'xlim' '=' interval
    | 'ylim' '=' interval
    | 'xlab' '=' string
    | 'ylab' '=' string
    | 'out' '=' string
    | 'grid' '=' ('"no"'|'"yes"')
    | 'bg' '=' ('"no"'|'"yes"')
    | 'gui' '=' ('"no"'|'"yes"')
    ;

string
    : string '+' string
    | stringLiteral
    | stringVariable
    | numberLiteral;
stringLiteral: STRING_LITERAL;
stringVariable: {IsKnownVariable(CurrentToken.Text)}? VARIABLE_NAME;

interval: '[' numberLiteral ',' numberLiteral ']';

// Assertions
assertion
    : 'assert' '(' expression assertionOperator expression ')' ;
assertionOperator
    : '='
    | '!='
    | '<'   // custom addition
    | '<='
    | '>'   // custom addition
    | '>='
    ;

// extra commands
printExpressionCommand
    : 'printExpression' '(' VARIABLE_NAME ')';
