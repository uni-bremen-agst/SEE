// This grammar is used to parse C# source code files to extract the documentation.
// It can extract both class and method documentation and also the signature of methods

grammar CSharpCommentsGrammar;

// Skip rules
WS: [ \t\r\n]+ -> skip;
Namespace : 'namespace';
Using : 'using';
SEMICOLON : ';';


fragment CommentText 
    : ~[\r\n<]* [/a-zA-Z0-9.#_="!-][/a-zA-Z0-9.;()_#"=!-]+;
fragment LinkText 
    : ~[\r\n<]* [/a-zA-Z0-9.#_=!-][/a-zA-Z0-9.;()_#=!-]+;

Dashes : '///';
SHORT_COMMENT: '//' TEXT* -> skip;
TEXT: [/a-zA-Z0-9&#_,.=()+|-]+;


EQUALS: '=';
LineComment: '/*' .*? '*/';


TEXT_SKIP: [/a-zA-Z0-9#_".!-][/a-zA-Z0-9.()_#!-]+ -> skip;
CURLY_BRACKET_OPEN: '{';
CURLY_BRACKET_CLOSE: '}';

// Startrule for parsing the comments
docs : Dashes* summary parameters? return? exceptionTag?;


paramref: '<paramref name="' TEXT '"/>';

//Language specific

// Rule for class links
classLink
    : '<see cref="' linkID=TEXT* '"/>' ;
    
parameters
    : parameter*;
    
parameter
    : '/// <param name="' paramName=TEXT* '">' paramDescription=TEXT* '</param>';
    
// summary context
summary
    : '/// <summary>' (comments | someText)* '/// </summary>';
    
exceptionType: TEXT+;

someText : (TEXT | '<' | '>')+;

exceptionTag:
    '/// <exception cref="' exceptionType '">' (someText | paramref)* ('/// </exception>' | '</exception>');


returnContent
    : (comments | (someText | classLink) )*;

return
    : '/// <returns>' returnContent ('/// </returns>' | '</returns>');


comments: 
        (Dashes (someText
        | classLink
        | paramref
        //| param
        )* );
