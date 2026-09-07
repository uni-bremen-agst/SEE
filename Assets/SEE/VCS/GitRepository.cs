using LibGit2Sharp;
using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.VCS
{
    /// <summary>
    /// Represents the needed information and configuration about a git repository for a <see cref="SEECityEvolution"/>.
    /// </summary>
    [Serializable]
    public class GitRepository
    {
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
        /// The access token for accessing the repository, if needed.
        /// </summary>
        /// <remarks>This attribute is not saved into the configuration file
        /// because of security reasons.</remarks>
        [Tooltip("Access token for accessing the repository, if needed. BE AWARE THAT THIS PROPERTY WILL BE SERIALIZED."),
         RuntimeTab(graphProviderFoldoutGroup)]
        public string AccessToken = "";

        /// <summary>
        /// Returns a string representation of the object, the repository path, more precisely.
        /// </summary>
        /// <returns>The repository path.</returns>
        public override string ToString()
        {
            return $"GitRepository: {RepositoryPath.Path}";
        }

        /// <summary>
        /// Used for the tab name in runtime config menu.
        /// </summary>
        private const string graphProviderFoldoutGroup = "Data";

        /// <summary>
        /// Constructor setting default values for the fields.
        /// </summary>
        public GitRepository()
        {
            // Intentionally left empty.
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="repositoryPath">Path to the repository.</param>
        /// <param name="filter">The filter to be used to retrieve the relevant files from the repository.</param>
        /// <param name="accessToken">The access token for this repository if needed.</param>
        public GitRepository(DataPath repositoryPath, Filter filter, string accessToken = null)
        {
            RepositoryPath = repositoryPath ??
                throw new ArgumentNullException(nameof(repositoryPath), "Repository path must not be null.");
            VCSFilter = filter;
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                this.AccessToken = accessToken;
            }
        }

        /// <summary>
        /// Clones the repository at <paramref name="url"/> into the <see cref="RepositoryPath"/>.
        /// </summary>
        /// <param name="url">URL for the repository.</param>
        /// <exception cref="Exception">Thrown in case of a Git cloning problem.</exception>
        public void Clone(string url)
        {
            try
            {
                CloneOptions options = new();

                options.FetchOptions.CredentialsProvider = (_url, _user, _types) =>
                        new UsernamePasswordCredentials
                        {
                            Username = AccessToken,
                            Password = string.Empty
                        };

                Debug.Log($"Cloned into {Repository.Clone(url, RepositoryPath.Path, options)}\n");
            }
            catch (LibGit2SharpException e)
            {
                throw new Exception
                       ($"Error while cloning repository from {url} into path {RepositoryPath.Path}: {e.Message}.\n");
            }
        }

        /// <summary>
        /// Opens a new <see cref="GitRepositorySession"/> based on the current configuration.
        /// </summary>
        /// <returns>A new disposable instance of <see cref="GitRepositorySession"/>.</returns>
        public GitRepositorySession OpenGitSession()
        {
            return new GitRepositorySession(this);
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
        /// <param name="writer">Used to write the attributes.</param>
        /// <param name="label">The label under which the attributes are written.</param>
        public void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            RepositoryPath.Save(writer, repositoryPathLabel);
            VCSFilter.Save(writer, vcsFilterLabel);
            // Note: We do not save the access token for security reasons.
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
