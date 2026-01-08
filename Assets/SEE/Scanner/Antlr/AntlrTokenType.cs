using System;
using System.Linq;

namespace SEE.Scanner.Antlr
{
    /// <summary>
    /// Represents a kind of token in an Antlr-supported programming language, with an associated color.
    /// For example, this may be a <see cref="Keyword"/> or an <see cref="Identifier"/>.
    /// </summary>
    public record AntlrTokenType : TokenType
    {
        /// <summary>
        /// Returns the corresponding <see cref="TokenType"/> for the given <paramref name="symbolicName"/>
        /// in the given <paramref name="language"/>. If it's not recognized, an exception is thrown.
        /// </summary>
        /// <param name="language">The language the <paramref name="symbolicName"/> is from.</param>
        /// <param name="symbolicName">Symbolic name from an antlr lexer.</param>
        /// <returns>The corresponding token for the given <paramref name="symbolicName"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="language"/> or <paramref name="symbolicName"/> is null.</exception>
        /// <exception cref="InvalidOperationException">If the <paramref name="symbolicName"/> is not recognized.</exception>
        public static TokenType FromAntlrType(AntlrLanguage language, string symbolicName)
        {
            if (language == null || symbolicName == null)
            {
                throw new ArgumentNullException();
            }

            string typeName = language.TypeName(symbolicName);
            TokenType type = AllTokens.SingleOrDefault(x => x.Name.Equals(typeName));
            return type ?? throw new InvalidOperationException
                                      ($"Unknown token type: type name='{typeName}', symbolic name='{symbolicName}' using lexer {language.Name}.");
        }

        #region Static TokenTypes

        // IMPORTANT: The name has to match with the name of the collection in AntlrLanguage!

        /// <summary>
        /// Keyword tokens. This also includes boolean literals and null literals.
        /// </summary>
        public static readonly AntlrTokenType Keyword = new("Keywords", "#D988F2"); // purple

        /// <summary>
        /// Branch keyword tokens.
        /// </summary>
        /// <remarks>We want <see cref="BranchKeyword"/>s have the same color as
        /// other <see cref="Keyword"/>s.</remarks>
        public static readonly AntlrTokenType BranchKeyword = new("BranchKeywords", "#D988F2"); // purple

        /// <summary>
        /// Number literal tokens. This includes integer literals, floating point literals, etc.
        /// </summary>
        public static readonly AntlrTokenType NumberLiteral = new("NumberLiterals", "#D48F35"); // orange

        /// <summary>
        /// String literal tokens. This also includes character literals.
        /// </summary>
        public static readonly AntlrTokenType StringLiteral = new("StringLiterals", "#92F288"); // light green

        /// <summary>
        /// Punctuation tokens, such as separators and operators.
        /// </summary>
        public static readonly AntlrTokenType Punctuation = new("Punctuation", "#96E5FF"); // light blue

        /// <summary>
        /// Identifier tokens, such as variable names.
        /// </summary>
        public static readonly AntlrTokenType Identifier = new("Identifiers", "#FFFFFF"); // white

        /// <summary>
        /// Comments of any kind.
        /// </summary>
        public static readonly AntlrTokenType Comment = new("Comments", "#6F708E"); // dark bluish gray

        /// <summary>
        /// Tokens that are not part of the language, but are still recognized by the lexer.
        /// </summary>
        public static readonly AntlrTokenType Ignored = new("Ignored", "#FF0000"); // red, should not be rendered

        #endregion

        private AntlrTokenType(string name, string color) : base(name, color)
        {
        }
    }
}
