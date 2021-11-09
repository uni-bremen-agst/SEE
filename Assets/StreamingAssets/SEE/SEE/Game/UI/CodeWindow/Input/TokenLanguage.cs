using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;

namespace SEE.Game.UI.CodeWindow
{
    /// <summary>
    /// Represents a language a <see cref="SEEToken.Type"/> is in.
    /// Symbolic names for the antlr lexer are specified here.
    /// </summary>
    public class TokenLanguage
    {
        /// <summary>
        /// Default number of spaces a tab is equivalent to.
        /// </summary>
        private const int DEFAULT_TAB_WIDTH = 4;

        /// <summary>
        /// Language-independent symbolic name for the end of file token.
        /// </summary>
        private const string EOF = "EOF";
        
        /// <summary>
        /// File extensions which apply for the given language.
        /// May not intersect any other languages file extensions.
        /// </summary>
        public ISet<string> FileExtensions { get; }
        
        /// <summary>
        /// Name of the antlr lexer file the keywords were taken from.
        /// </summary>
        public string LexerFileName { get; }

        /// <summary>
        /// Number of spaces equivalent to a tab in this language.
        /// If not specified, this will be <see cref="DEFAULT_TAB_WIDTH"/>.
        /// </summary>
        public int TabWidth { get; }
        
        /// <summary>
        /// Symbolic names for comments of a language, including block, line, and documentation comments.
        /// </summary>
        public ISet<string> Comments { get; }

        /// <summary>
        /// Symbolic names for keywords of a language. This also includes boolean literals and null literals.
        /// </summary>
        public ISet<string> Keywords { get; }
        
        /// <summary>
        /// Symbolic names for number literals of a language. This includes integer literals, floating point literals, etc.
        /// </summary>
        public ISet<string> NumberLiterals { get; }
        
        /// <summary>
        /// Symbolic names for string literals of a language. Also includes character literals.
        /// </summary>
        public ISet<string> StringLiterals { get; }
        
        /// <summary>
        /// Symbolic names for separators and operators of a language.
        /// </summary>
        public ISet<string> Punctuation { get; }
        
        /// <summary>
        /// Symbolic names for identifiers in a language.
        /// </summary>
        public ISet<string> Identifiers { get; }
        
        /// <summary>
        /// Symbolic names for whitespace in a language, excluding newlines.
        /// </summary>
        public ISet<string> Whitespace { get; }
            
        /// <summary>
        /// Symbolic names for newlines in a language.
        /// </summary>
        public ISet<string> Newline { get; }
        
        #region Java Language

        /// <summary>
        /// Name of the Java antlr grammar lexer.
        /// </summary>
        private const string javaFileName = "Java9Lexer.g4";

        /// <summary>
        /// Set of java file extensions.
        /// </summary>
        private static readonly HashSet<string> javaExtensions = new HashSet<string>
        {
            "java"
        };

