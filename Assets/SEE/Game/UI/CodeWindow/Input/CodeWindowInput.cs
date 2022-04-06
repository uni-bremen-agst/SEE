using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using SEE.Game.UI.Notification;
using SEE.Net.Dashboard;
using SEE.Net.Dashboard.Model.Issues;
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
        /// The needed padding for the line numbers.
        /// </summary>
        private int neededPadding = 0;

        /// <summary>
        /// A dictionary mapping each link ID to its issues.
        /// </summary>
        private readonly Dictionary<char, List<Issue>> issueDictionary = new Dictionary<char, List<Issue>>();

        /// <summary>
        /// Counter which represents the lowest unfilled position in the <see cref="issueDictionary"/>.
        /// Any index above it must not be filled either.
        /// </summary>
        private char linkCounter = char.MinValue;

        /// <summary>
        /// List of tokens for this code window.
        /// </summary>
        private List<SEEToken> TokenList;

        /// <summary>
        /// Characters representing newlines.
        /// Note that newlines may also consist of aggregations of this set (e.g. "\r\n").
        /// </summary>
        private static readonly char[] NewlineCharacters = {'\r', '\n'};

        /// <summary>
        /// Populates the code window with the content of the given token stream.
        /// </summary>
        /// <param name="tokens">Stream of tokens representing the source code of this code window.</param>
        /// <param name="issues">Issues for this file. If <c>null</c>, will be automatically retrieved.
        /// Entities spanning multiple lines (i.e. using <c>endLine</c>) are not supported.
        /// If you wish to use such issues, split the entities up into one per line (see <see cref="MarkIssues"/>).
        /// </param>
        /// <exception cref="ArgumentNullException">If <paramref name="tokens"/> is <c>null</c>.</exception>
        public void EnterFromTokens(IEnumerable<SEEToken> tokens,
                                    IDictionary<int, List<(SourceCodeEntity entity, Issue issue)>> issues = null)
        {
            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }

            // Avoid multiple enumeration in case iteration over the data source is expensive.
            TokenList = tokens.ToList();
            TokenLanguage language = TokenList.FirstOrDefault()?.Language;
            if (language == null)
            {
                Text = "<i>This file is empty.</i>";
                return;
            }

            // Unsurprisingly, each newline token corresponds to a new line.
            // However, we need to also add "hidden" newlines contained in other tokens, e.g. block comments.
            int assumedLines = TokenList.Count(x => x.TokenType.Equals(SEEToken.Type.Newline))
                               + TokenList.Where(x => !x.TokenType.Equals(SEEToken.Type.Newline))
                                          .Aggregate(0, (l, token) => token.Text.Count(x => x == '\n'));
            // Needed padding is the number of lines, because the line number will be at most this long.
             neededPadding = assumedLines.ToString().Length;

            Text = $"<color=#CCCCCC>{string.Join("", Enumerable.Repeat(" ", neededPadding - 1))}1</color> ";
            int lineNumber = 2; // Line number we'll write down next
            bool currentlyMarking = false;
            Dictionary<SEEToken, ISet<Issue>> issueTokens = new Dictionary<SEEToken, ISet<Issue>>();
            //TODO: Handle these issues

            foreach (SEEToken token in TokenList)
            {
                if (token.TokenType == SEEToken.Type.Unknown)
                {
                    Debug.LogError($"Unknown token encountered for text '{token.Text}'.\n");
                }

                if (token.TokenType == SEEToken.Type.Newline)
                {
                    AppendNewline(ref lineNumber, ref Text, neededPadding, token);
                }
                else if (token.TokenType != SEEToken.Type.EOF) // Skip EOF token completely.
                {
                    lineNumber = HandleToken(token);
                }
            }

            // Lines are equal to number of newlines, including the initial newline.
            lines = Text.Count(x => x.Equals('\n')); // No more weird CRLF shenanigans are present at this point.
            Text = Text.TrimStart('\n'); // Remove leading newline.

            # region Local Functions

            // Appends a newline to the text, assuming we're at theLineNumber and need the given padding.
            // Note that newlines MUST be added in this method, not anywhere else, else issue highlighting will break!
            void AppendNewline(ref int theLineNumber, ref string text, int padding, SEEToken token)
            {
                // Close an issue marking here if necessary
                if (currentlyMarking)
                {
                    text += "</mark></link>";
                    currentlyMarking = false;
                }

                // First, of course, the newline.
                text += "\n";
                // Add whitespace next to line number so it's consistent.
                text += string.Join("", Enumerable.Repeat(" ", padding - $"{theLineNumber}".Length));
                // Line number will be typeset in grey to distinguish it from the rest.
                text += $"<color=#CCCCCC>{theLineNumber}</color> ";

                if (issues?.ContainsKey(theLineNumber) ?? false)
                {
                    // If all issues in this line are content-based, we try to find the content within the line
                    if (issues[theLineNumber].Exists(x => x.entity.content == null)
                        || !HandleContentBasedIssue(theLineNumber, token))
                    {
                        // Otherwise, start new issue marking here if an issue in the line is line-based (has no content)
                        // or if we couldn't do the content-based issue marking for any reason.
                        HandleLineBasedIssue(theLineNumber, ref text);
                    }
                }

                theLineNumber++;
            }

            // Handles a token which may contain newlines and adds its syntax-highlighted content to the code window.
            // Returns the new line number.
            int HandleToken(SEEToken token)
            {
                string[] newlineStrings = NewlineCharacters.Select(x => x.ToString()).Concat(new[]
                {
                    // Apart from the characters themselves, we also want to look for the concatenation of them
                    NewlineCharacters.Aggregate("", (s, c) => s + c),
                    NewlineCharacters.Aggregate("", (s, c) => c + s)
                }).ToArray();
                string[] tokenLines = token.Text.Split(newlineStrings, StringSplitOptions.None);
                bool firstRun = true;
                foreach (string line in tokenLines)
                {
                    // Any entry after the first is on a separate line.
                    if (!firstRun)
                    {
                        AppendNewline(ref lineNumber, ref Text, neededPadding, token);
                    }

                    // Mark any potential issue
                    if (issueTokens.ContainsKey(token) && issueTokens[token].Count > 0)
                    {
                        if (currentlyMarking)
                        {
                            // If this line is already fully marked, we just add our issue to the corresponding link
                            Assert.IsNotNull(issueDictionary[linkCounter], "Entry must exist when we are currently marking!");
                            issueDictionary[linkCounter].AddRange(issueTokens[token]);
                        }
                        else
                        {
                            Color issueColor = DashboardRetriever.Instance.GetIssueColor(issueTokens[token].First());
                            string issueColorString = ColorUtility.ToHtmlStringRGB(issueColor);
                            IncreaseLinkCounter();
                            issueDictionary[linkCounter] = issueTokens[token].ToList();
                            Text += $"<link=\"{linkCounter.ToString()}\"><mark=#{issueColorString}33>";
                        }
                    }

                    if (token.TokenType == SEEToken.Type.Whitespace)
                    {
                        // We just copy the whitespace verbatim, no need to even color it.
                        // Note: We have to assume that whitespace will not interfere with TMP's XML syntax.
                        Text += line.Replace("\t", new string(' ', language.TabWidth));
                    }
                    else
                    {
                        Text += $"<color=#{token.TokenType.Color}><noparse>{line.Replace("/noparse", "")}</noparse></color>";
                    }

                    // Close any potential issue marking
                    if (issueTokens.ContainsKey(token) && !currentlyMarking)
                    {
                        Text += "</mark></link>";
                    }

                    firstRun = false;
                }

                return lineNumber;
            }

            // Returns true iff the content based issue could correctly be inserted
            bool HandleContentBasedIssue(int theLineNumber, SEEToken currentToken)
            {
                // Note: If there are any performance issues, I suspect the following loop body to be a major
                // contender for optimization. The easiest fix at the loss of functionality would be
                // to simply not mark the issues by content, but instead only use line-based markings.
                foreach ((SourceCodeEntity entity, Issue issue) in issues[theLineNumber])
                {
                    string entityContent = entity.content;
                    // We now have to determine whether this token is part of an issue entity.
                    // In order to do this, we look ahead in the token stream and construct the line we're on
                    // to determine whether the entity will arrive in this line or not.
                    IList<SEEToken> lineTokens =
                        TokenList.SkipWhile(x => x != currentToken).Skip(1)
                                 .TakeWhile(x => x.TokenType != SEEToken.Type.Newline
                                                 && !x.Text.Intersect(NewlineCharacters).Any()).ToList();
                    string line = lineTokens.Aggregate("", (s, t) => s + t.Text);
                    MatchCollection matches = Regex.Matches(line, Regex.Escape(entityContent));
                    if (matches.Count != 1)
                    {
                        // Switch to line-based marking instead.
                        // We do this if we found more than one occurence too, because in that case
                        // we have no way to determine which of the occurrences is the right one.
                        return false;
                    }
                    else
                    {
                        // We have to check at which token the entity begins and at which it (inclusively) ends.
                        // Note that this implies that we assume an entity will always encompass only whole
                        // tokens, never just parts of tokens. If this doesn't hold, the whole token will be
                        // highlighted anyway.
                        // TODO: It is possible to implement an algorithm which can also handle that,
                        // but that won't be done here, since it's out of scope.

                        // We first create a list of character-wise parts of the tokens, then match
                        // using the regex's index and length.
                        IList<SEEToken> matchTokens = lineTokens.SelectMany(t => t.Text.Select(_ => t))
                                                                .Skip(matches[0].Index)
                                                                .Take(entityContent.Length).ToList();
                        foreach (SEEToken matchToken in matchTokens.ToList())
                        {
                            if (!issueTokens.ContainsKey(matchToken))
                            {
                                issueTokens[matchToken] = new HashSet<Issue>();
                            }

                            issueTokens[matchToken].Add(issue);
                        }

                        Assert.IsTrue(matchTokens.Count > 0); // Regex Match necessitates at least 1 occurence!
                    }
                }

                return true;
            }

            // Returns the last line number which is part of the issue starting in this line
            void HandleLineBasedIssue(int theLineNumber, ref string text)
            {
                // Limitation: We can only use the color of the first issue, because we can't reliably detect the
                // order of the entities within a single line. Details for all issues are shown on hover.
                string issueColor = ColorUtility.ToHtmlStringRGB(DashboardRetriever.Instance.GetIssueColor(issues[theLineNumber][0].issue));
                IncreaseLinkCounter();
                issueDictionary[linkCounter] = issues[theLineNumber].Select(x => x.issue).ToList();
                text += $"<link=\"{linkCounter.ToString()}\"><mark=#{issueColor}33>"; //Transparency value of 0x33
                currentlyMarking = true;
            }

            // Increases the link counter to its next value
            void IncreaseLinkCounter()
            {
                Assert.IsTrue(linkCounter < char.MaxValue);
                char[] reservedCharacters = {'<', '>', '"', '\''}; // these characters would break our formatting
                // Increase link counter until it contains an allowed character
                while (reservedCharacters.Contains(++linkCounter))
                {
                    // intentionally left blank
                }
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

            neededPadding = $"{text.Length}".Length;
            Text = "";
            for (int i = 0; i < text.Length; i++)
            {
                // Add whitespace next to line number so it's consistent.
                Text += string.Join("", Enumerable.Repeat(" ", neededPadding - $"{i + 1}".Length));
                // Line number will be typeset in yellow to distinguish it from the rest.
                Text += $"<color=\"yellow\">{i + 1}</color> <noparse>{text[i].Replace("noparse", "")}</noparse>\n";
            }

            lines = text.Length;
        }

        /// <summary>
        /// Populates the code window with the contents of the given file.
        /// This will overwrite any existing text.
        /// </summary>
        /// <param name="filename">The platform-specific filename for the file to read.</param>
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
                //TODO: Maybe disable syntax highlighting for huge files, as it may impact performance badly.
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
            // First notification should stay as long as issues are still loading.
            Notification.Notification firstNotification = ShowNotification.Info("Loading issues...",
                                                                                "This may take a while.", -1f);
            string queryPath = Path.GetFileName(path);
            List<Issue> allIssues;
            try
            {
                allIssues = new List<Issue>(await DashboardRetriever.Instance.GetConfiguredIssues(fileFilter: $"\"*{queryPath}\""));
            }
            catch (DashboardException e)
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
            for (int skippedParts = path.Count(x => x == PATH_SEPARATOR) - 2; !MatchingPaths(allIssues); skippedParts--)
            {
                Assert.IsTrue(path.Contains(PATH_SEPARATOR));
                // Skip the first <c>skippedParts</c> parts, so that we query progressively larger parts.
                queryPath = string.Join(PATH_SEPARATOR.ToString(), path.Split(PATH_SEPARATOR).Skip(skippedParts));
                allIssues.RemoveAll(x => !x.Entities.Select(e => e.path).Any(p => p.EndsWith(queryPath)));
            }

            // Mapping from each line to the entities and issues contained therein.
            // Important: When an entity spans over multiple lines, it's split up into one entity per line.
            Dictionary<int, List<(SourceCodeEntity entity, Issue issue)>> entities =
                allIssues.SelectMany(x => x.Entities.SelectMany(SplitUpIntoLines).Select(e => (entity: e, issue: x)))
                         .Where(x => x.entity.path.EndsWith(queryPath))
                         .OrderBy(x => x.entity.line).GroupBy(x => x.entity.line)
                         .ToDictionary(x => x.Key, x => x.ToList());

            EnterFromTokens(TokenList, entities);

            await UniTask.SwitchToMainThread();
            firstNotification.Close();
            try
            {
                TextMesh.text = Text;
                TextMeshInputField.text = Text;
                TextMesh.ForceMeshUpdate();
            }
            catch (IndexOutOfRangeException)
            {
                //FIXME: Use multiple TMPs: Either one as an overlay, or split the main TMP up into multiple ones.
                ShowNotification.Error("File too big", "This file is too big to be displayed correctly.");
                return;
            }

            //TODO: This may as well be implemented as a loading bar, showing continuous progress as we iterate.
            ShowNotification.Info("Issues loaded", $"{allIssues.Count} issues have been found for {Title}.");

            // Returns true iff all issues are on the same path.
            static bool MatchingPaths(ICollection<Issue> issues)
            {
                // Every path in the first issue could be the "right" path, so we try them all.
                // If every issue has at least one path which matches that one, we can return true.
                return issues.First().Entities.Select(e => e.path)
                             .Any(path => issues.All(x => x.Entities.Any(e => e.path == path)));
            }

            // Splits up a SourceCodeEntity into one entity per line it is set on (ranging from line to endLine).
            // Each new entity will have a line attribute of the line it is split on and an endLine of null.
            // If the input parameter has no endLine, an enumerable with this entity as its only value will be returned.
            static IEnumerable<SourceCodeEntity> SplitUpIntoLines(SourceCodeEntity entity)
                => Enumerable.Range(entity.line, entity.endLine - entity.line + 1 ?? 1)
                             .Select(l => new SourceCodeEntity(entity.path, l, null, entity.content));
        }

        /// <summary>
        /// Returns the index within the "rich" text (with markup tags) from index in the "clean" text (with markup tags).
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
        /// corresponding index in the text with tags present, which in our example would be 20 and 38.
        /// </example>
        private int GetRichIndex(int cleanIndex)
        {
            return TextMesh.textInfo.characterInfo[cleanIndex].index;
        }

        /// <summary>
        /// Returns the clean index for a given rich index
        /// See also <see cref="GetRichIndex(int)"/>
        /// </summary>
        /// <param name="richIndex"></param>
        /// <returns>clean index</returns>
        private int GetCleanIndex(int richIndex)
        {
            return TextMesh.textInfo.characterInfo.Select((x, idx) => (x, idx)).First( x => x.x.index >= richIndex).idx;
        }

        /// <summary>
        /// Returns the Text without the XML Tags
        /// </summary>
        /// <returns>The clean text</returns>
        private async UniTask<string> AsyncGetCleanText()
        {
            await UniTask.SwitchToThreadPool();
            string ret = TextMesh.textInfo.characterInfo.Aggregate("", (result, c) => result += c.character);
            await UniTask.SwitchToMainThread();
            return ret;
        }
    }
}