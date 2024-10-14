using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Common superclass of all graph providers which read their data from
    /// a single file.
    /// </summary>
    [Serializable]
    public abstract class FileBasedSingleGraphProvider : SingleGraphProvider
    {
        /// <summary>
        /// The path to the file containing the additional data to be added to a graph.
        /// </summary>
        [Tooltip("Path to the input file."), RuntimeTab(GraphProviderFoldoutGroup), HideReferenceObjectPicker]
        public DataPath Path = new();

        /// <summary>
        /// Checks whether the assumptions on <see cref="Path"/> and <paramref name="city"/> hold.
        /// If not, exceptions are thrown accordingly.
        /// </summary>
        /// <param name="city">to be checked</param>
        /// <exception cref="ArgumentException">thrown in case <see cref="Path"/>
        /// is undefined or does not exist or <paramref name="city"/> is null</exception>
        protected void CheckArguments(AbstractSEECity city)
        {
            if (string.IsNullOrWhiteSpace(Path?.Path))
            {
                throw new ArgumentException("Undefined data path.\n");
            }
            if (city == null)
            {
                throw new ArgumentException("The given city is null.\n");
            }
        }

        #region Config I/O

        /// <summary>
        /// The label for <see cref="Path"/> in the configuration file.
        /// </summary>
        private const string pathLabel = "path";

        protected override void SaveAttributes(ConfigWriter writer)
        {
            Path.Save(writer, pathLabel);
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            Path.Restore(attributes, pathLabel);
        }

        #endregion
    }
}
