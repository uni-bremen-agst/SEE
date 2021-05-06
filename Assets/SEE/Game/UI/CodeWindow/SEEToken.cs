using System;
using System.Collections.Generic;
using System.Linq;

namespace SEE.Game.UI.CodeWindow
{
    /// <summary>
    /// Represents a token of some kind in a programming language, with an associated color.
    /// FIXME: This, along with <see cref="TokenLanguage"/> needs to be moved to a more fitting place.
    /// </summary>
    public class SEEToken
    {
        /// <summary>
        /// Name of the token.
        /// This has to match with the corresponding collection in <see cref="TokenLanguage"/>.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Color the token should be rendered in in hexadecimal RGB notation (no '#' sign).
        /// An optional fourth byte may be entered to define the alpha value.
        /// </summary>
        /// <example>Red would be "FF0000". Semitransparent black would be "00000088".</example>
        public string Color { get; }

        #region Static Types

        /// <summary>
        /// A list of all possible tokens.
        /// </summary>
        public static IList<SEEToken> AllTokens { get; } = new List<SEEToken>();

        /* IMPORTANT: The name has to match with the name of the collection in TokenLanguage! */
        
        /// <summary>
        /// Keyword tokens. This also includes boolean literals and null literals.
        /// </summary>
        public static readonly SEEToken Keyword = new SEEToken("Keywords", "D988F2"); // purple
        /// <summary>
        /// Number literal tokens. This includes integer literals, floating point literals, etc.
        /// </summary>
        public static readonly SEEToken NumberLiteral = new SEEToken("NumberLiterals", "D48F35"); // orange
        /// <summary>
        /// String literal tokens. This also includes character literals.
        /// </summary>
        public static readonly SEEToken StringLiteral = new SEEToken("StringLiterals", "92F288"); // light green
        /// <summary>
        /// Punctuation tokens, such as separators and operators.
        /// </summary>
        public static readonly SEEToken Punctuation = new SEEToken("Punctuation", "96E5FF"); // light blue
        /// <summary>
        /// Identifier tokens, such as variable names.
        /// </summary>
        public static readonly SEEToken Identifier = new SEEToken("Identifiers", "FFFFFF"); // white
        /// <summary>
        /// Whitespace tokens, excluding newlines.
        /// </summary>
        public static readonly SEEToken Whitespace = new SEEToken("Whitespace", "000000"); // color doesn't matter
        /// <summary>
        /// Newline tokens. Must contain exactly one newline.
        /// </summary>
        public static readonly SEEToken Newlines = new SEEToken("Newlines", "000000"); // color doesn't matter
        /// <summary>
        /// Unknown tokens, i.e. those not recognized by the lexer.
        /// </summary>
        public static readonly SEEToken Unknown = new SEEToken("Unknown", "FFFFFF"); // white
        
        //TODO: If we choose parsers instead of lexers, methods and class names may get their own color

        #endregion

        /// <summary>
        /// Constructor for this class.
        /// </summary>
        /// <remarks>Must never be accessible from the outside.</remarks>
        /// <param name="name">Name of this token</param>
        /// <param name="color">Color this tokens should be shown in</param>
        private SEEToken(string name, string color)
        {
            Color = color;
            Name = name;
            AllTokens.Add(this);
        }

        /// <summary>
        /// Returns the corresponding <see cref="SEEToken"/> for the given <paramref name="symbolicName"/> in the given
        /// <paramref name="language"/>. If it's not recognized, <see cref="Unknown"/> will be returned.
        /// </summary>
        /// <param name="language">The language the <paramref name="symbolicName"/> is from</param>
        /// <param name="symbolicName">Symbolic name from an antlr lexer</param>
        /// <returns>The corresponding token for the given <paramref name="symbolicName"/>.</returns>
        public static SEEToken fromAntlrType(TokenLanguage language, string symbolicName)
        {
            if (language == null || symbolicName == null)
            {
                throw new ArgumentNullException();
            }
            string typeName = language.TypeName(symbolicName);
            return AllTokens.SingleOrDefault(x => x.Name.Equals(typeName)) ?? Unknown;
        }
        
    }
}