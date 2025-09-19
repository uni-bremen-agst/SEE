using LibGit2Sharp;
using Microsoft.Extensions.FileSystemGlobbing;
using SEE.Game.City;
using SEE.GraphProviders.VCS;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace SEE.VCS
{
    /// <summary>
    /// Represents the needed information about a git repository for a <see cref="SEECityEvolution"/>.
    /// </summary>
    [Serializable]
    public class GitRepository: IDisposable
    {
        /// <summary>
        /// The underlying <see cref="Repository"/> object that provides access to
        /// the Git repository.
        /// </summary>
        private Repository repository;

        /// <summary>
        /// Disposes the repository if it is not null.
        /// </summary>
        public void Dispose()
        {
            // Dispose of the repository if it is not null to release resources.
            repository?.Dispose();
            // Calling GC.SuppressFinalize(this) to improve efficiency by preventing unnecessary
            // finalization when Dispose is called explicitly.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer in case <see cref="Dispose"/> is not called explicitly
        /// </summary>
        ~GitRepository()
        {
            Dispose();
        }

        /// <summary>
        /// Returns a string representation of the object, the repository path, more precisely.
        /// </summary>
        /// <returns>the repository path</returns>
        public override string ToString()
        {
            return $"GitRepository: {RepositoryPath.Path}";
        }

        /// <summary>
        /// Used for the tab name in runtime config menu.
        /// </summary>
        private const string graphProviderFoldoutGroup = "Data";

        /// <summary>
        /// The path to the git repository.
        /// </summary>
        [ShowInInspector, Tooltip("Path to the git repository."), HideReferenceObjectPicker,
            RuntimeTab(graphProviderFoldoutGroup)]
        public DataPath RepositoryPath = new();

        /// <summary>
        /// Filter to be used to retrieve the relevant files from the repository.
        /// </summary>
        [OdinSerialize]
        [ShowInInspector, ListDrawerSettings(ShowItemCount = true),
         Tooltip("Filter to identify the relevant files in the repository."),
         RuntimeTab(graphProviderFoldoutGroup),
         HideReferenceObjectPicker]
        public Filter VCSFilter = new();

        /// <summary>
        /// Constructor setting default values for the fields.
        /// </summary>
        public GitRepository()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="repositoryPath">path to the repository</param>
        /// <param name="filter">the filter to be used to retrieve the relevant files from the repository</param>
        public GitRepository(DataPath repositoryPath, Filter filter)
        {
            RepositoryPath = repositoryPath ??
                throw new ArgumentNullException(nameof(repositoryPath), "Repository path must not be null.");
            VCSFilter = filter;
        }

        /// <summary>
        /// Clones the repository at <paramref name="url"/> into the <see cref="RepositoryPath"/>.
        /// </summary>
        /// <param name="url">URL for the repository</param>
        /// <param name="accessToken">the access token for the repository</param>
        /// <exception cref="Exception"></exception>
        public void Clone(string url, string accessToken)
        {
            try
            {
                Debug.Log($"Cloned into {Repository.Clone(MergeToken(url, accessToken), RepositoryPath.Path, new CloneOptions())}\n");
            }
            catch (LibGit2SharpException e)
            {
                throw new Exception
                       ($"Error while cloning repository from {url} into path {RepositoryPath.Path}: {e.Message}.\n");
            }
        }

        /// <summary>
        /// Returns the <paramref name="url"/> with the added <paramref name="accessToken"/>.
        ///
        /// For instance, MergeToken("https://github.com/koschke/TestProjectForSEE.git", "mytoken")
        /// yields "https://mytoken@github.com/koschke/TestProjectForSEE.git".
        /// </summary>
        /// <param name="url">URL to the repository; must start with https://</param>
        /// <param name="accessToken">access token to be added</param>
        /// <returns><paramref name="url"/> where <paramref name="accessToken"/> has
        /// been added</returns>
        private static string MergeToken(string url, string accessToken) => url.Replace("https://", $"https://{accessToken}@");

        /// <summary>
        /// Creates a new <see cref="Repository"/> object for the given <see cref="RepositoryPath"/>
        /// if none exists yet.
        /// </summary>
        /// <exception cref="ArgumentException">thrown if <see cref="RepositoryPath"/> is null or empty</exception>
        private void OpenRepository()
        {
            if (repository == null)
            {
                if (RepositoryPath == null || string.IsNullOrWhiteSpace(RepositoryPath.Path))
                {
                    throw new ArgumentException("Repository path must not be null or empty.", nameof(RepositoryPath));
                }
                repository = new(RepositoryPath.Path);
            }
        }

        /// <summary>
        /// Fetches all remote branches for the given repository path.
        /// </summary>
        /// <returns>true if there are any changes (new, deleted, or changed remote branches); false otherwise</returns>
        /// <exception cref="Exception">Thrown if an error occurs while fetching the remotes.</exception>"
        public bool FetchRemotes()
        {
            OpenRepository();

            bool result = false;

            // Fetch all remotes; this is needed if there are multiple remotes.
            // As a matter of fact, a repository may have multiple remotes.
            foreach (LibGit2Sharp.Remote remote in repository.Network.Remotes)
            {
                IEnumerable<string> refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                try
                {
                    Dictionary<string, string> previousBranches = GetBranches(repository);
                    //Print(currentBranches, "previous");

                    // Fetch downloads new commits from the remote repository. These commits are stored
                    // locally but are not integrated into the working directory or local branches.
                    // They reside on remote-tracking branches (remotes/origin/main).
                    // Option Prune=true removes any remote-tracking references that no longer exist on the remote.
                    //Debug.Log($"Fetching remote {remote.Name} for repository path {RepositoryPath.Path}.\n");
                    Commands.Fetch(repository, remote.Name, refSpecs, new FetchOptions() { Prune = true}, "");

                    Dictionary<string, string> newBranches = GetBranches(repository);
                    //Print(currentBranches, "new");

                    // Compare previousBranches to newBranches for new and changed branches.
                    foreach (KeyValuePair<string, string> pair in newBranches)
                    {
                        if (previousBranches.TryGetValue(pair.Key, out string previousSha))
                        {
                            // Existed before.
                            if (previousSha != pair.Value)
                            {
                                // Has changed.
                                //Debug.Log($"Remote branch {pair.Key} has changed from SHA {previousSha} to SHA {pair.Value}.\n");
                                result = true;
                            }
                            else
                            {
                                // Unchanged.
                                //Debug.Log($"Remote branch {pair.Key} is unchanged with SHA {pair.Value}.\n");
                            }
                        }
                        else
                        {
                            // New branch.
                            //Debug.Log($"New remote branch {pair.Key} with SHA {pair.Value}.\n");
                            result = true;
                        }
                    }
                    // Compare previousBranches to newBranches for deleted branches.
                    foreach (KeyValuePair<string, string> pair in previousBranches)
                    {
                        if (!newBranches.ContainsKey(pair.Key))
                        {
                            //Debug.Log($"Remote branch {pair.Key} with SHA {pair.Value} has been deleted.\n");
                            result = true;
                        }
                    }
                }
                catch (LibGit2SharpException e)
                {
                    throw new Exception
                        ($"Error while running git fetch for repository path {RepositoryPath.Path} and remote name {remote.Name}: {e.Message}.\n");
                }
            }
            return result;

            static Dictionary<string, string> GetBranches(Repository repository)
            {
                Dictionary<string, string> remoteBranches = new();

                foreach (Branch remoteBranch in repository.Branches.Where(b => b.IsRemote))
                {
                    remoteBranches[remoteBranch.CanonicalName] = remoteBranch.Tip.Sha;
                }
                return remoteBranches;
            }

            static void Print(Dictionary<string, string> branches, string message)
            {
                Debug.Log($"The {message} branches are:\n");
                foreach (KeyValuePair<string, string> pair in branches)
                {
                    Debug.Log($"Branch: {pair.Key} SHA={pair.Value}\n");
                }
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
            OpenRepository();
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
        /// Yields all commits (excluding merge commits) after <paramref name="startDate"/>
        /// until today across all branches.
        /// </summary>
        /// <param name="startDate">the date after which commits should be retrieved</param>
        /// <returns>all commits (excluding merge commits) after <paramref name="startDate"/></returns>
        public IList<Commit> CommitsAfter(DateTime startDate)
        {
            OpenRepository();

            IEnumerable<Commit> commitList = repository.Commits
                .QueryBy(new CommitFilter { IncludeReachableFrom = repository.Branches, SortBy = CommitSortStrategies.None })
                // Commits after startDate
                .Where(commit =>
                    DateTime.Compare(commit.Author.When.Date, startDate) > 0)
                // Filter out merge commits.
                .Where(commit => commit.Parents.Count() <= 1);
            return commitList.ToList();
        }

        /// <summary>
        /// Returns the commit with the given <paramref name="commitId"/> from the repository
        /// or null if there is no such <paramref name="commitId"/>.
        /// </summary>
        /// <param name="commitId">Commit ID</param>
        /// <returns>The commit corresponding to <paramref name="commitId"/> or null.</returns>
        /// <remarks>This method is similar to <see cref="GetCheckedCommit(string)"/>,
        /// but returns null if the commit does not exist.</remarks>
        internal Commit GetCommit(string commitId)
        {
            OpenRepository();
            return repository.Lookup<Commit>(commitId);
        }

        /// <summary>
        /// Returns the commit with the given <paramref name="commitId"/> from the repository.
        /// If there is no such <paramref name="commitId"/>, an exception is thrown.
        /// </summary>
        /// <param name="commitId">Commit ID</param>
        /// <returns>The commit corresponding to <paramref name="commitId"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if the repository does not have a commit
        /// with the given <paramref name="CommitID"/>.</exception>
        /// <remarks>This method is similar to <see cref="GetCommit(string)"/>,
        /// but throws an exception if the commit does not exist.</remarks>
        internal Commit GetCheckedCommit(string CommitID)
        {
            Commit commit = repository.Lookup<Commit>(CommitID);
            return commit ?? throw new ArgumentException($"SHA1 {CommitID} does not exist in the repository {RepositoryPath.Path}.");
        }

        /// <summary>
        /// Returns the commit log between the two given commits.
        /// </summary>
        /// <param name="oldCommit">earlier commit ID</param>
        /// <param name="newCommit">later commit ID</param>
        /// <returns>commit log between the two given commits</returns>
        internal ICommitLog CommitLog(Commit oldCommit, Commit newCommit)
        {
            OpenRepository();
            return repository.Commits.QueryBy(new CommitFilter
            {
                IncludeReachableFrom = newCommit,
                ExcludeReachableFrom = oldCommit
            });
        }

        /// <summary>
        /// Returns the commit log of the repository, sorted topologically.
        /// </summary>
        /// <returns>commit log of the repository in topological order</returns>
        internal ICommitLog CommitLog()
        {
            OpenRepository();
            return repository.Commits.QueryBy(new CommitFilter { SortBy = CommitSortStrategies.Topological });
        }

        /// <summary>
        /// Returns the diff between the two given commits <paramref name="oldCommit"/>
        /// and <paramref name="newCommit"/> as a <see cref="Patch"/>.
        /// </summary>
        /// <param name="oldCommit">earlier commit ID; can be null</param>
        /// <param name="newCommit">later commit ID; must not be null</param>
        /// <returns>diff between the two given commits</returns>
        internal Patch Diff(Commit oldCommit, Commit newCommit)
        {
            OpenRepository();
            if (newCommit == null)
            {
                throw new ArgumentNullException(nameof(newCommit), "New commit must not be null.");
            }
            return repository.Diff.Compare<Patch>(oldCommit?.Tree, newCommit.Tree);
        }

        /// <summary>
        /// Generates a patch representing the differences between two commits.
        /// Analogous to <see cref="Diff(Commit, Commit)"/>, but takes commit IDs as strings.
        /// </summary>
        /// <param name="oldCommitID">The identifier of the older commit to compare.</param>
        /// <param name="newCommitID">The identifier of the newer commit to compare.</param>
        /// <returns>A <see cref="Patch"/> object containing the differences between the specified commits.</returns>
        internal Patch Diff(string oldCommitID, string newCommitID)
        {
            OpenRepository();
            return Diff(GetCheckedCommit(oldCommitID), GetCheckedCommit(newCommitID));
        }

        /// <summary>
        /// Returns the <see cref="Patch"/> needed to turn the parent of <paramref name="commit"/>
        /// into the <paramref name="commit"/> itself. If <paramref name="commit"/> has no
        /// parent (very first commit in the version history), the <see cref="Patch"/> f
        /// rom the empty tree to <paramref name="commit"/> is returned. If <paramref name="commit"/>
        /// has multiple parents, the <see cref="Patch"/> from the first parent to <paramref name="commit"/>
        /// is returned.
        /// </summary>
        /// <param name="commit">The commit whose <see cref="Patch"/> is to be returned.</param>
        /// <returns>The <see cref="Patch"/> from the parent to <paramref name="commit"/></returns>
        public Patch GetPatchRelativeToParent(Commit commit)
        {
            OpenRepository();

            if (commit.Parents.Any())
            {
                return repository.Diff.Compare<Patch>(commit.Parents.First().Tree, commit.Tree);
            }
            return repository.Diff.Compare<Patch>(null, commit.Tree);
        }

        /// <summary>
        /// Returns the diff between the two given commits <paramref name="parent"/>
        /// and <paramref name="commit"/> as <see cref="TreeChanges"/>.
        /// </summary>
        /// <param name="parent">earlier commit ID</param>
        /// <param name="commit">later commit ID</param>
        /// <returns>diff between the two given commits</returns>
        internal TreeChanges TreeDiff(Commit parent, Commit commit)
        {
            OpenRepository();
            return repository.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);
        }

        /// <summary>
        /// Returns the content of the file at <paramref name="repositoryFilePath"/>
        /// present in the repository in any of the branches passing the filter.
        ///
        /// Note: A file may exist in multiple branches, but this method will
        /// return the content of the first file found in the branches.
        /// </summary>
        /// <param name="repositoryFilePath">relative path of the file within the repository</param>
        /// <returns>the content of the file</returns>
        /// <exception cref="Exception">thrown if the file does not exist</exception>
        public string GetFileContent(string repositoryFilePath)
        {
            OpenRepository();

            if (string.IsNullOrWhiteSpace(repositoryFilePath))
            {
                throw new ArgumentException("Repository file path must not be null or empty.", nameof(repositoryFilePath));
            }

            foreach (Branch branch in RelevantBranches())
            {
                Blob blob = branch.Tip.Tree[repositoryFilePath]?.Target as Blob;
                if (blob != null)
                {
                    return blob.GetContentText();
                }
            }
            // Blob does not exist.
            throw new Exception($"File {repositoryFilePath} does not exist.\n");
        }

        /// <summary>
        /// Yields the canonical name of all branches in <paramref name="repository"/>.
        /// </summary>
        /// <returns>canonical name of all branches</returns>
        public IEnumerable<string> AllBranchNames()
        {
            OpenRepository();
            return repository.Branches.Select(b => b.CanonicalName);
        }

        /// <summary>
        /// Returns the hashes of all tip commits from all relevant branches in this <see cref="GitRepository"/>.
        /// </summary>
        /// <returns>The hashes of the tip commits of all branches.</returns>
        public IList<string> GetTipHashes()
        {
            OpenRepository();
            return RelevantBranches().Select(x => x.Tip.Sha).ToList();
        }

        /// <summary>
        /// If <see cref="VCSFilter"/> is null, all branches of <see cref="repository"/> are
        /// returned. Otherwise, yields all branches passing <see cref="VCSFilter"/>.
        /// </summary>
        /// <returns>all relevant branches of the <see cref="repository"/></returns>
        private IEnumerable<Branch> RelevantBranches()
        {
            if (VCSFilter == null)
            {
                return repository.Branches;
            }
            return repository.Branches.Where(branch => VCSFilter.Matches(branch));
        }

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
        /// <returns>all distinct file paths</returns>
        public HashSet<string> AllFiles(CancellationToken token = default)
        {
            OpenRepository();
            HashSet<string> result = new();
            foreach (Branch branch in RelevantBranches())
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
        /// <param name="tree">the tree for which to retrieve the files</param>
        /// <param name="filter">the filter to be used to retrieve the files</param>
        /// <returns>all distinct file paths</returns>
        /// <exception cref="ArgumentNullException">thrown if <paramref name="tree"/> is null</exception>
        internal HashSet<string> AllFiles(LibGit2Sharp.Tree tree, CancellationToken token = default)
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
        /// <returns>A collection of file paths representing all files in the specified commit.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="commitID"/> is null, empty,
        /// or consists only of whitespace.</exception>
        internal HashSet<string> AllFiles(string commitID, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(commitID))
            {
                throw new ArgumentNullException(nameof(commitID), "Commit ID must neither be null nor empty.");
            }
            OpenRepository();
            return AllFiles(GetCheckedCommit(commitID).Tree, token);
        }

        /// <summary>
        /// Adds the distinct filenames in the given <paramref name="tree"/> passing
        /// the criteria <see cref="Filter.RepositoryPaths"/> and <see cref="Filter.Matcher"/>
        /// of the given <paramref name="filter"/>.
        ///
        /// If <see cref="Filter.RepositoryPaths"/> is null or empty, all files in the entire
        /// <paramref name="tree"/> are retrieved. Otherwise, only the files in the subtrees
        /// denoted by <see cref="Filter.RepositoryPaths"/> are retrieved. In case a path
        /// does not exist in the <paramref name="tree"/>, it is ignored.
        /// </summary>
        /// <param name="tree">the tree for which to retrieve the files</param>
        /// <param name="paths">where the passing files are to be added</param>
        private void AllFiles(LibGit2Sharp.Tree tree, HashSet<string> paths, CancellationToken token = default)
        {
            if (VCSFilter.RepositoryPaths == null || VCSFilter.RepositoryPaths.Length == 0)
            {
                // We collect all files in the entire tree.
                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException(token);
                }
                CollectFiles(tree, VCSFilter.Matcher, paths, token);
            }
            else
            {
                // We collect all files in the subtrees denoted by the repository paths.
                foreach (string repositoryPath in VCSFilter.RepositoryPaths)
                {
                    if (!string.IsNullOrWhiteSpace(repositoryPath))
                    {
                        if (token.IsCancellationRequested)
                        {
                            throw new OperationCanceledException(token);
                        }
                        LibGit2Sharp.Tree subtree = Find(tree, repositoryPath);
                        // It can happen that we do not find the subtree, because
                        // it may exist in some branches, but not in others.
                        if (subtree != null)
                        {
                            CollectFiles(subtree, VCSFilter.Matcher, paths, token);
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
        /// <param name="tree">the root tree</param>
        /// <param name="repositoryPath">relative path of descendants nested in <paramref name="tree"/></param>
        /// <returns>the subtree or null if it does not exist</returns>
        private static LibGit2Sharp.Tree Find(LibGit2Sharp.Tree tree, string repositoryPath)
        {
            TreeEntry result = tree[repositoryPath];
            if (result == null)
            {
                // Path does not exist.
                return null;
            }
            if (result.TargetType == TreeEntryTargetType.Tree)
            {
                return (LibGit2Sharp.Tree)result.Target;
            }
            else
            {
                return null;
            }
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
        /// <param name="matcher">the inclusion/exclusion path globbings</param>
        /// <param name="paths">the set of paths to which the paths are to be added</param>
        /// <returns>the set of distinct paths.</returns>
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

        #region Author Aliasing
        /// <summary>
        /// If <paramref name="consultAliasMap"/> is false, the original <paramref name="author"/>
        /// will be returned.
        /// Otherwise: Returns the alias of the specified <paramref name="author"/> if it exists in the alias mapping;
        /// or else returns the original <paramref name="author"/>.
        ///
        /// For two <see cref="FileAuthor"/>s to match, they must have same name and email address,
        /// where the string comparison for both facets is case-insensitive.
        /// </summary>
        /// <param name="author">The author whose alias is to be retrieved.</param>
        /// <param name="consultAliasMap">If <paramref name="authorAliasMap"/> should be consulted at all.</param>
        /// <param name="authorAliasMap">Where to to look up an alias. Can be null if <paramref name="consultAliasMap"/>
        /// is false.</param>
        /// <returns>A <see cref="FileAuthor"/> instance representing the alias of the author if found,
        /// or the original author if no alias exists or if <paramref name="consultAliasMap"/> is false.</returns>
        public static FileAuthor GetAuthorAliasIfExists(FileAuthor author, bool consultAliasMap, AuthorMapping authorAliasMap)
        {
            // If the author is not in the alias map or combining of author aliases is disabled, use the original author
            return ResolveAuthorAliasIfEnabled(author, consultAliasMap, authorAliasMap) ?? author;

            static FileAuthor ResolveAuthorAliasIfEnabled(FileAuthor author, bool combineSimilarAuthors, AuthorMapping authorAliasMap)
            {
                if (!combineSimilarAuthors)
                {
                    return null;
                }
                return authorAliasMap
                    .FirstOrDefault(alias => alias.Value.Any(x => String.Equals(x.Email, author.Email, StringComparison.OrdinalIgnoreCase)
                                                               && String.Equals(x.Name, author.Name, StringComparison.OrdinalIgnoreCase))).Key;
            }
        }
        #endregion Author Aliasing

        #region Config I/O

        /// <summary>
        /// Label for serializing the <see cref="RepositoryPath"/> field.
        /// </summary>
        private const string repositoryPathLabel = "RepositoryPath";

        /// <summary>
        /// Label for serializing the <see cref="VCSFilter"/> field.
        /// </summary>
        private const string vcsFilterLabel = "VCSFilter";

        /// <summary>
        /// Saves the attributes to the configuration file under the given <paramref name="label"/>
        /// using <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">used to write the attributes</param>
        /// <param name="label">the label under which the attributes are written</param>
        public void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            RepositoryPath.Save(writer, repositoryPathLabel);
            VCSFilter.Save(writer, vcsFilterLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Restores the marker values from the given <paramref name="attributes"/> looked up
        /// under the given <paramref name="label"/>
        /// </summary>
        public void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                RepositoryPath.Restore(values, repositoryPathLabel);
                VCSFilter.Restore(values, vcsFilterLabel);
            }
        }
    }
    #endregion
}
