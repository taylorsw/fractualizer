grammar FPL;

fractal : input* 'fractal' identifier '{' global* distanceEstimator '}' ;

input : arg ;

distanceEstimator : 'DE()' block ;

global : globalVal | func ;
globalVal : 'global' localDecl ';' ;
func : retType identifier '(' arglist ')' block ;
arglist : arg?
		| arg (',' arg)*
		;
arg : argMod? type identifier ;
argMod : 'ref' ;

block : '{' blockStat* '}' ;

blockStat 
	: localDecl ';'
	| stat
	;

localDecl : type identifier '=' expr;

stat 
	: block
	| ifStat
	| forStat
	| expr ';'
	| ';'
	| keywordExpr
	;

keywordExpr
	: keywordSingle ';'
	| keywordPrefix expr ';'
	;

keywordSingle
	: 'break'
	| 'continue'
	;

keywordPrefix
	: 'return'
	| 'throw'
	;

ifStat : 'if' parExpr stat ('else' stat)? ;

forStat : 'for' '(' forInit? ';' expr? ';' forUpdate? ')' stat ;

forInit : localDecl | exprList ;

forUpdate : exprList ;

exprList
    :   expr (',' expr)*
    ;

parExpr : '(' expr ')' ;

expr 
	: identifier
	| expr '.' identifier
	| instantiation
	| funcCall
	| parExpr
	| expr binaryOp expr
	| expr assignmentOp expr
	| expr unaryOp
	| (prefixUnaryOp | unaryOp) expr
	| literal
	;

funcCall : identifier '(' exprList? ')' ;

instantiation : type '(' exprList? ')' ;

binaryOp : 
	'+' 
	| '-' 
	| '*' 
	| '%'
	| '/'
	| '<' 
	| '<=' 
	| '>' 
	| '>=' 
	| '==' 
	| '!=' 
	| '&&' 
	| '||'
	;

assignmentOp :
	'='
    | '+='
    | '-='
    | '*='
    | '/='
    | '&='
    | '|='
    | '^='
    | '>>='
    | '>>>='
    | '<<='
    | '%='
	;

unaryOp :
	'++'
	| '--'
	;

prefixUnaryOp :
	'+'
	| '-'
	| '!'
	;

retType : type | 'void' ;
type : 'float' | 'int' | 'v3' | 'v4' ;

literal :
	literalInt
	| literalFloat
	;

literalInt : IntLiteral ;
literalFloat : FloatLiteral;

identifier : ID ;

ID : Letter LetterOrDigit* ;

IntLiteral : DecimalNumeral;

FloatLiteral : [0-9]+.[0-9]+ ;

fragment
Letter : [a-zA-Z] ;

fragment
LetterOrDigit : [a-aA-z0-9$_] ;

fragment
DecimalNumeral
	: '0'
	| NonZeroDigit Digit*
	;

fragment
Digit :
	'0'
	| NonZeroDigit
	;

fragment
NonZeroDigit
	: [1-9]
	;

WS  :  [ \t\r\n\u000C]+ -> skip ;

COMMENT
    :   '/*' .*? '*/' -> skip
    ;

LINE_COMMENT
    :   '//' ~[\r\n]* -> skip
    ;