        /// <summary>
        /// Set of antlr type names for Java keywords.
        /// </summary>
        private static readonly HashSet<string> javaKeywords = new HashSet<string>
        {
            "ABSTRACT", "ASSERT", "BOOLEAN", "BREAK", "BYTE", "CASE", "CATCH", "CHAR", "CLASS", "CONST", "CONTINUE",
            "DEFAULT", "DO", "DOUBLE", "ELSE", "ENUM", "EXPORTS", "EXTENDS", "FINAL", "FINALLY", "FLOAT", "FOR",
            "IF", "GOTO", "IMPLEMENTS", "IMPORT", "INSTANCEOF", "INT", "INTERFACE", "LONG", "MODULE", "NATIVE", "NEW",
            "OPEN", "OPERNS", "PACKAGE", "PRIVATE", "PROTECTED", "PROVIDES", "PUBLIC", "REQUIRES", "RETURN", "SHORT",
            "STATIC", "STRICTFP", "SUPER", "SWITCH", "SYNCHRONIZED", "THIS", "THROW", "THROWS", "TO", "TRANSIENT",
            "TRANSITIVE", "TRY", "USES", "VOID", "VOLATILE", "WHILE", "WITH", "UNDER_SCORE",
            "BooleanLiteral", "NullLiteral"
        };
        /// <summary>
        /// Set of antlr type names for Java integer and floating point literals.
        /// </summary>
        private static readonly HashSet<string> javaNumbers = new HashSet<string> { "IntegerLiteral", "FloatingPointLiteral" };
        /// <summary>Set of antlr type names for Java character and string literals.</summary>
        private static readonly HashSet<string> javaStrings = new HashSet<string> { "CharacterLiteral", "StringLiteral" };
        /// <summary>Set of antlr type names for Java separators and operators.</summary>
        private static readonly HashSet<string> javaPunctuation = new HashSet<string> { "LPAREN", "RPAREN", "LBRACE",
            "RBRACE", "LBRACK", "RBRACK", "SEMI", "COMMA", "DOT", "ELLIPSIS", "AT", "COLONCOLON",
            "ASSIGN", "GT", "LT", "BANG", "TILDE", "QUESTION", "COLON", "ARROW", "EQUAL", "LE", "GE", "NOTEQUAL", "AND",
            "OR", "INC", "DEC", "ADD", "SUB", "MUL", "DIV", "BITAND", "BITOR", "CARET", "MOD",
            "ADD_ASSIGN", "SUB_ASSIGN", "MUL_ASSIGN", "DIV_ASSIGN", "AND_ASSIGN", "OR_ASSIGN", "XOR_ASSIGN",
            "MOD_ASSIGN", "LSHIFT_ASSIGN", "RSHIFT_ASSIGN", "URSHIFT_ASSIGN"
        };
        /// <summary>Set of antlr type names for Java identifiers.</summary>
        private static readonly HashSet<string> javaIdentifiers = new HashSet<string> { "Identifier" };
        /// <summary>
        /// Set of antlr type names for Java whitespace.
        /// </summary>
        private static readonly HashSet<string> javaWhitespace = new HashSet<string> { "WS" };
        /// <summary>
        /// Set of antlr type names for Java newlines.
        /// </summary>
        private static readonly HashSet<string> javaNewlines = new HashSet<string> { "NEWLINE" };
        /// <summary>
        /// Set of antlr type names for Java comments.
        /// </summary>
        private static readonly HashSet<string> javaComments = new HashSet<string> { "COMMENT", "LINE_COMMENT" };


        #endregion

        #region C# Language

        /// <summary>
        /// Name of the C# antlr grammar lexer.
        /// </summary>
        private const string cSharpFileName = "CSharpLexer.g4";

        /// <summary>
        /// Set of CSharp file extensions.
        /// </summary>
        private static readonly HashSet<string> cSharpExtensions = new HashSet<string>
        {
            "cs"
        };

