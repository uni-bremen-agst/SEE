using System.Collections.Generic;
using SEE.GraphProviders;
using SEE.GraphProviders.VCS;
using SEE.UI.RuntimeConfigMenu;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// This code city is used to visualize all changes which were
    /// made to a git repository across all branches from a given
    /// date until now.
    /// </summary>
    public class BranchCity : VCSCity
    {
        /// <summary>
        /// A date string in the dd/mm/yyyy format.
        ///
        /// All commits from the most recent to the latest commit
        /// before the date are used for the analysis.
        /// </summary>
        [InspectorName("Date Limit (DD/MM/YYYY)"),
         Tooltip("The date until commits should be analyzed (DD/MM/YYYY)"),
         TabGroup(VCSFoldoutGroup), RuntimeTab(VCSFoldoutGroup)]
        public string Date = "";

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
        /// A dictionary mapping a commit author's identity (<see cref="GitFileAuthor"/>) to a list of aliases.
        /// This is used to manually group commit authors with similar identities together.
        /// The mapping enables aggregating commit data under a single normalized author identity.
        /// </summary>
        //[OdinSerialize]
        [ShowInInspector, ListDrawerSettings(ShowItemCount = true),
         Tooltip("Author alias mapping"),
         ShowIf("CombineAuthors"),
         TabGroup(VCSFoldoutGroup),
         RuntimeTab(VCSFoldoutGroup),
         HideReferenceObjectPicker]
        public GitAuthorMapping AuthorAliasMap = new();


    }
}
