using LibGit2Sharp;
using Microsoft.Extensions.FileSystemGlobbing;
using SEE.DataModel.DG;
using SEE.Utils;
using SEE.VCS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace SEE.GraphProviders.VCS
{
    /// <summary>
    /// Builds a graph of the files having a certain extension and located in one
    /// of a set of directories: a node for every one of them that survived,
    /// carrying the number of lines added, the number of lines deleted, the
    /// number of commits and the authors, with an edge between every two files
    /// a commit changed together.
    ///
    /// Which commits are counted is asked for in one of two ways.
    /// <see cref="AddNodesAfterDate"/> takes a date and a set of branches, and
    /// counts what was authored since, over the union of what those branches
    /// reach, each commit counted once however many reach it.
    /// <see cref="AddNodesForCommit"/> takes two commits and counts the range
    /// between them. They differ in that and in nothing else.
    ///
    /// A file that survived but was not touched has a node all the same, its
    /// every count standing at nought.
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
    /// A rename is followed where the name given up is gone for good, hence all
    /// churn of such a file is aggregated under the name it carries last, and the
    /// names given up along the way are noted beside it. Where two branches
    /// renamed one file differently, the more recent rename decides.
    ///
    /// Where the old name is still present at the tip of some branch, though, the
    /// rename is not followed: a rename is a fact about the branch it was made on,
    /// and on the others that file is alive and still being worked on under the
    /// name it always had.
    ///
    /// LibGit2Sharp 0.29 has no equivalent of git's %aN, that is, it does not
    /// read .mailmap. <see cref="Mailmap"/> makes up for that, so that the
    /// authors reported here are the same ones git would report.
    /// </remarks>
    internal static class ChurnGraphGenerator
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
        /// Adds to <paramref name="graph"/> a node for every file of the
        /// repository named by <paramref name="repositoryConfiguration"/> that
        /// survived in one of the selected branches, carrying what the history
        /// since <paramref name="startDate"/> holds against it, and an edge
        /// between every two files a commit changed together.
        ///
        /// A date denotes the instant its day begins at in UTC, so that the
        /// boundary does not depend on the time zone of the machine running
        /// this and is not, around a switch to or from daylight saving time,
        /// ambiguous. A commit authored at that very instant is still taken
        /// into account.
        ///
        /// Which branches and which files are taken into account is stated by
        /// <see cref="GitRepository.VCSFilter"/> of
        /// <paramref name="repositoryConfiguration"/>, as follows.
        ///
        /// <see cref="Filter.RepositoryPaths"/> states the directories, relative
        /// to the root of the repository and separated by <c>/</c>, whose files
        /// are taken into account; nested directories are included. If it is
        /// null or empty, the whole repository is taken into account.
        ///
        /// <see cref="Filter.Globbing"/> states which of the files therein are
        /// taken into account: a file must match at least one inclusive pattern
        /// and no exclusive one.
        ///
        /// <see cref="Filter.Branches"/> is a set of regular expressions selecting
        /// the branches whose commits are to be taken into account, together.
        /// Each is matched against the friendly name of a branch as a whole, so
        /// <c>master</c> is the local master alone and <c>origin/.*</c> every
        /// remote-tracking branch of origin; <c>.*master</c> catches both masters.
        /// If the set is null or empty, every branch is taken into account.
        ///
        /// An expression selecting no branch at all is reported as a warning. A
        /// branch selected by several expressions is still walked only once.
        ///
        /// The commits reachable from the selected branches are walked once over
        /// and each is counted once, however many of the branches reach it.
        /// </summary>
        /// <param name="graph">The graph the nodes and edges are added to.</param>
        /// <param name="simplifyGraph">Whether a chain of directory nodes holding nothing but
        /// one another is to be collapsed into its innermost one.</param>
        /// <param name="repositoryConfiguration">The repository the graph is derived from,
        /// along with the filter stating which of its branches and files are taken into
        /// account.</param>
        /// <param name="repositoryName">The name of the root node standing for the
        /// repository.</param>
        /// <param name="startDate">The beginning of the period taken into account.</param>
        /// <param name="computeCoFileChanges">Whether to note which files a commit changed
        /// together. Mind that this is far from free: for SEE it comes to some hundred
        /// thousand pairs, and the node of one file may name several hundred others.</param>
        /// <param name="changePercentage">Callback to report progress from 0 to 1.</param>
        /// <param name="token">Cancellation token.</param>
        internal static void AddNodesAfterDate
              (Graph graph,
               bool simplifyGraph,
               GitRepository repositoryConfiguration,
               string repositoryName,
               DateTime startDate,
               bool computeCoFileChanges,
               Action<float> changePercentage,
               CancellationToken token)
        {
            string repositoryPath = repositoryConfiguration.RepositoryPath.Path;

            // Two handles on the one repository for the time being: the session
            // decides which branches are relevant and which files exist, the
            // repository is walked directly because the session offers no
            // comparison detecting renames. The second handle goes once it does.
            using GitRepositorySession session = repositoryConfiguration.OpenGitSession();
            using Repository repository = new(repositoryPath);

            Criteria criteria = new(new DateTimeOffset(startDate.Date, TimeSpan.Zero),
                                    repositoryConfiguration.VCSFilter,
                                    SelectedBranches(session, repositoryConfiguration.VCSFilter));
            changePercentage?.Invoke(0.1f);

            // The files present at the tip of at least one relevant branch, as
            // the session reports them, which is to say already passing the
            // globbing and the repository paths of the filter. A file deleted
            // meanwhile is not among them and is therefore left out of the
            // graph, however much churn its history holds.
            AddNodes(graph, criteria, session, repository, session.AllFiles(token),
                     repositoryPath, repositoryName, simplifyGraph, computeCoFileChanges,
                     changePercentage, token);
        }

        /// <summary>
        /// Adds to <paramref name="graph"/> a node for every file present at
        /// <paramref name="commitID"/>, carrying what the commits between
        /// <paramref name="baselineCommitID"/> and <paramref name="commitID"/>
        /// hold against it, and an edge between every two files one of those
        /// commits changed together.
        ///
        /// The commits taken into account are those reachable from
        /// <paramref name="commitID"/> and not from
        /// <paramref name="baselineCommitID"/>, so the baseline itself is left
        /// out and the named commit is taken in. Merges are passed over, having
        /// no churn of their own.
        ///
        /// Which files are taken into account is stated by
        /// <see cref="GitRepository.VCSFilter"/> of
        /// <paramref name="repositoryConfiguration"/>, as
        /// <see cref="AddNodesAfterDate"/> describes, except that
        /// <see cref="Filter.Branches"/> has no bearing here: the two commits
        /// settle what is walked.
        /// </summary>
        /// <param name="graph">The graph the nodes and edges are added to.</param>
        /// <param name="simplifyGraph">Whether a chain of directory nodes holding nothing but
        /// one another is to be collapsed into its innermost one.</param>
        /// <param name="repositoryConfiguration">The repository the graph is derived from,
        /// along with the filter stating which of its files are taken into account.</param>
        /// <param name="repositoryName">The name of the root node standing for the
        /// repository.</param>
        /// <param name="commitID">The commit the files are taken from and the changes are
        /// counted up to; it is itself taken into account.</param>
        /// <param name="baselineCommitID">The commit the changes are counted from; it is
        /// itself left out.</param>
        /// <param name="computeCoFileChanges">Whether to note which files a commit changed
        /// together.</param>
        /// <param name="changePercentage">Callback to report progress from 0 to 1.</param>
        /// <param name="token">Cancellation token.</param>
        internal static void AddNodesForCommit
              (Graph graph,
               bool simplifyGraph,
               GitRepository repositoryConfiguration,
               string repositoryName,
               string commitID,
               string baselineCommitID,
               bool computeCoFileChanges,
               Action<float> changePercentage,
               CancellationToken token)
        {
            string repositoryPath = repositoryConfiguration.RepositoryPath.Path;

            using GitRepositorySession session = repositoryConfiguration.OpenGitSession();
            using Repository repository = new(repositoryPath);

            // Held against the repository here rather than left to the walk,
            // where an unknown one surfaces as whatever libgit2 makes of it.
            Check(repository, commitID, nameof(commitID));
            Check(repository, baselineCommitID, nameof(baselineCommitID));

            graph.BasePath = repositoryPath;
            graph.Name = repositoryName;
            graph.SetCommitID(commitID);
            graph.SetRepositoryPath(repositoryPath);

            Criteria criteria = new(baselineCommitID, commitID, repositoryConfiguration.VCSFilter);
            changePercentage?.Invoke(0.1f);

            // The files present at the named commit, as the session reports
            // them, which is to say already passing the globbing and the
            // repository paths of the filter. A file deleted before it is not
            // among them and is therefore left out of the graph, however much
            // churn the range holds against it.
            AddNodes(graph, criteria, session, repository, session.AllFiles(commitID, token),
                     repositoryPath, repositoryName, simplifyGraph, computeCoFileChanges,
                     changePercentage, token);
        }

        /// <summary>
        /// Throws unless <paramref name="sha"/> names a commit of
        /// <paramref name="repository"/>.
        /// </summary>
        /// <param name="repository">The repository the commit is looked for in.</param>
        /// <param name="sha">The SHA naming it.</param>
        /// <param name="parameter">The name of the parameter <paramref name="sha"/> was
        /// passed as, to be named in the exception.</param>
        /// <exception cref="ArgumentException">Thrown where <paramref name="sha"/> is null,
        /// empty or names no commit of the repository.</exception>
        private static void Check(Repository repository, string sha, string parameter)
        {
            if (string.IsNullOrWhiteSpace(sha))
            {
                throw new ArgumentException("A commit must be named.", parameter);
            }
            if (repository.Lookup<Commit>(sha) == null)
            {
                throw new ArgumentException($"{sha} names no commit of the repository at "
                                            + $"{repository.Info.WorkingDirectory}.", parameter);
            }
        }

        /// <summary>
        /// Adds to <paramref name="graph"/> a node for every file of
        /// <paramref name="present"/>, carrying what the commits
        /// <paramref name="criteria"/> names hold against it, and an edge
        /// between every two files one of those commits changed together.
        /// </summary>
        /// <remarks>
        /// What the two entry points above have in common, which is everything
        /// but the commits walked and the files that survive them.
        /// </remarks>
        /// <param name="graph">The graph the nodes and edges are added to.</param>
        /// <param name="criteria">States which commits are walked, which of them count and
        /// which files are taken into account.</param>
        /// <param name="session">Used to read the content of a file, which the metrics of its
        /// code are gathered from.</param>
        /// <param name="repository">The repository to be walked.</param>
        /// <param name="present">The files that survived, which are those a node is made
        /// for.</param>
        /// <param name="repositoryPath">The path of the repository, which the mailmap is
        /// read from.</param>
        /// <param name="repositoryName">The name of the root node standing for the
        /// repository.</param>
        /// <param name="simplifyGraph">Whether a chain of directory nodes holding nothing but
        /// one another is to be collapsed into its innermost one.</param>
        /// <param name="computeCoFileChanges">Whether to note which files a commit changed
        /// together.</param>
        /// <param name="changePercentage">Callback to report progress from 0 to 1.</param>
        /// <param name="token">Cancellation token.</param>
        private static void AddNodes
              (Graph graph,
               Criteria criteria,
               GitRepositorySession session,
               Repository repository,
               HashSet<string> present,
               string repositoryPath,
               string repositoryName,
               bool simplifyGraph,
               bool computeCoFileChanges,
               Action<float> changePercentage,
               CancellationToken token)
        {
            Debug.Log(criteria.Walked + "\n");
            changePercentage?.Invoke(0.3f);
            if (present.Count == 0)
            {
                Debug.LogWarning("No files were matched.\n");
                changePercentage?.Invoke(1f);
                return;
            }

            Mailmap mailmap = Mailmap.Read(Path.Combine(repositoryPath, Mailmap.Filename));

            // Maps the name a file carries at the end onto the names it carried
            // before, gathered while the renames are followed.
            IDictionary<string, ISet<string>> formerNames = new Dictionary<string, ISet<string>>();
            IDictionary<string, Churn> churn
                = ChurnOf(repository, criteria, mailmap, formerNames,
                          present, computeCoFileChanges, out int walked, token);
            changePercentage?.Invoke(0.9f);

            int withChurn = churn.Count;
            churn = churn.Where(file => present.Contains(file.Key))
                         .ToDictionary(file => file.Key, file => file.Value);
            if (churn.Count == 0)
            {
                Debug.LogWarning(
                    $"Not one file of the repository is accounted for, though {withChurn} have "
                    + $"churn in the period and {present.Count} survived. Either nothing was "
                    + "changed in the period, or the paths the session reports differ in form "
                    + "from the paths a comparison of two commits yields.\n");
            }

            // A node for every file that survived, whether or not it was changed
            // in the period, as GitGraphGenerator does. One untouched carries the
            // metrics of its code and nothing else, every count of its history
            // standing at nought.
            IDictionary<string, Churn> all = new Dictionary<string, Churn>(churn);
            foreach (string path in present)
            {
                if (!all.ContainsKey(path))
                {
                    all[path] = new Churn();
                }
            }

            Fill(graph, all, formerNames, repositoryName, session, simplifyGraph);
            changePercentage?.Invoke(1f);

            // Counted rather than read off the graph by group, where a type no
            // edge has is not a group, so that the account would fall silent
            // just when the number is nought.
            int coChanges
                = graph.Edges().Count(edge => edge.Type == DataModel.DG.VCS.CoChangeType);
            Debug.Log($"{present.Count} files have a node, being what survived; "
                      + $"{churn.Count} of them were changed in the period, of {withChurn} "
                      + "changed in all, the rest having been deleted since. "
                      + $"{walked} commits were walked. "
                      + $"{coChanges} edges of type {DataModel.DG.VCS.CoChangeType} join two "
                      + "files a commit changed together.\n");
        }

        /// <summary>
        /// The branches <paramref name="session"/> holds to be relevant, keyed by
        /// their name and ordered by it. A branch selected by more than one of
        /// the expressions of the filter occurs only once.
        /// </summary>
        /// <param name="session">The session whose relevant branches are asked for.</param>
        /// <param name="filter">States the expressions selecting the branches. It is the one
        /// <paramref name="session"/> was opened with.</param>
        /// <returns>The selected branches, ordered by their name.</returns>
        private static ICollection<Branch> SelectedBranches
              (GitRepositorySession session, SEE.VCS.Filter filter)
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
            foreach (string pattern in filter.Branches ?? Enumerable.Empty<string>())
            {
                SEE.VCS.Filter alone = new(branches: new string[] { pattern });
                if (!byName.Values.Any(alone.Matches))
                {
                    Debug.LogWarning($"No branch is selected by {pattern}.\n");
                }
            }
            return byName.Values;
        }

        /// <summary>
        /// The churn of every file in scope, accumulated over the commits
        /// <paramref name="criteria"/> names, keyed by the name the file
        /// carries at the end.
        /// </summary>
        /// <remarks>
        /// One walk, however many branches are reported on: a commit reachable
        /// from several of them is one commit and is counted once. Summing what
        /// each branch yields separately would count the history they share once
        /// per branch, which for a file of SEE is a factor of some thirty.
        /// </remarks>
        /// <param name="repository">The repository to be walked.</param>
        /// <param name="criteria">States which commits are walked, which of them count and
        /// which files are taken into account.</param>
        /// <param name="mailmap">Used to map an author onto their canonical name.</param>
        /// <param name="formerNames">The names a renamed file carried before, keyed by the name
        /// it carries at the end; will be extended.</param>
        /// <param name="surviving">The files still present at the tip of one of the
        /// branches. A rename away from one of those is not followed.</param>
        /// <param name="computeCoFileChanges">Whether to note which files a commit changed
        /// together. Off, the co-changes of a file stay empty.</param>
        /// <param name="walked">How many commits the walk visited.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The churn per file.</returns>
        private static IDictionary<string, Churn> ChurnOf
              (Repository repository,
               Criteria criteria,
               Mailmap mailmap,
               IDictionary<string, ISet<string>> formerNames,
               ISet<string> surviving,
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
            walked = 0;
            foreach (Commit commit in repository.Commits.QueryBy(criteria.Commits))
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

                if (criteria.Counts(commit))
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
                            Note(renamedTo, formerNames, surviving, change.OldPath, target);
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
                    // The churn of a commit out of the period taken into account
                    // is never needed, its renames however are: leaving them out
                    // would break the chain of names and split a file over two
                    // nodes. A tree comparison yields them and is much cheaper
                    // than the line counts a patch would have to produce.
                    using TreeChanges treeChanges
                        = Compare<TreeChanges>(repository, criteria, parent, commit.Tree,
                                               compareOptions);
                    foreach (TreeEntryChanges change in treeChanges)
                    {
                        if (change.Status == ChangeKind.Renamed
                            && (criteria.InScope(change.Path) || criteria.InScope(change.OldPath)))
                        {
                            Note(renamedTo, formerNames, surviving, change.OldPath,
                                 Follow(renamedTo, change.Path));
                        }
                    }
                }
            }

            // A file renamed out of scope is dropped, because it no longer is a
            // file taken into account.
            return result.Where(file => criteria.InScope(file.Key))
                         .ToDictionary(file => file.Key, file => file.Value);
        }

        /// <summary>
        /// The comparison of <paramref name="newTree"/> against
        /// <paramref name="oldTree"/>, narrowed to the directories taken into
        /// account where there are any.
        /// </summary>
        /// <typeparam name="T">What the comparison is to yield.</typeparam>
        /// <param name="repository">The repository the trees belong to.</param>
        /// <param name="criteria">States the directories taken into account.</param>
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
        /// <param name="surviving">The files still present at the tip of one of the
        /// branches.</param>
        /// <param name="oldPath">The former name of the file.</param>
        /// <param name="target">The name the file carries later on.</param>
        /// <remarks>
        /// A rename is a fact about the one branch it was made on. Where the old
        /// name is still present at the tip of another, the two are distinct files,
        /// each alive and each with work of its own, and the rename is not followed:
        /// doing so would attribute everything ever done to the old one to a name
        /// its own branch has never borne, and leave its node empty.
        /// </remarks>
        private static void Note(IDictionary<string, string> renamedTo,
                                 IDictionary<string, ISet<string>> formerNames,
                                 ISet<string> surviving, string oldPath, string target)
        {
            // The first condition keeps a file renamed away and later renamed back
            // from becoming a cycle of length one; the second leaves a file that is
            // still there to its own history. See the remarks above.
            if (oldPath != target && !surviving.Contains(oldPath))
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
        /// Puts the metrics of <paramref name="churn"/> into
        /// <paramref name="graph"/>: one node per file that survived in a
        /// selected branch, nested in nodes standing for the directories holding
        /// them, under a single root standing for the repository, with an edge
        /// between every two files a commit changed together.
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
        /// <param name="graph">The graph the nodes and edges are added to.</param>
        /// <param name="churn">The churn of every file the graph is to hold a node for.
        /// A file untouched in the period is among them, with nothing counted against
        /// it.</param>
        /// <param name="formerNames">The names a renamed file carried before, keyed by the name
        /// it carries at the end.</param>
        /// <param name="repositoryName">The name of the root node standing for the
        /// repository.</param>
        /// <param name="session">Used to read the content of a file, which the metrics of its
        /// code are gathered from.</param>
        /// <param name="simplifyGraph">Whether a chain of directory nodes holding nothing but
        /// one another is to be collapsed into its innermost one.</param>
        private static void Fill(Graph graph, IDictionary<string, Churn> churn,
                                 IDictionary<string, ISet<string>> formerNames,
                                 string repositoryName, GitRepositorySession session,
                                 bool simplifyGraph)
        {
            // The node standing for each file, so that the edges between files
            // changed together can be drawn once every node exists.
            IDictionary<string, Node> nodes = new Dictionary<string, Node>();

            foreach (KeyValuePair<string, Churn> file in churn)
            {
                GitFileMetrics metrics = MetricsOf(file.Value);
                Node node = GraphUtils.GetOrAddFileNode(graph, file.Key);
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
            GitGraphGenerator.AddCodeMetrics(graph, session);
            AddCoChanges(graph, churn, nodes);
            graph.AddSingleRoot(out Node _, repositoryName, DataModel.DG.VCS.RepositoryType);
            // After the root, which the collapsing starts from, and again by
            // GitGraphGenerator rather than afresh. Only directory nodes are
            // affected, so the edges drawn above outlive it.
            GitGraphGenerator.Simplify(graph, simplifyGraph);
            graph.FinalizeNodeHierarchy();
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
                    // other file is accounted for at all: one changed together
                    // with this one but deleted since has no node to join.
                    if (string.CompareOrdinal(file.Key, coChange.Key) >= 0
                        || !churn.ContainsKey(coChange.Key))
                    {
                        continue;
                    }
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
        /// What is taken into account: which commits are walked, which of those
        /// count, and which files. Handed to everything needing to know, so
        /// that it is stated in one place only.
        /// </summary>
        /// <remarks>
        /// The two entry points differ in this and in nothing else. One walks
        /// the union of a set of branches and counts what was authored since a
        /// date, the older commits being walked for their renames alone; the
        /// other walks a range of commits and counts every one of them.
        /// </remarks>
        private class Criteria
        {
            /// <summary>
            /// The beginning of the period taken into account; commits authored
            /// at this very instant are still taken into account. Null where
            /// every commit walked is taken into account, which is so for a
            /// range: the two commits are the boundary.
            /// </summary>
            private readonly DateTimeOffset? since;

            /// <summary>
            /// The commits to be walked.
            /// </summary>
            internal CommitFilter Commits { get; }

            /// <summary>
            /// What is walked, in words, for the log. Emitted before the walk,
            /// so that a filter taking in far more than was intended shows at
            /// once rather than only once everything has been walked.
            /// </summary>
            internal string Walked { get; }

            /// <summary>
            /// The pathspec narrowing every comparison of a commit against its
            /// parent to the directories taken into account, or null where the
            /// whole repository is taken into account.
            /// </summary>
            internal IEnumerable<string> Pathspec { get; }

            /// <summary>
            /// Decides the globbing of the filter. Held here because
            /// <see cref="SEE.VCS.Filter.Matcher"/> builds a new one on every
            /// access, and this one is consulted for every file of every commit.
            /// </summary>
            /// <remarks>Can be null, in which case every file passes.</remarks>
            private readonly Matcher matcher;

            /// <summary>
            /// What is taken into account over a period: the commits reachable
            /// from <paramref name="branches"/>, of which those authored at or
            /// after <paramref name="since"/> count.
            /// </summary>
            /// <remarks>
            /// The commits before the period are walked all the same, their
            /// renames being needed: leaving them out would break the chain of
            /// names and split a file over two nodes.
            /// </remarks>
            /// <param name="since">The beginning of the period taken into account.</param>
            /// <param name="filter">States which files are taken into account.</param>
            /// <param name="branches">The branches whose commits are walked, together.</param>
            internal Criteria(DateTimeOffset since, SEE.VCS.Filter filter,
                              ICollection<Branch> branches)
                : this(filter)
            {
                this.since = since;
                Commits = new CommitFilter
                {
                    IncludeReachableFrom = branches,
                    // Newest commit first, just as git log reports them. A rename
                    // is thus seen before the commits preceding it, which still
                    // use the former name of the renamed file.
                    SortBy = CommitSortStrategies.Time
                };

                StringBuilder walked = new();
                walked.AppendLine($"Reporting on the union of {branches.Count} branches, "
                                  + "selected by "
                                  + string.Join(", ", filter.Branches ?? Enumerable.Empty<string>())
                                  + ":");
                foreach (Branch branch in branches)
                {
                    walked.AppendLine($"  {branch.FriendlyName}");
                }
                Walked = walked.ToString();
            }

            /// <summary>
            /// What is taken into account over a range: the commits reachable
            /// from <paramref name="commitID"/> and not from
            /// <paramref name="baselineCommitID"/>, every one of which counts.
            /// </summary>
            /// <param name="baselineCommitID">The commit the range starts after; it is
            /// itself left out.</param>
            /// <param name="commitID">The commit the range ends at; it is itself taken
            /// in.</param>
            /// <param name="filter">States which files are taken into account.</param>
            internal Criteria(string baselineCommitID, string commitID, SEE.VCS.Filter filter)
                : this(filter)
            {
                since = null;
                Commits = new CommitFilter
                {
                    IncludeReachableFrom = commitID,
                    ExcludeReachableFrom = baselineCommitID,
                    // Newest first, as above: the renames must be seen before
                    // the commits that precede them.
                    SortBy = CommitSortStrategies.Time
                };
                Walked = $"Reporting on the commits reachable from {commitID} and not from "
                         + $"{baselineCommitID}.";
            }

            /// <summary>
            /// Sets what the two constructors above have in common.
            /// </summary>
            /// <param name="filter">States which files are taken into account.</param>
            private Criteria(SEE.VCS.Filter filter)
            {
                matcher = filter.Matcher;
                Pathspec = filter.RepositoryPaths == null || filter.RepositoryPaths.Length == 0
                           ? null : filter.RepositoryPaths;
            }

            /// <summary>
            /// Whether the churn of <paramref name="commit"/> is taken into
            /// account, as against its renames alone, which always are.
            /// </summary>
            /// <param name="commit">The commit to be judged.</param>
            /// <returns>True if and only if its churn counts.</returns>
            internal bool Counts(Commit commit)
            {
                return since == null || commit.Author.When >= since.Value;
            }

            /// <summary>
            /// Whether <paramref name="path"/> denotes a file taken into account,
            /// that is, one lying in one of the directories of the filter and
            /// passing its globbing.
            /// </summary>
            /// <param name="path">The path to be checked, relative to the root of
            /// the repository and separated by <c>/</c>; may be null.</param>
            /// <returns>True if and only if the file is taken into account.</returns>
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
            /// Notes that <paramref name="other"/> was changed by the same
            /// commit as this file.
            /// </summary>
            /// <param name="other">The name the other file carries at the end.</param>
            internal void AlsoChanged(string other)
            {
                CoChanges.TryGetValue(other, out int count);
                CoChanges[other] = count + 1;
            }

            /// <summary>
            /// Accounts for one change of the file.
            /// </summary>
            /// <param name="linesAdded">The number of lines the change adds.</param>
            /// <param name="linesDeleted">The number of lines the change deletes.</param>
            /// <param name="sha">The SHA of the commit the change belongs to.</param>
            /// <param name="author">The author of that commit.</param>
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
