using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;

namespace SEE.Game.UI.CodeWindow
{
    /// <summary>
    /// Represents a token from a source code file, including <see cref="Text"/> and a <see cref="SEETokenType"/>.
    /// </summary>
    public readonly struct SEEToken
    {
        /// <summary>
        /// The text of the token.
        /// </summary>
        public readonly string Text;
        
        /// <summary>
        /// The type of this token.
        /// </summary>
        public readonly SEETokenType Type;
        
        /// <summary>
        /// The inclusive index of the beginning of this token, relative to the file this token is contained in.
        /// </summary>
        public readonly int StartOffset;
        
        /// <summary>
        /// The exclusive index of the end of this token, relative to the file this token is contained in.
        /// In other words, this is the index of the first character not belonging to this token.
        /// </summary>
        public readonly int EndOffset;

        /// <summary>
        /// Constructor for this class.
        /// </summary>
        /// <param name="text">Text of this token. Must not be <c>null</c>.</param>
        /// <param name="type">Type of this token. Must not be <c>null</c>.</param>
        /// <param name="startOffset">Start offset of this token. Must not be negative, except for the special value
        /// -1, which indicates the very first newline of the file</param>
        /// <param name="endOffset">End offset of this token. Must not be smaller than <paramref name="startOffset"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="text"/> or <paramref name="type"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="endOffset"/> is smaller than <paramref name="startOffset"/>
        /// </exception>
        /// <exception cref="ArgumentException">If <paramref name="startOffset"/> is less than -1.</exception>
        public SEEToken(string text, SEETokenType type, int startOffset, int endOffset)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Type = type ?? throw new ArgumentNullException(nameof(type));
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
                                SEETokenType.fromAntlrType(language, lexer.Vocabulary.GetSymbolicName(token.Type)),
                                token.StartIndex, token.StopIndex+1); // Antlr StopIndex is inclusive
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
        /// <li>The first token in this list will always be a token of type <see cref="SEETokenType.Newlines"/>
        /// with empty text.</li>
        /// </ul>
        /// </remarks>
        public static IEnumerable<SEEToken> fromFile(string filename)
        {
            TokenLanguage language = TokenLanguage.fromFileExtension(Path.GetExtension(filename)?.Substring(1));
            Lexer lexer = language.CreateLexer(File.ReadAllText(filename));
            CommonTokenStream tokenStream = new CommonTokenStream(lexer);
            tokenStream.Fill();
            // Generate list of SEETokens using the token stream and its language
            IList<SEEToken> tokenList = tokenStream.GetTokens().Select(x => fromAntlrToken(x, lexer, language)).ToList();
            tokenList.Insert(0, new SEEToken(string.Empty, SEETokenType.Newlines, -1, 0));
            return tokenList;
        }
    }
}