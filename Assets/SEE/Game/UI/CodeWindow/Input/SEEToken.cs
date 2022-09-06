using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;

namespace SEE.Game.UI.CodeWindow
{
    /// <summary>
    /// Represents a token from a source code file, including <see cref="Text"/> and a <see cref="Type"/>.
    /// </summary>
    public class SEEToken
    {
        /// <summary>
        /// The text of the token.
        /// </summary>
        public readonly string Text;

        /// <summary>
        /// The type of this token.
        /// </summary>
        public readonly Type TokenType;

        /// <summary>
        /// The inclusive index of the beginning of this token.
        /// The index is measured from the beginning of the file, whereas the first character has the index 0.
        /// </summary>
        public readonly int StartOffset;

        /// <summary>
        /// The exclusive index of the end of this token.
        /// In other words, this is the index of the first character not belonging to this token.
        /// The index is measured from the beginning of the file, whereas the first character has the index 0.
        /// </summary>
        public readonly int EndOffset;

        /// <summary>
        /// The language of the source code this token was parsed from.
        /// </summary>
        public readonly TokenLanguage Language;

        /// <summary>
        /// Constructor for this class.
        /// </summary>
        /// <param name="text">Text of this token. Must not be <c>null</c>.</param>
        /// <param name="type">Type of this token. Must not be <c>null</c>.</param>
        /// <param name="startOffset">Start offset of this token. Must not be negative, except for the special value
        /// -1, which indicates the very first newline of the file</param>
        /// <param name="endOffset">End offset of this token. Must not be smaller than <paramref name="startOffset"/>.</param>
        /// <param name="language">The language of the source code this token is from</param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="text"/> or <paramref name="type"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="endOffset"/> is smaller than <paramref name="startOffset"/>
        /// </exception>
        /// <exception cref="ArgumentException">If <paramref name="startOffset"/> is less than -1.</exception>
        public SEEToken(string text, Type type, int startOffset, int endOffset, TokenLanguage language)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            TokenType = type ?? throw new ArgumentNullException(nameof(type));
            if (endOffset < startOffset)
            {
                throw new ArgumentOutOfRangeException($"{nameof(endOffset)} must not be smaller than {nameof(startOffset)}!");
            }
            // endOffset is greater than startOffset at this point, so it can't be negative after this part
            else if (startOffset < -1)
            {
                throw new ArgumentException($"{nameof(startOffset)} must not be less than -1!");
            }

            StartOffset = startOffset;
            EndOffset = endOffset;
            Language = language;
        }

        /// <summary>
        /// Creates a new <see cref="SEEToken"/> from the given <paramref name="token"/> parsed by the given
        /// <paramref name="lexer"/>.
        /// </summary>
        /// <param name="token">The token which shall be converted to a <see cref="SEEToken"/></param>
        /// <param name="lexer">The lexer with which the token was created.</param>
        /// <param name="language">The language of the <paramref name="token"/>.
        /// If this is not given, the language will be inferred from the given <paramref name="lexer"/>'s grammar.</param>
        /// <returns>The <see cref="SEEToken"/> corresponding to the given <see cref="token"/>.</returns>
        public static SEEToken fromAntlrToken(IToken token, Lexer lexer, TokenLanguage language = null)
        {
            language ??= TokenLanguage.fromLexerFileName(lexer.GrammarFileName);
            return new SEEToken(token.Text,
                                Type.fromAntlrType(language, lexer.Vocabulary.GetSymbolicName(token.Type)),
                                token.StartIndex, token.StopIndex + 1, language); // Antlr StopIndex is inclusive
        }

        /// <summary>
        /// Returns a list of <see cref="SEEToken"/>s created by parsing the file at the supplied
        /// <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">Path to the source code file which shall be read and parsed.</param>
        /// <returns>A list of tokens created from the source code file.</returns>
        /// <remarks>
        /// <ul>
        /// <li>The language of the file will be determined by checking its file extension.</li>
        /// <li>Each token will be created by using <see cref="fromAntlrToken"/>.</li>
        /// </ul>
        /// </remarks>
        public static IList<SEEToken> fromFile(string filename)
        {
            TokenLanguage language = TokenLanguage.fromFileExtension(Path.GetExtension(filename)?.Substring(1));
            Lexer lexer = language.CreateLexer(File.ReadAllText(filename));
            CommonTokenStream tokenStream = new CommonTokenStream(lexer);
            tokenStream.Fill();
            // Generate list of SEETokens using the token stream and its language
            return tokenStream.GetTokens().Select(x => fromAntlrToken(x, lexer, language)).ToList();
        }

