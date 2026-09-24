using LibGit2Sharp;
using Microsoft.Extensions.FileSystemGlobbing;
using SEE.GraphProviders.VCS;
using SEE.Utils;
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
    /// Used for interacting with a git repository.
    /// This class should be disposed after use.
    /// </summary>
    public sealed class GitRepositorySession : IDisposable
    {
        /// <summary>
        /// The git repository to interact with.
        /// Will be disposed in <see cref="Dispose"/>
        /// </summary>
        private readonly Repository repository;

        /// <summary>
        /// The configuration for the git operations.
        /// </summary>
        private readonly GitRepository repositoryConfig;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="repositoryConfig">The configuration for the git operations.</param>
        public GitRepositorySession(GitRepository repositoryConfig)
        {
            repository = new Repository(repositoryConfig.RepositoryPath.Path);
            this.repositoryConfig = repositoryConfig;
        }

        /// <summary>
        /// The mapping the .mailmap file at the root of this repository states,
        /// read when first asked for. Where there is no such file, it maps
        /// nothing and every author is named as their commits name them.
        /// </summary>
        internal Mailmap Mailmap
            => mailmap ??= Mailmap.Read(Path.Combine(repositoryConfig.RepositoryPath.Path,
                                                     Mailmap.Filename));

        /// <summary>
        /// Backs <see cref="Mailmap"/>. Null until that is first asked for.
        /// </summary>
        private Mailmap mailmap;

        /// <summary>
        /// Fetches all remote branches for the given repository path.
        /// </summary>
        /// <returns>True if there are any changes (new, deleted, or changed remote branches); false otherwise.</returns>
        /// <exception cref="Exception">Thrown if an error occurs while fetching the remotes.</exception>"
        public bool FetchRemotes()
        {
            bool result = false;
            // A fetch may add, remove or move a branch, so what was held as relevant
            // is no longer to be trusted.
            relevantBranches = null;

            // Fetch all remotes; this is needed if there are multiple remotes.
            // As a matter of fact, a repository may have multiple remotes.
            foreach (Remote remote in repository.Network.Remotes)
            {
                IEnumerable<string> refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                try
                {
                    Dictionary<string, string> previousBranches = GetBranches(repository);

                    // Fetch downloads new commits from the remote repository. These commits are stored
                    // locally but are not integrated into the working directory or local branches.
                    // They reside on remote-tracking branches (remotes/origin/main).
                    // Option Prune=true removes any remote-tracking references that no longer exist on the remote.
                    Commands.Fetch(repository, remote.Name, refSpecs, new FetchOptions()
                    {
                        Prune = true,
                        CredentialsProvider = (_url, _user, _types) => new UsernamePasswordCredentials
                        {
                            Username = repositoryConfig.AccessToken,
                            Password = string.Empty
                        }
                    }, "");

                    Dictionary<string, string> newBranches = GetBranches(repository);

                    // Compare previousBranches to newBranches for new and changed branches.
                    foreach (KeyValuePair<string, string> pair in newBranches)
                    {
                        if (previousBranches.TryGetValue(pair.Key, out string previousSha))
                        {
                            // Existed before.
                            if (previousSha != pair.Value)
                            {
                                // Has changed.
                                result = true;
                            }
                        }
                        else
                        {
                            // New branch.
                            result = true;
                        }
                    }
                    // Compare previousBranches to newBranches for deleted branches.
                    result |= previousBranches.Any(pair => !newBranches.ContainsKey(pair.Key));
                }
                catch (LibGit2SharpException e)
                {
                    throw new Exception
                        ($"Error while running git fetch for repository path {repositoryConfig.RepositoryPath.Path} and remote name {remote.Name}: {e.Message}.\n");
                }
            }
            return result;

            // Returns a dictionary mapping the canonical name of each remote branch onto the SHA of its tip.
            static Dictionary<string, string> GetBranches(Repository repository)
            {
                Dictionary<string, string> remoteBranches = new();

                foreach (Branch remoteBranch in repository.Branches.Where(b => b.IsRemote))
                {
                    remoteBranches[remoteBranch.CanonicalName] = remoteBranch.Tip.Sha;
                }
                return remoteBranches;
            }
        }

        /// <summary>
        /// Returns the list of non-merge commits between the two given commits in reverse
        /// topological order, that is, a commit is returned before all its parents.
        /// A non-merge commit is a commit having more than one parent.
        ///
        /// For instance, in a commit history like this:
        ///
        ///     ---1----2----4----7
        ///         \              \
        ///          3----5----6----8---
        ///
        /// The result would be 1,2,4,7,3,5,6 or 1,3,5,6,2,4,7.
        /// Commit 8 is not included because it is a merge commit (it has two parents, 6 and 7).
        ///
        /// More precisely, it returns all commits that are backward reachable from
        /// <paramref name="newCommitId"/> (including <paramref name="newCommitId"/> itself),
        /// but not backward reachable from <paramref name="oldCommitId"/>. A commit is
        /// backard reachable from another commit if it is an ancestor of that commit in
        /// the commit history. Note that, by definition, every commit is backward reachable from itself.
        /// Thus, the results will include <paramref name="newCommitId"/> but not <paramref name="oldCommitId"/>.
        ///
        /// This method is equivalent to the command line query:
        ///    git rev-list --topo-order --reverse --no-merges <newCommitId> ^<oldCommitId>
        /// </summary>
        /// <param name="oldCommitId">SHA hash of the earlier commit serving as the baseline</param>
        /// <param name="newCommitId">SHA hash of the from which to search backward for relevant commits</param>
        /// <returns>The list of commits from <paramref name="oldCommitId"/> to <paramref name="newCommitId"/>.</returns>
        /// <exception cref="ArgumentException">thrown if <paramref name="oldCommitId"/> or
        /// <paramref name="newCommitId"/> is null or empty or if they do not identify
        /// any commit in the repository.</exception>
        public IEnumerable<Commit> CommitsBetween(string oldCommitId, string newCommitId)
        {
            if (string.IsNullOrWhiteSpace(oldCommitId) || string.IsNullOrWhiteSpace(newCommitId))
            {
                throw new ArgumentException("Both commit IDs must be non-empty strings.");
            }

            // The 'Walk' method with the 'Exclude' filter is the most efficient way to do this.
            // It walks the history starting from 'newCommit' and excludes any commits reachable from 'oldCommit'.
            return repository.Commits.QueryBy(new CommitFilter
            {
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse,
                IncludeReachableFrom = GetCheckedCommit(newCommitId),
                ExcludeReachableFrom = GetCheckedCommit(oldCommitId)
            }).Where(c => c.Parents.Count() <= 1); // ignore merge conflicts, i.e., commit with more than one parent
        }


        /// <summary>
        /// Yields the SHAs of all commits (excluding merge commits) authored at or after
        /// <paramref name="startDate"/>, which denotes the instant its day begins at, in UTC,
        /// until today across all branches.
        /// </summary>
        /// <param name="startDate">The date from which on commits should be retrieved.</param>
        /// <returns>All commits (excluding merge commits) authored at or after
        /// <paramref name="startDate"/>.</returns>
        public IList<string> CommitsAfter(DateTime startDate)
        {
            return CommitsAfter(repository, startDate).Select(c => c.Sha).ToList();
        }

        /// <summary>
        /// Yields all commits (excluding merge commits) authored at or after
        /// <paramref name="startDate"/> until today across all branches.
        ///
        /// A date denotes the instant it begins at, in UTC, and a commit authored at that
        /// very instant is yielded. The day named is thus taken in, not left out, and the
        /// boundary is one moment for every commit rather than one per author.
        /// </summary>
        /// <remarks>
        /// Every commit reachable from a branch is looked at. The walk used to stop at the
        /// first commit committed before <paramref name="startDate"/>, on the grounds that
        /// no commit beyond it could qualify. That holds only where the dates increase along
        /// the walk, and they need not: a rebase, a cherry-pick or a clock out of step all
        /// let a parent carry a later date than its child, and a commit that should have
        /// counted was then silently passed over. What the stopping saved has in any case
        /// dwindled, the commits being walked once over the union of the branches rather
        /// than once for each of them.
        ///
        /// Formerly the date of a commit was compared to <paramref name="startDate"/> by the
        /// day, and only a later day passed. Two shortcomings went with that. The day of a
        /// commit was the day in the offset of its own author, so two commits made at the
        /// same instant in different parts of the world could fall on either side of the
        /// boundary; and the day named was excluded entire, so a date of the first of
        /// January yielded commits from the second onwards.
        /// </remarks>
        /// <param name="repository">The repository from which to retrieve the commits.</param>
        /// <param name="startDate">The date from which on commits should be retrieved.</param>
        /// <returns>All commits (excluding merge commits) authored at or after
        /// <paramref name="startDate"/>.</returns>
        private static IEnumerable<Commit> CommitsAfter(Repository repository, DateTime startDate)
        {
            DateTimeOffset since = new(startDate.Date, TimeSpan.Zero);

            foreach (Commit commit in repository.Commits.QueryBy(new CommitFilter
            {
                IncludeReachableFrom = repository.Branches,
                SortBy = CommitSortStrategies.Time
            }))
            {
                // Every commit is looked at, the walk never stopping early. See
                // the remarks above for why.
                if (commit.Author.When >= since && commit.Parents.Count() <= 1)
                {
                    yield return commit;
                }
            }
        }

        /// <summary>
        /// Returns the commit with the given <paramref name="commitID"/> from the repository.
        /// If there is no such <paramref name="commitID"/>, an exception is thrown.
        /// </summary>
        /// <param name="commitID">Commit ID.</param>
        /// <returns>The commit corresponding to <paramref name="commitID"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="commitID"/> is null or
        /// empty or if the repository does not have a commit with the given <paramref name="commitID"/>.</exception>
        private Commit GetCheckedCommit(string commitID)
        {
            if (string.IsNullOrWhiteSpace(commitID))
            {
                throw new ArgumentException("Commit ID must not be null or empty.", nameof(commitID));
            }
            Commit commit = repository.Lookup<Commit>(commitID);
            return commit ?? throw new ArgumentException($"SHA1 {commitID} does not exist in the repository {repositoryConfig.RepositoryPath.Path}.");
        }

        /// <summary>
        /// Returns the content of the file at <paramref name="repositoryFilePath"/>
        /// present in the repository in any of the branches passing the filter.
        ///
        /// Note: A file may exist in multiple branches, but this method will
        /// return the content of the first file found in the branches.
        /// </summary>
        /// <param name="repositoryFilePath">Relative path of the file within the repository.</param>
        /// <returns>The content of the file.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="repositoryFilePath"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
        public string GetFileContent(string repositoryFilePath)
        {
           return GetBlob(repositoryFilePath).GetContentText();
        }

        /// <summary>
        /// Returns the content of the file at <paramref name="repositoryFilePath"/> (as a stream)
        /// present in the repository in any of the branches passing the filter.
        ///
        /// Note: A file may exist in multiple branches, but this method will
        /// return the content of the first file found in the branches.
        /// </summary>
        /// <param name="repositoryFilePath">Relative path of the file within the repository.</param>
        /// <returns>The content of the file as a stream.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="repositoryFilePath"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
        public Stream GetStream(string repositoryFilePath)
        {
            return GetBlob(repositoryFilePath).GetContentStream();
        }

        /// <summary>
        /// Returns the <see cref="Blob"/> object representing the file at <paramref name="repositoryFilePath"/>
        /// in any of <see cref="RelevantBranches"/>. The file can exist in multiple branches, but this method
        /// will return the first one found.
        /// </summary>
        /// <param name="repositoryFilePath">Relative path of the file within the repository.</param>
        /// <returns>The <see cref="Blob"/> object representing the file.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="repositoryFilePath"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
        private Blob GetBlob(string repositoryFilePath)
        {
            if (string.IsNullOrWhiteSpace(repositoryFilePath))
            {
                throw new ArgumentException("Repository file path must not be null or empty.", nameof(repositoryFilePath));
            }
            foreach (Branch branch in RelevantBranches())
            {
                Blob blob = branch.Tip.Tree[repositoryFilePath]?.Target as Blob;
                if (blob != null)
                {
                    return blob;
                }
            }
            // Blob does not exist.
            throw new FileNotFoundException($"File {repositoryFilePath} does not exist.\n");
        }

        /// <summary>
        /// Yields the canonical name of all branches in <paramref name="repository"/>.
        /// </summary>
        /// <returns>Canonical name of all branches.</returns>
        public IList<string> AllBranchNames()
        {
            return repository.Branches.Select(b => b.CanonicalName).ToList();
        }

        /// <summary>
        /// Returns the hashes of all tip SHA commits from all relevant branches in this <see cref="GitRepository"/>.
        /// </summary>
        /// <returns>The hashes of the tip SHA commits of all branches.</returns>
        public IList<string> GetTipHashes()
        {
            return RelevantBranches().Select(x => x.Tip.Sha).ToList();
        }

        /// <summary>
        /// If <see cref="Filter.Branches"/> of the <see cref="GitRepository.VCSFilter"/> this
        /// session was opened for is null, all branches of the repository are returned.
        /// Otherwise, yields all branches passing that filter, that is, those whose FriendlyName
        /// is matched as a whole by at least one of its regular expressions; see
        /// <see cref="Filter.Matches(Branch)"/>.
        ///
        /// The result is a collection rather than an enumeration, so that a caller may count
        /// the branches without walking them twice.
        ///
        /// It is worked out once and held. Neither the repository nor the filter changes
        /// while a session lasts, save through <see cref="FetchRemotes"/>, which discards
        /// what is held. This matters: <see cref="GetBlob"/> asks for the relevant branches
        /// once for every file it is asked about, and working them out anew each time means
        /// materialising every branch of the repository and matching it against every
        /// expression of the filter, over and over.
        /// </summary>
        /// <returns>All relevant branches of the repository.</returns>
        public ICollection<Branch> RelevantBranches()
        {
            return relevantBranches ??= Selected();

            // The branches of the repository the filter holds relevant.
            ICollection<Branch> Selected()
            {
                if (repositoryConfig.VCSFilter == null)
                {
                    return repository.Branches.ToList();
                }
                return repository.Branches
                                 .Where(branch => repositoryConfig.VCSFilter.Matches(branch))
                                 .ToList();
            }
        }

        /// <summary>
        /// Backs <see cref="RelevantBranches"/>. Null until first asked for, and again
        /// after <see cref="FetchRemotes"/>, which may have altered what there is to select
        /// from.
        /// </summary>
        private ICollection<Branch> relevantBranches;

        /// <summary>
        /// Yields all distinct file paths of the given <paramref name="repository"/>
        /// that fulfill the given <paramref name="filter"/>. If <paramref name="filter"/>
        /// is null, all files of all branches will be retrieved.
        ///
        /// If <paramref name="filter"/> is different from null, the constraints for a
        /// file to be reported are as follows:
        ///
        /// If attribute <see cref="Filter.Branches"/> of <paramref name="filter"/>
        /// is null or empty, the file can be contained in any of the current branches of the <paramref name="repository"/>.
        /// Otherwise, a file must be contained in at leasts one of the branches described in attribute
        /// <see cref="Filter.Branches"/>. The actual matching is done using the <see cref="Filter.Matches(Branch)"/>,
        /// thus, a branch is considered only if its FriendlyName is matched by at least one of the
        /// regular expressions in <see cref="Filter.Branches"/>.
        ///
        /// If attribute <see cref="Filter.RepositoryPaths"/> is null or empty, every file in
        /// the repository will be considered. Otherwise only the files contained in any of
        /// the subdirectories of the <paramref name="repository"/> listed in the <see cref="Filter.RepositoryPaths"/>.
        ///
        /// If attribute <see cref="Filter.Matcher"/> is null, a file path can take on any name.
        /// Otherwise a file will be reported only if it fulfills at least on inclusive criterion
        /// of <see cref="Filter.Matcher"/> and does not violate any exclusive criterion in
        /// <see cref="Filter.Matcher"/>.
        /// </summary>
        /// <param name="token">For cancelling this operation.</param>
        /// <returns>All distinct file paths.</returns>
        public HashSet<string> AllFiles(CancellationToken token = default)
        {
            HashSet<string> result = new();
            ICollection<Branch> branches = RelevantBranches();
            if (branches.Count == 0)
            {
                Debug.LogWarning("There are no branches matching the branch filter.\n");
                return result;
            }
            foreach (Branch branch in branches)
            {
                AllFiles(branch.Tip.Tree, result, token);
            }
            return result;
        }

        /// <summary>
        /// Yields all distinct file paths of the given <paramref name="tree"/>
        /// that fulfill the given <paramref name="filter"/>. If <paramref name="filter"/>
        /// is null, all files of <paramref name="tree"/> will be retrieved.
        ///
        /// If <paramref name="filter"/> is different from null, the constraints for a
        /// file to be reported are as follows:
        ///
        /// If attribute <see cref="Filter.RepositoryPaths"/> is null or empty, every file in
        /// the <paramref name="tree"/> will be considered. Otherwise only the files contained in any of
        /// the subdirectories of <paramref name="tree"/> listed in the <see cref="Filter.RepositoryPaths"/>.
        ///
        /// If attribute <see cref="Filter.Matcher"/> is null, a file path can take on any name.
        /// Otherwise a file will be reported only if it fulfills at least on inclusive criterion
        /// of <see cref="Filter.Matcher"/> and does not violate any exclusive criterion in
        /// <see cref="Filter.Matcher"/>.
        /// </summary>
        /// <param name="tree">The tree for which to retrieve the files.</param>
        /// <param name="token">For cancelling this operation.</param>
        /// <returns>All distinct file paths.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tree"/> is null.</exception>
        private HashSet<string> AllFiles(LibGit2Sharp.Tree tree, CancellationToken token = default)
        {
            if (tree == null)
            {
                throw new ArgumentNullException(nameof(tree));
            }
            HashSet<string> result = new();
            AllFiles(tree, result, token);
            return result;
        }

        /// <summary>
        /// Retrieves a collection of all file paths associated with the specified <paramref name="commitID"/>.
        ///
        /// Analogous to <see cref="AllFiles(LibGit2Sharp.Tree)"/>, where the tree for the commit is passed.
        /// </summary>
        /// <param name="commitID">The unique identifier of the commit. Must not be null, empty, or consist only of whitespace.</param>
        /// <param name="token">For cancelling this operation.</param>
        /// <returns>A collection of file paths representing all files in the specified commit.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="commitID"/> is null, empty,
        /// or consists only of whitespace.</exception>
        public HashSet<string> AllFiles(string commitID, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(commitID))
            {
                throw new ArgumentNullException(nameof(commitID), "Commit ID must neither be null nor empty.");
            }
            return AllFiles(GetCheckedCommit(commitID).Tree, token);
        }

        /// <summary>
        /// Adds the distinct filenames in the given <paramref name="tree"/> passing
        /// the criteria <see cref="Filter.RepositoryPaths"/> and <see cref="Filter.Matcher"/>
        /// to given set of <paramref name="paths"/>.
        ///
        /// If <see cref="Filter.RepositoryPaths"/> is null or empty, all files in the entire
        /// <paramref name="tree"/> are retrieved. Otherwise, only the files in the subtrees
        /// denoted by <see cref="Filter.RepositoryPaths"/> are retrieved. In case a path
        /// does not exist in the <paramref name="tree"/>, it is ignored.
        /// </summary>
        /// <param name="tree">The tree for which to retrieve the files.</param>
        /// <param name="paths">Where the passing files are to be added.</param>
        /// <param name="token">For cancelling this operation.</param>
        private void AllFiles(LibGit2Sharp.Tree tree, HashSet<string> paths, CancellationToken token = default)
        {
            if (repositoryConfig.VCSFilter.RepositoryPaths == null || repositoryConfig.VCSFilter.RepositoryPaths.Length == 0)
            {
                token.ThrowIfCancellationRequested();
                // We collect all files in the entire tree.
                CollectFiles(tree, repositoryConfig.VCSFilter.Matcher, paths, token);
            }
            else
            {
                // We collect all files in the subtrees denoted by the repository paths.
                foreach (string repositoryPath in repositoryConfig.VCSFilter.RepositoryPaths)
                {
                    if (!string.IsNullOrWhiteSpace(repositoryPath))
                    {
                        token.ThrowIfCancellationRequested();
                        LibGit2Sharp.Tree subtree = Find(tree, repositoryPath);
                        // It can happen that we do not find the subtree, because
                        // it may exist in some branches, but not in others.
                        if (subtree != null)
                        {
                            CollectFiles(subtree, repositoryConfig.VCSFilter.Matcher, paths, token);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the descendant tree for the given <paramref name="repositoryPath"/>.
        /// <paramref name="repositoryPath"/> is assumed to be a path relative to <paramref name="tree"/>.
        /// The delimiter to separate different subtrees is assumed to be the forward slash.
        /// For instance, "Assets/SEE/UI" identifies the folder UI, nested in folder SEE nested in folder
        /// Assets.
        /// </summary>
        /// <param name="tree">The root tree.</param>
        /// <param name="repositoryPath">Relative path of descendants nested in <paramref name="tree"/>.</param>
        /// <returns>The subtree or null if it does not exist.</returns>
        private static LibGit2Sharp.Tree Find(LibGit2Sharp.Tree tree, string repositoryPath)
        {
            TreeEntry result = tree[repositoryPath];
            if (result == null)
            {
                // Path does not exist.
                return null;
            }
            return result.TargetType == TreeEntryTargetType.Tree ? (LibGit2Sharp.Tree)result.Target : null;
        }

        /// <summary>
        /// Adds all distinct repository-relative paths contained in <paramref name="tree"/>
        /// fulfilling at least one inclusion and not fulfilling any exclusion criteria
        /// of <paramref name="matcher"/>. The search recursively descends into
        /// the subtrees of <paramref name="tree"/>. The paths are added to <paramref name="paths"/>
        ///
        /// By the nature of Git, paths always identify files, not directories.
        ///
        /// It is equivalent to "git ls-tree --name-only".
        /// </summary>
        /// <param name="tree">The tree whose files are requested.</param>
        /// <param name="matcher">The inclusion/exclusion path globbings.</param>
        /// <param name="paths">The set of paths to which the paths are to be added.</param>
        /// <param name="token">For cancelling this operation.</param>
        /// <returns>The set of distinct paths.</returns>
        private static void CollectFiles(LibGit2Sharp.Tree tree, Matcher matcher, HashSet<string> paths, CancellationToken token)
        {
            foreach (TreeEntry entry in tree)
            {
                if (entry.TargetType == TreeEntryTargetType.Blob)
                {
                    if (matcher == null || matcher.Matches(entry.Path))
                    {
                        paths.Add(entry.Path);
                    }
                }
                else if (entry.TargetType == TreeEntryTargetType.Tree)
                {
                    LibGit2Sharp.Tree subtree = (LibGit2Sharp.Tree)entry.Target;
                    CollectFiles(subtree, matcher, paths, token);
                }
            }
        }

        /// <summary>
        /// Disposes the LibGit2Sharp git repository instance.
        /// </summary>
        public void Dispose()
        {
            repository.Dispose();
        }
    }
}