        /// <summary>
        /// Set of antlr type names for CSharp keywords.
        /// </summary>
        private static readonly HashSet<string> cSharpKeywords = new HashSet<string>
        {
            // General keywords
            "ABSTRACT", "ADD", "ALIAS", "ARGLIST", "AS", "ASCENDING", "ASYNC", "AWAIT", "BASE", "BOOL", "BREAK", "BY",
            "BYTE", "CASE", "CATCH", "CHAR", "CHECKED", "CLASS", "CONST", "CONTINUE", "DECIMAL", "DEFAULT", "DELEGATE",
            "DESCENDING", "DO", "DOUBLE", "DYNAMIC", "ELSE", "ENUM", "EQUALS", "EVENT", "EXPLICIT", "EXTERN", "FALSE",
            "FINALLY", "FIXED", "FLOAT", "FOR", "FOREACH", "FROM", "GET", "GOTO", "GROUP", "IF", "IMPLICIT", "IN", "INT",
            "INTERFACE", "INTERNAL", "INTO", "IS", "JOIN", "LET", "LOCK", "LONG", "NAMEOF", "NAMESPACE", "NEW", "NULL_",
            "OBJECT", "ON", "OPERATOR", "ORDERBY", "OUT", "OVERRIDE", "PARAMS", "PARTIAL", "PRIVATE", "PROTECTED",
            "PUBLIC", "READONLY", "REF", "REMOVE","RETURN", "SBYTE", "SEALED", "SELECT", "SET", "SHORT", "SIZEOF",
            "STACKALLOC", "STATIC", "STRING", "STRUCT", "SWITCH", "THIS", "THROW", "TRUE", "TRY", "TYPEOF", "UINT",
            "ULONG", "UNCHECKED", "UNMANAGED", "UNSAFE", "USHORT", "USING", "VAR", "VIRTUAL", "VOID", "VOLATILE", "WHEN",
            "WHERE", "WHILE", "YIELD", "SHARP",
            // Directive keywords (anything within a directive is treated as a keyword, similar to IDEs
            "DIRECTIVE_TRUE", "DIRECTIVE_FALSE", "DEFINE", "UNDEF", "DIRECTIVE_IF",
            "ELIF", "DIRECTIVE_ELSE", "ENDIF", "LINE", "ERROR", "WARNING", "REGION", "ENDREGION", "PRAGMA", "NULLABLE", 
            "DIRECTIVE_DEFAULT", "DIRECTIVE_HIDDEN", "DIRECTIVE_OPEN_PARENS", "DIRECTIVE_CLOSE_PARENS", "DIRECTIVE_BANG",
            "DIRECTIVE_OP_EQ", "DIRECTIVE_OP_NE", "DIRECTIVE_OP_AND", "DIRECTIVE_OP_OR", "CONDITIONAL_SYMBOL"
        };
        /// <summary>
        /// Set of antlr type names for CSharp integer and floating point literals.
        /// </summary>
        private static readonly HashSet<string> cSharpNumbers = new HashSet<string> { 
            "LITERAL_ACCESS", "INTEGER_LITERAL", "HEX_INTEGER_LITERAL", "BIN_INTEGER_LITERAL", "REAL_LITERAL", "DIGITS"
        };
        /// <summary>Set of antlr type names for CSharp character and string literals.</summary>
        private static readonly HashSet<string> cSharpStrings = new HashSet<string> { 
            "CHARACTER_LITERAL", "REGULAR_STRING", "VERBATIUM_STRING", "INTERPOLATED_REGULAR_STRING_START",
            "INTERPOLATED_VERBATIUM_STRING_START", "VERBATIUM_DOUBLE_QUOTE_INSIDE",
            "DOUBLE_QUOTE_INSIDE", "REGULAR_STRING_INSIDE", "VERBATIUM_INSIDE_STRING"
        };
        /// <summary>Set of antlr type names for CSharp separators and operators.</summary>
        private static readonly HashSet<string> cSharpPunctuation = new HashSet<string> { 
            "OPEN_BRACE", "CLOSE_BRACE", "OPEN_BRACKET",
            "CLOSE_BRACKET", "OPEN_PARENS", "CLOSE_PARENS", "DOT", "COMMA", "COLON", "SEMICOLON", "PLUS", "MINUS", "STAR", "DIV",
            "PERCENT", "AMP", "BITWISE_OR", "CARET", "BANG", "TILDE", "ASSIGNMENT", "LT", "GT", "INTERR", "DOUBLE_COLON",
            "OP_COALESCING", "OP_INC", "OP_DEC", "OP_AND", "OP_OR", "OP_PTR", "OP_EQ", "OP_NE", "OP_LE", "OP_GE", "OP_ADD_ASSIGNMENT",
            "OP_SUB_ASSIGNMENT", "OP_MULT_ASSIGNMENT", "OP_DIV_ASSIGNMENT", "OP_MOD_ASSIGNMENT", "OP_AND_ASSIGNMENT", "OP_OR_ASSIGNMENT",
            "OP_XOR_ASSIGNMENT", "OP_LEFT_SHIFT", "OP_LEFT_SHIFT_ASSIGNMENT", "OP_COALESCING_ASSIGNMENT", "OP_RANGE",
            "DOUBLE_CURLY_INSIDE", "OPEN_BRACE_INSIDE", "REGULAR_CHAR_INSIDE"
        };
        /// <summary>Set of antlr type names for CSharp identifiers.</summary>
        private static readonly HashSet<string> cSharpIdentifiers = new HashSet<string>
        {
            "IDENTIFIER", "TEXT"
        };
        /// <summary>
        /// Set of antlr type names for CSharp whitespace.
        /// </summary>
        private static readonly HashSet<string> cSharpWhitespace = new HashSet<string>
        {
            "WHITESPACES", "DIRECTIVE_WHITESPACES"
        };
        /// <summary>
        /// Set of antlr type names for CSharp newlines.
        /// </summary>
        private static readonly HashSet<string> cSharpNewlines = new HashSet<string>
        {
            "NL", "TEXT_NEW_LINE", "DIRECTIVE_NEW_LINE"
        };
        /// <summary>
        /// Set of antlr type names for Java comments.
        /// </summary>
        private static readonly HashSet<string> cSharpComments = new HashSet<string>
        {
            "SINGLE_LINE_DOC_COMMENT", "DELIMITED_DOC_COMMENT", "SINGLE_LINE_COMMENT", "DELIMITED_COMMENT",
            "DIRECTIVE_SINGLE_LINE_COMMENT"
        };


