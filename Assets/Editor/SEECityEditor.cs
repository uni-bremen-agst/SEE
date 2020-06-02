﻿using UnityEngine;
using UnityEditor;
using SEE.Game;
using System.Collections.Generic;

namespace SEEEditor
{
    /// <summary>
    /// A custom editor for instances of SEECity.
    /// </summary>
    [CustomEditor(typeof(SEECity))]
    [CanEditMultipleObjects]
    public class SEECityEditor : StoredSEECityEditor
    {
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
            SEECity city = target as SEECity;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load"))
            {
                Load(city);
            }
            if (GUILayout.Button("Draw"))
            {
                Draw(city);
            }
            if (GUILayout.Button("Re-Draw"))
            {
                ReDraw(city);
            }
            if (GUILayout.Button("Delete"))
            {
                Reset(city);
            }
            if (GUILayout.Button("Save Layout"))
            {
                SaveLayout(city);
            }
            EditorGUILayout.EndHorizontal();
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
        /// Saves the current layout of the given <paramref name="city"/>.
        /// </summary>
        private void SaveLayout(SEECity city)
        {
            city.SaveLayout();
        }        
    }
}