grammar Mppg;

@lexer::members {
    // Syntax versioning.
    // Keywords are matched here, before any parser rule is reached. 
    // Keywords are gated since older scripts may use them as variable names.
    private int _syntaxVersionMajor = 1;
    private int _syntaxVersionMinor = 3;
    private bool _versionDirectiveApplied = false;

    public (int Major, int Minor) SyntaxVersion => (_syntaxVersionMajor, _syntaxVersionMinor);

    /// Programmatically sets the syntax version.
    public void SetSyntaxVersion(int major, int minor)
    {
        _syntaxVersionMajor = major;
        _syntaxVersionMinor = minor;
        _versionDirectiveApplied = true;
    }

    /// Applies a '#!syntax version X.Y' directive as it is lexed.
    /// Must be done early so that the keywords of the rest of the input are those of the declared version.
    /// Only the first directive of the program applies, and only if nothing but blanks precedes it, matching the preamble rule.
    private void TryApplyVersionDirective(string text)
    {
        if (_versionDirectiveApplied || !IsPrecededOnlyByBlanks())
            return;
        _versionDirectiveApplied = true;

        if (Unipi.Nancy.Playground.MppgParser.SyntaxVersion.TryParseShebang(text, out var version))
        {
            _syntaxVersionMajor = version.Major;
            _syntaxVersionMinor = version.Minor;
        }
    }

    /// True if nothing but spaces and tabs comes before the token being matched, i.e. it opens the
    /// input. A version directive is applied only there, which is where the preamble rule accepts it.
    private bool IsPrecededOnlyByBlanks()
    {
        if (TokenStartCharIndex == 0)
            return true;

        var before = ((ICharStream)InputStream)
            .GetText(Antlr4.Runtime.Misc.Interval.Of(0, TokenStartCharIndex - 1));

        foreach (var c in before)
        {
            if (c != ' ' && c != '\t')
                return false;
        }
        return true;
    }

    private bool IsVersionOrLater(int major, int minor)
    {
        return _syntaxVersionMajor > major
            || (_syntaxVersionMajor == major && _syntaxVersionMinor >= minor);
    }

    private bool IsVersion1_1OrLater() => IsVersionOrLater(1, 1);

    private bool IsVersion1_2OrLater() => IsVersionOrLater(1, 2);

    private bool IsVersion1_3OrLater() => IsVersionOrLater(1, 3);
}