        #endregion

        #region CPP Language

        /// <summary>
        /// Name of the antlr grammar lexer.
        /// </summary>
        private const string cppFileName = "CPP14Lexer.g4";

        /// <summary>
        /// Set of CPP file extensions.
        /// </summary>
        private static readonly HashSet<string> cppExtensions = new HashSet<string>
        {
            "cpp", "cxx", "hpp"
        };

        /// <summary>
        /// Set of antlr type names for CPP keywords.
        /// </summary>
        private static readonly HashSet<string> cppKeywords = new HashSet<string>
        {
            "Alignas", "Alignof", "Asm", "Auto", "Bool", "Break", "Case",
            "Catch", "Char", "Char16", "Char32", "Class", "Const", "Constexpr", "Const_cast",
            "Continue", "Decltype", "Default", "Delete", "Do", "Double", "Dynamic_cast",
            "Else", "Enum", "Explicit", "Export", "Extern", "False_", "Final", "Float",
            "For", "Friend", "Goto", "If", "Inline", "Int", "Long", "Mutable", "Namespace",
            "New", "Noexcept", "Nullptr", "Operator", "Override", "Private", "Protected",
            "Public", "Register", "Reinterpret_cast", "Return", "Short", "Signed",
            "Sizeof", "Static", "Static_assert", "Static_cast", "Struct", "Switch",
            "Template", "This", "Thread_local", "Throw", "True_", "Try", "Typedef",
            "Typeid_", "Typename_", "Union", "Unsigned", "Using", "Virtual", "Void",
            "Volatile", "Wchar", "While",
            "BooleanLiteral", "PointerLiteral", "UserDefinedLiteral",
            "MultiLineMacro", "Directive"
        };
        /// <summary>
        /// Set of antlr type names for CPP integer and floating point literals.
        /// </summary>
        private static readonly HashSet<string> cppNumbers = new HashSet<string>
        {
            "IntegerLiteral", "FloatingLiteral", "DecimalLiteral", "OctalLiteral", "HexadecimalLiteral",
            "BinaryLiteral", "Integersuffix", "UserDefinedIntegerLiteral", "UserDefinedFloatingLiteral"
        };
        /// <summary>Set of antlr type names for CPP character and string literals.</summary>
        private static readonly HashSet<string> cppStrings = new HashSet<string>
        {
            "StringLiteral", "CharacterLiteral", "UserDefinedStringLiteral", "UserDefinedCharacterLiteral"
        };
        /// <summary>Set of antlr type names for CPP separators and operators.</summary>
        private static readonly HashSet<string> cppPunctuation = new HashSet<string> 
        { 
            "LeftParen", "RightParen", "LeftBracket",
            "RightBracket", "LeftBrace", "RightBrace", "Plus", "Minus", "Star", "Div",
            "Mod", "Caret", "And", "Or", "Tilde", "Not", "Assign", "Less", "Greater",
            "PlusAssign", "MinusAssign", "StarAssign", "DivAssign", "ModAssign", "XorAssign",
            "AndAssign", "OrAssign", "LeftShiftAssign", "RightShiftAssign", "Equal",
            "NotEqual", "LessEqual", "GreaterEqual", "AndAnd", "OrOr", "PlusPlus",
            "MinusMinus", "Comma", "ArrowStar", "Arrow", "Question", "Colon", "Doublecolon",
            "Semi", "Dot", "DotStar", "Ellipsis"
        };
        /// <summary>Set of antlr type names for CPP identifiers.</summary>
        private static readonly HashSet<string> cppIdentifiers = new HashSet<string> { "Identifier" };
        /// <summary>
        /// Set of antlr type names for CPP whitespace.
        /// </summary>
        private static readonly HashSet<string> cppWhitespace = new HashSet<string> { "Whitespace" };
        /// <summary>
        /// Set of antlr type names for CPP newlines.
        /// </summary>
        private static readonly HashSet<string> cppNewlines = new HashSet<string> { "Newline" };
        /// <summary>
        /// Set of antlr type names for CPP comments.
        /// </summary>
        private static readonly HashSet<string> cppComments = new HashSet<string> { "BlockComment", "LineComment" };       

