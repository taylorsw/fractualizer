grammar FPL;

ID : [a-z]+ ;             // match lower-case identifiers
WS : [ \t\r\n]+ -> skip ; // skip spaces, tabs, newlines

type : 'float' | 'int' | 'v3' ;
retType : type | 'void' ;

fractal : 'fractal' ID '{' func* '}' ;
func : retType ID '(' arglist ')' '{' stat* '}' ;
arglist : arg?
		| arg (',' arg)*
		;

arg : type ID ;
stat : assignStat
	 | declareStat
	 ;

declareStat : type ID '=' expr;
assignStat : lval '=' expr ;
expr : INTLIT
	 | FLOATLIT
	 ;

lval : ID ;

INTLIT : [1-9]+[0-9]* ;
FLOATLIT : [0-9]+.[0-9]+ ;