@parser::members {
    public enum VariableType
    {
        Number,
        Function
    }

    private readonly Dictionary<string, VariableType> _variableTypes = new();
    // The keyword sets below are matched against the text of a token, so every lookup is paired with a
    // check that the token is not an IDENTIFIER: a gated keyword lexes as one where the declared
    // version has no such keyword, e.g. 'lowclosure' under 1.1, and is a variable there.
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
        "lowclosure",
        "nnlowclosure",
        "left-ext",
        "right-ext"
    };
    private static readonly HashSet<string> NumberReturningFunctionStarters = new()
    {
        "hDev",
        "hdev",
        "vDev",
        "vdev",
        "zDev",
        "zdev"
    };
    // Operators that return whatever kind their argument is, so their call site is classified by
    // scanning the argument rather than by the keyword alone.
    private static readonly HashSet<string> TypePreservingFunctionStarters = new()
    {
        "floor",
        "ceil"
    };
    // Operators that take scalars and return a scalar, so their call site is one wherever it appears.
    private static readonly HashSet<string> ScalarFunctionStarters = new()
    {
        "abs",
        "pow",
        "gcd",
        "lcm"
    };
    // Plot option names, grouped by the kind of value each takes.
    // They are contextual keywords: recognized as options only inside a plot argument list, and lexed
    // as IDENTIFIER everywhere else, so they stay available as variable names.
    private static readonly HashSet<string> StringPlotOptionNames = new()
    {
        "main",
        "title",
        "xlab",
        "ylab",
        "out"
    };
    private static readonly HashSet<string> IntervalPlotOptionNames = new()
    {
        "xlim",
        "ylim"
    };
    private static readonly HashSet<string> YesNoPlotOptionNames = new()
    {
        "grid",
        "bg",
        "gui"
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

    private bool IsPlotStringOption(string name) => StringPlotOptionNames.Contains(name);

    private bool IsPlotIntervalOption(string name) => IntervalPlotOptionNames.Contains(name);

    private bool IsPlotYesNoOption(string name) => YesNoPlotOptionNames.Contains(name);

    private bool IsFunctionSampleStart() =>
            IsFunctionVariable(CurrentToken.Text) && TokenStream.LT(2).Text == "(";

    private bool IsFunctionVariableReferenceAt(int lookaheadIndex)
    {
        var token = TokenStream.LT(lookaheadIndex);
        return token.Type == IDENTIFIER
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

        if (token.Type == IDENTIFIER && IsFunctionVariableReferenceAt(lookaheadIndex))
            return true;

        var typePreservingArgument = TryGetTypePreservingCallArgumentStart(lookaheadIndex);
        if (typePreservingArgument > 0)
            return ExpressionSegmentContainsFunction(typePreservingArgument);

        // version-gated keywords can be used as variable names in older versions
        if (token.Type != IDENTIFIER && FunctionExpressionStarters.Contains(text))
            return true;

        return false;
    }

    // True if the tokens from lookaheadIndex are a call of one of the given keywords.
    private bool IsCallOf(int lookaheadIndex, HashSet<string> starters)
    {
        var token = TokenStream.LT(lookaheadIndex);

        return token.Type != IDENTIFIER
            && starters.Contains(token.Text)
            && TokenStream.LT(lookaheadIndex + 1).Text == "(";
    }

    // Index at which the argument of a type-preserving call, i.e. floor(...) or ceil(...), starts,
    // or -1 if the tokens from lookaheadIndex are not such a call.
    private int TryGetTypePreservingCallArgumentStart(int lookaheadIndex)
    {
        return IsCallOf(lookaheadIndex, TypePreservingFunctionStarters) ? lookaheadIndex + 2 : -1;
    }

    private bool IsFunctionProductExpressionStart(int lookaheadIndex) =>
        IsFunctionOperandStart(lookaheadIndex)
        || IsNumberFunctionOperationStart(lookaheadIndex, "*")
        || IsNumberFunctionOperationStart(lookaheadIndex, "comp");

    private bool IsNumberFunctionOperationStart(int lookaheadIndex, string operation)
    {
        var numberEnd = TryGetNumberProductExpressionEnd(lookaheadIndex);
        return numberEnd > 0
            && TokenStream.LT(numberEnd).Text == operation
            && IsFunctionOperandStart(numberEnd + 1);
    }

    private bool IsNumberProductExpressionStart(int lookaheadIndex) =>
        TryGetNumberProductExpressionEnd(lookaheadIndex) > 0;

    // Index just past a chain of number operands joined by the product-level operators, matching numberProductExpression.
    // It stops at an operator whose right side is not a number, which is exactly where a mixed form like 1/2 * f or x * y * f hands over to the function side.
    private int TryGetNumberProductExpressionEnd(int lookaheadIndex)
    {
        var end = TryGetNumberUnaryExpressionEnd(lookaheadIndex);
        if (end < 0)
            return -1;

        while (IsNumberProductOperator(TokenStream.LT(end)))
        {
            var operandEnd = TryGetNumberUnaryExpressionEnd(end + 1);
            if (operandEnd < 0)
                return end;

            end = operandEnd;
        }

        return end;
    }

    // Index just past a number operand behind any number of signs, matching numberUnaryExpression.
    private int TryGetNumberUnaryExpressionEnd(int lookaheadIndex)
    {
        var index = lookaheadIndex;
        while (TokenStream.LT(index).Text is "+" or "-")
            index++;

        return TryGetNumberEnclosedExpressionEnd(index);
    }

    private bool IsNumberProductOperator(IToken token) =>
        token.Text == "*"
        || token.Text == "/"
        || token.Text == "div"
        || (token.Type != IDENTIFIER && token.Text == "mod");

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

        var typePreservingArgument = TryGetTypePreservingCallArgumentStart(lookaheadIndex);
        if (typePreservingArgument > 0)
            return !ExpressionSegmentContainsFunction(typePreservingArgument);

        if (IsCallOf(lookaheadIndex, ScalarFunctionStarters))
            return true;

        if (token.Type != IDENTIFIER)
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

        var typePreservingArgument = TryGetTypePreservingCallArgumentStart(lookaheadIndex);
        if (typePreservingArgument > 0)
            return FindMatchingRightParenthesis(typePreservingArgument - 1);

        if (IsCallOf(lookaheadIndex, ScalarFunctionStarters))
            return FindMatchingRightParenthesis(lookaheadIndex + 1);

        var numberReturningCallEnd = TryGetNumberReturningFunctionCallEnd(lookaheadIndex);
        if (numberReturningCallEnd > 0)
            return numberReturningCallEnd;

        if (token.Type == IDENTIFIER && IsNumberVariable(text))
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
    private bool ExpressionSegmentContainsFunctionUntil(int startIndex, Func<IToken, bool> isDelimiter)
    {
        var depth = 0;

        for (var index = startIndex;; index++)
        {
            var token = TokenStream.LT(index);
            var text = token.Text;

            if (token.Type == TokenConstants.EOF || token.Type == NEW_LINE)
                return false;

            if (depth == 0 && isDelimiter(token))
                return false;

            var numberReturningCallEnd = TryGetNumberReturningFunctionCallEnd(index);
            if (numberReturningCallEnd > 0)
            {
                index = numberReturningCallEnd - 1;
                continue;
            }

            if (token.Type == IDENTIFIER && IsFunctionVariableReferenceAt(index))
                return true;

            if (token.Type != IDENTIFIER && FunctionExpressionStarters.Contains(text))
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

    private bool IsExpressionDelimiter(IToken token) =>
        token.Text == ","
        || token.Text == ")"
        || token.Text == "="
        || token.Text == "!="
        || token.Text == "<"
        || token.Text == "<="
        || token.Text == ">"
        || token.Text == ">=";

    private bool IsSumExpressionDelimiter(IToken token) =>
        token.Text == "+"
        || token.Text == "-"
        || token.Text == "/\\"
        || token.Text == "\\/"
        || IsExpressionDelimiter(token);

    private bool IsProductExpressionDelimiter(IToken token) =>
        token.Text == "comp"
        || token.Text == "*"
        || token.Text == "*_"
        || token.Text == "*^"
        || token.Text == "/"
        || token.Text == "/_"
        || token.Text == "/^"
        || token.Text == "div"
        || (token.Type != IDENTIFIER && token.Text == "mod")
        || IsSumExpressionDelimiter(token);

    private int TryGetNumberReturningFunctionCallEnd(int lookaheadIndex)
    {
        var token = TokenStream.LT(lookaheadIndex);
        var text = token.Text;

        var isNumberReturningFunctionCall =
            token.Type == IDENTIFIER && IsFunctionVariable(text)
            || token.Type != IDENTIFIER && NumberReturningFunctionStarters.Contains(text);

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
VERSION_DIRECTIVE_START: '#!syntax' [\p{L}\p{Nd}\p{P}\p{S} \t]* { TryApplyVersionDirective(Text); };
DIRECTIVE_START: '#!' [\p{L}\p{Nd}\p{P}\p{S} \t]*;
INLINABLE_COMMENT: ('//'|'%'|'#') [\p{L}\p{Nd}\p{P}\p{S} \t]*;

// Keywords introduced after version 1.0.
// Each is a keyword only from the version that introduced it, and lexes as IDENTIFIER before that.
// A false predicate prunes the rule wherever it sits, which is what lets the name fall through: [Parr13] §15.7.
PRINT_EXPRESSION : 'printExpression' {IsVersion1_1OrLater()}?;
PLOT_TIKZ : 'plotTikz' {IsVersion1_1OrLater()}?;
SUBADD_CLOSURE : 'subaddclosure' {IsVersion1_2OrLater()}?;
SUPERADD_CLOSURE : 'superaddclosure' {IsVersion1_2OrLater()}?;
LOWCLOSURE : 'lowclosure' {IsVersion1_2OrLater()}?;
NNLOWCLOSURE : 'nnlowclosure' {IsVersion1_2OrLater()}?;
FLOOR : 'floor' {IsVersion1_3OrLater()}?;
CEIL : 'ceil' {IsVersion1_3OrLater()}?;
ABS : 'abs' {IsVersion1_3OrLater()}?;
POW : 'pow' {IsVersion1_3OrLater()}?;
MOD_OP : 'mod' {IsVersion1_3OrLater()}?;
GCD : 'gcd' {IsVersion1_3OrLater()}?;
LCM : 'lcm' {IsVersion1_3OrLater()}?;

IDENTIFIER : [a-zA-Z_][a-zA-Z_0-9]*;

// Parser rules

// Entry point for parsing a self-contained script
program : preamble? statementLine (NEW_LINE statementLine)* NEW_LINE? EOF;

// Entry points for parsing one line, or one expression, in isolation.
// Needed to anchor those parses at EOF, so that input left over is reported rather than silently dropped.
statementEntry : statementLine EOF;
expressionEntry : expression EOF;

preamble : preambleStatement (NEW_LINE preambleStatement)* NEW_LINE?;
preambleStatement : versionDirective | directive;
versionDirective : VERSION_DIRECTIVE_START;
directive : DIRECTIVE_START;
statementLine: statement inlineComment? ;
statement
    : assignment
    | expressionCommand
    | plotCommand
    | plotTikzCommand
    | assertion
    | printExpressionCommand
    | versionDirective
    | directive
    | comment
    | empty;
assignment : name=IDENTIFIER ASSIGN value=expression { DeclareVariable($name.text, $value.ctx); } ;
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
// A predicate steers a choice only where prediction meets it first, i.e. at the left edge: [Parr13] §15.7.
functionSumStart
    : {IsFunctionProductExpressionStart(1)}? functionProductExpression #functionSumFunctionStart
    | {IsNumberProductExpressionStart(1)}? numberProductExpression op=(PLUS|MINUS|WEDGE|VEE) functionProductExpression #functionShiftMinMaxRev
    ;

functionSumSuffix
    : {SumOperandContainsFunction(2)}? op=(PLUS|MINUS|WEDGE|VEE) functionProductExpression #functionSumSubMinMaxSuffix
    | op=(PLUS|MINUS|WEDGE|VEE) numberProductExpression #functionShiftMinMaxSuffix
    ;

functionProductExpression
    : functionProductStart functionProductSuffix* #functionProductChain
    ;

// Product-level predicates distinguish convolution/composition from scalar multiplication, division, and sampling forms that share the same tokens.
// The scalar on the left of a product operator binds at the product tier: 1/2 * f groups as (1/2) * f, and x/y * f as (x/y) * f.
// The scalar side ends at the first operator whose right side is not a number: in x * y * f it is x * y.
functionProductStart
    : {IsFunctionOperandStart(1)}? functionUnaryExpression #functionProductFunctionStart
    | {IsNumberProductExpressionStart(1)}? numberProductExpression '*' functionUnaryExpression #functionScalarMulRev
    | {IsNumberProductExpressionStart(1)}? numberProductExpression 'comp' functionUnaryExpression #functionScalarCompositionRev
    ;

// The scalar on the right of a product operator binds at the unary tier, one operand at a time, so that a chain keeps folding left to right.
// f / 1/2 groups as (f / 1) / 2, as a / 1/2 does between scalars.
// The unary tier is what lets that operand carry a sign: f * -x.
functionProductSuffix
    : {ProductOperandContainsFunction(2)}? '*' functionUnaryExpression #functionMinPlusConvolutionSuffix
    | '*' numberUnaryExpression #functionScalarMulSuffix
    | '*_' functionUnaryExpression #functionMinPlusConvolutionSuffix
    | '*^' functionUnaryExpression #functionMaxPlusConvolutionSuffix
    | {ProductOperandContainsFunction(2)}? '/' functionUnaryExpression #functionMinPlusDeconvolutionSuffix
    | '/' numberUnaryExpression #functionScalarDivSuffix
    | '/_' functionUnaryExpression #functionMinPlusDeconvolutionSuffix
    | '/^' functionUnaryExpression #functionMaxPlusDeconvolutionSuffix
    | {IsFunctionOperandStart(2)}? 'comp' functionUnaryExpression #functionComposition
    | 'comp' numberUnaryExpression #functionScalarCompositionSuffix
    ;

functionUnaryExpression
    : PLUS functionUnaryExpression #functionPositive
    | MINUS functionUnaryExpression #functionNegative
    | functionEnclosedExpression #functionEnclosedExpressionExp
    ;

functionEnclosedExpression
    : {ExpressionSegmentContainsFunction(2)}? '(' functionExpression ')' #functionBrackets
    | 'star' '(' functionExpression ')' #functionSubadditiveClosure
    | SUBADD_CLOSURE '(' functionExpression ')' #functionSubadditiveClosure
    | SUPERADD_CLOSURE '(' functionExpression ')' #functionSuperadditiveClosure
    | ('hShift'|'hshift') '(' functionExpression ',' numberExpression ')' #functionHShift
    | ('vShift'|'vshift') '(' functionExpression ',' numberExpression ')' #functionVShift
    | ('inv'|'low_inv') '(' functionExpression ')' #functionLowerPseudoInverse
    | 'up_inv' '(' functionExpression ')' #functionUpperPseudoInverse
    | 'upclosure' '(' functionExpression ')' #functionUpNonDecreasingClosure
    | 'nnupclosure' '(' functionExpression ')' #functionNonNegativeUpNonDecreasingClosure
    | LOWCLOSURE '(' functionExpression ')' #functionLowNonDecreasingClosure
    | NNLOWCLOSURE '(' functionExpression ')' #functionNonNegativeLowNonDecreasingClosure
    | 'left-ext' '(' functionExpression ')' #functionLeftExt
    | 'right-ext' '(' functionExpression ')' #functionRightExt
    // floor and ceil return the kind of their argument: these take a curve, and the scalar forms are alternatives of numberExpression.
    | FLOOR '(' functionExpression ')' #functionFloor
    | CEIL '(' functionExpression ')' #functionCeil
    | functionConstructor #functionConstructorExp
    | {IsFunctionVariable(CurrentToken.Text)}? IDENTIFIER #functionVariableExp
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
increment: ',' rationalLiteral periodLenght?;
periodLenght: ',' rationalLiteral;

// Segments, the elements uaf and upp are built from.
sequence: element+;
element: point | segment;
point: '[' endpoint ']';
segment
    : segmentLeftOpenRightOpen
    | segmentLeftOpenRightClosed
    | segmentLeftClosedRightOpen
    | segmentLeftClosedRightClosed
    ;
// A segment runs between two endpoints, with the slope between them optional.
// The brackets say which endpoints it includes: '[' and ']' closed, ']' and '[' open.
endpoint: '(' numberExpression ',' numberExpression ')';
segmentLeftOpenRightOpen: ']' endpoint numberExpression? endpoint '[';
segmentLeftOpenRightClosed: ']' endpoint numberExpression? endpoint ']';
segmentLeftClosedRightOpen: '[' endpoint numberExpression? endpoint '[';
segmentLeftClosedRightClosed: '[' endpoint numberExpression? endpoint ']';

// Numbers
// One hierarchy of tiers, sum -> product -> unary -> atom, so that the atoms are spelled once and every construct takes the operand granularity it needs.
// A mixed scalar/function operator binds its scalar side at the product tier: f + 1/2 takes the whole fraction, and f - x + y groups as (f - x) + y.
numberExpression
    : numberExpression op=(PLUS|MINUS|WEDGE|VEE) numberProductExpression #numberSumSubMinMax
    | numberProductExpression #numberSumAtom
    ;

numberEnclosedExpression
    : numberReturningfunctionOperation #encNumberReturningfunctionOperationExp
    | FLOOR '(' numberExpression ')' #encNumberFloor
    | CEIL '(' numberExpression ')' #encNumberCeil
    | ABS '(' numberExpression ')' #encNumberAbs
    | POW '(' numberExpression ',' numberExpression ')' #encNumberPow
    | GCD '(' numberExpression ',' numberExpression ')' #encNumberGcd
    | LCM '(' numberExpression ',' numberExpression ')' #encNumberLcm
    | {!(ExpressionSegmentContainsFunction(2))}? '(' numberExpression ')' #encNumberBrackets
    | {IsNumberVariable(CurrentToken.Text)}? IDENTIFIER #encNumberVariableExp
    | numberLiteral #encNumberLiteralExp
    ;

// The product tier, and the scalar operand of a mixed sum operator (f + 1/2): a chain of number atoms joined by the product-level operators.
// It excludes +, -, /\ and \/: in f - x + y the scalar side is x alone, and the sum folds as (f - x) + y.
// It is left-recursive, so a chain folds left to right: 1/2/3 groups as (1/2)/3.
numberProductExpression
    : numberUnaryExpression #numberProductAtom
    | numberProductExpression op=(PROD_SIGN|DIV_SIGN|DIV_OP|MOD_OP) numberUnaryExpression #numberProductMulDiv
    ;

// The unary tier.
// The atom comes first so that a signed literal stays one literal: -3 is the literal -3.
// -x, -(x + y) and -f(3), which no literal can spell, take the sign alternatives.
numberUnaryExpression
    : numberEnclosedExpression #numberUnaryAtom
    | PLUS numberUnaryExpression #numberPositive
    | MINUS numberUnaryExpression #numberNegative
    ;

numberLiteral: (PLUS|MINUS)? NUMBER_ABS_LITERAL;

// A literal-only rational, for the positions that take a constant rather than an expression: the pseudo-period fields of upp, which are informational, and the plot intervals.
// It exists so that a fraction, which is how Nancy writes a non-decimal rational, is accepted wherever the same value written as an integer or a decimal is.
rationalLiteral: numberLiteral (DIV_SIGN numberLiteral)?;

// Number-returning function operations
numberReturningfunctionOperation
    : functionValueAt
    | functionLeftLimitAt
    | functionRightLimitAt
    | functionHorizontalDeviation
    | functionVerticalDeviation
    | functionZDeviation;
functionValueAt: {IsFunctionSampleStart()}? functionName '(' numberExpression ')';
functionLeftLimitAt: {IsFunctionSampleStart()}? functionName '(' numberExpression '~'? MINUS ')';
functionRightLimitAt: {IsFunctionSampleStart()}? functionName '(' numberExpression '~'? PLUS ')';
functionHorizontalDeviation : ('hDev'|'hdev') '(' functionExpression ',' functionExpression ')';
functionVerticalDeviation : ('vDev'|'vdev') '(' functionExpression ',' functionExpression ')';
functionZDeviation : ('zDev'|'zdev') '(' functionExpression ',' functionExpression ')';

// Plots
plotCommand: 'plot' '(' plotArg (',' plotArg)* ')';
plotTikzCommand: PLOT_TIKZ '(' plotArg (',' plotArg)* ')';
plotArg: functionName | plotOption;
functionName: {IsFunctionVariable(CurrentToken.Text)}? IDENTIFIER;
plotOption
    : {IsPlotStringOption(CurrentToken.Text)}? IDENTIFIER '=' stringExpression
    | {IsPlotIntervalOption(CurrentToken.Text)}? IDENTIFIER '=' interval
    | {IsPlotYesNoOption(CurrentToken.Text)}? IDENTIFIER '=' ('"no"'|'"yes"')
    ;

stringExpression
    : stringExpression '+' stringExpression
    | stringLiteral
    | stringVariable
    | numberLiteral;
stringLiteral: STRING_LITERAL;
stringVariable: {IsKnownVariable(CurrentToken.Text)}? IDENTIFIER;

interval: '[' rationalLiteral ',' rationalLiteral ']';

// Assertions
assertion
    : 'assert' '(' expression assertionOperator expression ')' ;
assertionOperator
    : '='
    | '!='
    | '<'
    | '<='
    | '>'
    | '>='
    ;

printExpressionCommand
    : PRINT_EXPRESSION '(' IDENTIFIER ')';
