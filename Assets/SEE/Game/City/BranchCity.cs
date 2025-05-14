using SEE.GraphProviders.VCS;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// This code city is used to visualize all changes which were
    /// made to a git repository across all branches from a given
    /// date until now.
    /// </summary>
    public class BranchCity : VCSCity, ISelfValidator
    {
        /// <summary>
        /// A date string in the <see cref="SEEDate.DateFormat"/> format.
        ///
        /// All commits from the most recent to the latest commit
        /// before the date are used for the analysis.
        /// </summary>
        [InspectorName("Date Limit (" + SEEDate.DateFormat + ")"),
         Tooltip("The beginning date after which commits should be considered (" + SEEDate.DateFormat + ")"),
         TabGroup(VCSFoldoutGroup), RuntimeTab(VCSFoldoutGroup)]
        public string Date = SEEDate.Now();

        /// <summary>
        /// If this is true, the authors of the commits with similar identities will be combined.
        /// This binding can either be done manually (by specifing the aliases in <see cref="AuthorAliasMap"/>)
        /// or automatically (by setting <see cref="AutoMapAuthors"/> to true).
        /// <seealso cref="AutoMapAuthors"/>
        /// <seealso cref="AuthorAliasMap"/>
        /// </summary>
        [Tooltip("If true, the authors of the commits with similar identities will be combined."),
         TabGroup(VCSFoldoutGroup),
         RuntimeTab(VCSFoldoutGroup)]
        public bool CombineAuthors;

        /// <summary>
        /// A dictionary mapping a commit author's identity (<see cref="FileAuthor"/>) to a list of aliases.
        /// This is used to manually group commit authors with similar identities together.
        /// The mapping enables aggregating commit data under a single normalized author identity.
        /// </summary>
        [NonSerialized, OdinSerialize,
         DictionaryDrawerSettings(
              DisplayMode = DictionaryDisplayOptions.CollapsedFoldout,
              KeyLabel = "Author", ValueLabel = "Aliases"),
         Tooltip("Author alias mapping."),
         ShowIf("CombineAuthors"),
         TabGroup(VCSFoldoutGroup),
         RuntimeTab(VCSFoldoutGroup),
         HideReferenceObjectPicker]
        public AuthorMapping AuthorAliasMap = new();

        /// <summary>
        /// Validates <see cref="Date"/>.
        /// </summary>
        /// <param name="result">where the error results are to be added (if any)</param>
        /// <remarks>Will be used by Odin Validator.</remarks>
        public void Validate(SelfValidationResult result)
        {
            if (!SEEDate.IsValid(Date))
            {
                result.AddError("Invalid date!");
            }
        }

        #region Config I/O

        /// <summary>
        /// Label of attribute <see cref="Date"/> in the configuration file.
        /// </summary>
        private const string dateLabel = "Date";

        /// <summary>
        /// Label of attribute <see cref="CombineAuthors"/> in the configuration file.
        /// </summary>
        private const string combineAuthorsLabel = "CombineAuthors";

        /// <summary>
        /// Label of attribute <see cref="CombineAuthors"/> in the configuration file.
        /// </summary>
        private const string authorAliasMapLabel = "AuthorAliasMap";

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            writer.Save(Date, dateLabel);
            writer.Save(CombineAuthors, combineAuthorsLabel);
            AuthorAliasMap.Save(writer, authorAliasMapLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            ConfigIO.Restore(attributes, dateLabel, ref Date);
            ConfigIO.Restore(attributes, combineAuthorsLabel, ref CombineAuthors);
            AuthorAliasMap.Restore(attributes, authorAliasMapLabel);
        }

        #endregion
    }
}
