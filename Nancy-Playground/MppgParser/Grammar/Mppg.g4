grammar Mppg;

// lexer rules
NEW_LINE : [\r\n]+;
WHITE_SPACE : [ \t]+ -> skip;
VARIABLE_NAME : [a-zA-Z_][a-zA-Z_0-9]*;
ASSIGN : ':=';
STRING_LITERAL : '"' ~([\r\n"])*? '"';
COMMENT: '//' [a-zA-Z0-9 "'\t_\-,.:]+;

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
    | comment;
assignment : VARIABLE_NAME ASSIGN expression ;
expression : functionExpression | numberExpression;
comment: COMMENT;

// Functions
functionExpression
    :  '(' functionExpression ')' #functionBrackets
    | functionExpression '/\\' functionExpression #functionMinimum
    | functionExpression '\\/' functionExpression #functionMaximum
    | functionExpression ('*'|'*_') functionExpression #functionMinPlusConvolution
    | functionExpression '*^' functionExpression #functionMaxPlusConvolution
    | functionExpression ('/'|'/_') functionExpression #functionMinPlusDeconvolution
    | functionExpression '/^' functionExpression #functionMaxPlusDeconvolution
    | functionExpression 'comp' functionExpression #functionComposition
    | functionExpression '*' numberExpression #functionScalarMultiplicationLeft
    | numberExpression '*' functionExpression #functionScalarMultiplicationRight
    | functionExpression '/' numberExpression #functionScalarDivision
    | functionExpression '+' functionExpression #functionSum
    | functionExpression '-' functionExpression #functionSubtraction
    | functionConstructor #functionConstructorExp
    | 'star' '(' functionExpression ')' #functionSubadditiveClosure
    | 'left-ext' '(' functionExpression ')' #functionLeftExt
    | 'right-ext' '(' functionExpression ')' #functionRightExt
    | VARIABLE_NAME #functionVariableExp
    ;

// Function constructors
functionConstructor 
    : rateLatency 
    | tokenBucket
    | affineFunction
    | stairFunction
    | zeroFunction
    ;

rateLatency : 'ratency' '(' numberExpression ',' numberExpression ')';
tokenBucket : 'bucket' '(' numberExpression ',' numberExpression ')';
affineFunction : 'affine' '(' numberExpression ',' numberExpression ')';
stairFunction : 'stair' '(' numberExpression ',' numberExpression ',' numberExpression ')';
zeroFunction : 'zero';

// Numbers
numberExpression 
    : numberReturningfunctionOperation #numberReturningfunctionOperationExp 
    | '(' numberExpression ')' #numberBrackets
    | numberExpression '*' numberExpression #numberMultiplication
    | numberExpression '+' numberExpression #numberSum
    | numberExpression '/\\' numberExpression #numberMinimum
    | numberExpression '\\/' numberExpression #numberMaximum
    | VARIABLE_NAME #numberVariableExp
    | NUMBER_LITERAL #numberLiteralExp
    ;

// Number-returning function operations
numberReturningfunctionOperation 
    : functionHorizontalDeviation 
    | functionVerticalDeviation;
functionHorizontalDeviation : 'hDev' '(' functionExpression ',' functionExpression ')';
functionVerticalDeviation : 'vDev' '(' functionExpression ',' functionExpression ')';

// Plots
plotCommand: 'plot' '(' functionsToPlot (',' plotArgs)? ')';
functionsToPlot: VARIABLE_NAME (',' VARIABLE_NAME)*;
plotArgs: plotArg (',' plotArg)*;
plotArg
    : 'main' '=' string
    | 'xlim' '=' interval
    | 'ylim' '=' interval
    | 'xlab' '=' string
    | 'ylab' '=' string
    | 'out' '=' string
    | 'grid' '=' '"no"'
    | 'bg' '=' '"no"'
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
