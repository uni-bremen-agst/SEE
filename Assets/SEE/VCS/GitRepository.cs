using LibGit2Sharp;
using Microsoft.Extensions.FileSystemGlobbing;
using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public SEE.VCS.Filter VCSFilter = new();

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
        public GitRepository(DataPath repositoryPath, SEE.VCS.Filter filter)
        {
            RepositoryPath = repositoryPath ??
                throw new ArgumentNullException(nameof(repositoryPath), "Repository path must not be null.");
            VCSFilter = filter;
        }

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
        /// <exception cref="Exception">Thrown if an error occurs while fetching the remotes.</exception>"
        public void FetchRemotes()
        {
            OpenRepository();

            // Fetch all remote branches
            foreach (Remote remote in repository.Network.Remotes)
            {
                IEnumerable<string> refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                try
                {
                    Commands.Fetch(repository, remote.Name, refSpecs, null, "");
                }
                catch (LibGit2SharpException e)
                {
                    throw new Exception
                        ($"Error while running git fetch for repository path {RepositoryPath.Path} and remote name {remote.Name}: {e.Message}.\n");
                }
            }
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
        /// Returns the commit with the given <paramref name="commitId"/> from the repository.
        /// </summary>
        /// <param name="commitId">commit ID</param>
        /// <returns>the commit corresponding to <paramref name="commitId"/></returns>
        internal Commit GetCommit(string commitId)
        {
            OpenRepository();
            return repository.Lookup<Commit>(commitId);
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
            return repository.Diff.Compare<Patch>(oldCommit?.Tree, newCommit.Tree);
        }

        /// <summary>
        /// Returns all changed files in a commit.
        /// </summary>
        /// <param name="commit">The commit which files should be returned.</param>
        /// <param name="repo">The git repository in which the commit was made.</param>
        /// <returns>A list of all changed files (<see cref="PatchEntryChanges"/>).</returns>
        public Patch GetFileChanges(Commit commit)
        {
            OpenRepository();

            if (commit.Parents.Any())
            {
                return repository.Diff.Compare<Patch>(commit.Tree, commit.Parents.First().Tree);
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
        /// present in the repository at the given <paramref name="commitID"/>.
        /// </summary>
        /// <param name="repositoryFilePath">relative path of the file within the repository</param>
        /// <param name="commitID">commit ID</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string GetFileContent(string repositoryFilePath, string commitID)
        {
            OpenRepository();

            Blob blob = repository.Lookup<Blob>($"{commitID}:{repositoryFilePath}");

            if (blob != null)
            {
                return blob.GetContentText();
            }
            else
            {
                // Blob does not exist.
                throw new Exception($"File {repositoryFilePath} does not exist.\n");
            }
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
        /// Returns the hashes of all tip commits from all branches in this <see cref="GitRepository"/>.
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
        /// <param name="repository">the repository from which to retrieve the paths</param>
        /// <param name="filter">the filter to be used to retrieve the files</param>
        /// <returns>all distinct file paths</returns>
        public HashSet<string> AllFiles()
        {
            OpenRepository();
            HashSet<string> result = new();
            foreach (Branch branch in RelevantBranches())
            {
                AllFiles(branch.Tip.Tree, result);
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
        internal HashSet<string> AllFiles(LibGit2Sharp.Tree tree)
        {
            if (tree == null)
            {
                throw new ArgumentNullException(nameof(tree));
            }
            HashSet<string> result = new();
            AllFiles(tree, result);
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
        internal HashSet<string> AllFiles(string commitID)
        {
            if (string.IsNullOrWhiteSpace(commitID))
            {
                throw new ArgumentNullException(nameof(commitID), "Commit ID must neither be null nor empty.");
            }
            OpenRepository();
            return AllFiles(repository.Lookup<Commit>(commitID).Tree);
        }

        /// <summary>
        /// Adds the distinct filenames in the given <paramref name="tree"/> passing
        /// the criteria <see cref="Filter.RepositoryPaths"/> and <see cref="Filter.Matcher"/>
        /// of the given <paramref name="filter"/>.
        /// </summary>
        /// <param name="tree">the tree for which to retrieve the files</param>
        /// <param name="paths">where the passing files are to be added</param>
        /// <exception cref="Exception">thrown if attribute <see cref="Filter.RepositoryPaths"/> of
        /// <paramref name="filter"/> is different from null and at least one of the paths in
        /// <see cref="Filter.RepositoryPaths"/> does not exist in the <paramref name="tree"/>
        /// or does not denote a directory.</exception>
        private void AllFiles(LibGit2Sharp.Tree tree, HashSet<string> paths)
        {
            if (VCSFilter.RepositoryPaths == null || VCSFilter.RepositoryPaths.Length == 0)
            {
                CollectFiles(tree, VCSFilter.Matcher, paths);
            }
            else
            {
                foreach (string repositoryPath in VCSFilter.RepositoryPaths)
                {
                    if (!string.IsNullOrWhiteSpace(repositoryPath))
                    {
                        CollectFiles(Find(tree, repositoryPath), VCSFilter.Matcher, paths);
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
        /// <returns>the subtree</returns>
        /// <exception cref="Exception">thrown if <paramref name="repositoryPath"/> does not match
        /// any descendant in <paramref name="tree"/> or if <paramref name="repositoryPath"/>
        /// is not a tree (for instance, a blob).</exception>
        private static LibGit2Sharp.Tree Find(LibGit2Sharp.Tree tree, string repositoryPath)
        {
            TreeEntry result = tree[repositoryPath] ?? throw new Exception($"The path {repositoryPath} does not exist in the repository.");
            if (result.TargetType == TreeEntryTargetType.Tree)
            {
                return (LibGit2Sharp.Tree)result.Target;
            }
            else
            {
                throw new Exception($"The path {repositoryPath} is not a directory in the repository.");
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
        private static void CollectFiles(LibGit2Sharp.Tree tree, Matcher matcher, HashSet<string> paths)
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
                    CollectFiles(subtree, matcher, paths);
                }
            }
        }

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
