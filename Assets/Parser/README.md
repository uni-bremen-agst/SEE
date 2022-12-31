# Adding a new grammar
It is recommended to use the grammars from [this repository](https://github.com/antlr/grammars-v4).
The repository is very extensive and should cover any required languages.

For some grammars, such as the [java grammar](https://github.com/antlr/grammars-v4/tree/master/java),
you have to choose an additional subdirectory, most likely the version of the language.
In our case, we have chosen [java9](https://github.com/antlr/grammars-v4/tree/master/java/java9).
In this directory, there should be at least one `.g4` file and a `CSharp` directory.
If the latter is not present, please contact someone somewhat knowledgeable about Antlr (probably Marcel) and ask them what to do.
If it is present, copy the file(s) inside that directory into this `Parser` directory.

## Guidelines for grammars
The grammar file will have the `.g4` file extension. 
Make sure you choose the lexer grammar, not the parser grammar, this should be indicated in the filename.
This grammar needs to conform to a few guidelines in order to be integrated into the code windows.
The java9 lexer grammar will be used as an example below.
If any of these conditions aren't true, please edit the grammar accordingly:
- No rule must resolve to `skip` or `channel(HIDDEN)`.
  - Example: 
  ```antlr
  COMMENT : '/*' .*? '*/' -> channel(HIDDEN);
  // this should be changed to the following instead:
  COMMENT : '/*' .*? '*/';
  ```
- There must be separate rules for whitespace and newlines.
  - Example (note that all three possible types of newlines must be accounted for):
  ```antlr
  WS : [ \t\r\n\u000C]+;
  // this should be changed to the following instead:
  WS : [ \t\u000C]+;
  NEWLINE : ('\r\n'|'\n'|'\r');
  ```
- There should be symbolic names (the names on the left-hand sides, e.g. `NEWLINE`) for the following:
  - Keywords, e.g. `new`, `protected`, `true`, `null` in Java (note that this includes boolean literals and null literals)
  - Number literals, e.g. `4` or `1.0f` in Java
  - String literals, e.g. `"hello"` or `'h'` in Java (note that this includes character literals)
  - Punctuation, e.g. `+`, `;`, `(`, `}` in Java (note that this includes separators and operators)
  - Identifiers, e.g. variable names (this category isn't strictly necessary, as it's rendered in the default color)
  - Comments, e.g. `// line` or `/* block */` in Java
  - Whitespaces and newlines, as mentioned above
  - If any of these are missing, this isn't a huge deal, the corresponding text will just be rendered white.
    The only token types which are absolutely required are the whitespace and newline tokens.
- Keep in mind that only symbolic names which don't have `fragment` in front of or above them are usable for us.

You can then drop the (modified, if necessary) grammar file into this directory.

## Generating Lexer Code
To generate the C# lexer code so we can use the grammar in SEE, do the following:
1. Open a terminal in this directory.
2. Run the `GenerateLexerCode.bat` file.
3. Verify that a new lexer file with the ending `.cs` has appeared. Remember its name.
   (You can ignore `.interp` and `.tokens` files.)

## Integrating with Code Windows
Finally, you have to edit the file `Assets/SEE/Game/UI/CodeWindow/TokenLanguage.cs` and add the new language:
1. There should be a region for other languages, e.g. `#region Java Language`, with the end marked by `#endregion`.
   Create a region for your language and name it accordingly.
2. In this region, create the following, using the Java Language region as an example, 
   and where `lang` is the name of your language:
   - `langFileName`: The name of the `.g4` grammar file.
   - `langExtensions`: A hashset of file extensions for your language.
     This is how your language's files will be identified.
   - `langKeywords`: A hashset of the **symbolic names** of your language's keywords.
     - Important note: These are the **symbolic names** on the left side of the grammar entries, 
       _not_ the keywords themselves (e.g. for Java it's `UNDER_SCORE` instead of `_`).
       This is true for all the other entries where we refer to symbolic names as well.
   - `langNumbers`: A hashset of the **symbolic names** for number literals of your language.
   - `langStrings`: A hashset of the **symbolic names** for string literals of your language.
   - `langPunctuation`: A hashset of the **symbolic names** for "punctuation" of your language.
   - `langIdentifiers`: A hashset of the **symbolic names** for identifiers of your language.
   - `langWhitespace`: A hashset of the **symbolic names** for whitespace of your language.
   - `langNewlines`: A hashset of the **symbolic names** for newlines of your language.
   - `langComments`: A hashset of the **symbolic names** for comments of your language.
   - If any of these have no symbolic names, use empty sets.
3. There is another region named `Static Types`, where each language has its own entry.
   Use the `Java` entry as an example, and create your entry like the following 
   (replacing `lang` with the name of your language):
   ```c#
    public static readonly TokenLanguage Lang = new TokenLanguage(langFileName, langExtensions, langKeywords, langNumbers,
        langStrings, langPunctuation, langIdentifiers, langWhitespace, langNewlines, langComments);
   ```
   - If, in your language, it is customary to use a number different than 4 spaces to be equivalent to a tab,
    add this number as the final parameter to the `new TokenLanguage` constructor call.
4. Finally, you'll need to edit the `CreateLexer` method.
   Add another switch branch below the existing ones like the following, once again
   replacing `lang` with the name of your language and replacing `LangLexer` with the name
   of the new lexer (most likely the name of the lexer file created earlier, you need to import its namespace):
   ```cs
    langFileName => new LangLexer(input),
   ```
5. And you're done! Try loading a file of this type into SEE and open it as a code window.
   If it works, you should see some color. If you don't, or if something seems wrong, check the logs.
