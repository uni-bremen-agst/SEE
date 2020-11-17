﻿#if UNITY_EDITOR

using SEE.Game;
using UnityEditor;
using UnityEngine;
using SEE.Utils;

namespace SEEEditor
{
    /// <summary>
    /// A custom editor for instances of SEECity.
    /// </summary>
    [CustomEditor(typeof(SEECity))]
    [CanEditMultipleObjects]
    public class SEECityEditor : StoredSEECityEditor
    {

    public string fileName="Backup-V1";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            SEECity city = target as SEECity;
            Attributes();
            ShowNodeTypes(city);
            Buttons();
        }

        /// <summary>
        /// Creates the buttons for loading and deleting a city.
        /// </summary>
        protected void Buttons()
        {
       SerializedProperty pathPrefix = serializedObject.FindProperty("pathPrefix");
            SEECity city = target as SEECity;
            EditorGUILayout.BeginHorizontal();
            if (city.LoadedGraph == null && GUILayout.Button("Load Graph"))
            {
                Load(city);
            }
            if (city.LoadedGraph != null && GUILayout.Button("Delete Graph"))
            {
                Reset(city);
            }
            if (city.LoadedGraph != null && GUILayout.Button("Save Graph"))
            {
                Save(city);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (city.LoadedGraph != null && GUILayout.Button("Draw"))
            {
                Draw(city);
            }
            if (city.LoadedGraph != null && GUILayout.Button("Re-Draw"))
            {
                ReDraw(city);
            }

            if (city.LoadedGraph != null && GUILayout.Button("Save Layout"))
            {
                SaveLayout(city);
            }
            EditorGUILayout.EndHorizontal();
             EditorGUILayout.BeginHorizontal();
            fileName = EditorGUILayout.TextField("Name of File: ", fileName);
            if (GUILayout.Button("Save City"))
            {
                string exportPath = Filenames.OnCurrentPlatform(EditorUtility.OpenFolderPanel("Select saving directory", pathPrefix.stringValue, ""));
                city.SaveSelection(exportPath, fileName);
            }
            EditorGUILayout.EndHorizontal();
            if(GUILayout.Button("Load City from json"))
            {
                string importPath = Filenames.OnCurrentPlatform(EditorUtility.OpenFilePanel("Select loading directory", pathPrefix.stringValue, ""));
                 city.RestoreCity(importPath,city);
            }
        }

        /// <summary>
        /// Shows and sets the attributes of the SEECity managed here.
        /// This method should be overridden by subclasses if they have additional
        /// attributes to manage.
        /// </summary>
        protected virtual void Attributes()
        {
            SEECity city = target as SEECity;
            city.gxlPath = EditorGUILayout.TextField("GXL file", city.gxlPath);
            city.csvPath = EditorGUILayout.TextField("CSV file", city.csvPath);
        }

        /// <summary>
        /// Loads the graph data and metric data from disk, aggregates the metrics to
        /// inner nodes.
        /// </summary>
        /// <param name="city">the city to be set up</param>
        protected virtual void Load(SEECity city)
        {
            city.LoadData();
        }

        /// <summary>
        /// Renders the graph in the scene.
        /// </summary>
        /// <param name="city">the city to be set up</param>
        protected virtual void Draw(SEECity city)
        {
            city.DrawGraph();
        }

        /// <summary>
        /// Renders the graph in the scene once again without deleting the underlying graph loaded.
        /// </summary>
        /// <param name="city">the city to be re-drawn</param>
        protected virtual void ReDraw(SEECity city)
        {
            city.ReDrawGraph();
        }

        /// <summary>
        /// Deletes the underlying graph data of the given city and deletes all its game
        /// objects.
        /// </summary>
        private void Reset(SEECity city)
        {
            city.Reset();
        }

        /// <summary>
        /// Saves the underlying graph of the current city.
        /// </summary>
        private void Save(SEECity city)
        {
            city.SaveData();
        }

        /// <summary>
        /// Saves the current layout of the given <paramref name="city"/>.
        /// </summary>
        private void SaveLayout(SEECity city)
        {
            city.SaveLayout();
        }
    }
}

#endif
