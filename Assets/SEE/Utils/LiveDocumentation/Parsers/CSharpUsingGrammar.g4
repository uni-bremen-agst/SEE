// A Antlr4 grammar for extrating using directives in C#

grammar CSharpUsingGrammar;

// Ignore whitespaces
WS
    : [ \t\r\n]+ -> skip;

Using : 'using';
Semicolon : ';';

TEXT: [/a-zA-Z0-9&#_.=(){}<>!"$:+|-]+ | '[' | ']' | '\'' | ',' ;

// An actual using statement
usingDirective 
    : Using namespace=TEXT* Semicolon;

// This parser rule should be used when extracting the using statements
start
    : (usingDirective | TEXT | Semicolon)*;