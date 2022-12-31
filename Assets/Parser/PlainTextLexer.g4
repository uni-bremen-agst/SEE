// This lexer was not part of the grammars-v4 repository (see README.md) but was instead adapted 
// and in the process majorly changed from https://stackoverflow.com/a/28909022.

lexer grammar PlainTextLexer;

// A pseudoword is a word containing at least one special (OTHER) character.
PSEUDOWORD : (NUMBER|LETTERS)* OTHER (LETTERS|NUMBER|SPECIAL)+;

// A word consists of at least one letter and any number of numbers.
WORD : (NUMBER)* LETTER (LETTERS|NUMBER)+;

NEWLINES : NEWLINE+;

WHITESPACES : WHITESPACE+;

NUMBER : DIGIT+ ;

LETTERS : LETTER+ ;

SIGNS : SIGN+ ;

SPECIAL : OTHER+;

// Any non-space, non-newline, non-letter, non-sign, non-digit character.
fragment OTHER : ~('a'..'z' | 'A'..'Z' | '\u00C0'..'\u01F7' | [ \t\u000C] | '\n' | '\r' 
               | [.?!,;:*/()"'#$%^&_+=`~|<>{}] | '-' | '\u2010'..'\u2015' | '[' | ']' | '\\' | '0'..'9');

fragment WHITESPACE : [ \t\u000C];

fragment NEWLINE : ('\r\n'|'\n'|'\r');

// This encompasses all possible umlauts within this code range as well
fragment LETTER : ('a'..'z' | 'A'..'Z' | '\u00C0'..'\u01F7') ;

fragment SIGN : ([.?!,;:*/()"'#$%^&_+=`~|<>{}]|'-'|'\u2010'..'\u2015'|'['|']'|'\\');

fragment DIGIT : ('0'..'9') ;
