grammar FPL;

fractal : 'fractal' Identifier '{' func* '}' ;
func : retType Identifier '(' arglist ')' block ;
arglist : arg?
		| arg (',' arg)*
		;
arg : type Identifier ;

block : '{' blockStat* '}' ;

blockStat 
	: localDecl ';'
	| stat
	;

localDecl : type Identifier '=' expr;

stat 
	: block
	| ifStat
	| forStat
	| expr ';'
	| ';'
	;

ifStat : 'if' parExpr stat ('else' stat)? ;

forStat : 'for' '(' forInit? ';' expr? ';' expr? ')' block ;

forInit : localDecl | expr ;

parExpr : '(' expr ')' ;

expr 
	: Identifier
	| INTLIT
	| FLOATLIT
	;

retType : type | 'void' ;
type : 'float' | 'int' | 'v3' ;

Identifier : LETTER LETTERORDIGIT* ;

LETTER : [a-zA-Z] ;
LETTERORDIGIT : [a-aA-z0-9$_] ;

INTLIT : [1-9]+[0-9]* ;
FLOATLIT : [0-9]+.[0-9]+ ;

WS  :  [ \t\r\n\u000C]+ -> skip ;

COMMENT
    :   '/*' .*? '*/' -> skip
    ;

LINE_COMMENT
    :   '//' ~[\r\n]* -> skip
    ;