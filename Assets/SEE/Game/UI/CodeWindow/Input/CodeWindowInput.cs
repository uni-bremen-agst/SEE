using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using SEE.Game.UI.Notification;
using SEE.Net.Dashboard;
using SEE.Net.Dashboard.Model.Issues;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.UI.CodeWindow
{
    /// <summary>
    /// Partial class containing methods related to processing input for the code windows.
    /// </summary>
    public partial class CodeWindow
    {

        /// <summary>
        /// A dictionary mapping each issue ID to their issue.
        /// </summary>
        private readonly Dictionary<int, Issue> issueDictionary = new Dictionary<int, Issue>();
        
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
                        Text += $"<color=#{token.TokenType.Color}><noparse>{line.Replace("noparse", "")}</noparse></color>";
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
                Text += $"<color=\"yellow\">{i+1}</color> <noparse>{text[i].Replace("noparse", "")}</noparse>\n";
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
                ShowNotification.Error("File not found", $"Couldn't find file '{filename}'.");
                Destroy(this);
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
                        MarkIssues(filename).Forget(); // initiate issue search
                    }
                    catch (ArgumentException e)
                    {
                        // In case the filetype is not supported, we render the text normally.
                        Debug.LogError($"Encountered an exception, disabling syntax highlighting: {e}");
                    }
                }
                else
                {
                    EnterFromText(File.ReadAllLines(filename));
                    MarkIssues(filename).Forget(); // initiate issue search
                }
            }
            catch (IOException exception)
            {
                ShowNotification.Error("File access error", $"Couldn't access file {filename}: {exception}");
                Destroy(this);
            }
        }

        private async UniTaskVoid MarkIssues(string path)
        {
            const string NOPARSE = "<noparse>";
            const string NOPARSE_CLOSE = "</noparse>";
            
            // First notification should stay as long as issues are still loading.
            Notification.Notification firstNotification = ShowNotification.Info("Loading issues...", 
                                                                                "This may take a while.", -1f);
            string queryPath = Path.GetFileName(path);
            List<Issue> allIssues;
            try
            {
                allIssues = new List<Issue>(await DashboardRetriever.Instance.GetConfiguredIssues(fileFilter: $"\"*{queryPath}\""));
            } catch (DashboardException e)
            {
                firstNotification.Close();
                ShowNotification.Error("Couldn't load issues", e.Message);
                return;
            }

            await UniTask.SwitchToThreadPool(); // don't interrupt main UI thread
            if (allIssues.Count == 0)
            {
                return;
            }

            const char PATH_SEPARATOR = '/';
            // When there are different paths in the issue table, this implies that there are some files 
            // which aren't actually the one we're looking for (because we've only matched by filename so far).
            // In this case, we'll gradually refine our results until this isn't the case anymore.
            for (int skippedParts = path.Count(x => x == PATH_SEPARATOR)-2; !MatchingPaths(allIssues); skippedParts--)
            {
                Assert.IsTrue(path.Contains(PATH_SEPARATOR));
                // Skip the first <c>skippedParts</c> parts, so that we query progressively larger parts.
                queryPath = string.Join(PATH_SEPARATOR.ToString(), path.Split(PATH_SEPARATOR).Skip(skippedParts));
                allIssues.RemoveAll(x => !x.Entities.Select(e => e.path).Any(p => p.EndsWith(queryPath)));
            }

            // Mapping from each line to the entities and issues contained therein
            //FIXME: There may still be issues here if some line range (endLine) overlaps something else
            Dictionary<int, List<(SourceCodeEntity entity, Issue issue)>> entities = 
                allIssues.SelectMany(x => x.Entities.Select(e => (entity: e, issue: x)))
                      .Where(x => x.entity.path.EndsWith(queryPath))
                      .OrderBy(x => x.entity.line).GroupBy(x => x.entity.line)
                      .ToDictionary(x => x.Key, x => x.ToList());
            
            int shift = 0;
            foreach (KeyValuePair<int, List<(SourceCodeEntity entity, Issue issue)>> lineIssues in entities)
            {
                lineIssues.Value.ForEach(x => issueDictionary[x.issue.id] = x.issue);
                
                // If we have more than one issue in this line, or alternatively
                // no occurence or more than one occurence of the content within the entity,
                // we instead fall back to highlighting the whole line, because we have no real way of doing these
                // kinds of highlights with TextMeshPro.
                (int, int)? contentIndices = null;
                if (lineIssues.Value.Count == 1)
                {
                    contentIndices = GetContentIndices(lineIssues.Value[0].entity.line, 
                                                       lineIssues.Value[0].entity.content, shift);
                }
                if (contentIndices.HasValue)
                {
                    HighlightPart(contentIndices.Value, lineIssues.Value.Select(x => x.issue).ToList(), ref shift);
                }
                else
                {
                    // Apply highlight to all lines between line and endLine, if endLine is defined

                    int? endLine = lineIssues.Value.Where(x => x.entity.endLine.HasValue).Max(x => x.entity.endLine);
                    IEnumerable<int> entityLines = !endLine.HasValue ? new[] {lineIssues.Key} 
                        : Enumerable.Range(lineIssues.Key, endLine.Value - lineIssues.Key);

                    foreach (int line in entityLines)
                    {
                        HighlightPart(GetLineIndices(line), lineIssues.Value.Select(x => x.issue).ToList(), ref shift);
                    }
                }
            }

            await UniTask.SwitchToMainThread();
            try
            {
                TextMesh.text = Text;
                TextMesh.ForceMeshUpdate();
            }
            catch (IndexOutOfRangeException)
            {
                //FIXME: Split up TMP into multiple game objects.
                ShowNotification.Error("File too big", "The file is too big to display any issues, sorry.");
                return;
            }
            finally
            {
                firstNotification.Close();
            }

            //TODO: This may as well be implemented as a loading bar, showing continuous progress as we iterate.
            ShowNotification.Info("Issues loaded", $"{allIssues.Count} issues have been found for {Title}.");

            #region Local Methods

            void HighlightPart((int richStartIndex, int richEndIndex) content, IList<Issue> issues, 
                               ref int indexShift)
            {
                // Limitation: We can only use the color of the first issue, because we can't reliably detect the
                // order of the entities within a single line. Details for all issues are shown on hover.
                string MARK = $"<mark=#{ColorUtility.ToHtmlStringRGB(DashboardRetriever.Instance.GetIssueColor(issues[0]))}33>";
                const string MARK_CLOSE = "</mark>";
                string LINK = $"<link=\"{issues.Select(x => x.id).ToArray().IntToString()}\">";
                const string LINK_CLOSE = "</link>";
                (int startIndex, int endIndex) = content;

                // These tell us which other tags our indices are contained in
                bool noparseStart = ContentInTag(startIndex, NOPARSE, NOPARSE_CLOSE);
                bool noparseEnd = ContentInTag(endIndex, NOPARSE, NOPARSE_CLOSE);

                // We put the closing tag in first, otherwise our endIndex would shift.
                Text = Text.Insert(endIndex, MARK_CLOSE).Insert(startIndex, MARK)
                           .Insert(endIndex + MARK_CLOSE.Length + MARK.Length, LINK_CLOSE)
                           .Insert(startIndex, LINK);
                indexShift += MARK_CLOSE.Length + MARK.Length + LINK.Length + LINK_CLOSE.Length;
                if (noparseEnd)
                {
                    // We want to "unescape" <mark> and </mark> in the <noparse> segments.
                    // </noparse> is shifted right by <mark>.
                    Text = Text.Insert(endIndex + MARK.Length + MARK_CLOSE.Length + LINK.Length + LINK_CLOSE.Length, NOPARSE)
                               .Insert(endIndex + MARK.Length + LINK.Length, NOPARSE_CLOSE);
                    indexShift += NOPARSE_CLOSE.Length + NOPARSE.Length;
                }
                if (noparseStart)
                {
                    Text = Text.Insert(startIndex + MARK.Length + LINK.Length, NOPARSE)
                               .Insert(startIndex, NOPARSE_CLOSE);
                    indexShift += NOPARSE_CLOSE.Length + NOPARSE.Length;
                }
            }

            // Returns true iff all issues are on the same path.
            static bool MatchingPaths(ICollection<Issue> issues)
            {
                // Every path in the first issue could be the "right" path, so we try them all.
                // If every issue has at least one path which matches that one, we can return true.
                return issues.First().Entities.Select(e => e.path)
                             .Any(path => issues.All(x => x.Entities.Any(e => e.path == path)));
            }
            
            // Note that this method will return null if the given content either can't be found at the given line,
            // or if it's found more than once (in which case it's impossible to find out what to highlight).
            (int richStartIndex, int richEndIndex)? GetContentIndices(int line, string content, int indexShift)
            {
                if (content == null)
                {
                    return null;
                }
                // For the motivation behind what happens here, please see method GetRichIndex.
                // As a TL;DR: Our TMP contains rich tags, while the given line and content don't account for them.
                
                (string lineContent, IList<TMP_CharacterInfo> contentInfo) = GetLineInfo(line);
                // We get the start and end index within this list, not accounting for rich tags ("clean").
                int cleanStartIndex = lineContent.IndexOf(content, StringComparison.Ordinal);
                if (cleanStartIndex == -1 || Regex.Matches(lineContent, Regex.Escape(content)).Count > 1)
                {
                    return null;
                }
                int cleanEndIndex = cleanStartIndex + content.Length;
                // The "rich" start index can then be inferred using the index property.
                int richStartIndex = contentInfo[cleanStartIndex].index + indexShift;
                int richEndIndex = contentInfo[cleanEndIndex].index + indexShift;

                return (richStartIndex, richEndIndex);
            }
            
            (int richStartIndex, int richEndIndex) GetLineIndices(int line)
            {
                List<string> splitText = Text.Split('\n').ToList();
                string lineContent = splitText[line-1];
                // We want to count newlines as well (+ line - 1)
                int lineShift = line == 1 ? 0 : splitText.GetRange(0, line - 1).SelectMany(c => c).Count() + line - 1;
                int startIndex = lineShift + lineContent.IndexOf("</color>", StringComparison.Ordinal);
                int endIndex = lineShift + lineContent.Length;

                return (startIndex, endIndex);
            }

            (string cleanLine, IList<TMP_CharacterInfo> lineInfo) GetLineInfo(int line)
            {
                // We get a list of CharacterInfos from the target line
                IList<TMP_CharacterInfo> contentInfo = TextMesh.textInfo.characterInfo
                                                               .SkipWhile(x => x.lineNumber+1 != line) 
                                                               .TakeWhile(x => x.lineNumber+1 == line).ToList();
                
                string lineContent = string.Concat(contentInfo.Select(x => x.character));
                return (lineContent, contentInfo);
            }

            // Returns whether or not the content at the rich index is in the given tag (by checking the line it's in)
            bool ContentInTag(int richIndex, string startTag, string endTag, bool escapeRegex = true)
            {
                //FIXME: This will not work if used in a file which contains the given tag (for example, this file).
                // Either "true" XML parsing needs to be used, or an alternative solution must be found,
                // e.g. removing all content in a <noparse> tag first.
                
                string untilContent = "";
                // This gets the beginning of the line containing the richIndex until the richIndex
                for (int i = 0; i < richIndex; i++)
                {
                    char current = Text[i];
                    if (current == '\n')
                    {
                        // Reset on new line
                        untilContent = "";
                    } 
                    else 
                    {
                        untilContent += current;
                    }
                }
                // Content is in <noparse> if the tag has been opened more times than it has been closed
                return Regex.Matches(untilContent, escapeRegex ? Regex.Escape(startTag) : startTag).Count
                       > Regex.Matches(untilContent, escapeRegex ? Regex.Escape(endTag) : endTag).Count;
            }

            #endregion
        }

        /// <summary>
        /// Get index within the "rich" text (with markup tags) from index in the "clean" text (with markup tags).
        /// </summary>
        /// <param name="cleanIndex">The index within the "clean" text.</param>
        /// <returns>The index within the "rich" text.</returns>
        /// <example>
        /// Assume we have the source line <c>&lt;color red&gt;private class&lt;/color&gt; Test {</c>.
        /// As we can see, rich tags have been inserted so that the "private class" keywords are rendered in red.
        /// This is a problem when we later want to e.g. highlight "class Test". It's no longer possible to simply
        /// search the text for "class Test" and highlight that part, because it's broken up by <c>&lt;/color&gt;</c>.
        /// To remedy this, you can call this method with an index in the "clean" version.
        /// In our example, this would be 9 (before "class") and 19 (after "Test"). This method will then return the
        /// corresponding indices in the text with tags present, which in our example would be 20 and 38.
        /// </example>
        private int GetRichIndex(int cleanIndex)
        {
            return TextMesh.textInfo.characterInfo[cleanIndex].index;
        }

    }
}