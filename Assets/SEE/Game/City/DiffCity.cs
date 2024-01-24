using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// A city for the differences between two revisions of a software
    /// stored in a version control system (VCS).
    /// </summary>
    public class DiffCity : SEECity
    {
        //----------------------------------------------------------------
        // Odin Inspector Attributes
        //----------------------------------------------------------------

        /// <summary>
        /// Name of the Inspector foldout group for the version control system (VCS) setttings.
        /// </summary>
        protected const string VCSFoldoutGroup = "VCS";

        /// <summary>
        /// The path to the VCS containing the two revisions to be compared.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("VCS path"), TabGroup(VCSFoldoutGroup), RuntimeTab(VCSFoldoutGroup)]
        public DirectoryPath VCSPath = new();

        /// <summary>
        /// The VCS identifier for the revision that constitutes the baseline of the
        /// comparison (the 'old' revision).
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Old revision"), TabGroup(VCSFoldoutGroup), RuntimeTab(VCSFoldoutGroup)]
        public string OldRevision = string.Empty;

        /// <summary>
        /// The VCS identifier for the revision that constitutes the new revision
        /// against which the <see cref="OldRevision"/> is to be compared.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("New revision"), TabGroup(VCSFoldoutGroup), RuntimeTab(VCSFoldoutGroup)]
        public string NewRevision = string.Empty;

        /// <summary>
        /// The Version control system identifier, to get the source code from both revision.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Version control system"), TabGroup(VCSFoldoutGroup), RuntimeTab(VCSFoldoutGroup)]
        public string VersionControlSystem = string.Empty;

        /// <summary>
        /// The repository path identifier, to get the source code from both revision.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Repository path"), TabGroup(VCSFoldoutGroup), RuntimeTab(VCSFoldoutGroup)]
        public string RepositoryPath = string.Empty;

        /// <summary>
        /// The old commit identifier, to get the source code from both revision.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Old commit identifier"), TabGroup(VCSFoldoutGroup), RuntimeTab(VCSFoldoutGroup)]
        public string OldCommitIdentifier = string.Empty;

        /// <summary>
        /// The VCS identifier for the revision that constitutes the new revision
        /// against which the <see cref="OldRevision"/> is to be compared.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("New commit identifier"), TabGroup(VCSFoldoutGroup), RuntimeTab(VCSFoldoutGroup)]
        public string NewCommitIdentifier = string.Empty;

        #region Config I/O
        //--------------------------------
        // Configuration file input/output
        //--------------------------------

        /// <summary>
        /// Label of attribute <see cref="VCSPath"/> in the configuration file.
        /// </summary>
        private const string vcsPathLabel = "VCSPath";

        /// <summary>
        /// Label of attribute <see cref="VersionControlSystem"/> in the configuration file.
        /// </summary>
        private const string versionControlSystemLabel = "VersionControlSystem";

        /// <summary>
        /// Label of attribute <see cref="RepositoryPath"/> in the configuration file.
        /// </summary>
        private const string repositoryPathLabel = "RepositoryPath";

        /// <summary>
        /// Label of attribute <see cref="OldCommitIdentifier"/> in the configuration file.
        /// </summary>
        private const string oldCommitIdentifierLabel = "OldCommitIdentifier";

        /// <summary>
        /// Label of attribute <see cref="NewCommitIdentifier"/> in the configuration file.
        /// </summary>
        private const string newCommitIdentifierLabel = "NewCommitIdentifier";

        /// <summary>
        /// Label of attribute <see cref="OldRevision"/> in the configuration file.
        /// </summary>
        private const string oldRevisionLabel = "OldRevision";

        /// <summary>
        /// Label of attribute <see cref="NewRevision"/> in the configuration file.
        /// </summary>
        private const string newRevisionLabel = "NewRevision";

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            VCSPath.Save(writer, vcsPathLabel);
            writer.Save(OldRevision, oldRevisionLabel);
            writer.Save(NewRevision, newRevisionLabel);
            writer.Save(VersionControlSystem, versionControlSystemLabel);
            writer.Save(RepositoryPath, repositoryPathLabel);
            writer.Save(OldCommitIdentifier, oldCommitIdentifierLabel);
            writer.Save(NewCommitIdentifier, newCommitIdentifierLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            VCSPath.Restore(attributes, vcsPathLabel);
            ConfigIO.Restore(attributes, oldRevisionLabel, ref OldRevision);
            ConfigIO.Restore(attributes, newRevisionLabel, ref NewRevision);
            ConfigIO.Restore(attributes, versionControlSystemLabel, ref VersionControlSystem);
            ConfigIO.Restore(attributes, repositoryPathLabel, ref RepositoryPath);
            ConfigIO.Restore(attributes, oldCommitIdentifierLabel, ref OldCommitIdentifier);
            ConfigIO.Restore(attributes, newCommitIdentifierLabel, ref NewCommitIdentifier);
        }

        #endregion Config I/O
    }
}
