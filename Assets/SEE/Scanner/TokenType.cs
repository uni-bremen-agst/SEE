using System.Collections.Generic;

namespace SEE.Scanner
{
    /// <summary>
    /// Represents a kind of token in a programming language, with an associated color.
    /// </summary>
    public record TokenType
    {
        #region Static TokenTypes

        /// <summary>
        /// List of all token types.
        /// </summary>
        protected static readonly IList<TokenType> AllTokens = new List<TokenType>();

        /// <summary>
        /// Whitespace tokens, excluding newlines.
        /// </summary>
        public static readonly TokenType Whitespace = new("Whitespace", "#000000"); // color doesn't matter

        /// <summary>
        /// Newline tokens. Must contain exactly one newline.
        /// </summary>
        public static readonly TokenType Newline = new("Newline", "#000000"); // color doesn't matter

        /// <summary>
        /// End-Of-File token.
        /// </summary>
        public static readonly TokenType EOF = new("eof", "#000000"); // color doesn't matter


        #endregion

        protected TokenType(string name, string color)
        {
            Name = name;
            Color = color.TrimStart('#');

            AllTokens.Add(this);
        }

        /// <summary>Name of the token type.</summary>
        public string Name { get; }

        /// <summary>
        /// Color the token type should be rendered in hexadecimal RGB notation (no '#' sign).
        /// An optional fourth byte may be entered to define the alpha value.
        /// </summary>
        public string Color { get; }
    }
}
