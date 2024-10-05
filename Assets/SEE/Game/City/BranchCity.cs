using SEE.UI.RuntimeConfigMenu;
using Sirenix.OdinInspector;
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
    }
}