        #endregion

        #region Static Types

        /// <summary>
        /// A list of all token languages there are.
        /// </summary>
        public static readonly IList<TokenLanguage> AllTokenLanguages = new List<TokenLanguage>();

        /// <summary>
        /// Token Language for Java.
        /// </summary>
        public static readonly TokenLanguage Java = new TokenLanguage(javaFileName, javaExtensions, javaKeywords, javaNumbers,
            javaStrings, javaPunctuation, javaIdentifiers, javaWhitespace, javaNewlines, javaComments);

        /// <summary>
        /// Token Language for C#.
        /// </summary>
        public static readonly TokenLanguage CSharp = new TokenLanguage(cSharpFileName, cSharpExtensions, cSharpKeywords, cSharpNumbers,
            cSharpStrings, cSharpPunctuation, cSharpIdentifiers, cSharpWhitespace, cSharpNewlines, cSharpComments);

        /// <summary>
        /// Token Language for CPP.
        /// </summary>
        public static readonly TokenLanguage CPP = new TokenLanguage(cppFileName, cppExtensions, cppKeywords, cppNumbers,
            cppStrings, cppPunctuation, cppIdentifiers, cppWhitespace, cppNewlines, cppComments);


        #endregion

        /// <summary>
        /// Constructor for the token language.
        /// </summary>
        /// <remarks>Should never be accessible from outside this class.</remarks>
        /// <param name="lexerFileName">Name of this lexer grammar</param>
        /// <param name="fileExtensions">List of file extensions for this language</param>
        /// <param name="keywords">Keywords of this language</param>
        /// <param name="numberLiterals">Number literals of this language</param>
        /// <param name="stringLiterals">String literals of this language</param>
        /// <param name="punctuation">Punctuation for this language</param>
        /// <param name="identifiers">Identifiers for this language</param>
        /// <param name="whitespace">Whitespace for this language</param>
        /// <param name="newline">Newlines for this language</param>
        /// <param name="comments">Comments for this language</param>
        /// <param name="tabWidth">Number of spaces a tab is equivalent to</param>
        private TokenLanguage(string lexerFileName, ISet<string> fileExtensions, ISet<string> keywords, 
                              ISet<string> numberLiterals, ISet<string> stringLiterals, ISet<string> punctuation,
                              ISet<string> identifiers, ISet<string> whitespace, ISet<string> newline,
                              ISet<string> comments, int tabWidth = DEFAULT_TAB_WIDTH)
        {
            if (AllTokenLanguages.Any(x => x.LexerFileName.Equals(lexerFileName) || x.FileExtensions.Overlaps(fileExtensions)))
            {
                throw new ArgumentException("Lexer file name and file extensions must be unique per language!");
            }
            if (AnyOverlaps())
            {
                throw new ArgumentException("Symbolic names may not appear in more than one set each!");
            }
            LexerFileName = lexerFileName;
            FileExtensions = fileExtensions;
            Keywords = keywords;
            NumberLiterals = numberLiterals;
            StringLiterals = stringLiterals;
            Punctuation = punctuation;
            Identifiers = identifiers;
            Whitespace = whitespace;
            Newline = newline;
            Comments = comments;
            TabWidth = tabWidth;
            
            AllTokenLanguages.Add(this);

            // Check whether any of the symbolic names are used twice
            bool AnyOverlaps()
            {
                return keywords.Intersect(numberLiterals).Intersect(stringLiterals).Intersect(punctuation)
                               .Intersect(identifiers).Intersect(whitespace).Intersect(newline)
                               .Intersect(comments).Any();
            }
        }

