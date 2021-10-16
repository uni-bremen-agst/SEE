#if UNITY_EDITOR

using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Game;
using UnityEditor;

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
        /// <summary>
        /// Whether the "Relevant node types" foldout should be expanded.
        /// </summary>
        private bool showNodeTypes = false;

        /// <summary>
        /// Enables the user to select the node types to be visualized.
        /// </summary>
        /// <param name="city">city whose node types are to be selected</param>
        protected void ShowNodeTypes(AbstractSEECity city)
        {
            showNodeTypes = EditorGUILayout.Foldout(showNodeTypes,
                                                    "Relevant node types", true, EditorStyles.foldoutHeader);
            if (showNodeTypes)
            {
                // Make a copy to loop over the dictionary while making changes.
                Dictionary<string, bool> selection = new Dictionary<string, bool>(city.SelectedNodeTypes);

                int countSelected = 0;
                foreach (KeyValuePair<string, bool> entry in selection)
                {
                    // If selection contains the artifial node type, we like to neglect that
                    // and do not show this to the user.
                    if (!(entry.Key.Equals(Graph.UnknownType)))
                    {
                        city.SelectedNodeTypes[entry.Key] = EditorGUILayout.Toggle("  " + entry.Key, entry.Value);
                        if (city.SelectedNodeTypes[entry.Key])
                        {
                            countSelected++;
                        }
                    }
                }

                if (city.CoseGraphSettings.LoadedForNodeTypes == null || city.CoseGraphSettings.LoadedForNodeTypes.Count == 0)
                {
                    showGraphListing = true;
                }
                else
                {
                    bool allTypes = true;
                    foreach (KeyValuePair<string, bool> kvp in city.CoseGraphSettings.LoadedForNodeTypes)
                    {
                        if (city.SelectedNodeTypes.ContainsKey(kvp.Key))
                        {
                            allTypes = allTypes && city.SelectedNodeTypes[kvp.Key];
                        }
                        else
                        {
                            allTypes = false;
                        }
                    }

                    if (allTypes && countSelected != city.CoseGraphSettings.LoadedForNodeTypes.Count)
                    {
                        allTypes = false;
                    }
                    showGraphListing = allTypes;
                }
            }
        }
    }
}

#endif
