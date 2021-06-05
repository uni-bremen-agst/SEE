using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SEE.Game.UI.CodeWindow
{
    /// <summary>
    /// Partial class containing methods related to processing input for the code windows.
    /// </summary>
    public partial class CodeWindow
    {
        /// <summary>
        /// Populates the code window with the content of the given non-filled lexer's token stream.
        /// </summary>
        /// <param name="lexer">Lexer with which the source code file was read. Must not be filled.</param>
        /// <param name="language">Token language for the given lexer. May be <c>null</c>, in which case it is
        /// determined by the lexer name.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="lexer"/> is <c>null</c></exception>
        /// <exception cref="ArgumentException">
        /// If the given <paramref name="lexer"/> is not of a supported grammar.
        /// </exception>
        public void EnterFromTokens(IEnumerable<SEEToken> tokens)
        {
            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }
            
            // Avoid multiple enumeration in case iteration over the data source is expensive.
            List<SEEToken> tokenList = tokens.ToList();
            if (!tokenList.Any())
            {
                Text = "<i>This file is empty.</i>";
                return;
            }

            TokenLanguage language = tokenList.First().Language;
            
            // We need to insert this pseudo token here so that the first line gets a line number.
            tokenList.Insert(0, new SEEToken(string.Empty, SEEToken.Type.Newline, -1, 0, language));

            // Unsurprisingly, each newline token corresponds to a new line.
            // However, we need to also add "hidden" newlines contained in other tokens, e.g. block comments.
            int assumedLines = tokenList.Count(x => x.TokenType.Equals(SEEToken.Type.Newline)) 
                               + tokenList.Where(x => !x.TokenType.Equals(SEEToken.Type.Newline)) 
                                          .Aggregate(0, (l, token) => token.Text.Count(x => x == '\n'));
            // Needed padding is the number of lines, because the line number will be at most this long.
            int neededPadding = assumedLines.ToString().Length;
            int lineNumber = 1;
            foreach (SEEToken token in tokenList)
            {
                if (token.TokenType == SEEToken.Type.Unknown)
                {
                    Debug.LogError($"Unknown token encountered for text '{token.Text}'.\n");
                }

                if (token.TokenType == SEEToken.Type.Newline)
                {
                    AppendNewline(ref lineNumber, ref Text, neededPadding);
                }
                else if (token.TokenType != SEEToken.Type.EOF) // Skip EOF token completely.
                {
                    lineNumber = HandleMultilineToken(token);
                }
            }

            // Lines are equal to number of newlines, including the initial newline.
            lines = Text.Count(x => x.Equals('\n')); // No more weird CRLF shenanigans are present at this point.
            Text = Text.TrimStart('\n'); // Remove leading newline.

            # region Local Functions
            // Appends a newline to the text, assuming we're at theLineNumber and need the given padding.
            static void AppendNewline(ref int theLineNumber, ref string text, int padding)
            {
                // First, of course, the newline.
                text += "\n";
                // Add whitespace next to line number so it's consistent.
                text += string.Join("", Enumerable.Repeat(" ", padding - $"{theLineNumber}".Length));
                // Line number will be typeset in grey to distinguish it from the rest.
                text += $"<color=#CCCCCC>{theLineNumber}</color> ";
                theLineNumber++;
            }

            // Handles a token which may contain newlines and adds its syntax-highlighted content to the code window.
            int HandleMultilineToken(SEEToken token)
            {
                bool firstRun = true;
                string[] tokenLines = token.Text.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);
                foreach (string line in tokenLines)
                {
                    // Any entry after the first is on a separate line.
                    if (!firstRun)
                    {
                        AppendNewline(ref lineNumber, ref Text, neededPadding);
                    }

                    // No "else if" because we still want to display this token.
                    if (token.TokenType == SEEToken.Type.Whitespace)
                    {
                        // We just copy the whitespace verbatim, no need to even color it.
                        // Note: We have to assume that whitespace will not interfere with TMP's XML syntax.
                        Text += line.Replace("\t", new string(' ', language.TabWidth));
                    }
                    else
                    {
                        Text += $"<color=#{token.TokenType.Color}><noparse>{line}</noparse></color>";
                    }

                    firstRun = false;
                }

                return lineNumber;
            }
            #endregion
        }
        
        /// <summary>
        /// Populates the code window with the contents of the given file.
        /// This will overwrite any existing text.
        /// </summary>
        /// <param name="text">An array of lines to use for the code window.</param>
        /// <exception cref="ArgumentException">If <paramref name="text"/> is empty or <c>null</c></exception>
        public void EnterFromText(string[] text)
        {
            if (text == null || text.Length <= 0)
            {
                throw new ArgumentException("Given text must not be empty or null.\n");
            }

            int neededPadding = $"{text.Length}".Length;
            Text = "";
            for (int i = 0; i < text.Length; i++)
            {
                // Add whitespace next to line number so it's consistent.
                Text += string.Join("", Enumerable.Repeat(" ", neededPadding-$"{i+1}".Length));
                // Line number will be typeset in yellow to distinguish it from the rest.
                Text += $"<color=\"yellow\">{i+1}</color> <noparse>{text[i]}</noparse>\n";
            }
            lines = text.Length;
        }

        /// <summary>
        /// Populates the code window with the contents of the given file.
        /// This will overwrite any existing text.
        /// </summary>
        /// <param name="filename">The filename for the file to read.</param>
        /// <param name="syntaxHighlighting">Whether syntax highlighting shall be enabled.
        /// The language will be detected by looking at the file extension.
        /// If the language is not supported, an ArgumentException will be thrown.</param>
        public void EnterFromFile(string filename, bool syntaxHighlighting = true)
        {
            FilePath = filename;
            
            // Try to read the file, otherwise display the error message.
            if (!File.Exists(filename))
            {
                Text = $"<color=\"red\"><b>Couldn't find file '<noparse>{filename}</noparse>'.</b></color>";
                Debug.LogError($"Couldn't find file {filename}");
                return;
            }
            try
            {
                //TODO: Maybe disable syntax highlighting for huge files, as it would impact performance badly.
                if (syntaxHighlighting)
                {
                    try
                    {
                        EnterFromTokens(SEEToken.fromFile(filename));
                        return;
                    }
                    catch (ArgumentException e)
                    {
                        // In case the filetype is not supported, we render the text normally.
                        Debug.LogError($"Encountered an exception, disabling syntax highlighting: {e}");
                    }
                }

                EnterFromText(File.ReadAllLines(filename));
            }
            catch (IOException exception)
            {
                Text = $"<color=\"red\"><noparse>{exception}</noparse></color>";
                Debug.LogError(exception);
            }
        }

        
    }
}