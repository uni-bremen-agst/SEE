using System;
using System.Collections.Generic;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SEE.GraphProviders
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class GitRepository
    {
        
        protected const string GraphProviderFoldoutGroup = "Data";
   
        /// <summary>
        /// The path to the git repository.
        /// </summary>
        [ShowInInspector, Tooltip("Path to the git repository."), HideReferenceObjectPicker,RuntimeTab(GraphProviderFoldoutGroup)]
        public DirectoryPath RepositoryPath = new();
        
        /// <summary>
        /// The List of filetypes that get included/excluded.
        /// </summary>
        [OdinSerialize]
        [ShowInInspector, ListDrawerSettings(ShowItemCount = true),
         Tooltip("Paths and their inclusion/exclusion status."), RuntimeTab(GraphProviderFoldoutGroup),
         HideReferenceObjectPicker]
        public Dictionary<string, bool> PathGlobbing = new()
        {
            { "", false }
        };
        
    }
}