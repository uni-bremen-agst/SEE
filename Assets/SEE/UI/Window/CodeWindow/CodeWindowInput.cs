using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.GO;
using SEE.UI.Notification;
using SEE.Net.Dashboard;
using SEE.Net.Dashboard.Model.Issues;
using SEE.Scanner;
using SEE.Scanner.Antlr;
using SEE.Scanner.LSP;
using SEE.Tools.LSP;
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
        private readonly Dictionary<char, HashSet<IDisplayableIssue>> issueDictionary = new();

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
        /// A list of starting offsets of each line in the code window (without rich tags), sorted in ascending order.
        ///
        /// In other words, this contains the character indices of newlines in the rendered text (the text without
        /// the rich tags in them, but with the line numbers at the beginning of each line).
        /// </summary>
        private List<int> CodeWindowOffsets { get; } = new();

        /// <summary>
        /// The graph associated with the code city this code window is in.
        /// </summary>
        private Graph AssociatedGraph;

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
        private void EnterFromTokens(IEnumerable<SEEToken> tokens,
                                     IDictionary<int, List<IDisplayableIssue>> issues = null)
        {
            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }

            // Avoid multiple enumeration in case iteration over the data source is expensive.
            tokenList = tokens.ToList();
            if (!tokenList.Any())
            {
                text = "<i>This file is empty.</i>";
                return;
            }

            // Unsurprisingly, each newline token corresponds to a new line.
            // However, we need to also add "hidden" newlines contained in other tokens, e.g. block comments.
            int assumedLines = tokenList.Count(x => x.TokenType.Equals(TokenType.Newline))
                + tokenList.Where(x => !x.TokenType.Equals(TokenType.Newline))
                           .Aggregate(0, (_, token) => token.Text.Count(x => x == '\n'));
            // Needed padding is the number of lines, because the line number will be at most this long.
            neededPadding = Mathf.FloorToInt(Mathf.Log10(assumedLines)) + 1;
            text = $"<color=#CCCCCC>{string.Join("", Enumerable.Repeat(" ", neededPadding - 1))}1</color> ";

            CodeWindowOffsets.Clear();
             // The first line starts at the beginning of the text after the line number.
            CodeWindowOffsets.Add(0);

             // Line number we'll write down next
            int lineNumber = 2;
             // Offset of the current character in the text (excluding rich tags)
            int characterOffset = neededPadding + 1; // + 1 for the space after the line number
            // The issue that we're currently marking, if any.
            IDisplayableIssue currentlyMarking = null;
            // We need reference equality here.
            Dictionary<SEEToken, ISet<IDisplayableIssue>> issueTokens = new(ReferenceEqualityComparer.Instance);

            foreach (SEEToken token in tokenList)
            {
                if (token.TokenType == TokenType.Newline)
                {
                    AppendNewline(ref lineNumber, ref text, token);
                }
                else if (token.TokenType != TokenType.EOF) // Skip EOF token completely.
                {
                    HandleToken(token);
                }
            }

            // End any issue marking that may still be open.
            EndIssueSegment();

            // Lines are equal to number of newlines, including the initial newline.
            lines = text.Count(x => x.Equals('\n')); // No more weird CRLF shenanigans are present at this point.
            text = text.TrimStart('\n'); // Remove leading newline.

            # region Local Functions

            // Appends a newline to the text, assuming we're at theLineNumber and need the given padding.
            // Note that newlines MUST be added in this method, not anywhere else, else issue highlighting will break!
            void AppendNewline(ref int theLineNumber, ref string text, SEEToken token)
            {
                // Close an issue marking here if necessary
                EndIssueSegment();

                // First, of course, the newline.
                text += "\n";
                // At this point, we need to remember the offset of this new line.
                Assert.AreEqual(CodeWindowOffsets.Count, theLineNumber-1);
                // + 1 for the newline
                CodeWindowOffsets.Add(++characterOffset);
                // Add whitespace next to line number, so it's consistent.
                int padding = neededPadding - (Mathf.FloorToInt(Mathf.Log10(theLineNumber)) + 1);
                // Line number will be typeset in grey to distinguish it from the rest.
                text += $"<color=#CCCCCC>{string.Join(string.Empty, Enumerable.Repeat(' ', padding))}{theLineNumber}</color> ";
                characterOffset += neededPadding + 1;

                if (issues?.ContainsKey(theLineNumber) ?? false)
                {
                    HandleIssuesInLine(theLineNumber, token);
                }

                theLineNumber++;
            }

            // Handles a token which may contain newlines and adds its syntax-highlighted content to the code window.
            void HandleToken(SEEToken token)
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
                        AppendNewline(ref lineNumber, ref text, token);
                    }

                    // Mark any potential issue
                    if (issueTokens.ContainsKey(token) && issueTokens[token].Count > 0)
                    {
                        if (currentlyMarking != null)
                        {
                            // We're already marking something.
                            Assert.IsNotNull(issueDictionary[linkCounter], "Entry must exist when we are currently marking!");
                            if (issueTokens[token].Intersect(issueDictionary[linkCounter]).Any())
                            {
                                // If this token contains the same issue, we just need to add any new issues to the current segment.
                                issueDictionary[linkCounter].UnionWith(issueTokens[token]);
                            }
                            else
                            {
                                // If it doesn't, we close the current segment and start a new one.
                                EndIssueSegment();
                                StartIssueSegment(token);
                            }
                        }
                        else
                        {
                            // We're not marking anything, so we can start a new segment.
                            StartIssueSegment(token);
                        }
                    }
                    else
                    {
                        // No issue is being marked. We should stop marking if we were marking something.
                        EndIssueSegment();
                    }

                    if (token.TokenType == TokenType.Whitespace)
                    {
                        // We just copy the whitespace verbatim, no need to even color it.
                        // Note: We have to assume that whitespace will not interfere with TMP's XML syntax.
                        string replaced = line.Replace("\t", new string(' ', token.Language.TabWidth));
                        text += replaced;
                        characterOffset += replaced.Length;
                    }
                    else
                    {
                        List<string> tags = token.Modifiers.AsEnumerable().Select(x => x.ToRichTextTag())
                                                 .Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
                        foreach (string textTag in tags)
                        {
                            text += $"<{textTag}>";
                        }
                        if (currentlyMarking is not { HasColorTags: true })
                        {
                            text += $"<color=#{token.TokenType.Color}>";
                        }
                        string replaced = line.Replace("</noparse>", @"<\noparse>");
                        text += $"<noparse>{replaced}</noparse>";
                        characterOffset += replaced.Length;
                        if (currentlyMarking is not { HasColorTags: true })
                        {
                            text += "</color>";
                        }
                        tags.Reverse();
                        foreach (string textTag in tags)
                        {
                            text += $"</{textTag}>";
                        }
                    }

                    firstRun = false;
                }
            }

            // Begins marking a new issue segment starting with the given token.
            void StartIssueSegment(SEEToken token)
            {
                Assert.IsNull(currentlyMarking, "We must not start a new marking segment while we're already marking!");
                IncreaseLinkCounter();
                issueDictionary[linkCounter] = issueTokens[token].ToHashSet();
                currentlyMarking = issueTokens[token].First();
                text += $"<link=\"{linkCounter.ToString()}\">{currentlyMarking.OpeningRichTags}";
            }

            // Ends marking the current issue segment, if there is any.
            void EndIssueSegment()
            {
                if (currentlyMarking != null)
                {
                    text += $"{currentlyMarking.ClosingRichTags}</link>";
                }
                currentlyMarking = null;
            }

            // Prepares issueTokens for the line number, assuming the current token is the newline token
            // delineating the beginning of this line.
            void HandleIssuesInLine(int theLineNumber, SEEToken currentToken)
            {
                // We have to determine whether a given token is part of an issue entity.
                // In order to do this, we look ahead in the token stream and construct the line we're on
                // to determine whether the entity will arrive in this line or not.
                IList<SEEToken> lineTokens =
                    tokenList.SkipWhile(x => !ReferenceEquals(x, currentToken)).Skip(1)
                             .TakeWhile(x => x.TokenType != TokenType.Newline
                                            && !x.Text.Intersect(newlineCharacters).Any()).ToList();
                string line = lineTokens.Aggregate(string.Empty, (s, t) => s + t.Text);

                foreach (IDisplayableIssue issue in issues[theLineNumber])
                {
                    (int startCharacter, int endCharacter)? characterRange = issue.GetCharacterRangeForLine(FilePath, theLineNumber, line);
                    if (!characterRange.HasValue)
                    {
                        // Switch to line-based marking instead.
                        IEnumerable<SEEToken> matchTokens = lineTokens.SkipWhile(t => t.TokenType == TokenType.Whitespace);
                        foreach (SEEToken matchToken in matchTokens)
                        {
                            issueTokens.GetOrAdd(matchToken, () => new HashSet<IDisplayableIssue>()).UnionWith(issues[theLineNumber]);
                        }
                        return;
                    }
                    else
                    {
                        // We have to check at which token the entity begins and at which it (inclusively) ends.
                        // Note that this implies that we assume an entity will always encompass only whole
                        // tokens, never just parts of tokens. If this doesn't hold, the whole token will be
                        // highlighted anyway.

                        // We first create a list of character-wise parts of the tokens, then match
                        // using the result's index and length.
                        IEnumerable<SEEToken> matchTokens = lineTokens
                                                            .SelectMany(t => Enumerable.Repeat(t, t.Text.Length))
                                                            .Skip(characterRange.Value.startCharacter)
                                                            // Exclusive end character.
                                                            .Take(characterRange.Value.endCharacter - characterRange.Value.startCharacter - 1);
                        foreach (SEEToken matchToken in matchTokens)
                        {
                            issueTokens.GetOrAdd(matchToken, () => new HashSet<IDisplayableIssue>()).Add(issue);
                        }
                    }
                }
            }

            // Increases the link counter to its next value.
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
                this.text += $"<color=#CCCCCC>{i + 1}</color> ";
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
        /// Populates the code window with the syntax-highlighted contents of the given file.
        /// This will overwrite any existing text.
        /// Syntax-highlighting will be done using the LSP, or Antlr if LSP is not configured for this code city.
        /// </summary>
        /// <param name="filename">The platform-specific filename for the file to read.</param>
        public async UniTask EnterFromFileAsync(string filename)
        {
            FilePath = filename;

            // Try to read the file, otherwise display the error message.
            if (!File.Exists(filename))
            {
                ShowNotification.Error("File not found", $"Couldn't find file '{filename}'.");
                Destroyer.Destroy(this);
                return;
            }

            text = "<color=\"orange\">Loading code window text...</color>";
            lines = 1;

            // TODO (#250): Maybe disable syntax highlighting for huge files, as it may impact performance badly.
            using (LoadingSpinner.ShowIndeterminate($"Loading {Path.GetFileName(filename)}..."))
            {
                GameObject go = SceneQueries.GetCodeCity(transform).gameObject;
                IEnumerable<SEEToken> tokens;
                try
                {
                    // Usage of LSP in code windows must be configured in the LSPHandler,
                    // the language server must support semantic tokens, and the language of the file
                    // (inferred from the file extension) must be supported by the server.
                    if (go.TryGetComponent(out lspHandler) && lspHandler.UseInCodeWindows
                        && lspHandler.ServerCapabilities.SemanticTokensProvider != null
                        && TryGetLanguageOrLog(lspHandler, out LSPLanguage language))
                    {
                        lspHandler.enabled = true;
                        lspHandler.OpenDocument(filename);
                        tokens = await LSPToken.FromFileAsync(filename, lspHandler, language);
                    }
                    else
                    {
                        lspHandler = null;
                        tokens = await AntlrToken.FromFileAsync(filename);
                    }
                }
                catch (IOException exception)
                {
                    ShowNotification.Error("File access error", $"Couldn't access file {filename}: {exception}");
                    Destroyer.Destroy(this);
                    return;
                }
                EnterFromTokens(tokens);

                if (HasStarted)
                {
                    textMesh.SetText(text);
                    await UniTask.Yield(); // Wait one frame for the text meshes to be updated.
                    SetupBreakpoints();
                }

                if (go.TryGetComponentOrLog(out AbstractSEECity city))
                {
                    AssociatedGraph = city.LoadedGraph;
                    bool useDashboardIssues = city.ErosionSettings.ShowDashboardIssuesInCodeWindow;
                    bool useLspIssues = lspHandler != null && lspHandler.UseInCodeWindows;
                    MarkIssuesAsync(filename, useDashboardIssues, useLspIssues).Forget(); // initiate issue search in background
                }
            }
            return;

            // Returns true iff the language for the given filename is supported by the LSP server.
            bool TryGetLanguageOrLog(LSPHandler handler, out LSPLanguage language)
            {
                string extension = Path.GetExtension(filename).TrimStart('.');
                language = handler.Server.Languages.FirstOrDefault(x => x.FileExtensions.Contains(extension));
                if (language == null)
                {
                    ShowNotification.Warn("Unsupported LSP language",
                                          $"Language for extension '{extension}' not supported by the configured LSP server. "
                                          + "Falling back to Antlr for syntax highlighting, LSP capabilities will not be available.");
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Loads code issues for the file at the given path, and fills the CodeWindow
        /// with the collected tokens while marking all detected issues.
        /// </summary>
        /// <param name="path">The path to the file whose issues shall be marked.</param>
        /// <param name="useDashboardIssues">Whether to use issues from the Axivion Dashboard.</param>
        /// <param name="useLspIssues">Whether to use issues from the LSP server.</param>
        private async UniTaskVoid MarkIssuesAsync(string path, bool useDashboardIssues, bool useLspIssues)
        {
            if (!useDashboardIssues && !useLspIssues)
            {
                return;
            }
            using (LoadingSpinner.ShowIndeterminate($"Loading issues for {Title}..."))
            {
                List<IDisplayableIssue> allIssues = new();

                if (useDashboardIssues)
                {
                    allIssues.AddRange(await GetDashboardIssuesAsync(path));
                }
                if (useLspIssues)
                {
                    allIssues.AddRange(GetLspIssues(path));
                }

                if (allIssues.Count == 0)
                {
                    Debug.Log($"No issues found for {path}");
                    return;
                }

                await UniTask.SwitchToThreadPool(); // don't interrupt main UI thread

                string queryPath = Path.GetFileName(path);
                // Mapping from each line to the entities and issues contained therein.
                // Important: When an entity spans over multiple lines, it's split up into one entity per line.
                IDictionary<int, List<IDisplayableIssue>> issues =
                    allIssues.SelectMany(issue => issue.Occurrences
                                                       .SelectMany(e => e.Range.SplitIntoLines()
                                                                         .Select(range => (path, range, issue))))
                             .Where(x => x.path.EndsWith(queryPath))
                             .OrderBy(x => x.range.StartLine).GroupBy(x => x.range.StartLine)
                             .ToDictionary(x => x.Key, x => x.Select(y => y.issue).ToList());

                EnterFromTokens(tokenList, issues);

                await UniTask.SwitchToMainThread();

                try
                {
                    textMesh.text = text;
                    textMesh.ForceMeshUpdate();
                    // Will need to be marked again after the text has been updated.
                    MarkLine(ScrolledVisibleLine);
                    SetupBreakpoints();
                }
                catch (IndexOutOfRangeException)
                {
                    // FIXME (#250): Use multiple TMPs: Either one as an overlay, or split the main TMP up into multiple ones.
                    ShowNotification.Error("File too big", "This file is too big to be displayed correctly.");
                }
            }
        }

        /// <summary>
        /// Retrieves all issues for the given <paramref name="path"/> from the LSP server.
        /// </summary>
        /// <param name="path">The path of the file to get issues for.</param>
        /// <returns>A list of all issues for the given path.</returns>
        private List<LSPIssue> GetLspIssues(string path) =>
            lspHandler.GetPublishedDiagnosticsForPath(path)
                      .SelectMany(x => x.Diagnostics)
                      .Select(x => new LSPIssue(path, x))
                      .ToList();

        /// <summary>
        /// Retrieves all issues for the given <paramref name="path"/> from the Axivion Dashboard.
        /// </summary>
        /// <param name="path">The path of the file to get issues for.</param>
        /// <returns>A list of all issues for the given path.</returns>
        private static async UniTask<List<Issue>> GetDashboardIssuesAsync(string path)
        {
            string queryPath = Path.GetFileName(path);
            List<Issue> allIssues;
            try
            {
                allIssues = new List<Issue>(await DashboardRetriever.Instance.GetConfiguredIssuesAsync(fileFilter: $"\"*{queryPath}\""));
            }
            catch (DashboardException e)
            {
                ShowNotification.Error("Couldn't load dashboard issues", e.Message);
                return new List<Issue>();
            }

            const char pathSeparator = '/';
            // When there are different paths in the issue table, this implies that there are some files
            // which aren't actually the one we're looking for (because we've only matched by filename so far).
            // In this case, we'll gradually refine our results until this isn't the case anymore.
            for (int skippedParts = path.Count(x => x == pathSeparator) - 2; !AllMatchingPaths(allIssues); skippedParts--)
            {
                Assert.IsTrue(path.Contains(pathSeparator));
                // Skip the first <c>skippedParts</c> parts, so that we query progressively larger parts.
                queryPath = string.Join(pathSeparator.ToString(), path.Split(pathSeparator).Skip(skippedParts));
                allIssues.RemoveAll(x => !x.Occurrences.Any(e => e.Path.EndsWith(queryPath)));
            }

            return allIssues;

            // Returns true iff all issues are on the same path.
            static bool AllMatchingPaths(ICollection<Issue> issues)
            {
                if (!issues.Any())
                {
                    return true;
                }
                // Every path in the first issue could be the "right" path, so we try them all.
                // If every issue has at least one path which matches that one, we can return true.
                return issues.First().Occurrences.Select(e => e.Path)
                             .Any(path => issues.All(x => x.Occurrences.Any(e => e.Path == path)));
            }
        }
    }
}
