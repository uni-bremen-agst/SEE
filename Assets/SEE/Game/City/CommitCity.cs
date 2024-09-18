using System.Collections.Generic;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// Former DiffCity.
    ///
    /// This city can visualize the differences between two commits in a VCS system.
    /// Currently only git is supported here.
    /// </summary>
    public class CommitCity : VCSCity
    {
        /// <summary>
        /// The VCS identifier for the revision that constitutes the baseline of the
        /// comparison (the 'old' revision).
        /// </summary>
        [ShowInInspector, Tooltip("Old revision"),
         TabGroup(VCSFoldoutGroup), RuntimeTab(VCSFoldoutGroup)]
        public string OldRevision = string.Empty;

        /// <summary>
        /// The VCS identifier for the revision that constitutes the new revision
        /// against which the <see cref="OldRevision"/> is to be compared.
        /// </summary>
        [ShowInInspector, Tooltip("New revision"),
         TabGroup(VCSFoldoutGroup), RuntimeTab(VCSFoldoutGroup)]
        public string NewRevision = string.Empty;

        #region Config I/O

        /// <summary>
        /// Label of attribute <see cref="OldRevision"/> in the configuration file.
        /// </summary>
        private const string oldRevisionLabel = "OldRevision";

        /// <summary>
        /// Label of attribute <see cref="NewRevision"/> in the configuration file.
        /// </summary>
        private const string newRevisionLabel = "NewRevision";

        /// <summary>
        /// Saves the current city configuration to <paramref name="writer"/>
        /// </summary>
        /// <param name="writer">ConfigWriter to write the config to</param>
        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            writer.Save(OldRevision, oldRevisionLabel);
            writer.Save(NewRevision, newRevisionLabel);
        }

        /// <summary>
        /// Loads the configuration from the given <paramref name="attributes"/>
        /// </summary>
        /// <param name="attributes">The attributes to load</param>
        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            ConfigIO.Restore(attributes, oldRevisionLabel, ref OldRevision);
            ConfigIO.Restore(attributes, newRevisionLabel, ref NewRevision);
        }

        #endregion Config I/O
    }
}
