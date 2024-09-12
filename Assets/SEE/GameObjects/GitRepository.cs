using System;
using System.Collections.Generic;
using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Represents the needed information about a git repository for a <see cref="SEECityEvolution"/>
    /// </summary>
    [Serializable]
    public class GitRepository
    {
        /// <summary>
        /// Used for the tab name in runtime config menu
        /// </summary>
        protected const string GraphProviderFoldoutGroup = "Data";

        /// <summary>
        /// The path to the git repository.
        /// </summary>
        [ShowInInspector, Tooltip("Path to the git repository."), HideReferenceObjectPicker, RuntimeTab(GraphProviderFoldoutGroup)]
        public DataPath RepositoryPath = new();

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
