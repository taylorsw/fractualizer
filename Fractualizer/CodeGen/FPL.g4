grammar FPL;

prog : pdefines (fractal | raytracer) ;
pdefines : pdefine* ;

raytracer : inputs 'raytracer' identifier '{' global* tracer '}' ;
tracer : 'RgbaTrace(v4 pos)' block ;

fractal : inputs 'fractal' identifier '{' global* distanceEstimator colorFunc '}' ;
distanceEstimator : 'DE(v3 pos)' block ;
colorFunc : 'v3 Color(v3 pt)' block ;

inputs : (input | texture)* ;
input : ((inputType identifier ('=' literal)?) | (inputType identifier arrayDecl)) ';' ;
inputType : type ;
texture : 'texture' identifier ';' ;

global : globalVal | func ;
globalVal : 'global' localDecl ';' ;

pdefine : '#define' identifier ;
defCond : ((pifdef | pifndef) identifier) | pendif | pelse ;
pifdef: '#ifdef' ;
pifndef: '#ifndef' ;
pendif: '#endif' ;
pelse: '#else' ;

func : retType identifier '(' arglist ')' block ;
arglist : arg?
		| arg (',' arg)*
		;
arg : argMod? type identifier ;
argMod : 'ref' | 'out' ;
block : '{' blockStat* '}' ;

blockStat 
	: localDecl ';'
	| stat
	| optionalBlock
	;
	
optionalBlock : 'OPTIONAL' '{' blockStat* '}' ;

localDecl : (type identifier ('=' expr)?) | (type identifier arrayDecl+);
arrayDecl : '[' expr ']' ;

stat 
	: block
	| ifStat
	| forStat
	| expr ';'
	| ';'
	| keywordExpr
	| defCond
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
	| inputAccess
	| sample
	| fractalAccess
	| expr '.' identifier
	| expr '[' expr ']'
	| instantiation
	| funcCall
	| parExpr
	| expr binaryOp expr
	| expr assignmentOp expr
	| expr unaryOp
	| (prefixUnaryOp | unaryOp) expr
	| expr ternary
	| literal
	;

sample : 'sample' '(' inputAccess ',' expr ')' ;

inputAccess : 'inputs.' identifier ;
fractalAccess : 'fractal.' (identifier | funcCall) ;

ternary : '?' expr ':' expr ;

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
type : FloatType | IntType | BoolType | V2Type | V3Type | V4Type;

V2Type : 'v2' ;
V3Type : 'v3' ;
V4Type : 'v4' ;
BoolType : 'bool' ;
FloatType : 'float' ;
IntType : 'int' ;

literal :
	literalInt
	| literalFloat
	;

literalInt : IntLiteral ;
literalFloat : FloatLiteral;

identifier : ID ;

ID : Nondigit (Nondigit | Digit)* ;

IntLiteral : DecimalNumeral;

FloatLiteral : [0-9]+.[0-9]+ ;

fragment
Nondigit
    :   [a-zA-Z_]
    ;

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