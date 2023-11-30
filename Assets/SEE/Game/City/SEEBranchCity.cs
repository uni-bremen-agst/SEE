using SEE.DataModel.DG;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SEE.Game.City
{
    public class SEEBranchCity : SEECity
    {
        /// <summary>
        /// The path to the Version Control System
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Path of VersionControlSystem"), TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup)]
        public BranchesLayoutAttributes VersionControlSystem = new();



        public void CompareBranches()
        {
            /// <summary>
            /// Check if paths to the version control system and the branches have been specified
            /// </summary>
            if (string.IsNullOrEmpty(VersionControlSystem.FilePath.Path) || string.IsNullOrEmpty(VersionControlSystem.BranchA) || string.IsNullOrEmpty(VersionControlSystem.BranchB))
            {
                Debug.LogError("Please specify the path to the version control system and the branches to be compared");
            }
            
        }



    }
}

