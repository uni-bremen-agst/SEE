using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using SEE.VCS;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Provides a graph for git repositories.
    /// </summary>
    internal abstract class GitGraphProvider : SingleGraphProvider
    {
        /// <summary>
        /// The git repository which should be analyzed.
        /// </summary>
        [OdinSerialize, ShowInInspector, SerializeReference, HideReferenceObjectPicker,
         Tooltip("The Git repository from which to retrieve the data."),
         ListDrawerSettings(DefaultExpandedState = true, ListElementLabelName = "Repository"),
         RuntimeTab("Data")]
        public GitRepository GitRepository = new();

        /// <summary>
        /// If true, the graph will be simplified by merging serial chains of nested
        /// directories into one.
        /// </summary>
        [Tooltip("If true, chains in the hierarchy will be simplified.")]
        public bool SimplifyGraph = false;

        #region Config I/O
        /// <summary>
        /// Label for serializing the <see cref="SimplifyGraph"/> field.
        /// </summary>
        private const string simplifyGraphLabel = "SimplifyGraph";

        /// <summary>
        /// Label for serializing the <see cref="GitRepository"/> field.
        /// </summary>
        private const string gitRepositoryLabel = "Repository";

        /// <summary>
        /// Saves the attributes of this provider to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to save the attributes to.</param>
        protected override void SaveAttributes(ConfigWriter writer)
        {
            writer.Save(SimplifyGraph, simplifyGraphLabel);
            GitRepository.Save(writer, gitRepositoryLabel);
        }

        /// <summary>
        /// Restores the attributes of this provider from <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">The attributes to restore from.</param>
        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            ConfigIO.Restore(attributes, simplifyGraphLabel, ref SimplifyGraph);
            GitRepository.Restore(attributes, gitRepositoryLabel);
        }

        #endregion Config I/O
    }
}
