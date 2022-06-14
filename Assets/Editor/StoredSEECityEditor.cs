#if UNITY_EDITOR

using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Game.City;
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
                                                    "Node types", true, EditorStyles.foldoutHeader);
            if (showNodeTypes)
            {
                // Make a copy to loop over the dictionary while making changes.
                Dictionary<string, VisualNodeAttributes> selection = new Dictionary<string, VisualNodeAttributes>(city.NodeTypes);

                int countSelected = 0;
                foreach (KeyValuePair<string, VisualNodeAttributes> entry in selection)
                {
                    // If selection contains the artifial node type, we like to neglect that
                    // and do not show this to the user.
                    string nodeType = entry.Key;
                    if (nodeType != Graph.UnknownType)
                    {
                        city.NodeTypes[nodeType].IsRelevant = EditorGUILayout.Toggle("  " + nodeType, entry.Value.IsRelevant);
                        if (city.NodeTypes[nodeType].IsRelevant)
                        {
                            countSelected++;
                            NodeAttributes(entry.Value);
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
                        if (city.NodeTypes.ContainsKey(kvp.Key))
                        {
                            allTypes = allTypes && city.NodeTypes[kvp.Key].IsRelevant;
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
