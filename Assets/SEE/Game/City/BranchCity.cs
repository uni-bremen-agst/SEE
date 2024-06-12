using SEE.UI.RuntimeConfigMenu;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SEE.Game.City
{
    public class BranchCity : DiffCity
    {
        /// <summary>
        /// The date limit until commits should be analysed
        /// </summary>
        [ShowInInspector, InspectorName("Date Limit"),
         Tooltip("The date until commits should be analysed (DD/MM/YYYY)"),  TabGroup(VCSFoldoutGroup),RuntimeTab(VCSFoldoutGroup)]
        public string Date = "";
    }
}
