grammar CSharpCommentsGrammar;

// Skip rules
WS: [ \t\r\n]+ -> skip;

// Define the rule for comments
Comment: ('///' | '///' ~[\r\n<]* [/a-zA-Z0-9.#_="!-][/a-zA-Z0-9.;()_#"=!-]+);
PARAM: '<param name="' TEXT* '">' TEXT* '</param>';
TEXT: [/a-zA-Z0-9&#_."=()+|;-][/a-zA-Z0-9.;()_&#=|-]+;
SHORT_COMMENT: '//' -> skip;

EQUALS: '=';
LineComment: '/*' .*? '*/';
Classname: [a-zA-Z_][a-zA-Z0-9_]*;

TEXT_SKIP: [/a-zA-Z0-9#_".!-][/a-zA-Z0-9.;()_#!-]+ -> skip;
CURLY_BRACKET_OPEN: '{' -> skip;
CURLY_BRACKET_CLOSE: '}';
CLASS_LINK: '<see cref="' TEXT '"/>';

className: Classname;
PARAMREF: '<paramref name="' TEXT '"/>';

//Language specific
classLink: CLASS_LINK;
paramref: PARAMREF;
param: Comment* PARAM;
summary: '/// <summary>' comment* '/// </summary>';
return: '/// <returns>' (comment | TEXT| classLink)* ('/// </returns>' | '</returns>');
comment: summary
        | return TEXT
        | Comment (classLink)? Comment*
        | paramref TEXT*
        | param;
line_comment: LineComment (classLink)?;

start: (TEXT | CURLY_BRACKET_OPEN | CURLY_BRACKET_CLOSE | EQUALS | comment)*;