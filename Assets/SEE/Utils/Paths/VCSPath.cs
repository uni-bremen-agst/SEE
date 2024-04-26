using System.Collections.Generic;
using SEE.UI.RuntimeConfigMenu;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SEE.Utils.Paths
{
    public class VCSPath
    {
        /// <summary>
        /// The path to the git repository.
        /// </summary>
        [ShowInInspector, Tooltip("Path to the git repository."), HideReferenceObjectPicker]
        public DirectoryPath RepositoryPath = new();

        /// <summary>
        /// The List of filetypes that get included/excluded.
        /// </summary>
        [OdinSerialize]
        [ShowInInspector, ListDrawerSettings(ShowItemCount = true),
         Tooltip("Paths and their inclusion/exclusion status."), RuntimeTab("Data"), HideReferenceObjectPicker]
        public Dictionary<string, bool> PathGlobbing = new()
        {
            { "", false }
        };
    }
}