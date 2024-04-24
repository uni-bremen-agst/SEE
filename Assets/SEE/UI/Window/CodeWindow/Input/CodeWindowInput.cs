using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using SEE.Game;
using SEE.Game.City;
using SEE.GO;
using SEE.UI.Notification;
using SEE.Net.Dashboard;
using SEE.Net.Dashboard.Model.Issues;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.UI.Window.CodeWindow
{
    /// <summary>
    /// Partial class containing methods related to processing input for the code windows.
    /// </summary>
    public partial class CodeWindow
    {
        /// <summary>
        /// The needed padding for the line numbers.
        /// </summary>
        private int neededPadding;

        /// <summary>
        /// A dictionary mapping each link ID to its issues.
        /// </summary>
        private readonly Dictionary<char, List<Issue>> issueDictionary = new();

        /// <summary>
        /// Counter which represents the lowest unfilled position in the <see cref="issueDictionary"/>.
        /// Any index above it must not be filled either.
        /// </summary>
        private char linkCounter = char.MinValue;

        /// <summary>
        /// List of tokens for this code window.
        /// </summary>
        private List<SEEToken> tokenList;

        /// <summary>
        /// Characters representing newlines.
        /// Note that newlines may also consist of aggregations of this set (e.g. "\r\n").
        /// </summary>
        private static readonly char[] newlineCharacters = { '\r', '\n' };

        /// <summary>
        /// Populates the code window with the content of the given token stream.
        /// </summary>
        /// <param name="tokens">Stream of tokens representing the source code of this code window.</param>
        /// <param name="issues">Issues for this file. If <c>null</c>, will be automatically retrieved.
        /// Entities spanning multiple lines (i.e. using <c>endLine</c>) are not supported.
        /// If you wish to use such issues, split the entities up into one per line (see <see cref="MarkIssuesAsync"/>).
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
            tokenList = tokens.ToList();
            TokenLanguage language = tokenList.FirstOrDefault()?.Language;
            if (language == null)
            {
                text = "<i>This file is empty.</i>";
                return;
            }

            // Unsurprisingly, each newline token corresponds to a new line.
            // However, we need to also add "hidden" newlines contained in other tokens, e.g. block comments.
            int assumedLines = tokenList.Count(x => x.TokenType.Equals(SEEToken.Type.Newline))
                + tokenList.Where(x => !x.TokenType.Equals(SEEToken.Type.Newline))
                           .Aggregate(0, (_, token) => token.Text.Count(x => x == '\n'));
            // Needed padding is the number of lines, because the line number will be at most this long.
            neededPadding = assumedLines.ToString().Length;

            text = $"<color=#CCCCCC>{string.Join("", Enumerable.Repeat(" ", neededPadding - 1))}1</color> ";
            int lineNumber = 2; // Line number we'll write down next
            bool currentlyMarking = false;
            Dictionary<SEEToken, ISet<Issue>> issueTokens = new();

            foreach (SEEToken token in tokenList)
            {
                if (token.TokenType == SEEToken.Type.Unknown)
                {
                    Debug.LogError($"Unknown token encountered for text '{token.Text}'.\n");
                }

                if (token.TokenType == SEEToken.Type.Newline)
                {
                    AppendNewline(ref lineNumber, ref text, neededPadding, token);
                }
                else if (token.TokenType != SEEToken.Type.EOF) // Skip EOF token completely.
                {
                    lineNumber = HandleToken(token);
                }
            }

            // Lines are equal to number of newlines, including the initial newline.
            lines = text.Count(x => x.Equals('\n')); // No more weird CRLF shenanigans are present at this point.
            text = text.TrimStart('\n'); // Remove leading newline.

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
                    if (issues[theLineNumber].Exists(x => x.entity.Content == null)
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
                string[] newlineStrings = newlineCharacters.Select(x => x.ToString()).Concat(new[]
                {
                    // Apart from the characters themselves, we also want to look for the concatenation of them
                    newlineCharacters.Aggregate("", (s, c) => s + c),
                    newlineCharacters.Aggregate("", (s, c) => c + s)
                }).ToArray();
                string[] tokenLines = token.Text.Split(newlineStrings, StringSplitOptions.None);
                bool firstRun = true;
                foreach (string line in tokenLines)
                {
                    // Any entry after the first is on a separate line.
                    if (!firstRun)
                    {
                        AppendNewline(ref lineNumber, ref text, neededPadding, token);
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
                            text += $"<link=\"{linkCounter.ToString()}\"><mark=#{issueColorString}33>";
                        }
                    }

                    if (token.TokenType == SEEToken.Type.Whitespace)
                    {
                        // We just copy the whitespace verbatim, no need to even color it.
                        // Note: We have to assume that whitespace will not interfere with TMP's XML syntax.
                        text += line.Replace("\t", new string(' ', language.TabWidth));
                    }
                    else
                    {
                        text += $"<color=#{token.TokenType.Color}><noparse>{line.Replace("/noparse", "")}</noparse></color>";
                    }

                    // Close any potential issue marking
                    if (issueTokens.ContainsKey(token) && !currentlyMarking)
                    {
                        text += "</mark></link>";
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
                    string entityContent = entity.Content;
                    // We now have to determine whether this token is part of an issue entity.
                    // In order to do this, we look ahead in the token stream and construct the line we're on
                    // to determine whether the entity will arrive in this line or not.
                    IList<SEEToken> lineTokens =
                        tokenList.SkipWhile(x => x != currentToken).Skip(1)
                                 .TakeWhile(x => x.TokenType != SEEToken.Type.Newline
                                                && !x.Text.Intersect(newlineCharacters).Any()).ToList();
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
                char[] reservedCharacters = { '<', '>', '"', '\'' }; // these characters would break our formatting
                // Increase link counter until it contains an allowed character
                while (reservedCharacters.Contains(++linkCounter))
                {
                    // intentionally left blank
                }
            }

            #endregion
        }

        /// <summary>
        /// Populates the code window with the given <paramref name="text"/>.
        /// This will overwrite any existing text.
        /// </summary>
        /// <param name="text">An array of lines to use for the code window.</param>
        /// <param name="asIs">if true, the <paramref name="text"/> will be added as is, that is,
        /// without being included into a noparse clause</param>
        /// <exception cref="ArgumentException">If <paramref name="text"/> is empty or <c>null</c></exception>
        public void EnterFromText(string[] text, bool asIs = false)
        {
            if (text is not { Length: > 0 })
            {
                throw new ArgumentException("Given text must not be empty or null.\n");
            }

            neededPadding = $"{text.Length}".Length;
            this.text = "";
            for (int i = 0; i < text.Length; i++)
            {
                // Add whitespace next to line number so it's consistent.
                this.text += string.Join("", Enumerable.Repeat(" ", neededPadding - $"{i + 1}".Length));
                // Line number will be typeset in yellow to distinguish it from the rest.
                this.text += $"<color=\"yellow\">{i + 1}</color> ";
                if (asIs)
                {
                    this.text += text[i] + "\n";
                }
                else
                {
                    this.text += $"<noparse>{text[i].Replace("noparse", "")}</noparse>\n";
                }
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
                Destroyer.Destroy(this);
                return;
            }

            try
            {
                // TODO (#250): Maybe disable syntax highlighting for huge files, as it may impact performance badly.
                if (syntaxHighlighting)
                {
                    try
                    {
                        EnterFromTokens(SEEToken.FromFile(filename));
                        GameObject go = SceneQueries.GetCodeCity(transform)?.gameObject;
                        if (go && go.TryGetComponentOrLog(out AbstractSEECity city)
                            && city.ErosionSettings.ShowIssuesInCodeWindow)
                        {
                            MarkIssuesAsync(filename).Forget(); // initiate issue search
                        } else if (HasStarted)
                        {
                            textMesh.SetText(text);
                            SetupBreakpoints();
                        }
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
                Destroyer.Destroy(this);
            }
        }

        /// <summary>
        /// Loads code issues for the file at the given path, and fills the CodeWindow
        /// with the collected tokens while marking all detected issues.
        /// </summary>
        /// <param name="path">The path to the file whose issues shall be marked.</param>
        private async UniTaskVoid MarkIssuesAsync(string path)
        {
            using (LoadingSpinner.Show($"Loading issues for {Title}..."))
            {
                string queryPath = Path.GetFileName(path);
                List<Issue> allIssues;
                try
                {
                    allIssues = new List<Issue>(await DashboardRetriever.Instance.GetConfiguredIssuesAsync(fileFilter: $"\"*{queryPath}\""));
                }
                catch (DashboardException e)
                {
                    ShowNotification.Error("Couldn't load issues", e.Message);
                    return;
                }

                await UniTask.SwitchToThreadPool(); // don't interrupt main UI thread
                if (allIssues.Count == 0)
                {
                    return;
                }

                const char pathSeparator = '/';
                // When there are different paths in the issue table, this implies that there are some files
                // which aren't actually the one we're looking for (because we've only matched by filename so far).
                // In this case, we'll gradually refine our results until this isn't the case anymore.
                for (int skippedParts = path.Count(x => x == pathSeparator) - 2; !MatchingPaths(allIssues); skippedParts--)
                {
                    Assert.IsTrue(path.Contains(pathSeparator));
                    // Skip the first <c>skippedParts</c> parts, so that we query progressively larger parts.
                    queryPath = string.Join(pathSeparator.ToString(), path.Split(pathSeparator).Skip(skippedParts));
                    allIssues.RemoveAll(x => !x.Entities.Select(e => e.Path).Any(p => p.EndsWith(queryPath)));
                }

                // Mapping from each line to the entities and issues contained therein.
                // Important: When an entity spans over multiple lines, it's split up into one entity per line.
                Dictionary<int, List<(SourceCodeEntity entity, Issue issue)>> entities =
                    allIssues.SelectMany(x => x.Entities.SelectMany(SplitUpIntoLines).Select(e => (entity: e, issue: x)))
                             .Where(x => x.entity.Path.EndsWith(queryPath))
                             .OrderBy(x => x.entity.Line).GroupBy(x => x.entity.Line)
                             .ToDictionary(x => x.Key, x => x.ToList());

                EnterFromTokens(tokenList, entities);

                await UniTask.SwitchToMainThread();

                try
                {
                    textMesh.text = text;
                    textMesh.ForceMeshUpdate();
                    SetupBreakpoints();
                }
                catch (IndexOutOfRangeException)
                {
                    // FIXME (#250): Use multiple TMPs: Either one as an overlay, or split the main TMP up into multiple ones.
                    ShowNotification.Error("File too big", "This file is too big to be displayed correctly.");
                }
            }
            return;

            // Returns true iff all issues are on the same path.
            static bool MatchingPaths(ICollection<Issue> issues)
            {
                // Every path in the first issue could be the "right" path, so we try them all.
                // If every issue has at least one path which matches that one, we can return true.
                return issues.First().Entities.Select(e => e.Path)
                             .Any(path => issues.All(x => x.Entities.Any(e => e.Path == path)));
            }

            // Splits up a SourceCodeEntity into one entity per line it is set on (ranging from line to endLine).
            // Each new entity will have a line attribute of the line it is split on and an endLine of null.
            // If the input parameter has no endLine, an enumerable with this entity as its only value will be returned.
            static IEnumerable<SourceCodeEntity> SplitUpIntoLines(SourceCodeEntity entity)
                => Enumerable.Range(entity.Line, entity.EndLine - entity.Line + 1 ?? 1)
                             .Select(l => new SourceCodeEntity(entity.Path, l, null, entity.Content));
        }
    }
}
