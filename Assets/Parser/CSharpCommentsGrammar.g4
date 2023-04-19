grammar CSharpCommentsGrammar;

// Skip rules
WS: [ \t\r\n]+ -> skip;
Namespace : 'namespace';
//Public: 'public';
//Private : 'private';
//Protected : 'protected';
//Static : 'static';
Using : 'using';
SEMICOLON : ';';


fragment CommentText : ~[\r\n<]* [/a-zA-Z0-9.#_="!-][/a-zA-Z0-9.;()_#"=!-]+;
fragment LinkText : ~[\r\n<]* [/a-zA-Z0-9.#_=!-][/a-zA-Z0-9.;()_#=!-]+;

// Define the rule for comments
Comment: ('///' | '///' CommentText);
//PARAM: '<param name="' TEXT* '">' TEXT* '</param>';


TEXT: [/a-zA-Z0-9&#_.=()+|-]+;
//[/a-zA-Z0-9.()_&#=|-]+;
SHORT_COMMENT: '//' -> skip;

EQUALS: '=';
LineComment: '/*' .*? '*/';
//Classname: [a-zA-Z_][a-zA-Z0-9_]*;

TEXT_SKIP: [/a-zA-Z0-9#_".!-][/a-zA-Z0-9.()_#!-]+ -> skip;
CURLY_BRACKET_OPEN: '{';
CURLY_BRACKET_CLOSE: '}';
//CLASS_LINK: '<see cref="' TEXT '"/>';

//className: TEXT;
PARAMREF: '<paramref name="' TEXT '"/>';

//Language specific

// Rule for class links
classLink
    : '<see cref="' linkID=TEXT* '"/>' ;
    
parameter
    : '<param name="' paramName=TEXT* '">' paramDescription=TEXT* '</param>';
    
paramref: PARAMREF;
//param: Comment* PARAM;
summary: '/// <summary>' comments '/// </summary>';
return: '/// <returns>' (comments | TEXT| classLink) ('/// </returns>' | '</returns>');

comment
    : Comment ((classLink) | parameter | (TEXT))*  ;

comments: 
        ( return
        | comment
        //| classLink
        | paramref TEXT*
        //| param
        )*;
        
line_comment
    : LineComment (classLink)?;

methodSignature
    :  nameAndParameters=TEXT+;

methodContent
    : (TEXT+ | SEMICOLON | EQUALS )*;

methodDeclaration
    : summary? methodSignature CURLY_BRACKET_OPEN methodContent CURLY_BRACKET_CLOSE;

// A Simple C# Scope
// This means a block of {} which also can inlude more other scopes or methods or classes   
scope
    : CURLY_BRACKET_OPEN ((scope | methodDeclaration | classDefinition)* | TEXT*) CURLY_BRACKET_CLOSE;  
     
 classContent
    : (scope
        |methodDeclaration
        | TEXT+
        | SEMICOLON)+;

// Declaration of an class
// All modifiers of the class signature are ignored since we dont need them
// If we need them in the future you might consider to use a predefined C# Grammar.
// We just dont use them because they usasly dont support the parsing of C# documentation.
classDefinition
    : summary? TEXT* 'class' className=TEXT CURLY_BRACKET_OPEN 
      classContent CURLY_BRACKET_CLOSE;

usingClause
    : Using TEXT+ SEMICOLON;

namespaceDeclaration
    : Namespace nameSpaceName=TEXT+ CURLY_BRACKET_OPEN 
      (namespaceDeclaration | classDefinition)* 
      CURLY_BRACKET_CLOSE;

start
    : ( classDefinition 
    | namespaceDeclaration
    | methodDeclaration
    | usingClause
    | CURLY_BRACKET_OPEN 
    | CURLY_BRACKET_CLOSE 
    | Comment
    | EQUALS )*;