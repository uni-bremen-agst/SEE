using SEE.GraphProviders.VCS;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using SEE.VCS;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Abstract superclass for all <see cref="GraphProvider"/>s for Git repositories.
    /// </summary>
    internal abstract class GitGraphProvider : SingleGraphProvider
    {
        /// <summary>
        /// The git repository which should be analyzed.
        /// </summary>
        [OdinSerialize, ShowInInspector, SerializeReference, HideReferenceObjectPicker,
         Tooltip("The Git repository from which to retrieve the data."),
         ListDrawerSettings(DefaultExpandedState = true, ListElementLabelName = "Repository")]
        public GitRepository GitRepository = new();

        /// <summary>
        /// If true, the graph will be simplified by merging serial chains of nested
        /// directories into one.
        /// </summary>
        [Tooltip("If true, chains in the hierarchy will be simplified.")]
        public bool SimplifyGraph = false;

        /// <summary>
        /// If this is true, the authors of the commits with similar identities will be combined.
        /// This binding can either be done manually (by specifing the aliases in <see cref="AuthorAliasMap"/>)
        /// or automatically (by setting <see cref="AutoMapAuthors"/> to true).
        /// </summary>
        [Tooltip("If true, the authors of the commits with similar identities will be combined.")]
        public bool CombineAuthors = false;

        /// <summary>
        /// If for each file the co-changed files for the commit should be computed.
        /// Co-changed files are files that are changed in the same commit as other files.
        /// </summary>
        [Tooltip("If true, the co-changed files (files that are changed in the same commit as other files.) will be calculated for each file.")]
        public bool ComputeCoFileChanges = false;

        /// <summary>
        /// A dictionary mapping a commit author's identity (<see cref="FileAuthor"/>) to a list of aliases.
        /// This is used to manually group commit authors with similar identities together.
        /// The mapping enables aggregating commit data under a single normalized author identity.
        /// </summary>
        [NonSerialized, OdinSerialize,
         DictionaryDrawerSettings(
              DisplayMode = DictionaryDisplayOptions.CollapsedFoldout,
              KeyLabel = "Author", ValueLabel = "Aliases"),
         Tooltip("Author alias mapping. Can be used to specify a list of aliases of a given author."),
         ShowIf("CombineAuthors"),
         RuntimeShowIf("CombineAuthors"),
         HideReferenceObjectPicker]
        public AuthorMapping AuthorAliasMap = new();

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
        /// Label of attribute <see cref="CombineAuthors"/> in the configuration file.
        /// </summary>
        private const string combineAuthorsLabel = "CombineAuthors";

        /// <summary>
        /// Label of attribute <see cref="AuthorAliasMap"/> in the configuration file.
        /// </summary>
        private const string authorAliasMapLabel = "AuthorAliasMap";

        /// <summary>
        /// Label of attribute <see cref="ComputeCoFileChanges"/> in the configuration file.
        /// </summary>
        private const string computeCoFileChangesLabel = "ComputeCoFileChanges";

        /// <summary>
        /// Saves the attributes of this provider to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to save the attributes to.</param>
        protected override void SaveAttributes(ConfigWriter writer)
        {
            writer.Save(SimplifyGraph, simplifyGraphLabel);
            GitRepository.Save(writer, gitRepositoryLabel);
            writer.Save(CombineAuthors, combineAuthorsLabel);
            AuthorAliasMap.Save(writer, authorAliasMapLabel);
            writer.Save(ComputeCoFileChanges, computeCoFileChangesLabel);
        }

        /// <summary>
        /// Restores the attributes of this provider from <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">The attributes to restore from.</param>
        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            ConfigIO.Restore(attributes, simplifyGraphLabel, ref SimplifyGraph);
            GitRepository.Restore(attributes, gitRepositoryLabel);
            ConfigIO.Restore(attributes, combineAuthorsLabel, ref CombineAuthors);
            AuthorAliasMap.Restore(attributes, authorAliasMapLabel);
            ConfigIO.Restore(attributes, computeCoFileChangesLabel, ref ComputeCoFileChanges);
        }

        #endregion Config I/O
    }
}
