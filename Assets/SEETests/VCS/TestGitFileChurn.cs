using LibGit2Sharp;
using Microsoft.Extensions.FileSystemGlobbing;
using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.GraphProviders.VCS;
using SEE.Utils;
using SEE.Utils.Paths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace SEE.VCS
{
    /// <summary>
    /// Reports, for every file having a certain extension, located in one of a
    /// set of directories and still present in the repository, the number of
    /// lines added, the number of lines deleted, the number of commits, and the
    /// list of authors, taking only commits into account that were authored at
    /// or after a chosen date. The commits are those reachable from any of a set
    /// of selected branches, each counted once however many of them reach it.
    /// </summary>
    /// <remarks>
    /// This is the LibGit2Sharp counterpart of the following query, aggregated
    /// per file:
    ///
    /// <code>
    /// git log &lt;branch&gt;... --no-merges --full-history --find-renames=50% --numstat -z
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
    /// name the file carries last. Where two branches renamed one file
    /// differently, the more recent rename decides; the names given up along the
    /// way are reported beside the file.
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
        /// The share of the churn of a file its core developers are to answer
        /// for, the truck factor being how many of them that takes. The value
        /// <see cref="GitGraphGenerator"/> uses.
        /// </summary>
        private const float truckFactorCoreDevRatio = 0.8f;

        /// <summary>
        /// The configurations <see cref="TestChurn"/> is run on, one
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
        /// test runner is <c>SEE.VCS.Filter</c> over and over.
        /// </remarks>
        /// <returns>One test case per configuration.</returns>
        private static IEnumerable<TestCaseData> Configurations()
        {
            // The beginning of the period to be reported on; commits authored at
            // this very instant are still taken into account. The UTC offset is
            // stated explicitly, because the instant denoted would otherwise depend
            // on the time zone of the machine running this test and would, around a
            // switch to or from daylight saving time, be ambiguous.
            DateTimeOffset since = new(2025, 1, 1, 0, 0, 0, TimeSpan.FromHours(1));

            Filter filter = new
                (// A file is reported on if it matches at least one inclusive
                 // pattern and no exclusive one. Add a pattern with the value
                 // false to leave something out, say { "**/*.Designer.cs", false }.
                 globbing: new Globbing() { { "**/*.cs", true } },
                 // The directories, relative to the root of the repository, whose
                 // files are reported on. Nested directories are included.
                 repositoryPaths: new string[] { "Assets/SEE", "Assets/SEETests" },
                 // Regular expressions selecting the branches to be reported on,
                 // their commits taken together. Each is matched against
                 // the friendly name of a branch as a whole, so "master" is the
                 // local master alone and "origin/.*" every remote-tracking
                 // branch of origin. To catch both masters, write ".*master".
                 //
                 // Every expression must select at least one branch; one
                 // selecting none fails this test rather than silently narrowing
                 // the report. A branch selected by several of them is still
                 // reported on only once.
                 //
                 // The commits reachable from the branches are walked once
                 // over, so selecting many branches costs far less than as many
                 // walks; what they share is visited a single time.
                 branches: new string[] { "master", "origin/.*" });

            GitRepository repositoryConfiguration = new(new DataPath(DataPath.ProjectFolder()), filter);

            yield return new TestCaseData(since, repositoryConfiguration)
                .SetName("SEE sources and tests");
        }

        /// <summary>
        /// Emits the report announced for <see cref="TestGitFileChurn"/> to the
        /// console and holds it against the baseline of the previous run.
        /// </summary>
        /// <param name="since">The beginning of the period to be reported on.</param>
        /// <param name="repositoryConfiguration">The repository configuration based on which the
        /// report is derived.</param>
        [TestCaseSource(nameof(Configurations))]
        public void TestChurn(DateTimeOffset since, GitRepository repositoryConfiguration)
        {
            Outcome outcome = AddNodesAfterDate(repositoryConfiguration, since,
                                                simplifyGraph: true, computeCoFileChanges: true,
                                                default, default);
            Baseline.CompareOrWrite(TestContext.CurrentContext.Test.Name, outcome);
        }

        /// <summary>
        /// Emits the report on the repository named by
        /// <paramref name="repositoryConfiguration"/> to the console.
        ///
        /// <paramref name="since"/> is the beginning of the period to be reported on;
        /// commits authored at this very instant are still taken into account. The UTC offset is
        /// stated explicitly, because the instant denoted would otherwise depend
        /// on the time zone of the machine running this test and would, around a
        /// switch to or from daylight saving time, be ambiguous.
        ///
        /// Which branches and which files are reported on is stated by
        /// <see cref="GitRepository.VCSFilter"/> of
        /// <paramref name="repositoryConfiguration"/>, as follows.
        ///
        /// <see cref="Filter.RepositoryPaths"/> states the
        /// directories, relative to the root of the repository and separated by <c>/</c>,
        /// whose files are to be reported on; nested directories are included. If it is
        /// null or empty, the whole repository is reported on.
        ///
        /// <see cref="Filter.Globbing"/> states which of the files therein are reported
        /// on: a file must match at least one inclusive pattern and no exclusive one.
        ///
        /// <see cref="Filter.Branches"/> is a set of regular expressions selecting
        /// the branches whose commits are to be taken into account, together.
        /// Each is matched against the friendly name of a branch as a whole, so
        /// <c>master</c> is the local master alone and <c>origin/.*</c> every
        /// remote-tracking branch of origin; <c>.*master</c> catches both masters.
        /// If the set is null or empty, every branch is reported on.
        ///
        /// Every expression must select at least one branch; one selecting none
        /// fails this test rather than silently narrowing the report. A branch
        /// selected by several expressions is still reported on only once.
        ///
        /// The commits reachable from the selected branches are walked once over
        /// and each is counted once, however many of the branches reach it.
        /// </summary>
        /// <param name="repositoryConfiguration">The repository configuration based on which the
        /// report is derived.</param>
        /// <param name="since">The beginning of the period to be reported on.</param>
        /// <param name="simplifyGraph">Whether a chain of directory nodes holding nothing but
        /// one another is to be collapsed into its innermost one.</param>
        /// <param name="computeCoFileChanges">Whether to note which files a commit changed
        /// together. Mind that this is far from free: for SEE it comes to some hundred
        /// thousand pairs, and the node of one file may name several hundred others.</param>
        /// <param name="changePercentage">Callback to report progress from 0 to 1.</param>
        /// <param name="token">Cancellation token.</param>
        private static Outcome AddNodesAfterDate
              (GitRepository repositoryConfiguration,
               DateTimeOffset since,
               bool simplifyGraph,
               bool computeCoFileChanges,
               Action<float> changePercentage,
               CancellationToken token)
        {
            string repositoryPath = repositoryConfiguration.RepositoryPath.Path;
            Criteria criteria = new(since, repositoryConfiguration.VCSFilter);
            Mailmap mailmap = Mailmap.Read(Path.Combine(repositoryPath, ".mailmap"));

            // Two handles on the one repository for the time being: the session
            // decides which branches are relevant and which files exist, the
            // repository is walked directly because the session offers no
            // comparison detecting renames. The second handle goes once it does.
            using GitRepositorySession session = repositoryConfiguration.OpenGitSession();
            using Repository repository = new(repositoryPath);

            ICollection<Branch> selected = SelectedBranches(session, criteria);
            Debug.Log(Selection(selected, criteria));
            changePercentage?.Invoke(0.1f);

            // The files present at the tip of at least one relevant branch, as
            // the session reports them, which is to say already passing the
            // globbing and the repository paths of the filter. A file deleted
            // meanwhile is not among them and is therefore left out of the
            // report, however much churn its history holds.
            HashSet<string> present = session.AllFiles(token);
            changePercentage?.Invoke(0.3f);

            // Maps the name a file carries at the end onto the names it carried
            // before, gathered while the renames are followed.
            IDictionary<string, ISet<string>> formerNames = new Dictionary<string, ISet<string>>();
            IDictionary<string, Churn> churn
                = ChurnOf(repository, selected, criteria, mailmap, formerNames,
                          computeCoFileChanges, out int walked, token);
            changePercentage?.Invoke(0.9f);

            int withChurn = churn.Count;
            churn = churn.Where(file => present.Contains(file.Key))
                         .ToDictionary(file => file.Key, file => file.Value);
            Assert.That(churn, Is.Not.Empty,
                        $"Not one file of the repository is reported on, though {withChurn} have "
                        + $"churn in the period and {present.Count} are present at a tip. Either "
                        + "nothing was changed in the period, or the paths the session reports "
                        + "differ in form from the paths a comparison of two commits yields.");

            Graph graph = GraphOf(churn, formerNames, repositoryPath, session, simplifyGraph);
            string report = Summary(graph) + Report(churn, formerNames, criteria, selected);
            Debug.Log(report);
            foreach (KeyValuePair<string, Churn> file in churn)
            {
                Assert.That(file.Value.Commits, Is.GreaterThan(0),
                            $"{file.Key} is reported without any commit.");
                Assert.That(file.Value.Authors, Is.Not.Empty,
                            $"{file.Key} is reported without any author.");
                // What the authors of a file churned between them is what the
                // file was churned, every change being made by one of them.
                Assert.That(file.Value.AuthorsChurn.Values.Sum(),
                            Is.EqualTo(file.Value.LinesAdded + file.Value.LinesDeleted),
                            $"The churn of {file.Key} is not what its authors churned.");
                // Its core developers are some of its authors, and where it has
                // any they cannot be none: somebody churned what was churned.
                Assert.That(TruckFactorOf(file.Value.AuthorsChurn),
                            Is.InRange(1, file.Value.AuthorsChurn.Count),
                            $"The truck factor of {file.Key} is not a number of its authors.");
            }
            changePercentage?.Invoke(1f);

            // Counted rather than read off the account of the graph, where the
            // edges are grouped by type: a type no edge has is not a group, so
            // that account would fall silent just when the number is nought.
            int coChanges
                = graph.Edges().Count(edge => edge.Type == DataModel.DG.VCS.CoChangeType);
            Debug.Log($"{churn.Count} files are reported on: of the {withChurn} with churn in the "
                      + $"period, that many are still present at the tip of one of the "
                      + $"{selected.Count} branches, which hold {present.Count} files in all. "
                      + $"{walked} commits were walked. "
                      + $"{coChanges} edges of type {DataModel.DG.VCS.CoChangeType} join two "
                      + "files a commit changed together.\n");
            SortedDictionary<string, string> tips = new(StringComparer.Ordinal);
            foreach (Branch branch in selected)
            {
                tips[branch.FriendlyName] = branch.Tip.Sha;
            }
            return new Outcome(report, repositoryPath, criteria, walked, tips);
        }

        /// <summary>
        /// The branches <paramref name="session"/> holds to be relevant, keyed by
        /// their name and ordered by it. A branch selected by more than one of
        /// the expressions of the filter occurs only once.
        /// </summary>
        /// <param name="session">The session whose relevant branches are asked for.</param>
        /// <param name="criteria">States the expressions selecting the branches. Its filter
        /// is the one <paramref name="session"/> was opened with.</param>
        /// <returns>The selected branches, ordered by their name.</returns>
        private static ICollection<Branch> SelectedBranches
              (GitRepositorySession session, Criteria criteria)
        {
            SortedDictionary<string, Branch> byName = new(StringComparer.Ordinal);
            // A symbolic reference, origin/HEAD in particular, only points at
            // another branch. Taking it along would walk that branch twice,
            // which costs time even though the union absorbs the repetition.
            foreach (Branch branch in session.RelevantBranches()
                                             .Where(branch => !branch.FriendlyName
                                                     .EndsWith("/HEAD", StringComparison.Ordinal)))
            {
                byName[branch.FriendlyName] = branch;
            }

            // Reported per expression rather than for the set as a whole, so
            // that a mistyped one is named. Holding an expression against the
            // branches already selected suffices: a branch matching it would
            // have been selected by it.
            foreach (string pattern in criteria.Filter.Branches ?? Enumerable.Empty<string>())
            {
                Filter alone = new(branches: new string[] { pattern });
                Assert.That(byName.Values.Any(alone.Matches), Is.True,
                            $"No branch is selected by {pattern}.");
            }
            return byName.Values;
        }

        /// <summary>
        /// The announcement of the branches <paramref name="selected"/> to be
        /// walked. Emitted before the first walk, so that an expression in
        /// <paramref name="criteria"/> selecting far more branches than intended
        /// shows at once rather than only once all of them have been walked.
        /// </summary>
        /// <param name="selected">The branches selected.</param>
        /// <param name="criteria">States the expressions the branches were selected by.</param>
        /// <returns>The announcement.</returns>
        private static string Selection(ICollection<Branch> selected, Criteria criteria)
        {
            StringBuilder result = new();
            result.AppendLine($"Reporting on the union of {selected.Count} branches, selected by "
                              + string.Join(", ", criteria.Filter.Branches ?? Enumerable.Empty<string>())
                              + ":");
            foreach (Branch branch in selected)
            {
                result.AppendLine($"  {branch.FriendlyName}");
            }
            return result.ToString();
        }

        /// <summary>
        /// The churn of every file in the scope of this test, accumulated over
        /// the union of the commits reachable from <paramref name="selected"/>,
        /// keyed by the name the file carries at the end.
        /// </summary>
        /// <remarks>
        /// One walk over the union, not one per branch: a commit reachable from
        /// several of the branches is one commit and is counted once. Summing
        /// what each branch yields separately would count the history they share
        /// once per branch, which for a file of SEE is a factor of some thirty.
        /// </remarks>
        /// <param name="repository">The repository to be walked.</param>
        /// <param name="selected">The branches whose commits are to be taken into account.</param>
        /// <param name="criteria">States from when and which files are reported on.</param>
        /// <param name="mailmap">Used to map an author onto their canonical name.</param>
        /// <param name="formerNames">The names a renamed file carried before, keyed by the name
        /// it carries at the end; will be extended.</param>
        /// <param name="computeCoFileChanges">Whether to note which files a commit changed
        /// together. Off, the co-changes of a file stay empty.</param>
        /// <param name="walked">How many commits the walk visited.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The churn per file.</returns>
        private static IDictionary<string, Churn> ChurnOf
              (Repository repository,
               ICollection<Branch> selected,
               Criteria criteria,
               Mailmap mailmap,
               IDictionary<string, ISet<string>> formerNames,
               bool computeCoFileChanges,
               out int walked,
               CancellationToken token)
        {
            Dictionary<string, Churn> result = new();
            // Maps the former name of a renamed file onto the name that file
            // carries later in the history. One map, the walk being one: where
            // two branches renamed a file differently, the rename seen first,
            // and thus the most recent one, decides.
            Dictionary<string, string> renamedTo = new();

            CompareOptions compareOptions = new()
            {
                Algorithm = DiffAlgorithm.Myers,
                Similarity = new SimilarityOptions
                {
                    RenameDetectionMode = RenameDetectionMode.Renames,
                    RenameThreshold = renameThreshold
                }
            };
            CommitFilter filter = new()
            {
                IncludeReachableFrom = selected,
                // Newest commit first, just as git log reports them. A rename is
                // thus seen before the commits preceding it, which still use the
                // former name of the renamed file.
                SortBy = CommitSortStrategies.Time
            };

            walked = 0;
            foreach (Commit commit in repository.Commits.QueryBy(filter))
            {
                token.ThrowIfCancellationRequested();
                walked++;

                if (commit.Parents.Skip(1).Any())
                {
                    // A merge has no churn of its own, exactly as for --no-merges.
                    continue;
                }
                // A commit without any parent is the initial one; it is compared
                // against the empty tree, which a null tree denotes.
                LibGit2Sharp.Tree parent = commit.Parents.FirstOrDefault()?.Tree;
                Signature author = commit.Author;
                bool within = author.When >= criteria.Since;

                if (within)
                {
                    using Patch patch = Compare<Patch>(repository, criteria, parent, commit.Tree,
                                                       compareOptions);
                    // The files this one commit changed, under the names they
                    // carry at the end. Gathered as they are recorded, because
                    // which files changed together is known only once they all
                    // are. A set, two entries of one patch being able to lead to
                    // one name where a rename is followed.
                    ISet<string> touched = new HashSet<string>();
                    foreach (PatchEntryChanges change in patch)
                    {
                        if (!criteria.InScope(change.Path) && !criteria.InScope(change.OldPath))
                        {
                            continue;
                        }
                        string target = Follow(renamedTo, change.Path);
                        Record(result, target, change.LinesAdded, change.LinesDeleted,
                               commit.Sha, mailmap.AuthorOf(author));
                        touched.Add(target);
                        if (change.Status == ChangeKind.Renamed)
                        {
                            Note(renamedTo, formerNames, change.OldPath, target);
                        }
                    }
                    if (computeCoFileChanges && touched.Count > 1)
                    {
                        foreach (string path in touched)
                        {
                            Churn file = result[path];
                            foreach (string other in touched)
                            {
                                if (other != path)
                                {
                                    file.AlsoChanged(other);
                                }
                            }
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
                        = Compare<TreeChanges>(repository, criteria, parent, commit.Tree,
                                               compareOptions);
                    foreach (TreeEntryChanges change in treeChanges)
                    {
                        if (change.Status == ChangeKind.Renamed
                            && (criteria.InScope(change.Path) || criteria.InScope(change.OldPath)))
                        {
                            Note(renamedTo, formerNames, change.OldPath,
                                 Follow(renamedTo, change.Path));
                        }
                    }
                }
            }

            // A file renamed out of the scope of this test is dropped, because it
            // no longer is a file this test reports on.
            return result.Where(file => criteria.InScope(file.Key))
                         .ToDictionary(file => file.Key, file => file.Value);
        }

        /// <summary>
        /// The comparison of <paramref name="newTree"/> against
        /// <paramref name="oldTree"/>, narrowed to the directories reported on
        /// where there are any.
        /// </summary>
        /// <typeparam name="T">What the comparison is to yield.</typeparam>
        /// <param name="repository">The repository the trees belong to.</param>
        /// <param name="criteria">States the directories reported on.</param>
        /// <param name="oldTree">The tree compared against; null denotes the empty tree.</param>
        /// <param name="newTree">The tree to be compared.</param>
        /// <param name="compareOptions">The options of the comparison.</param>
        /// <returns>The comparison.</returns>
        private static T Compare<T>(Repository repository, Criteria criteria,
                                    LibGit2Sharp.Tree oldTree, LibGit2Sharp.Tree newTree,
                                    CompareOptions compareOptions)
            where T : class, IDiffResult
        {
            // The overload taking no pathspec is used where there is none,
            // rather than handing it a null, which libgit2 is not documented
            // to accept.
            return criteria.Pathspec == null
                   ? repository.Diff.Compare<T>(oldTree, newTree, compareOptions)
                   : repository.Diff.Compare<T>(oldTree, newTree, criteria.Pathspec,
                                                compareOptions);
        }

        /// <summary>
        /// Accounts in <paramref name="churn"/> for a change of the stated size
        /// having been made to the file named <paramref name="path"/> by the
        /// commit with the stated SHA and author.
        /// </summary>
        /// <param name="churn">The churn accumulated so far; will be extended.</param>
        /// <param name="path">The name the changed file carries at the end.</param>
        /// <param name="linesAdded">The number of lines the change adds.</param>
        /// <param name="linesDeleted">The number of lines the change deletes.</param>
        /// <param name="sha">The SHA of the commit making the change.</param>
        /// <param name="author">The canonical identity of the author of that commit.</param>
        private static void Record(IDictionary<string, Churn> churn, string path, int linesAdded,
                                   int linesDeleted, string sha, FileAuthor author)
        {
            if (!churn.TryGetValue(path, out Churn file))
            {
                file = new Churn();
                churn[path] = file;
            }
            // A binary file is reported with no added and no deleted line, hence
            // it contributes nothing but the commit and its author.
            file.Add(linesAdded, linesDeleted, sha, author);
        }

        /// <summary>
        /// Notes in <paramref name="renamedTo"/> that the file once named
        /// <paramref name="oldPath"/> is named <paramref name="target"/> later on.
        /// </summary>
        /// <param name="renamedTo">The renames noted so far; will be extended.</param>
        /// <param name="formerNames">The former names noted so far; will be extended.</param>
        /// <param name="oldPath">The former name of the file.</param>
        /// <param name="target">The name the file carries later on.</param>
        private static void Note(IDictionary<string, string> renamedTo,
                                 IDictionary<string, ISet<string>> formerNames,
                                 string oldPath, string target)
        {
            // The condition keeps a file renamed away and later renamed back from
            // becoming a cycle of length one.
            if (oldPath != target)
            {
                // The first rename seen for a name wins, it being the most
                // recent one: the walk yields the newest commit first. A later,
                // that is older, rename of the same name is a different step of
                // the same chain only if it leads elsewhere, in which case two
                // branches disagree and the more recent one is to decide.
                if (!renamedTo.ContainsKey(oldPath))
                {
                    renamedTo[oldPath] = target;
                }
                if (!formerNames.TryGetValue(target, out ISet<string> names))
                {
                    names = new SortedSet<string>(StringComparer.Ordinal);
                    formerNames[target] = names;
                }
                names.Add(oldPath);
            }
        }

        /// <summary>
        /// The name <paramref name="path"/> has after all renames noted in
        /// <paramref name="renamedTo"/> have been applied to it.
        /// </summary>
        /// <param name="renamedTo">The renames noted so far.</param>
        /// <param name="path">The name to start from.</param>
        /// <returns>The name at the end of the chain of renames.</returns>
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
        /// <param name="churn">The churn to be reported.</param>
        /// <param name="formerNames">The names a renamed file carried before, keyed by the name
        /// it carries at the end.</param>
        /// <param name="criteria">States the period reported on.</param>
        /// <param name="selected">The branches whose union was walked.</param>
        /// <returns>The report.</returns>
        private static string Report(IDictionary<string, Churn> churn,
                                     IDictionary<string, ISet<string>> formerNames,
                                     Criteria criteria, ICollection<Branch> selected)
        {
            StringBuilder result = new();
            // The separators are quoted, because an unquoted '-' and ':' stand
            // for the date and the time separator of the current culture.
            // The number of files stands in the heading so that a report holding
            // a different number of them differs from its baseline in the first
            // line, rather than only somewhere down the table.
            result.AppendLine($"===== {churn.Count} files, {selected.Count} branches, "
                              + "author date >= "
                              + criteria.Since.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'sszzz")
                              + " =====");
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
                                  + string.Join(", ", file.Value.Authors.Select(a => a.Name)));
                // On a line of its own: a file that has been renamed is the
                // exception, and a name is as long as the one above it.
                if (formerNames.TryGetValue(file.Key, out ISet<string> names))
                {
                    result.AppendLine($"{string.Empty,30}formerly {string.Join(", ", names)}");
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// What one run produced: the report, and the circumstances it was
        /// produced under.
        /// </summary>
        private class Outcome
        {
            /// <summary>
            /// The report. This alone is held against
            /// the baseline.
            /// </summary>
            internal string Report { get; }

            /// <summary>
            /// The path of the repository reported on.
            /// </summary>
            internal string RepositoryPath { get; }

            /// <summary>
            /// What was reported on.
            /// </summary>
            internal Criteria Criteria { get; }

            /// <summary>
            /// How many distinct commits were examined.
            /// </summary>
            internal int Examined { get; }

            /// <summary>
            /// The commit at the tip of each branch reported on, keyed by the
            /// name of the branch. Recorded beside the report and not compared
            /// with it, so that a report differing from the baseline merely
            /// because the repository has gained commits can be told apart from
            /// one differing because the code producing it has changed.
            /// </summary>
            internal IDictionary<string, string> Tips { get; }

            /// <summary>
            /// Constructor setting all properties from the parameters of the
            /// same name.
            /// </summary>
            /// <param name="report">The report.</param>
            /// <param name="repositoryPath">The path of the repository reported on.</param>
            /// <param name="criteria">What was reported on.</param>
            /// <param name="examined">How many distinct commits were examined.</param>
            /// <param name="tips">The commit at the tip of each branch reported on.</param>
            internal Outcome(string report, string repositoryPath, Criteria criteria, int examined,
                             IDictionary<string, string> tips)
            {
                Report = report;
                RepositoryPath = repositoryPath;
                Criteria = criteria;
                Examined = examined;
                Tips = tips;
            }
        }

        /// <summary>
        /// The report of the previous run of a test case, kept so that the next
        /// run can be held against it. Its purpose is to show that a change to
        /// the code producing the report leaves the report itself alone.
        /// </summary>
        /// <remarks>
        /// Mind that the report depends on the history of the repository as much
        /// as on the code deriving it. Every commit added to a branch reported on
        /// may legitimately change it, so a difference is not by itself a defect.
        /// The tips recorded beside the report say which of the two it is: same
        /// tips and a differing report means the code changed its answer.
        ///
        /// A baseline is kept outside the repository, under the temporary
        /// directory, as the other tests here keep what they write. To accept the
        /// report of the latest run as the one to come, delete the file; the next
        /// run writes it afresh.
        /// </remarks>
        private static class Baseline
        {
            /// <summary>
            /// Separates the circumstances of a run from its report. Only what
            /// follows it is compared.
            /// </summary>
            private const string marker = "----- report -----";

            /// <summary>
            /// The directory the baselines are kept in.
            /// </summary>
            internal static string Directory
                => Path.Combine(Path.GetTempPath(), "SEE", nameof(TestGitFileChurn));

            /// <summary>
            /// Holds <paramref name="outcome"/> against the baseline of the test
            /// case named <paramref name="testCase"/>, failing the test where the
            /// report differs. Where there is no baseline yet, writes one.
            /// </summary>
            /// <param name="testCase">The name of the test case the outcome belongs to.</param>
            /// <param name="outcome">What the run produced.</param>
            internal static void CompareOrWrite(string testCase, Outcome outcome)
            {
                string file = Path.Combine(Directory, FilenameOf(testCase) + ".txt");
                if (!File.Exists(file))
                {
                    System.IO.Directory.CreateDirectory(Directory);
                    File.WriteAllText(file, Content(outcome));
                    Debug.Log($"No baseline yet; wrote one to {file}.\n");
                    return;
                }

                string[] stored = Lines(Payload(File.ReadAllText(file)));
                string[] produced = Lines(outcome.Report);
                int line = FirstDifference(stored, produced);
                if (line < 0)
                {
                    Debug.Log($"The report is the one in {file}.\n");
                    return;
                }
                Assert.Fail(Difference(file, stored, produced, line, outcome));
            }

            /// <summary>
            /// What is written to a baseline for <paramref name="outcome"/>: its
            /// circumstances, the <see cref="marker"/>, then its report.
            /// </summary>
            /// <param name="outcome">What the run produced.</param>
            /// <returns>The content of the baseline.</returns>
            private static string Content(Outcome outcome)
            {
                StringBuilder result = new();
                result.AppendLine($"repository: {outcome.RepositoryPath}");
                result.AppendLine($"since:      {outcome.Criteria.Since:o}");
                result.AppendLine($"paths:      "
                                  + string.Join(", ", outcome.Criteria.Pathspec
                                                      ?? Enumerable.Empty<string>()));
                result.AppendLine($"branches:   "
                                  + string.Join(", ", outcome.Criteria.Filter.Branches
                                                      ?? Enumerable.Empty<string>()));
                result.AppendLine($"examined:   {outcome.Examined}");
                foreach (KeyValuePair<string, string> tip in outcome.Tips)
                {
                    result.AppendLine($"tip {tip.Key} {tip.Value}");
                }
                result.AppendLine(marker);
                result.Append(outcome.Report);
                return result.ToString();
            }

            /// <summary>
            /// The part of <paramref name="content"/> that is compared, that is,
            /// what follows the <see cref="marker"/>.
            /// </summary>
            /// <param name="content">The content of a baseline.</param>
            /// <returns>The report therein.</returns>
            private static string Payload(string content)
            {
                int at = content.IndexOf(marker, StringComparison.Ordinal);
                return at < 0 ? content : content.Substring(at + marker.Length);
            }

            /// <summary>
            /// The part of <paramref name="content"/> that is not compared, that
            /// is, what precedes the <see cref="marker"/>.
            /// </summary>
            /// <param name="content">The content of a baseline.</param>
            /// <returns>The circumstances recorded therein.</returns>
            private static string Head(string content)
            {
                int at = content.IndexOf(marker, StringComparison.Ordinal);
                return at < 0 ? string.Empty : content.Substring(0, at);
            }

            /// <summary>
            /// The commit at the tip of each branch as the baseline in
            /// <paramref name="content"/> records it.
            /// </summary>
            /// <param name="content">The content of a baseline.</param>
            /// <returns>The tips, keyed by the name of the branch.</returns>
            private static IDictionary<string, string> Tips(string content)
            {
                Dictionary<string, string> result = new();
                foreach (string line in Lines(Head(content)))
                {
                    string[] parts = line.Split(' ');
                    if (parts.Length == 3 && parts[0] == "tip")
                    {
                        result[parts[1]] = parts[2];
                    }
                }
                return result;
            }

            /// <summary>
            /// <paramref name="text"/> split into lines, whichever line endings
            /// it uses, with no empty line at its end.
            /// </summary>
            /// <param name="text">The text to be split.</param>
            /// <returns>The lines of the text.</returns>
            private static string[] Lines(string text)
            {
                return text.Replace("\r\n", "\n").Trim('\n').Split('\n');
            }

            /// <summary>
            /// The first index at which <paramref name="stored"/> and
            /// <paramref name="produced"/> differ, or -1 where they do not.
            /// </summary>
            /// <param name="stored">The lines of the baseline.</param>
            /// <param name="produced">The lines of the report just produced.</param>
            /// <returns>The index of the first difference, or -1.</returns>
            private static int FirstDifference(string[] stored, string[] produced)
            {
                for (int i = 0; i < Math.Min(stored.Length, produced.Length); i++)
                {
                    if (stored[i] != produced[i])
                    {
                        return i;
                    }
                }
                return stored.Length == produced.Length ? -1 : Math.Min(stored.Length,
                                                                        produced.Length);
            }

            /// <summary>
            /// The account of how the report just produced differs from its
            /// baseline, and of whether the repository has meanwhile moved on.
            /// </summary>
            /// <param name="file">The baseline the report is held against.</param>
            /// <param name="stored">The lines of the baseline.</param>
            /// <param name="produced">The lines of the report just produced.</param>
            /// <param name="line">The index of the first differing line.</param>
            /// <param name="outcome">What the run produced.</param>
            /// <returns>The account.</returns>
            private static string Difference(string file, string[] stored, string[] produced,
                                             int line, Outcome outcome)
            {
                StringBuilder result = new();
                result.AppendLine($"The report differs from the baseline in {file}.");
                result.AppendLine($"It has {produced.Length} lines where the baseline has "
                                  + $"{stored.Length}; the first of them to differ is line "
                                  + $"{line + 1}:");
                result.AppendLine($"  baseline: {At(stored, line)}");
                result.AppendLine($"  now:      {At(produced, line)}");

                IDictionary<string, string> before = Tips(File.ReadAllText(file));
                ICollection<string> moved
                    = outcome.Tips.Where(tip => !before.TryGetValue(tip.Key, out string sha)
                                                || sha != tip.Value)
                                  .Select(tip => tip.Key).ToList();
                if (moved.Count == 0)
                {
                    result.AppendLine("No branch has moved since, so the difference is not one "
                                      + "of history: the code deriving the report has changed "
                                      + "its answer.");
                }
                else
                {
                    result.AppendLine($"{moved.Count} of {outcome.Tips.Count} branches have moved "
                                      + "since the baseline was written, which may account for "
                                      + "the difference: "
                                      + string.Join(", ", moved.Take(5))
                                      + (moved.Count > 5 ? ", ..." : string.Empty));
                }
                result.AppendLine($"Delete {file} to accept the report of this run instead.");
                return result.ToString();
            }

            /// <summary>
            /// The line of <paramref name="lines"/> at <paramref name="index"/>,
            /// or a note that there is none.
            /// </summary>
            /// <param name="lines">The lines to index into.</param>
            /// <param name="index">The index of the line asked for.</param>
            /// <returns>The line or a note.</returns>
            private static string At(string[] lines, int index)
            {
                return index < lines.Length ? lines[index] : "(no such line)";
            }

            /// <summary>
            /// <paramref name="testCase"/> with everything a filename may not
            /// contain replaced by an underscore.
            /// </summary>
            /// <param name="testCase">The name of a test case.</param>
            /// <returns>A name fit for a file.</returns>
            private static string FilenameOf(string testCase)
            {
                return string.Join("_", testCase.Split(Path.GetInvalidFileNameChars()));
            }
        }

        /// <summary>
        /// The account of <paramref name="graph"/> that is held against the
        /// baseline: how many nodes of each type it holds, and what its metrics
        /// come to over all of them.
        /// </summary>
        /// <remarks>
        /// The graph itself is far too large to keep in a baseline, yet a change
        /// to the way it is built should not pass unnoticed. These few numbers
        /// move whenever the nodes or their attributes do.
        /// </remarks>
        /// <param name="graph">The graph to be accounted for.</param>
        /// <returns>The account.</returns>
        private static string Summary(Graph graph)
        {
            StringBuilder result = new();
            result.AppendLine("===== graph =====");
            foreach (IGrouping<string, Node> ofType in graph.Nodes()
                                                            .GroupBy(node => node.Type)
                                                            .OrderBy(group => group.Key,
                                                                     StringComparer.Ordinal))
            {
                result.AppendLine($"{ofType.Count(),8}  nodes of type {ofType.Key}");
            }
            foreach (string metric in new string[] { DataModel.DG.VCS.NumberOfDevelopers,
                                                     DataModel.DG.VCS.NumberOfCommits,
                                                     DataModel.DG.VCS.LinesAdded,
                                                     DataModel.DG.VCS.LinesRemoved,
                                                     DataModel.DG.VCS.Churn,
                                                     DataModel.DG.VCS.TruckNumber,
                                                     Metrics.LOC,
                                                     Metrics.Comments,
                                                     Metrics.NumberOfTokens,
                                                     Metrics.McCabe })
            {
                int sum = graph.Nodes().Sum(node => node.TryGetInt(metric, out int value) ? value : 0);
                result.AppendLine($"{sum,8}  {metric} over all nodes");
            }
            // One attribute per author of a file, so neither their number nor
            // their sum is had by asking for a name known beforehand.
            string perAuthor = DataModel.DG.VCS.Churn + ":";
            IEnumerable<KeyValuePair<string, int>> churnOfAuthors
                = graph.Nodes().SelectMany(node => node.IntAttributes)
                       .Where(attribute => attribute.Key.StartsWith(perAuthor, StringComparison.Ordinal));
            result.AppendLine($"{churnOfAuthors.Count(),8}  attributes {perAuthor}<author>");
            result.AppendLine($"{churnOfAuthors.Sum(attribute => attribute.Value),8}  "
                              + $"{perAuthor}<author> over all nodes");
            // Counted, not summed: the Halstead metrics are fractions, and a
            // fraction written out is a poor thing to hold a baseline against.
            result.AppendLine($"{graph.Nodes().Count(HasHalstead),8}  nodes with "
                              + $"{Halstead.Prefix}* attributes");
            foreach (IGrouping<string, Edge> ofType in graph.Edges()
                                                            .GroupBy(edge => edge.Type)
                                                            .OrderBy(group => group.Key,
                                                                     StringComparer.Ordinal))
            {
                result.AppendLine($"{ofType.Count(),8}  edges of type {ofType.Key}");
            }
            int together
                = graph.Edges()
                       .Sum(edge => edge.TryGetInt(DataModel.DG.VCS.ChangedTogether, out int value)
                                    ? value : 0);
            result.AppendLine($"{together,8}  {DataModel.DG.VCS.ChangedTogether} over all edges");
            return result.ToString();
        }

        /// <summary>
        /// Whether <paramref name="node"/> carries any of the Halstead metrics.
        /// </summary>
        /// <param name="node">The node to be checked.</param>
        /// <returns>True if and only if it carries one.</returns>
        private static bool HasHalstead(Node node)
        {
            return node.FloatAttributes.Keys.Any(name => name.StartsWith(Halstead.Prefix,
                                                                         StringComparison.Ordinal));
        }

        /// <summary>
        /// The metrics of <paramref name="churn"/> as a graph: one node per file,
        /// nested in nodes standing for the directories holding them, under a
        /// single root standing for the repository, with an edge between every
        /// two files a commit changed together.
        /// </summary>
        /// <remarks>
        /// The attributes set here are those <see cref="GitGraphGenerator"/> sets
        /// from a <see cref="GitFileMetrics"/>, all of them now.
        ///
        /// Two things go beyond what <see cref="GitGraphGenerator"/> does. It
        /// gathers the files changed together with a file but puts them nowhere;
        /// here they are edges, a relation between two files being what an edge
        /// is for. And it has no notion of the names a file carried before.
        ///
        /// The metrics of the code itself are gathered by
        /// <see cref="GitGraphGenerator.AddCodeMetrics"/>, called here rather
        /// than written afresh. Mind that it reads and lexes the content of
        /// every file, which for a repository the size of SEE is the greater
        /// part of the time this takes.
        ///
        /// A chain of directory nodes holding nothing but one another is
        /// collapsed into its innermost one where <paramref name="simplifyGraph"/>
        /// says so, again by <see cref="GitGraphGenerator"/>. Only directory
        /// nodes go, so the edges between files outlive it.
        /// </remarks>
        /// <param name="churn">The churn per file the graph is to hold.</param>
        /// <param name="formerNames">The names a renamed file carried before, keyed by the name
        /// it carries at the end.</param>
        /// <param name="repositoryPath">The path of the repository the graph stands for.</param>
        /// <param name="session">Used to read the content of a file, which the metrics of its
        /// code are gathered from.</param>
        /// <param name="simplifyGraph">Whether a chain of directory nodes holding nothing but
        /// one another is to be collapsed into its innermost one.</param>
        /// <returns>The graph.</returns>
        private static Graph GraphOf(IDictionary<string, Churn> churn,
                                     IDictionary<string, ISet<string>> formerNames,
                                     string repositoryPath, GitRepositorySession session,
                                     bool simplifyGraph)
        {
            string repositoryName = Filenames.InnermostDirectoryName(repositoryPath);
            Graph result = new(repositoryPath, repositoryName);
            // The node standing for each file, so that the edges between files
            // changed together can be drawn once every node exists.
            IDictionary<string, Node> nodes = new Dictionary<string, Node>();

            foreach (KeyValuePair<string, Churn> file in churn)
            {
                GitFileMetrics metrics = MetricsOf(file.Value);
                Node node = GraphUtils.GetOrAddFileNode(result, file.Key);
                node.SetInt(DataModel.DG.VCS.NumberOfDevelopers, metrics.Authors.Count);
                node.SetInt(DataModel.DG.VCS.NumberOfCommits, metrics.NumberOfCommits);
                node.SetInt(DataModel.DG.VCS.LinesAdded, metrics.LinesAdded);
                node.SetInt(DataModel.DG.VCS.LinesRemoved, metrics.LinesRemoved);
                node.SetInt(DataModel.DG.VCS.Churn, metrics.Churn);
                node.SetInt(DataModel.DG.VCS.TruckNumber, metrics.TruckFactor);
                if (metrics.Authors.Any())
                {
                    node.SetString(DataModel.DG.VCS.AuthorsAttributeName,
                                   string.Join(',', metrics.Authors));
                }
                // One attribute per author, named for them, as
                // GitGraphGenerator names them.
                foreach (KeyValuePair<FileAuthor, int> authorChurn in metrics.AuthorsChurn)
                {
                    node.SetInt(DataModel.DG.VCS.Churn + ":" + authorChurn.Key, authorChurn.Value);
                }
                nodes[file.Key] = node;
                // Joined into one attribute, as the authors above are. Should
                // one attribute per former name be wanted instead, this is the
                // one place to say so.
                if (formerNames.TryGetValue(file.Key, out ISet<string> names))
                {
                    node.SetString(DataModel.DG.VCS.FormerNames, string.Join(',', names));
                }
            }

            // The metrics of the code itself, gathered by GitGraphGenerator
            // from the content of each file, rather than from its history.
            GitGraphGenerator.AddCodeMetrics(result, session);
            AddCoChanges(result, churn, nodes);
            result.AddSingleRoot(out Node _, repositoryName, DataModel.DG.VCS.RepositoryType);
            // After the root, which the collapsing starts from, and again by
            // GitGraphGenerator rather than afresh. Only directory nodes are
            // affected, so the edges drawn above outlive it.
            GitGraphGenerator.Simplify(result, simplifyGraph);
            result.FinalizeNodeHierarchy();
            return result;
        }

        /// <summary>
        /// Draws an edge of type <see cref="DataModel.DG.VCS.CoChangeType"/> between every two
        /// files of <paramref name="churn"/> that a commit changed together,
        /// counting on it how often that happened.
        /// </summary>
        /// <remarks>
        /// One edge for the two of them, not one each way. Being changed by the
        /// same commit is symmetric, so the two counts are equal and the second
        /// edge would say nothing the first does not. Which of the two files an
        /// edge leaves is therefore arbitrary, and settled by their names, so
        /// that one and the same repository always yields the same graph.
        /// </remarks>
        /// <param name="graph">The graph the edges are drawn in.</param>
        /// <param name="churn">The churn per file, stating which files changed together.</param>
        /// <param name="nodes">The node standing for each of those files.</param>
        private static void AddCoChanges(Graph graph, IDictionary<string, Churn> churn,
                                         IDictionary<string, Node> nodes)
        {
            foreach (KeyValuePair<string, Churn> file in churn)
            {
                foreach (KeyValuePair<string, int> coChange in file.Value.CoChanges)
                {
                    // Only one of the two orders is drawn, and only where the
                    // other file is reported on at all: one changed together
                    // with this one but deleted since has no node to join.
                    if (string.CompareOrdinal(file.Key, coChange.Key) >= 0
                        || !churn.TryGetValue(coChange.Key, out Churn other))
                    {
                        continue;
                    }
                    other.CoChanges.TryGetValue(file.Key, out int back);
                    Assert.That(back, Is.EqualTo(coChange.Value),
                                $"{file.Key} and {coChange.Key} were changed together "
                                + "a different number of times as seen from either of them.");
                    // The names differ, the comparison above having let this
                    // pair through. Two names standing for one node would still
                    // make a loop of the edge, which is what this rules out.
                    Assert.That(nodes[coChange.Key], Is.Not.SameAs(nodes[file.Key]),
                                $"{file.Key} and {coChange.Key} are one and the same node, "
                                + "so an edge between them would be a self loop.");
                    Edge edge = graph.AddEdge(nodes[file.Key], nodes[coChange.Key],
                                              DataModel.DG.VCS.CoChangeType);
                    edge.SetInt(DataModel.DG.VCS.ChangedTogether, coChange.Value);
                }
            }
        }

        /// <summary>
        /// How many of the authors of a file answer for
        /// <see cref="truckFactorCoreDevRatio"/> of its churn between them, the
        /// heaviest contributors counted first.
        /// </summary>
        /// <remarks>
        /// The heuristic of Yamashita et al. for the set of core developers,
        /// as <see cref="GitGraphGenerator"/> computes it.
        ///
        /// Which authors those are is not settled where two churned equally,
        /// yet how many it takes is, the sum reached after so many of them
        /// being the same however equals are ordered.
        ///
        /// Source: https://doi.org/10.1145/2804360.2804366
        /// </remarks>
        /// <param name="authorsChurn">What each author churned of the file.</param>
        /// <returns>The truck factor of the file.</returns>
        private static int TruckFactorOf(IDictionary<FileAuthor, int> authorsChurn)
        {
            if (authorsChurn.Count == 0)
            {
                return 0;
            }
            int total = authorsChurn.Values.Sum();
            if (total == 0)
            {
                // Nobody churned anything, the file having been renamed and not
                // otherwise touched in the period. One is what
                // GitGraphGenerator arrives at here, by way of a ratio of zero
                // over zero, and so this says as much without the arithmetic.
                return 1;
            }

            float cumulative = 0;
            int coreDevs = 0;
            foreach (int churn in authorsChurn.Values.OrderByDescending(value => value))
            {
                if (cumulative > truckFactorCoreDevRatio)
                {
                    break;
                }
                cumulative += (float)churn / total;
                coreDevs++;
            }
            return coreDevs;
        }

        /// <summary>
        /// The churn <paramref name="churn"/> holds, as the
        /// <see cref="GitFileMetrics"/> a graph node carries.
        /// </summary>
        /// <param name="churn">The churn of one file.</param>
        /// <returns>The metrics of that file.</returns>
        private static GitFileMetrics MetricsOf(Churn churn)
        {
            GitFileMetrics result = new(churn.Commits, churn.Authors.ToHashSet(),
                                        churn.LinesAdded, churn.LinesDeleted);
            result.AuthorsChurn = churn.AuthorsChurn;
            result.FilesChangesTogether = churn.CoChanges;
            result.TruckFactor = TruckFactorOf(result.AuthorsChurn);
            return result;
        }

        /// <summary>
        /// What is reported on: from when, which files, which branches. Handed
        /// to everything needing to know, so that it is stated in one place only.
        /// </summary>
        private class Criteria
        {
            /// <summary>
            /// The beginning of the period reported on; commits authored at this
            /// very instant are still taken into account.
            /// </summary>
            internal DateTimeOffset Since { get; }

            /// <summary>
            /// States which branches and which files are reported on.
            /// </summary>
            internal Filter Filter { get; }

            /// <summary>
            /// The pathspec narrowing every comparison of a commit against its
            /// parent to the directories reported on, or null where the whole
            /// repository is reported on.
            /// </summary>
            internal IEnumerable<string> Pathspec { get; }

            /// <summary>
            /// Decides the globbing of <see cref="Filter"/>. Held here because
            /// <see cref="Filter.Matcher"/> builds a new one on every access,
            /// and this one is consulted for every file of every commit.
            /// </summary>
            /// <remarks>Can be null, in which case every file passes.</remarks>
            private readonly Matcher matcher;

            /// <summary>
            /// Constructor setting all properties from the parameters of the
            /// same name.
            /// </summary>
            /// <param name="since">The beginning of the period reported on.</param>
            /// <param name="filter">States which branches and which files are reported on.</param>
            internal Criteria(DateTimeOffset since, Filter filter)
            {
                Since = since;
                Filter = filter;
                matcher = filter.Matcher;
                Pathspec = filter.RepositoryPaths == null || filter.RepositoryPaths.Length == 0
                           ? null : filter.RepositoryPaths;
            }

            /// <summary>
            /// Whether <paramref name="path"/> denotes a file reported on, that
            /// is, one lying in one of the directories of <see cref="Filter"/>
            /// and passing its globbing.
            /// </summary>
            /// <param name="path">The path to be checked, relative to the root of
            /// the repository and separated by <c>/</c>; may be null.</param>
            /// <returns>True if and only if the file is reported on.</returns>
            internal bool InScope(string path)
            {
                return !string.IsNullOrEmpty(path)
                    && (Pathspec == null
                        || Pathspec.Any(directory =>
                                        path.StartsWith(directory + "/", StringComparison.Ordinal)))
                    && (matcher == null || matcher.Matches(path));
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
            /// without repetition. Identities rather than names, an author being
            /// a name and an address, which is what <see cref="GitFileMetrics"/>
            /// holds as well.
            /// </summary>
            private readonly IList<FileAuthor> authors = new List<FileAuthor>();

            /// <summary>
            /// The number of commits touching the file.
            /// </summary>
            internal int Commits => commits.Count;

            /// <summary>
            /// The authors of those commits, in the order they first occur in.
            /// </summary>
            internal IEnumerable<FileAuthor> Authors => authors;

            /// <summary>
            /// How often each other file was changed by the same commit as this
            /// one, keyed by the name that other file carries at the end.
            /// </summary>
            internal IDictionary<string, int> CoChanges { get; }
                = new Dictionary<string, int>();

            /// <summary>
            /// What each of those authors added to and deleted from the file,
            /// taken together. Summed over all authors, this is the churn of
            /// the file.
            /// </summary>
            internal IDictionary<FileAuthor, int> AuthorsChurn { get; }
                = new Dictionary<FileAuthor, int>();

            /// <summary>
            /// Accounts for one change of the file.
            /// </summary>
            /// <param name="linesAdded">The number of lines the change adds.</param>
            /// <param name="linesDeleted">The number of lines the change deletes.</param>
            /// <param name="sha">The SHA of the commit the change belongs to.</param>
            /// <param name="author">The author of that commit.</param>
            /// <summary>
            /// Notes that <paramref name="other"/> was changed by the same
            /// commit as this file.
            /// </summary>
            /// <param name="other">The name the other file carries at the end.</param>
            internal void AlsoChanged(string other)
            {
                CoChanges.TryGetValue(other, out int count);
                CoChanges[other] = count + 1;
            }

            internal void Add(int linesAdded, int linesDeleted, string sha, FileAuthor author)
            {
                LinesAdded += linesAdded;
                LinesDeleted += linesDeleted;
                AuthorsChurn.TryGetValue(author, out int churn);
                AuthorsChurn[author] = churn + linesAdded + linesDeleted;
                commits.Add(sha);
                if (!authors.Contains(author))
                {
                    authors.Add(author);
                }
            }
        }
    }
}
