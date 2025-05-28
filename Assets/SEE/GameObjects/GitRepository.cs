using LibGit2Sharp;
using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Represents the needed information about a git repository for a <see cref="SEECityEvolution"/>.
    /// </summary>
    [Serializable]
    public class GitRepository
    {
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
        /// Fetches all remote branches for the given repository path.
        /// </summary>
        /// <exception cref="Exception">Thrown if an error occurs while fetching the remotes.</exception>"
        public void FetchRemotes()
        {
            using Repository repository = new(RepositoryPath.Path);

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
        /// until today.
        /// </summary>
        /// <param name="repository">the repository from which to retrieve the commits</param>
        /// <param name="startDate">the date after which commits should be retrieved</param>
        /// <returns></returns>
        public IEnumerable<Commit> CommitsAfter(DateTime startDate)
        {
            using Repository repository = new(RepositoryPath.Path);

            IEnumerable<Commit> commitList = repository.Commits
                .QueryBy(new CommitFilter { IncludeReachableFrom = repository.Branches })
                // Commits after startDate
                .Where(commit =>
                    DateTime.Compare(commit.Author.When.Date, startDate) > 0)
                // Filter out merge commits.
                .Where(commit => commit.Parents.Count() <= 1);
            return commitList;
        }

        /// <summary>
        /// Yields the canonical name of all branches in <paramref name="repository"/>.
        /// </summary>
        /// <param name="repository"></param>
        /// <returns>canonical name of all branches</returns>
        public IEnumerable<string> AllBranches()
        {
            using Repository repository = new(RepositoryPath.Path);
            return repository.Branches.Select(b => b.CanonicalName);
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
                VCSFilter.Restore(attributes, vcsFilterLabel);
            }
        }
    }
    #endregion
}
