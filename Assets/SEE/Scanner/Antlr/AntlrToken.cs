using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Cysharp.Threading.Tasks;

namespace SEE.Scanner.Antlr
{
    /// <summary>
    /// Represents a token from a source code file, including <see cref="Text"/>, a <see cref="TokenType"/>,
    /// and some <see cref="Modifiers"/>, emitted by an Antlr lexer.
    /// </summary>
    public record AntlrToken : SEEToken
    {
        private AntlrToken(string Text, TokenType TokenType, AntlrLanguage Language) : base(Text, TokenType, Language) { }

        /// <summary>
        /// Creates a new <see cref="AntlrToken"/> from the given <paramref name="token"/> scanned by the given
        /// Antlr <paramref name="lexer"/>.
        /// </summary>
        /// <param name="token">The token which shall be converted to an <see cref="AntlrToken"/></param>
        /// <param name="lexer">The Antlr lexer with which the token was created.</param>
        /// <param name="language">The language of the <paramref name="token"/>.
        /// If this is not given, the language will be inferred from the given <paramref name="lexer"/>'s grammar.</param>
        /// <returns>The <see cref="SEEToken"/> corresponding to the given <see cref="token"/>.</returns>
        private static AntlrToken FromAntlrIToken(IToken token, Lexer lexer, AntlrLanguage language = null)
        {
            return new AntlrToken(token.Text,
                                  AntlrTokenType.FromAntlrType(language, lexer.Vocabulary.GetSymbolicName(token.Type)),
                                  language ?? AntlrLanguage.FromLexerFileName(lexer.GrammarFileName));
        }

        /// <summary>
        /// Returns a stream of <see cref="AntlrToken"/>s created by parsing the file at the supplied
        /// <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">Path to the source code file which shall be read and parsed.</param>
        /// <returns>A list of tokens created from the source code file.</returns>
        /// <remarks>
        /// <ul>
        /// <li>The language of the file will be determined by checking its file extension.</li>
        /// <li>Each token will be created by using <see cref="FromAntlrToken"/>.</li>
        /// </ul>
        /// </remarks>
        public static async UniTask<IEnumerable<AntlrToken>> FromFileAsync(string filePath)
        {
            AntlrLanguage language = AntlrLanguage.FromFileExtension(Path.GetExtension(filePath)?[1..]);
            Lexer lexer = language.CreateLexer(await File.ReadAllTextAsync(filePath));
            CommonTokenStream tokenStream = new(lexer);
            tokenStream.Fill();
            // Generate list of SEETokens using the token stream and its language
            return tokenStream.GetTokens().Select(x => FromAntlrIToken(x, lexer, language));
        }

        /// <summary>
        /// Returns a list of <see cref="AntlrToken"/>s created by parsing the given <paramref name="text"/>, assuming
        /// it's in the given <paramref name="language"/>.
        /// </summary>
        /// <param name="text">Text from which the token stream shall be created.</param>
        /// <param name="language">Language the given <paramref name="text"/> is written in</param>
        /// <returns>A list of tokens created from the source code file.</returns>
        public static IList<AntlrToken> FromString(string text, AntlrLanguage language)
        {
            Lexer lexer = language.CreateLexer(text);
            CommonTokenStream tokenStream = new(lexer);
            tokenStream.Fill();
            // Generate list of SEETokens using the token stream and its language
            return tokenStream.GetTokens().Select(x => FromAntlrIToken(x, lexer, language)).ToList();
        }
    }
}