        /// <summary>
        /// Returns the matching token language for the given <paramref name="lexerFileName"/>.
        /// If no matching token language is found, an exception will be thrown.
        /// </summary>
        /// <param name="lexerFileName">File name of the antlr lexer. Can be found in <c>lexer.GrammarFileName</c></param>
        /// <returns>The matching token language</returns>
        /// <exception cref="ArgumentException">If the given <paramref name="lexerFileName"/> is not supported.</exception>
        public static TokenLanguage fromLexerFileName(string lexerFileName)
        {
            return AllTokenLanguages.SingleOrDefault(x => x.LexerFileName.Equals(lexerFileName))
                   ?? throw new ArgumentException($"The given {nameof(lexerFileName)} is not of a supported grammar. Supported grammars are "
                                                  + string.Join(", ", AllTokenLanguages.Select(x => x.LexerFileName)));
        }

        /// <summary>
        /// Returns the matching token language for the given <paramref name="extension"/>.
        /// If no matching token language is found, an exception will be thrown.
        /// </summary>
        /// <param name="extension">File extension for the language.</param>
        /// <returns>The matching token language.</returns>
        /// <exception cref="ArgumentException">If the given <paramref name="extension"/> is not supported.</exception>
        public static TokenLanguage fromFileExtension(string extension)
        {
            return AllTokenLanguages.SingleOrDefault(x => x.FileExtensions.Contains(extension))
                   ?? throw new ArgumentException("The given filetype is not supported. Supported filetypes are "
                                                  + string.Join(", ", AllTokenLanguages.SelectMany(x => x.FileExtensions)));
        }

        /// <summary>
        /// Creates a new lexer matching the <see cref="LexerFileName"/> of this language.
        /// </summary>
        /// <param name="content">The string which shall be parsed by the lexer.</param>
        /// <returns>the new matching lexer</returns>
        /// <exception cref="InvalidOperationException">If no lexer is defined for this language.</exception>
        public Lexer CreateLexer(string content)
        {
            ICharStream input = CharStreams.fromString(content);
            return LexerFileName switch
            {
                javaFileName => new Java9Lexer(input),
                cSharpFileName => new CSharpLexer(input),
                cppFileName => new CPP14Lexer(input),
                _ => throw new InvalidOperationException("No lexer defined for this language yet.")
            };
        }

        /// <summary>
        /// Returns the type of token this is.
        /// The type of token will be represented by the name of the collection it is in.
        /// Returns <c>null</c> if the token is not any known type.
        /// </summary>
        /// <param name="token">a symbolic name from the antlr lexer for this language</param>
        /// <returns>name of the type the given <paramref name="token"/> is, or <c>null</c> if it isn't known.</returns>
        public string TypeName(string token)
        {
            // We go through each category and check whether it contains the token.
            // I know that this looks like it may be abstracted because the same thing is done on different objects
            // in succession, but due to the usage of nameof() a refactoring of this kind would break it.
            if (Keywords.Contains(token))
            {
                return nameof(Keywords);
            }
            if (NumberLiterals.Contains(token))
            {
                return nameof(NumberLiterals);
            }
            if (StringLiterals.Contains(token))
            {
                return nameof(StringLiterals);
            }
            if (Punctuation.Contains(token))
            {
                return nameof(Punctuation);
            }
            if (Identifiers.Contains(token))
            {
                return nameof(Identifiers);
            }
            if (Comments.Contains(token))
            {
                return nameof(Comments);
            }
            if (Whitespace.Contains(token))
            {
                return nameof(Whitespace);
            }
            if (Newline.Contains(token))
            {
                return nameof(Newline);
            }
            return EOF.Equals(token) ? nameof(EOF) : null;
        }
    }
}