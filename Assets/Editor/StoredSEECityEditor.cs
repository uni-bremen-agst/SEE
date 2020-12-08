#if UNITY_EDITOR

using SEE.Game;
using SEE.Utils;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using SEE.DataModel.DG;

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
        /// The name of the file where a city and the node-selection will be saved
        /// </summary>
        public string fileName = "CitySettings";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }

        /// <summary>
        /// Enables the user to select the node types to be visualized.
        /// </summary>
        /// <param name="city">city whose node types are to be selected</param>
        protected void ShowNodeTypes(AbstractSEECity city)
        {
            string sourcename = null;    
            if (city is SEECityEvolution) {
                
                SEECityEvolution c = (SEECityEvolution)city;
                Node n = c.singleRoot;
                sourcename = null;
                if (n != null)
                {
                    sourcename = n.Type;
                }
            }
            GUILayout.Label("Node types:", EditorStyles.boldLabel);
            // Make a copy to loop over the dictionary while making changes.
            Dictionary<string, bool> selection = new Dictionary<string, bool>(city.SelectedNodeTypes);
            int countSelected = 0;

              foreach (KeyValuePair<string, bool> entry in selection)
                {   //If selection contains the artifial ROOT as directory, we like to neglect that
                    // and do not show this to the user.
                     if (!(entry.Key.Equals(sourcename) && sourcename != null)) 
                    { 
                        city.SelectedNodeTypes[entry.Key] = EditorGUILayout.Toggle("  " + entry.Key, entry.Value);
                    if (city.SelectedNodeTypes[entry.Key])
                    {
                        countSelected++;
                    }
                }
            }

            if (city.CoseGraphSettings.loadedForNodeTypes.Count == 0)
            {
                city.CoseGraphSettings.showGraphListing = true;
                return;
            }

            bool allTypes = true;
            foreach (KeyValuePair<string, bool> kvp in city.CoseGraphSettings.loadedForNodeTypes)
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

            if (allTypes)
            {
                if (countSelected != city.CoseGraphSettings.loadedForNodeTypes.Count)
                {
                    allTypes = false;
                }
            }

            city.CoseGraphSettings.showGraphListing = allTypes;
        }

        /// <summary>
        /// Loads and restores the settings of a city from a json-file in the existing <paramref name="city"/>
        /// </summary>
        /// <param name="city">the city to be overwritten by the json-file</param>
        protected void LoadCityFromJSON(AbstractSEECity city)
        {
            string loadingPath = city.JsonDirectory;
            string importPath = Filenames.OnCurrentPlatform(EditorUtility.OpenFilePanel("Select loading directory", loadingPath, ""));
            if (!string.IsNullOrEmpty(importPath))
            {
               city.JsonDirectory = importPath;
               city.RestoreCity(importPath, city);
            }
        }

        /// <summary>
        /// Saves a city in a json-file. If there is already a file with the same same existing, it can be overwritten or canceled.
        /// </summary>
        /// <param name="city">the city whose to be saved in a json-file</param>
        protected void SaveCityInJSON(AbstractSEECity city)
        {
            string savingPath = city.JsonDirectory;
            string exportPath = Filenames.OnCurrentPlatform(EditorUtility.SaveFilePanel("Select saving directory", savingPath, fileName , "json"));
            if (!string.IsNullOrEmpty(exportPath))
            {
                city.JsonDirectory = exportPath;
                city.SaveSelection(exportPath);
            }
        }     
    }
}

#endif
