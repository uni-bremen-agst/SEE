using LibGit2Sharp;
using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.Utils.Paths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace SEE.VCS
{
    /// <summary>
    /// Reports, for every branch selected by <see cref="branches"/> and every file having
    /// an extension in <see cref="extensions"/> and located in one of the
    /// <see cref="directories"/>, the number of lines added, the number of lines
    /// deleted, the number of commits, and the list of authors, taking only
    /// commits into account that were authored at or after <see cref="since"/>.
    /// </summary>
    /// <remarks>
    /// This is the LibGit2Sharp counterpart of the following query, run once per
    /// branch and aggregated per file:
    ///
    /// <code>
    /// git log &lt;branch&gt; --no-merges --full-history --find-renames=50% --numstat -z
    ///     --pretty=format:'#%H%x09%at%x09%aN' -- &lt;directories&gt;
    /// </code>
    ///
    /// The <c>--full-history</c> is what this walk amounts to, every commit
    /// reachable from the branch being compared against its first parent.
    /// Without it, git simplifies the history it reports for a path: where a
    /// merge is identical to one of its parents in that path, the other side is
    /// pruned, and commits having changed the file there go unreported. That is
    /// the right answer to "which commits explain the file as it stands", but
    /// not to "how much work went into it", which is what is counted here.
    ///
    /// Identity and date are the author's rather than the committer's, so that
    /// rebases, cherry-picks and the merges GitHub records under its own name do
    /// neither reattribute the work nor move it across the date boundary.
    /// Renames are followed, hence all churn of a file is aggregated under the
    /// name the file carries at the tip of the branch.
    ///
    /// LibGit2Sharp 0.29 has no equivalent of git's %aN, that is, it does not
    /// read .mailmap. <see cref="Mailmap"/> makes up for that, so that the
    /// authors reported here are the same ones git would report.
    /// </remarks>
    internal class TestGitFileChurn
    {
        /// <summary>
        /// The similarity, in percent, at and above which a deletion and an
        /// addition are considered a rename. This is git's own default.
        /// </summary>
        private const int renameThreshold = 50;

        /// <summary>
        /// The maximal number of renames a single file may have undergone. Only
        /// a guard against a cycle, which a file renamed back and forth creates.
        /// </summary>
        private const int maximalRenameChain = 1000;

        /// <summary>
        /// The configurations <see cref="TestChurnPerBranch"/> is run on, one
        /// test case each.
        /// </summary>
        /// <remarks>
        /// A source rather than a <see cref="TestCaseAttribute"/>, because the
        /// argument of an attribute must be a constant expression, and
        /// <c>new DateTimeOffset(...)</c> is not one: an object creation never
        /// is, and neither is the <c>TimeSpan.FromHours(1)</c> within it. The
        /// arrays would have been allowed, an array of constants being a
        /// constant expression, but the date alone rules the attribute out.
        ///
        /// Being a method, this is moreover evaluated when the test is
        /// discovered rather than when this class is initialized, so it may
        /// read the fields above whatever order those are declared in.
        ///
        /// Name every case: with none, all that tells two of them apart in the
        /// test runner is <c>System.String[]</c> three times over.
        /// </remarks>
        /// <returns>one test case per configuration</returns>
        private static IEnumerable<TestCaseData> Configurations()
        {
            /// <summary>
            /// The beginning of the period to be reported on; commits authored at
            /// this very instant are still taken into account. The UTC offset is
            /// stated explicitly, because the instant denoted would otherwise depend
            /// on the time zone of the machine running this test and would, around a
            /// switch to or from daylight saving time, be ambiguous.
            /// </summary>
            DateTimeOffset since
            = new(2025, 1, 1, 0, 0, 0, TimeSpan.FromHours(1));

            /// <summary>
            /// The directories, relative to the root of the repository and separated
            /// by <c>/</c>, whose files are to be reported on. Nested directories are
            /// included.
            /// </summary>
            string[] directories
                = { "Assets/SEE", "Assets/SEETests" };

            /// <summary>
            /// The extensions, leading dot included, a file must have to be reported
            /// on.
            /// </summary>
            string[] extensions = { ".cs" };

            /// <summary>
            /// Regular expressions selecting the branches to be reported on, one
            /// report for each branch selected. A branch is selected if one of these
            /// expressions matches its name as a whole. That name is the one
            /// <c>git branch -a</c> prints: <c>heads/master</c> for a local branch
            /// and <c>remotes/origin/master</c> for a remote-tracking one. Hence
            /// <c>remotes/origin/.*</c> selects every remote branch and
            /// <c>.*/master</c> both the local and the remote master.
            ///
            /// Every expression must select at least one branch; one selecting none
            /// fails this test rather than silently narrowing the report. A branch
            /// selected by several expressions is still reported on only once.
            ///
            /// Mind that each selected branch costs a walk of its entire history, so
            /// an expression selecting many branches makes for a long-running test.
            /// </summary>
            string[] branches
                = { "heads/master", "heads/996-add-better-support-for-profiling", "remotes/origin/.*" };

            yield return new TestCaseData(since, directories, extensions, branches)
                    .SetName("SEE sources and tests");
        }

        /// <summary>
        /// Emits the report announced for <see cref="TestGitFileChurn"/>, one
        /// section per branch, to the console.
        /// </summary>
        /// <param name="since">The beginning of the period to be reported on.</param>
        /// <param name="directories">The directories whose files are to be reported on.</param>
        /// <param name="extensions">The extensions a file must have to be reported on.</param>
        /// <param name="branches">Regular expressions selecting the branches to be reported on.</param>
        [TestCaseSource(nameof(Configurations))]
        public void TestChurnPerBranch
              (DateTimeOffset since,
               string[] directories,
               string[] extensions,
               string[] branches)
        {
            AddNodesAfterDate(DataPath.ProjectFolder(), since, directories, extensions, branches, default, default);
        }

        /// <summary>
        /// Emits the report on the repository at <paramref name="repositoryPath"/>,
        /// one section per branch, to the console.
        /// </summary>
        /// <param name="repositoryPath">the path of the repository to be reported on</param>
        /// <param name="since">the beginning of the period to be reported on</param>
        /// <param name="directories">the directories whose files are to be reported on</param>
        /// <param name="extensions">the extensions a file must have to be reported on</param>
        /// <param name="branches">regular expressions selecting the branches to be reported on</param>
        /// <param name="changePercentage">Callback to report progress from 0 to 1.</param>
        /// <param name="token">Cancellation token.</param>
        private static void AddNodesAfterDate
              (string repositoryPath,
               DateTimeOffset since,
               string[] directories,
               string[] extensions,
               string[] branches,
               Action<float> changePercentage,
               CancellationToken token)
        {
            Criteria criteria = new(since, directories, extensions, branches);
            Mailmap mailmap = Mailmap.Read(Path.Combine(repositoryPath, ".mailmap"));

            using Repository repository = new(repositoryPath);
            ICollection<KeyValuePair<string, Branch>> selected
                = SelectedBranches(repository, criteria);
            Debug.Log(Selection(selected, criteria));

            // Shared by all branches, because what a commit changes does not
            // depend on the branch it is reached from.
            Examination examined = new(repository, mailmap, criteria);

            int processed = 0;
            foreach (KeyValuePair<string, Branch> branch in selected)
            {
                token.ThrowIfCancellationRequested();

                IDictionary<string, Churn> churn
                    = ChurnOf(repository, branch.Value, examined, criteria, token);
                Debug.Log(Report(branch.Key, churn, criteria));

                foreach (KeyValuePair<string, Churn> file in churn)
                {
                    Assert.That(file.Value.Commits, Is.GreaterThan(0),
                                $"{file.Key} is reported without any commit.");
                    Assert.That(file.Value.Authors, Is.Not.Empty,
                                $"{file.Key} is reported without any author.");
                }
                processed++;
                // The cast is needed: both operands being integers, the division
                // would otherwise be an integer one and yield zero throughout.
                changePercentage?.Invoke((float)processed / selected.Count);
            }
            Debug.Log($"{examined.Count} distinct commits were examined for "
                      + $"{selected.Count} branches.\n");
        }

        /// <summary>
        /// The branches of <paramref name="repository"/> selected by
        /// <paramref name="criteria"/>, keyed by their name and ordered by it. A
        /// branch selected by more than one of those expressions occurs only
        /// once.
        /// </summary>
        /// <param name="repository">the repository whose branches are to be selected</param>
        /// <param name="criteria">states the expressions selecting the branches</param>
        /// <returns>the selected branches, keyed by their name</returns>
        private static ICollection<KeyValuePair<string, Branch>> SelectedBranches
              (Repository repository, Criteria criteria)
        {
            // A symbolic reference, remotes/origin/HEAD in particular, only
            // points at another branch. Reporting on it would repeat that
            // branch under a second name.
            IDictionary<string, Branch> candidates
                = repository.Branches
                            .Where(branch => !NameOf(branch).EndsWith("/HEAD", StringComparison.Ordinal))
                            .ToDictionary(NameOf, branch => branch);

            SortedDictionary<string, Branch> result = new(StringComparer.Ordinal);
            foreach (string pattern in criteria.Branches)
            {
                Regex expression = Expression(pattern);
                ICollection<string> selected
                    = candidates.Keys.Where(name => expression.IsMatch(name)).ToList();
                Assert.That(selected, Is.Not.Empty,
                            $"No branch of {repository.Info.Path} is selected by {pattern}.");
                foreach (string name in selected)
                {
                    result[name] = candidates[name];
                }
            }
            return result;
        }

        /// <summary>
        /// The announcement of the branches <paramref name="selected"/> to be
        /// walked. Emitted before the first walk, so that an expression in
        /// <paramref name="criteria"/> selecting far more branches than intended
        /// shows at once rather than only once all of them have been walked.
        /// </summary>
        /// <param name="selected">the branches selected, keyed by their name</param>
        /// <param name="criteria">states the expressions the branches were selected by</param>
        /// <returns>the announcement</returns>
        private static string Selection(ICollection<KeyValuePair<string, Branch>> selected,
                                        Criteria criteria)
        {
            StringBuilder result = new();
            result.AppendLine($"Reporting on {selected.Count} branches, selected by "
                              + string.Join(", ", criteria.Branches) + ":");
            foreach (KeyValuePair<string, Branch> branch in selected)
            {
                result.AppendLine($"  {branch.Key}");
            }
            return result.ToString();
        }

        /// <summary>
        /// The name of <paramref name="branch"/> as <c>git branch -a</c> prints
        /// it, which is its canonical name without the leading <c>refs/</c>.
        /// </summary>
        /// <param name="branch">the branch whose name is asked for</param>
        /// <returns>the name of the branch</returns>
        private static string NameOf(Branch branch)
        {
            const string prefix = "refs/";
            return branch.CanonicalName.StartsWith(prefix, StringComparison.Ordinal)
                   ? branch.CanonicalName.Substring(prefix.Length)
                   : branch.CanonicalName;
        }

        /// <summary>
        /// <paramref name="pattern"/> as a regular expression matching a branch
        /// name as a whole. The anchors keep an expression from selecting a
        /// branch whose name merely contains what was asked for; the group
        /// around <paramref name="pattern"/> keeps them from binding to only
        /// the first and the last alternative of an alternation.
        /// </summary>
        /// <param name="pattern">the regular expression as stated in <see cref="branches"/></param>
        /// <returns>the anchored regular expression</returns>
        private static Regex Expression(string pattern)
        {
            try
            {
                return new Regex($"^(?:{pattern})$", RegexOptions.CultureInvariant);
            }
            catch (ArgumentException exception)
            {
                Assert.Fail($"{pattern} is not a regular expression: {exception.Message}");
                throw; // Never reached; Assert.Fail does not return.
            }
        }

        /// <summary>
        /// The churn of every file in the scope of this test, accumulated over
        /// all commits reachable from <paramref name="branch"/>, keyed by the
        /// name the file carries at the tip of that branch.
        /// </summary>
        /// <param name="repository">the repository to be walked</param>
        /// <param name="branch">the branch whose commits are to be taken into account</param>
        /// <param name="examined">what is known of the commits examined so far; will be extended</param>
        /// <param name="criteria">states which files are reported on</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>the churn per file</returns>
        private static IDictionary<string, Churn> ChurnOf(Repository repository, Branch branch,
                                                          Examination examined, Criteria criteria,
                                                          CancellationToken token)
        {
            Dictionary<string, Churn> result = new();
            // Maps the former name of a renamed file onto the name that file
            // carries later in the history. Per branch, because it is the name
            // at the tip of this branch that a file is reported under, and two
            // branches may well have renamed one file differently.
            Dictionary<string, string> renamedTo = new();

            CommitFilter filter = new()
            {
                IncludeReachableFrom = branch,
                // Newest commit first, just as git log reports them. A rename is
                // thus seen before the commits preceding it, which still use the
                // former name of the renamed file.
                SortBy = CommitSortStrategies.Time
            };

            foreach (Commit commit in repository.Commits.QueryBy(filter))
            {
                // Checked per commit rather than per branch: walking one branch
                // of a repository the size of SEE is itself a matter of seconds.
                token.ThrowIfCancellationRequested();

                Examined examination = examined.Of(commit);
                if (examination.IsMerge)
                {
                    // A merge has no churn of its own, exactly as for --no-merges.
                    continue;
                }
                foreach (FileChange change in examination.Changes)
                {
                    string target = Follow(renamedTo, change.Path);
                    if (examination.Within)
                    {
                        Record(result, target, change, examination);
                    }
                    if (change.IsRename)
                    {
                        Note(renamedTo, change.OldPath, target);
                    }
                }
            }

            // A file renamed out of the scope of this test is dropped, because it
            // no longer is a file this test reports on.
            return result.Where(file => criteria.InScope(file.Key))
                         .ToDictionary(file => file.Key, file => file.Value);
        }

        /// <summary>
        /// Accounts in <paramref name="churn"/> for <paramref name="change"/>
        /// having been made to the file named <paramref name="path"/> by
        /// <paramref name="examination"/>'s commit.
        /// </summary>
        /// <param name="churn">the churn accumulated so far; will be extended</param>
        /// <param name="path">the name the changed file carries at the tip of the branch</param>
        /// <param name="change">the change to be accounted for</param>
        /// <param name="examination">what is known of the commit making the change</param>
        private static void Record(IDictionary<string, Churn> churn, string path, FileChange change,
                                   Examined examination)
        {
            if (!churn.TryGetValue(path, out Churn file))
            {
                file = new Churn();
                churn[path] = file;
            }
            // A binary file is reported with no added and no deleted line, hence
            // it contributes nothing but the commit and its author.
            file.Add(change.LinesAdded, change.LinesDeleted, examination.Sha, examination.Author);
        }

        /// <summary>
        /// Notes in <paramref name="renamedTo"/> that the file once named
        /// <paramref name="oldPath"/> is named <paramref name="target"/> later on.
        /// </summary>
        /// <param name="renamedTo">the renames noted so far; will be extended</param>
        /// <param name="oldPath">the former name of the file</param>
        /// <param name="target">the name the file carries later on</param>
        private static void Note(IDictionary<string, string> renamedTo, string oldPath, string target)
        {
            // The condition keeps a file renamed away and later renamed back from
            // becoming a cycle of length one.
            if (oldPath != target)
            {
                renamedTo[oldPath] = target;
            }
        }

        /// <summary>
        /// The name <paramref name="path"/> has after all renames noted in
        /// <paramref name="renamedTo"/> have been applied to it.
        /// </summary>
        /// <param name="renamedTo">the renames noted so far</param>
        /// <param name="path">the name to start from</param>
        /// <returns>the name at the end of the chain of renames</returns>
        private static string Follow(IDictionary<string, string> renamedTo, string path)
        {
            // The bound guards against a cycle; see the note in Note.
            for (int steps = 0;
                 steps < maximalRenameChain && renamedTo.TryGetValue(path, out string next);
                 steps++)
            {
                path = next;
            }
            return path;
        }

        /// <summary>
        /// The report on <paramref name="churn"/> as a table, one line per file,
        /// the file with the most added lines first.
        /// </summary>
        /// <param name="branchName">the branch the report is on</param>
        /// <param name="churn">the churn to be reported</param>
        /// <param name="criteria">states the period reported on</param>
        /// <returns>the report</returns>
        private static string Report(string branchName, IDictionary<string, Churn> churn,
                                     Criteria criteria)
        {
            StringBuilder result = new();
            // The separators are quoted, because an unquoted '-' and ':' stand
            // for the date and the time separator of the current culture.
            result.AppendLine($"===== branch: {branchName} (author date >= "
                              + criteria.Since.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'sszzz")
                              + ") =====");
            if (churn.Count == 0)
            {
                result.AppendLine("no commit in the period reported on");
                return result.ToString();
            }

            int width = Math.Max("file".Length, churn.Keys.Max(path => path.Length));
            result.AppendLine($"{"added",8}  {"deleted",8}  {"commits",8}  {"file".PadRight(width)}  authors");
            foreach (KeyValuePair<string, Churn> file
                     in churn.OrderByDescending(file => file.Value.LinesAdded)
                             .ThenBy(file => file.Key, StringComparer.Ordinal))
            {
                result.AppendLine($"{file.Value.LinesAdded,8}  {file.Value.LinesDeleted,8}  "
                                  + $"{file.Value.Commits,8}  {file.Key.PadRight(width)}  "
                                  + string.Join(", ", file.Value.Authors));
            }
            return result.ToString();
        }

        /// <summary>
        /// What is reported on: which commits, which files, which branches.
        /// Handed to everything needing to know, so that these four values are
        /// stated in one place only.
        /// </summary>
        private class Criteria
        {
            /// <summary>
            /// The beginning of the period reported on; commits authored at this
            /// very instant are still taken into account.
            /// </summary>
            internal DateTimeOffset Since { get; }

            /// <summary>
            /// The directories, relative to the root of the repository and
            /// separated by <c>/</c>, whose files are reported on. Nested
            /// directories are included. Doubles as the pathspec narrowing every
            /// comparison of a commit against its parent.
            /// </summary>
            internal IReadOnlyList<string> Directories { get; }

            /// <summary>
            /// The extensions, leading dot included, a file must have to be
            /// reported on.
            /// </summary>
            internal IReadOnlyList<string> Extensions { get; }

            /// <summary>
            /// The regular expressions selecting the branches reported on.
            /// </summary>
            internal IReadOnlyList<string> Branches { get; }

            /// <summary>
            /// Constructor setting all properties from the parameters of the
            /// same name.
            /// </summary>
            /// <param name="since">the beginning of the period reported on</param>
            /// <param name="directories">the directories whose files are reported on</param>
            /// <param name="extensions">the extensions a file must have to be reported on</param>
            /// <param name="branches">the expressions selecting the branches reported on</param>
            internal Criteria(DateTimeOffset since, IReadOnlyList<string> directories,
                              IReadOnlyList<string> extensions, IReadOnlyList<string> branches)
            {
                Since = since;
                Directories = directories;
                Extensions = extensions;
                Branches = branches;
            }

            /// <summary>
            /// Whether <paramref name="path"/> denotes a file reported on, that
            /// is, one located in one of the <see cref="Directories"/> and having
            /// one of the <see cref="Extensions"/>.
            /// </summary>
            /// <param name="path">the path to be checked, relative to the root of
            /// the repository and separated by <c>/</c>; may be null</param>
            /// <returns>true if and only if the file is reported on</returns>
            internal bool InScope(string path)
            {
                return !string.IsNullOrEmpty(path)
                    && Extensions.Any(extension => path.EndsWith(extension, StringComparison.Ordinal))
                    && Directories.Any(directory =>
                                       path.StartsWith(directory + "/", StringComparison.Ordinal));
            }
        }

        /// <summary>
        /// One change a commit makes to one file in the scope of this test.
        /// </summary>
        private class FileChange
        {
            /// <summary>
            /// The name of the file after the change.
            /// </summary>
            internal string Path { get; }

            /// <summary>
            /// The name of the file before the change. Meaningful only where
            /// <see cref="IsRename"/> holds.
            /// </summary>
            internal string OldPath { get; }

            /// <summary>
            /// Whether the change renames the file.
            /// </summary>
            internal bool IsRename { get; }

            /// <summary>
            /// The number of lines the change adds.
            /// </summary>
            internal int LinesAdded { get; }

            /// <summary>
            /// The number of lines the change deletes.
            /// </summary>
            internal int LinesDeleted { get; }

            /// <summary>
            /// Constructor setting all properties from the parameters of the
            /// same name.
            /// </summary>
            /// <param name="path">the name of the file after the change</param>
            /// <param name="oldPath">the name of the file before the change</param>
            /// <param name="isRename">whether the change renames the file</param>
            /// <param name="linesAdded">the number of lines the change adds</param>
            /// <param name="linesDeleted">the number of lines the change deletes</param>
            internal FileChange(string path, string oldPath, bool isRename,
                                int linesAdded, int linesDeleted)
            {
                Path = path;
                OldPath = oldPath;
                IsRename = isRename;
                LinesAdded = linesAdded;
                LinesDeleted = linesDeleted;
            }
        }

        /// <summary>
        /// Everything this test needs to know about a single commit.
        /// </summary>
        private class Examined
        {
            /// <summary>
            /// Whether the commit is a merge.
            /// </summary>
            internal bool IsMerge { get; }

            /// <summary>
            /// Whether the commit was authored within the period reported on.
            /// </summary>
            internal bool Within { get; }

            /// <summary>
            /// The SHA of the commit.
            /// </summary>
            internal string Sha { get; }

            /// <summary>
            /// The canonical name of the author of the commit. Determined only
            /// where <see cref="Within"/> holds, because it is needed nowhere
            /// else.
            /// </summary>
            internal string Author { get; }

            /// <summary>
            /// The changes the commit makes to the files in the scope of this
            /// test, relative to its first parent. Only the renames among them
            /// where <see cref="Within"/> does not hold; see
            /// <see cref="Examination.Of"/>.
            /// </summary>
            internal IList<FileChange> Changes { get; }

            /// <summary>
            /// Constructor setting all properties from the parameters of the
            /// same name.
            /// </summary>
            /// <param name="isMerge">whether the commit is a merge</param>
            /// <param name="within">whether the commit falls in the period reported on</param>
            /// <param name="sha">the SHA of the commit</param>
            /// <param name="author">the canonical name of the author of the commit</param>
            /// <param name="changes">the changes the commit makes</param>
            internal Examined(bool isMerge, bool within, string sha, string author,
                              IList<FileChange> changes)
            {
                IsMerge = isMerge;
                Within = within;
                Sha = sha;
                Author = author;
                Changes = changes;
            }
        }

        /// <summary>
        /// What has been found out about the commits examined so far, so that a
        /// commit is examined only once however many of the selected branches it
        /// is reachable from.
        /// </summary>
        /// <remarks>
        /// This is where the time goes. What a commit changes relative to its
        /// first parent does not depend on the branch it is reached from, yet
        /// the branches of a repository share the greater part of their history:
        /// in SEE, 91 branches reach 15117 distinct commits between them but
        /// 1088641 times in total, so without this the same comparison is made
        /// some 72 times over on average.
        /// </remarks>
        private class Examination
        {
            /// <summary>
            /// The repository whose commits are examined.
            /// </summary>
            private readonly Repository repository;

            /// <summary>
            /// Used to map an author onto their canonical name.
            /// </summary>
            private readonly Mailmap mailmap;

            /// <summary>
            /// States the period and the files reported on.
            /// </summary>
            private readonly Criteria criteria;

            /// <summary>
            /// The options of every comparison made here.
            /// </summary>
            private readonly CompareOptions compareOptions = new()
            {
                Algorithm = DiffAlgorithm.Myers,
                Similarity = new SimilarityOptions
                {
                    RenameDetectionMode = RenameDetectionMode.Renames,
                    RenameThreshold = renameThreshold
                }
            };

            /// <summary>
            /// The commits examined so far, keyed by their identifier.
            /// </summary>
            private readonly IDictionary<ObjectId, Examined> examined
                = new Dictionary<ObjectId, Examined>();

            /// <summary>
            /// Constructor setting all fields from the parameters of the same
            /// name.
            /// </summary>
            /// <param name="repository">the repository whose commits are examined</param>
            /// <param name="mailmap">used to map an author onto their canonical name</param>
            /// <param name="criteria">states the period and the files reported on</param>
            internal Examination(Repository repository, Mailmap mailmap, Criteria criteria)
            {
                this.repository = repository;
                this.mailmap = mailmap;
                this.criteria = criteria;
            }

            /// <summary>
            /// The number of commits examined so far.
            /// </summary>
            internal int Count => examined.Count;

            /// <summary>
            /// What is to be known about <paramref name="commit"/>, examining it
            /// unless that has been done already.
            /// </summary>
            /// <param name="commit">the commit to be examined</param>
            /// <returns>what is known about the commit</returns>
            internal Examined Of(Commit commit)
            {
                if (!examined.TryGetValue(commit.Id, out Examined result))
                {
                    result = Examine(commit);
                    examined[commit.Id] = result;
                }
                return result;
            }

            /// <summary>
            /// What is to be known about <paramref name="commit"/>, found out by
            /// comparing it against its first parent.
            /// </summary>
            /// <param name="commit">the commit to be examined</param>
            /// <returns>what is known about the commit</returns>
            private Examined Examine(Commit commit)
            {
                List<FileChange> changes = new();
                if (commit.Parents.Skip(1).Any())
                {
                    return new Examined(true, false, null, null, changes);
                }
                // A commit without any parent is the initial one; it is compared
                // against the empty tree, which a null tree denotes.
                LibGit2Sharp.Tree parent = commit.Parents.FirstOrDefault()?.Tree;
                Signature author = commit.Author;
                bool within = author.When >= criteria.Since;

                if (within)
                {
                    using Patch patch
                        = repository.Diff.Compare<Patch>(parent, commit.Tree,
                                                         criteria.Directories, compareOptions);
                    foreach (PatchEntryChanges change in patch)
                    {
                        if (criteria.InScope(change.Path) || criteria.InScope(change.OldPath))
                        {
                            changes.Add(new FileChange(change.Path, change.OldPath,
                                                       change.Status == ChangeKind.Renamed,
                                                       change.LinesAdded, change.LinesDeleted));
                        }
                    }
                }
                else
                {
                    // The churn of a commit out of the period reported on is
                    // never needed, its renames however are: leaving them out
                    // would break the chain of names and split a file over two
                    // reported entries. A tree comparison yields them and is much
                    // cheaper than the line counts a patch would have to produce.
                    using TreeChanges treeChanges
                        = repository.Diff.Compare<TreeChanges>(parent, commit.Tree,
                                                               criteria.Directories, compareOptions);
                    foreach (TreeEntryChanges change in treeChanges)
                    {
                        if (change.Status == ChangeKind.Renamed
                            && (criteria.InScope(change.Path) || criteria.InScope(change.OldPath)))
                        {
                            changes.Add(new FileChange(change.Path, change.OldPath, true, 0, 0));
                        }
                    }
                }
                return new Examined(false, within, commit.Sha,
                                    within ? mailmap.NameOf(author) : null, changes);
            }
        }

        /// <summary>
        /// The churn of a single file, accumulated over the commits touching it.
        /// </summary>
        private class Churn
        {
            /// <summary>
            /// The number of lines added to the file.
            /// </summary>
            internal int LinesAdded { get; private set; }

            /// <summary>
            /// The number of lines deleted from the file.
            /// </summary>
            internal int LinesDeleted { get; private set; }

            /// <summary>
            /// The commits touching the file, identified by their SHA. A set,
            /// because a commit renaming a file may report it more than once.
            /// </summary>
            private readonly ISet<string> commits = new HashSet<string>();

            /// <summary>
            /// The authors of those commits, in the order they first occur in and
            /// without repetition.
            /// </summary>
            private readonly IList<string> authors = new List<string>();

            /// <summary>
            /// The number of commits touching the file.
            /// </summary>
            internal int Commits => commits.Count;

            /// <summary>
            /// The authors of those commits, in the order they first occur in.
            /// </summary>
            internal IEnumerable<string> Authors => authors;

            /// <summary>
            /// Accounts for one change of the file.
            /// </summary>
            /// <param name="linesAdded">the number of lines the change adds</param>
            /// <param name="linesDeleted">the number of lines the change deletes</param>
            /// <param name="sha">the SHA of the commit the change belongs to</param>
            /// <param name="author">the author of that commit</param>
            internal void Add(int linesAdded, int linesDeleted, string sha, string author)
            {
                LinesAdded += linesAdded;
                LinesDeleted += linesDeleted;
                commits.Add(sha);
                if (!authors.Contains(author))
                {
                    authors.Add(author);
                }
            }
        }

        /// <summary>
        /// The mapping of a .mailmap file, reduced to what is needed here: the
        /// canonical name of an author. LibGit2Sharp 0.29 exposes nothing of
        /// libgit2's own mailmap support, so the file is read here.
        /// </summary>
        /// <remarks>
        /// The forms git defines are all accepted. Of those, only the ones
        /// naming a canonical name have an effect here, because only names are
        /// reported; <c>&lt;proper@email&gt; &lt;commit@email&gt;</c>, which maps
        /// an address onto another address, leaves the name as it is.
        /// </remarks>
        private class Mailmap
        {
            /// <summary>
            /// Maps the address recorded in a commit onto the canonical name of
            /// its author.
            /// </summary>
            private readonly IDictionary<string, string> byEmail
                = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// Maps the name and the address recorded in a commit, in the form
            /// produced by <see cref="Key"/>, onto the canonical name of its
            /// author. Takes precedence over <see cref="byEmail"/>.
            /// </summary>
            private readonly IDictionary<string, string> byNameAndEmail
                = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// The mapping given by the .mailmap file at <paramref name="path"/>.
            /// If there is no such file, the result maps nothing, which leaves
            /// every author named as their commits name them.
            /// </summary>
            /// <param name="path">the path of the .mailmap file</param>
            /// <returns>the mapping</returns>
            internal static Mailmap Read(string path)
            {
                Mailmap result = new();
                if (!File.Exists(path))
                {
                    return result;
                }
                foreach (string line in File.ReadAllLines(path))
                {
                    result.Add(line);
                }
                return result;
            }

            /// <summary>
            /// Adds the entry <paramref name="line"/> states, if it states one.
            /// Comments and blank lines are ignored.
            /// </summary>
            /// <param name="line">the line of a .mailmap file to be added</param>
            private void Add(string line)
            {
                int comment = line.IndexOf('#');
                string entry = (comment >= 0 ? line.Substring(0, comment) : line).Trim();
                if (entry.Length == 0)
                {
                    return;
                }

                // An entry consists of up to two addresses in angle brackets,
                // each preceded by a name that may be empty.
                List<string> names = new();
                List<string> emails = new();
                int cursor = 0;
                while (true)
                {
                    int open = entry.IndexOf('<', cursor);
                    if (open < 0)
                    {
                        break;
                    }
                    int close = entry.IndexOf('>', open + 1);
                    if (close < 0)
                    {
                        break;
                    }
                    names.Add(entry.Substring(cursor, open - cursor).Trim());
                    emails.Add(entry.Substring(open + 1, close - open - 1).Trim());
                    cursor = close + 1;
                }

                if (emails.Count == 1 && names[0].Length > 0)
                {
                    // "Proper Name <proper@email>".
                    byEmail[emails[0]] = names[0];
                }
                else if (emails.Count == 2 && names[0].Length > 0)
                {
                    if (names[1].Length > 0)
                    {
                        // "Proper Name <proper@email> Commit Name <commit@email>".
                        byNameAndEmail[Key(names[1], emails[1])] = names[0];
                    }
                    else
                    {
                        // "Proper Name <proper@email> <commit@email>".
                        byEmail[emails[1]] = names[0];
                    }
                }
            }

            /// <summary>
            /// The canonical name of <paramref name="author"/>, or the name their
            /// commit records if this mapping has nothing to say about them.
            /// </summary>
            /// <param name="author">the author whose name is asked for</param>
            /// <returns>the canonical name</returns>
            internal string NameOf(Signature author)
            {
                if (byNameAndEmail.TryGetValue(Key(author.Name, author.Email), out string byBoth))
                {
                    return byBoth;
                }
                if (byEmail.TryGetValue(author.Email, out string byAddress))
                {
                    return byAddress;
                }
                return author.Name;
            }

            /// <summary>
            /// The key under which <see cref="byNameAndEmail"/> stores an entry
            /// for <paramref name="name"/> and <paramref name="email"/>.
            /// </summary>
            /// <param name="name">the name recorded in a commit</param>
            /// <param name="email">the address recorded in a commit</param>
            /// <returns>the key</returns>
            private static string Key(string name, string email)
            {
                return $"{name} <{email}>";
            }
        }
    }
}