        /// <summary>
        /// Returns a list of <see cref="SEEToken"/>s created by parsing the given <paramref name="text"/>, assuming
        /// it's in the given <paramref name="language"/>.
        /// </summary>
        /// <param name="text">Text from which the token stream shall be created.</param>
        /// <param name="language">Language the given <paramref name="text"/> is written in</param>
        /// <returns>A list of tokens created from the source code file.</returns>
        public static IList<SEEToken> FromString(string text, TokenLanguage language)
        {
            Lexer lexer = language.CreateLexer(text);
            CommonTokenStream tokenStream = new CommonTokenStream(lexer);
            tokenStream.Fill();
            // Generate list of SEETokens using the token stream and its language
            return tokenStream.GetTokens().Select(x => fromAntlrToken(x, lexer, language)).ToList();
        }

        /// <summary>
        /// Represents a kind of token in a programming language, with an associated color.
        /// For example, this may be a <see cref="Keyword"/> or an <see cref="Identifier"/>.
        /// </summary>
        public class Type
        {
            /// <summary>
            /// Name of the token type.
            /// This has to match with the corresponding collection in <see cref="TokenLanguage"/>.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Color the token type should be rendered in in hexadecimal RGB notation (no '#' sign).
            /// An optional fourth byte may be entered to define the alpha value.
            /// </summary>
            /// <example>Red would be "FF0000". Semitransparent black would be "00000088".</example>
            public string Color { get; }

            #region Static Types

            /// <summary>
            /// A list of all possible tokens.
            /// </summary>
            public static IList<Type> AllTokens { get; } = new List<Type>();

            /* IMPORTANT: The name has to match with the name of the collection in TokenLanguage! */

            /// <summary>
            /// Keyword tokens. This also includes boolean literals and null literals.
            /// </summary>
            public static readonly Type Keyword = new Type("Keywords", "D988F2"); // purple

            /// <summary>
            /// Number literal tokens. This includes integer literals, floating point literals, etc.
            /// </summary>
            public static readonly Type NumberLiteral = new Type("NumberLiterals", "D48F35"); // orange

            /// <summary>
            /// String literal tokens. This also includes character literals.
            /// </summary>
            public static readonly Type StringLiteral = new Type("StringLiterals", "92F288"); // light green

            /// <summary>
            /// Punctuation tokens, such as separators and operators.
            /// </summary>
            public static readonly Type Punctuation = new Type("Punctuation", "96E5FF"); // light blue

            /// <summary>
            /// Identifier tokens, such as variable names.
            /// </summary>
            public static readonly Type Identifier = new Type("Identifiers", "FFFFFF"); // white

            /// <summary>
            /// Comments of any kind.
            /// </summary>
            public static readonly Type Comment = new Type("Comments", "6F708E"); // dark bluish gray

            /// <summary>
            /// Whitespace tokens, excluding newlines.
            /// </summary>
            public static readonly Type Whitespace = new Type("Whitespace", "000000"); // color doesn't matter

            /// <summary>
            /// Newline tokens. Must contain exactly one newline.
            /// </summary>
            public static readonly Type Newline = new Type("Newline", "000000"); // color doesn't matter

            /// <summary>
            /// End-Of-File token.
            /// </summary>
            public static readonly Type EOF = new Type("EOF", "000000"); // color doesn't matter

            /// <summary>
            /// Unknown tokens, i.e. those not recognized by the lexer.
            /// </summary>
            public static readonly Type Unknown = new Type("Unknown", "FFFFFF"); // white

            #endregion

            /// <summary>
            /// Constructor for this class.
            /// </summary>
            /// <remarks>Must never be accessible from the outside.</remarks>
            /// <param name="name">Name of this token type</param>
            /// <param name="color">Color this token type should be shown in</param>
            private Type(string name, string color)
            {
                Color = color;
                Name = name;
                AllTokens.Add(this);
            }

            /// <summary>
            /// Returns the corresponding <see cref="Type"/> for the given <paramref name="symbolicName"/> in the given
            /// <paramref name="language"/>. If it's not recognized, <see cref="Unknown"/> will be returned.
            /// </summary>
            /// <param name="language">The language the <paramref name="symbolicName"/> is from</param>
            /// <param name="symbolicName">Symbolic name from an antlr lexer</param>
            /// <returns>The corresponding token for the given <paramref name="symbolicName"/>.</returns>
            public static Type fromAntlrType(TokenLanguage language, string symbolicName)
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
}