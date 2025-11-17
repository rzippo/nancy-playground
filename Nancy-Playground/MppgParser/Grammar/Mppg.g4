grammar Mppg;

// lexer rules
NEW_LINE : [\r\n]+;
WHITE_SPACE : [ \t]+ -> skip;
VARIABLE_NAME : [a-zA-Z_][a-zA-Z_0-9]*;
ASSIGN : ':=';
STRING_LITERAL : '"' ~([\r\n"])*? '"';
COMMENT: ('//'|'%'|'#'|'>') [\p{L}\p{Nd}\p{P}\p{S} \t]+;

// Number literals
NUMBER_LITERAL : RATIONAL_NUMBER_LITERAL | DECIMAL_NUMBER_LITERAL | INFINITE_NUMBER_LITERAL;
RATIONAL_NUMBER_LITERAL : [-+]?[0-9]+('/'[0-9]+)?;
DECIMAL_NUMBER_LITERAL : [-+]?[0-9]+('.'[0-9]+)?;
INFINITE_NUMBER_LITERAL : [-+]('inf'|'infinity'|'Infinity');

// parser rules
program : statement (NEW_LINE statement)* NEW_LINE? EOF;
statement 
    : assignment 
    | expression 
    | plotCommand 
    | printExpressionCommand
    | comment
    | empty;
assignment : VARIABLE_NAME ASSIGN expression ;
expression : functionExpression | numberExpression;
comment: COMMENT;
empty: ;

// Functions
functionExpression
    :  '(' functionExpression ')' #functionBrackets
    | functionExpression '/\\' functionExpression #functionMinimum
    | functionExpression '/\\' numberEnclosedExpression #functionMinimum
    | functionExpression '\\/' functionExpression #functionMaximum
    | functionExpression '\\/' numberEnclosedExpression #functionMaximum
    | functionExpression ('*'|'*_') functionExpression #functionMinPlusConvolution
    | functionExpression '*^' functionExpression #functionMaxPlusConvolution
    | functionExpression ('/'|'/_') functionExpression #functionMinPlusDeconvolution
    | functionExpression '/^' functionExpression #functionMaxPlusDeconvolution
    | functionExpression 'comp' functionExpression #functionComposition
    | functionExpression '*' numberEnclosedExpression #functionScalarMultiplicationLeft
    | numberEnclosedExpression '*' functionExpression #functionScalarMultiplicationRight
    | functionExpression '/' numberEnclosedExpression #functionScalarDivision
    | functionExpression '+' functionExpression #functionSum
    | functionExpression '+' numberEnclosedExpression #functionSum
    | functionExpression '-' functionExpression #functionSubtraction
    | functionExpression '-' numberEnclosedExpression #functionSubtraction
    | 'star' '(' functionExpression ')' #functionSubadditiveClosure
    | ('hShift'|'hshift') '(' functionExpression ',' numberExpression ')' #functionHShift
    | ('vShift'|'vshift') '(' functionExpression ',' numberExpression ')' #functionVShift
    | ('inv'|'low_inv') '(' functionExpression ')' #functionLowerPseudoInverse
    | 'up_inv' '(' functionExpression ')' #functionUpperPseudoInverse
    | 'upclosure' '(' functionExpression ')' #functionUpNonDecreasingClosure
    | 'nnupclosure' '(' functionExpression ')' #functionNonNegativeUpNonDecreasingClosure
    | 'left-ext' '(' functionExpression ')' #functionLeftExt
    | 'right-ext' '(' functionExpression ')' #functionRightExt
    | functionConstructor #functionConstructorExp
    | VARIABLE_NAME #functionVariableExp
    ;

// Function constructors
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
sequence: segment+;
segment
    : segmentLeftOpenRightOpen
    | segmentLeftOpenRightClosed
    | segmentLeftClosedRightOpen
    | segmentLeftClosedRightClosed
    ;

point: '(' numberLiteral ',' numberLiteral ')';
segmentLeftOpenRightOpen: ']' point numberLiteral point '[';
segmentLeftOpenRightClosed: ']' point numberLiteral point ']';
segmentLeftClosedRightOpen: '[' point numberLiteral point '[';
segmentLeftClosedRightClosed: '[' point numberLiteral point ']';

// Numbers
numberExpression 
    : numberReturningfunctionOperation #numberReturningfunctionOperationExp 
    | '(' numberExpression ')' #numberBrackets
    | numberExpression '*' numberExpression #numberMultiplication
    | numberExpression '/' numberExpression #numberDivision
    | numberExpression 'div' numberExpression #numberDivision
    | numberExpression '+' numberExpression #numberSum
    | numberExpression '-' numberExpression #numberSub
    | numberExpression '/\\' numberExpression #numberMinimum
    | numberExpression '\\/' numberExpression #numberMaximum
    | VARIABLE_NAME #numberVariableExp
    | numberLiteral #numberLiteralExp
    ;

numberEnclosedExpression
    : numberReturningfunctionOperation #encNumberReturningfunctionOperationExp
    | '(' numberExpression ')' #encNumberBrackets
    | VARIABLE_NAME #encNumberVariableExp
    | numberLiteral #encNumberLiteralExp
    ;

numberLiteral: NUMBER_LITERAL;

// Number-returning function operations
numberReturningfunctionOperation 
    : functionValueAt
    | functionLeftLimitAt
    | functionRightLimitAt
    | functionHorizontalDeviation 
    | functionVerticalDeviation;
functionValueAt: functionName '(' numberExpression ')';
functionLeftLimitAt: functionName '(' numberExpression '-' ')';
functionRightLimitAt: functionName '(' numberExpression '+' ')';
functionHorizontalDeviation : ('hDev'|'hdev') '(' functionExpression ',' functionExpression ')';
functionVerticalDeviation : ('vDev'|'vdev') '(' functionExpression ',' functionExpression ')';

// Plots
plotCommand: 'plot' '(' plotArg (',' plotArg)* ')';
plotArg: functionName | plotOption;
functionName: VARIABLE_NAME;
plotOption
    : 'main' '=' string
    | 'xlim' '=' interval
    | 'ylim' '=' interval
    | 'xlab' '=' string
    | 'ylab' '=' string
    | 'out' '=' string
    | 'grid' '=' ('"no"'|'"yes"')
    | 'bg' '=' ('"no"'|'"yes"')
    | 'browser' '=' ('"no"'|'"yes"')
    ;

string
    : string '+' string
    | STRING_LITERAL
    | VARIABLE_NAME
    | NUMBER_LITERAL;

interval: '[' NUMBER_LITERAL ',' NUMBER_LITERAL ']';

// extra commands
printExpressionCommand
    : 'printExpression' '(' VARIABLE_NAME ')';
