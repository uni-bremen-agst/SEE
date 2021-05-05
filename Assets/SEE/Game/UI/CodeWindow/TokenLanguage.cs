using System.Collections.Generic;

namespace SEE.Game.UI.CodeWindow
{
    /// <summary>
    /// Represents a language a <see cref="SEEToken"/> is in.
    /// Symbolic names for the antlr lexer are specified here.
    /// </summary>
    public class TokenLanguage
    {
        /// <summary>
        /// Name of the antlr lexer the keywords were taken from.
        /// </summary>
        public string LexerName { get; }

        /// <summary>
        /// Symbolic names for keywords of a language. This also includes boolean literals and null literals.
        /// </summary>
        public ICollection<string> Keywords { get; }
        
        /// <summary>
        /// Symbolic names for number literals of a language. This includes integer literals, floating point literals, etc.
        /// </summary>
        public ICollection<string> NumberLiterals { get; }
        
        /// <summary>
        /// Symbolic names for string literals of a language. Also includes character literals.
        /// </summary>
        public ICollection<string> StringLiterals { get; }
        
        /// <summary>
        /// Symbolic names for separators and operators of a language.
        /// </summary>
        public ICollection<string> Punctuation { get; }
        
        /// <summary>
        /// Symbolic names for identifiers in a language.
        /// </summary>
        public ICollection<string> Identifiers { get; }
        
        /// <summary>
        /// Symbolic names for whitespace in a language, excluding newlines.
        /// </summary>
        public ICollection<string> Whitespace { get; }
            
        /// <summary>
        /// Symbolic names for newlines in a language.
        /// </summary>
        public ICollection<string> Newlines { get; }
        
        #region Language Specifics (symbolic type names)

        /// <summary>
        /// Name of the antlr grammar lexer.
        /// </summary>
        private static readonly string javaName = "Java9Lexer";
        /// <summary>
        /// List of antlr type names for Java keywords.
        /// </summary>
        private static readonly List<string> javaKeywords = new List<string>
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
        /// List of antlr type names for Java integer and floating point literals.
        /// </summary>
        private static readonly List<string> javaNumbers = new List<string> { "IntegerLiteral", "FloatingPointLiteral" };
        /// <summary>List of antlr type names for Java character and string literals.</summary>
        private static readonly List<string> javaStrings = new List<string> { "CharacterLiteral", "StringLiteral" };
        /// <summary>List of antlr type names for Java separators and operators.</summary>
        private static readonly List<string> javaPunctuation = new List<string> { "LPAREN", "RPAREN", "LBRACE",
            "RBRACE", "LBRACK", "RBRACK", "SEMI", "COMMA", "DOT", "ELLIPSIS", "AT", "COLONCOLON",
            "ASSIGN", "GT", "LT", "BANG", "TILDE", "QUESTION", "COLON", "ARROW", "EQUAL", "LE", "GE", "NOTEQUAL", "AND",
            "OR", "INC", "DEC", "ADD", "SUB", "MUL", "DIV", "BITAND", "BITOR", "CARET", "MOD",
            "ADD_ASSIGN", "SUB_ASSIGN", "MUL_ASSIGN", "DIV_ASSIGN", "AND_ASSIGN", "OR_ASSIGN", "XOR_ASSIGN",
            "MOD_ASSIGN", "LSHIFT_ASSIGN", "RSHIFT_ASSIGN", "URSHIFT_ASSIGN"
        };
        /// <summary>List of antlr type names for Java identifiers.</summary>
        private static readonly List<string> javaIdentifiers = new List<string> { "Identifier" };
        /// <summary>
        /// List of antlr type names for Java whitespace.
        /// </summary>
        private static readonly List<string> javaWhitespace = new List<string> { "WS" };
        /// <summary>
        /// List of antlr type names for Java newlines.
        /// </summary>
        private static readonly List<string> javaNewlines = new List<string> { /* FIXME: Not in lexer grammar */ };
        
        #endregion

        
        #region Static Types

        /// <summary>
        /// A list of all token languages there are.
        /// </summary>
        public static readonly IList<TokenLanguage> AllTokenLanguages = new List<TokenLanguage>();

        /// <summary>
        /// Token Language for Java.
        /// </summary>
        public static readonly TokenLanguage Java = new TokenLanguage(javaName, javaKeywords, javaNumbers,
            javaStrings, javaPunctuation, javaIdentifiers, javaWhitespace, javaNewlines);

        #endregion

        /// <summary>
        /// Constructor for the token language.
        /// </summary>
        /// <remarks>Should never be accessible from outside this class.</remarks>
        /// <param name="lexerName">Name of this lexer grammar</param>
        /// <param name="keywords">Keywords of this language</param>
        /// <param name="numberLiterals">Number literals of this language</param>
        /// <param name="stringLiterals">String literals of this language</param>
        /// <param name="punctuation">Punctuation for this language</param>
        /// <param name="identifiers">Identifiers for this language</param>
        /// <param name="whitespace">Whitespace for this language</param>
        /// <param name="newlines">Newlines for this language</param>
        private TokenLanguage(string lexerName, ICollection<string> keywords, ICollection<string> numberLiterals,
                                   ICollection<string> stringLiterals, ICollection<string> punctuation,
                                   ICollection<string> identifiers, ICollection<string> whitespace,
                                   ICollection<string> newlines)
        {
            LexerName = lexerName;
            Keywords = keywords;
            NumberLiterals = numberLiterals;
            StringLiterals = stringLiterals;
            Punctuation = punctuation;
            Identifiers = identifiers;
            Whitespace = whitespace;
            Newlines = newlines;
            
            AllTokenLanguages.Add(this);
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
            // in succession, but due to the usage of nameof() a refactoring of this kind would break this.
            if (Keywords.Contains(token))
            {
                return nameof(token);
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
            if (Whitespace.Contains(token))
            {
                return nameof(Whitespace);
            }
            if (Newlines.Contains(token))
            {
                return nameof(Newlines);
            }

            return null;
        }
    }
}