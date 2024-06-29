using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using MoreLinq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using SEE.Tools.LSP;
using UnityEngine.Assertions;

namespace SEE.Scanner.LSP
{
    /// <summary>
    /// Represents a semantic token from a source code file, including <see cref="Text"/>, a <see cref="TokenType"/>,
    /// and some <see cref="Modifiers"/>, emitted by a language server.
    /// </summary>
    public record LSPToken : SEEToken
    {
        private LSPToken(string Text, TokenType TokenType, TokenModifiers Modifiers, LSPLanguage Language) : base(Text, TokenType, Language, Modifiers) { }

        /// <summary>
        /// Returns a stream of <see cref="LSPToken"/>s created by parsing the file at the supplied
        /// <paramref name="filePath"/>. The tokens are generated for the given <paramref name="language"/>,
        /// using the given LSP <paramref name="handler"/>.
        /// </summary>
        /// <param name="filePath">Path to the source code file which shall be read and parsed.</param>
        /// <param name="handler">The LSP handler used to retrieve the semantic tokens.</param>
        /// <param name="language">The language of the file.</param>
        /// <returns>A stream of tokens created from the source code file.</returns>
        public static async UniTask<IEnumerable<LSPToken>> FromFileAsync(string filePath, LSPHandler handler, LSPLanguage language)
        {
            const int delayMs = 100;
            const int maxTries = 30;

            string fileContents = await System.IO.File.ReadAllTextAsync(filePath);
            // We may need to wait for the server to process the file, so we just retry for a bit
            // (with the above constants, at most 3 seconds) until we get the tokens.
            SemanticTokens tokens;
            int tries = 0;
            do
            {
                tokens = await handler.GetSemanticTokensAsync(filePath);
                if (tokens.Data.Length == 0)
                {
                    await UniTask.Delay(delayMs);
                }
            } while (tokens.Data.Length == 0 && ++tries < maxTries);
            if (handler.ServerCapabilities.SemanticTokensProvider == null)
            {
                throw new InvalidOperationException("The server does not support semantic tokens.");
            }
            return FromSemanticTokens(tokens, handler.ServerCapabilities.SemanticTokensProvider.Legend, language, fileContents);
        }

        /// <summary>
        /// Returns a stream of <see cref="LSPToken"/>s from the given LSP-generated <paramref name="tokens"/>
        /// and <paramref name="legend"/>, for the given <paramref name="language"/> and <paramref name="fileContents"/>.
        /// </summary>
        /// <param name="tokens">The semantic tokens to be converted to <see cref="LSPToken"/>s.</param>
        /// <param name="legend">The legend used to interpret the semantic tokens.</param>
        /// <param name="language">The language of the file.</param>
        /// <param name="fileContents">The contents of the file the tokens are from as a single string.</param>
        /// <returns>A stream of tokens created from the semantic tokens.</returns>
        private static IEnumerable<LSPToken> FromSemanticTokens(SemanticTokens tokens, SemanticTokensLegend legend, LSPLanguage language, string fileContents)
        {
            IList<SemanticTokenModifier> modifierLegend = legend.TokenModifiers.ToList();
            IList<SemanticTokenType> typeLegend = legend.TokenTypes.ToList();

            Assert.IsTrue(tokens.Data.Length % 5 == 0, "Semantic tokens data length must be a multiple of 5.");

            // Both cursors are indices within the fileContents.
            // tokenCursor is the cursor for the end of the current LSPToken.
            int tokenCursor = 0;
            // semanticTokenCursor is the cursor for the start of the current semantic token.
            int semanticTokenCursor = 0;
            for (int i = 0; i < tokens.Data.Length; i += 5)
            {
                // For a description of the encoding, see the section on semantic tokens in the LSP specification.

                // Token line number, relative to the previous token.
                int deltaLine = tokens.Data[i];
                // Token start character, relative to either 0 or the previous token’s start if they are on the same line
                int deltaStart = tokens.Data[i + 1];
                // Length of the token
                int length = tokens.Data[i + 2];
                // The type of the token, represented as an index within the typeLegend.
                int type = tokens.Data[i + 3];
                // The modifiers of the token, represented as a bitmask of indices within the modifierLegend.
                int modifiers = tokens.Data[i + 4];

                if (deltaLine > 0)
                {
                    // We need to skip ahead until the cursor is at the start of the relevant line
                    // (depending on deltaLine).
                    for (int j = 0; j < deltaLine; j++)
                    {
                        semanticTokenCursor = fileContents.IndexOf('\n', semanticTokenCursor) + 1;
                    }
                }

                // Then, we need to move the cursor ahead by deltaStart.
                semanticTokenCursor += deltaStart;

                // The tokens given by LSP may not encompass the whole document.
                // For example, there are no LSP token types for newlines and whitespace.
                // Hence, we have to fill these gaps ourselves by constructing tokens for them manually.
                if (semanticTokenCursor > tokenCursor)
                {
                    // We now have to differentiate between Newlines, Whitespace, and other tokens.
                    string gap = fileContents.Substring(tokenCursor, semanticTokenCursor - tokenCursor);
                    foreach (LSPToken token in HandleGap(gap))
                    {
                        yield return token;
                    }
                }

                tokenCursor = semanticTokenCursor + length;

                string tokenText = fileContents[semanticTokenCursor..tokenCursor];
                LSPTokenType tokenType = LSPTokenType.FromSemanticTokenType(typeLegend[type]);
                // Modifiers are a bitmask, so we need to map them to the actual modifiers using the legend.
                TokenModifiers tokenModifiers = modifierLegend.Where((_, index) => (modifiers & (1 << index)) != 0)
                                                              .Aggregate(TokenModifiers.None, (x, y) => x | y.FromLspTokenModifier());
                yield return new LSPToken(tokenText, tokenType, tokenModifiers, language);
            }

            // There may be leftover tokens at the end of the file.
            if (tokenCursor < fileContents.Length)
            {
                string gap = fileContents[tokenCursor..];
                foreach (LSPToken token in HandleGap(gap))
                {
                    yield return token;
                }
            }

            yield return new LSPToken(string.Empty, TokenType.EOF, TokenModifiers.None, language);
            yield break;

            IEnumerable<LSPToken> HandleGap(string gap)
            {
                // This gap may consist of multiple tokens.
                // For example, the string ";\n    //" consists of four tokens:
                // A semicolon, a newline, four spaces, and two slashes.
                return Regex.Split(gap, @"(?<=\S)(?=\s)|(?<=\s)(?=\S)").SelectMany(HandleGapToken);
            }

            IEnumerable<LSPToken> HandleGapToken(string gapToken)
            {
                if (string.IsNullOrWhiteSpace(gapToken))
                {
                    Assert.IsNotNull(gapToken);
                    // This can still contain newlines, which we need to handle separately.
                    string[] gapTokens = gapToken.Split('\n');
                    foreach (string whitespace in gapTokens.Interleave(MoreEnumerable.Return("\n").Repeat(gapTokens.Length - 1)))
                    {
                        if (whitespace == "\n")
                        {
                            yield return new LSPToken(whitespace, TokenType.Newline, TokenModifiers.None, language);
                        }
                        else
                        {
                            yield return new LSPToken(whitespace, TokenType.Whitespace, TokenModifiers.None, language);
                        }
                    }
                }
                else
                {
                    yield return new LSPToken(gapToken, LSPTokenType.Type, TokenModifiers.None, language);
                }
            }
        }
    }
}
