using UnityEditor;
using SEE.Game;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using SEE.Utils;

namespace SEEEditor
{
    /// <summary>
    /// A custom editor for instances of AbstractSEECity showing graphs loaded from disk 
    /// as an extension of the AbstractSEECityEditor. A text field and directory chooser
    /// is added that allows a user to set the PathPrefix of the AbstractSEECity.
    /// </summary>
    [CustomEditor(typeof(AbstractSEECity))]
    [CanEditMultipleObjects]
    public abstract class StoredSEECityEditor : AbstractSEECityEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            AbstractSEECity city = target as AbstractSEECity;

            EditorGUILayout.BeginHorizontal();
            {
                city.PathPrefix = EditorGUILayout.TextField("Data path prefix", Filenames.OnCurrentPlatform(city.PathPrefix));
                if (GUILayout.Button("Select"))
                {
                    city.PathPrefix = Filenames.OnCurrentPlatform(EditorUtility.OpenFolderPanel("Select GXL graph data directory", city.PathPrefix, ""));
                    GUIUtility.ExitGUI(); // This call is to avoid the error "EndLayoutGroup: BeginLayoutGroup must be called first."
                }
                // city.PathPrefix must end with a directory separator
                if (city.PathPrefix.Length > 0 && city.PathPrefix[city.PathPrefix.Length - 1] != Path.DirectorySeparatorChar)
                {
                    city.PathPrefix = city.PathPrefix + Path.DirectorySeparatorChar;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Enables the user to select the node types to be visualized.
        /// </summary>
        /// <param name="city">city whose node types are to be selected</param>
        protected void ShowNodeTypes(AbstractSEECity city)
        {
            GUILayout.Label("Node types:", EditorStyles.boldLabel);
            // Make a copy to loop over the dictionary while making changes.
            Dictionary<string, bool> selection = new Dictionary<string, bool>(city.SelectedNodeTypes);
            foreach (var entry in selection)
            {
                city.SelectedNodeTypes[entry.Key] = EditorGUILayout.Toggle("  " + entry.Key, entry.Value);
            }
        }
    }
}
