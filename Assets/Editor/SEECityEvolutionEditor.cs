﻿#if UNITY_EDITOR

using SEE.DataModel.DG;
using SEE.Game;
using UnityEditor;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// A custom editor for instances of SEECityEvolution as an extension 
    /// of the AbstractSEECityEditor.
    /// </summary>
    [CustomEditor(typeof(SEECityEvolution))]
    [CanEditMultipleObjects]
    public class SEECityEvolutionEditor : StoredSEECityEditor
    {
        public override void OnInspectorGUI()
        {            
            base.OnInspectorGUI();
            SEECityEvolution city = target as SEECityEvolution;
            Attributes();
            showAnimationFoldOut = EditorGUILayout.Foldout(showAnimationFoldOut, "Animation", true, EditorStyles.foldoutHeader);
            if (showAnimationFoldOut)
            {
                city.MaxRevisionsToLoad = EditorGUILayout.IntField("Maximal revisions", city.MaxRevisionsToLoad);
                city.MarkerWidth = Mathf.Max(0, EditorGUILayout.FloatField("Width of markers", city.MarkerWidth));
                city.MarkerHeight = Mathf.Max(0, EditorGUILayout.FloatField("Height of markers", city.MarkerHeight));
                city.AdditionBeamColor = EditorGUILayout.ColorField("Color of addition markers", city.AdditionBeamColor);
                city.ChangeBeamColor = EditorGUILayout.ColorField("Color of change markers", city.ChangeBeamColor);
                city.DeletionBeamColor = EditorGUILayout.ColorField("Color of deletion markers", city.DeletionBeamColor);
            }
            ShowNodeTypes(city);
            Buttons();
        }

        /// <summary>
        /// The loaded graph. It is the first one in the series of graphs.
        /// </summary>
        private Graph firstGraph = null;

        /// <summary>
        /// Whether the animation foldout should be expanded.
        /// </summary>
        private bool showAnimationFoldOut = false;

        /// <summary>
        /// Creates the buttons for loading the first graph of the evolution series.
        /// </summary>
        private void Buttons()
        {
            SEECityEvolution city = target as SEECityEvolution;
          
            if (firstGraph == null && GUILayout.Button("Load First Graph"))
            {
                firstGraph = city.LoadFirstGraph();
                city.InspectSchema(firstGraph);
            }
            if (firstGraph != null && GUILayout.Button("Draw"))
            {
                DrawGraph(city, firstGraph);
            }
            if (firstGraph != null && GUILayout.Button("Delete Graph"))
            {
                city.Reset(); // will not clear the selected node types
                firstGraph = null;
            }
        }

        /// <summary>
        /// Draws given <paramref name="graph"/> using the settings of <paramref name="city"/>.
        /// 
        /// Precondition: graph != null.
        /// </summary>
        /// <param name="city">the city settings for drawing the graph</param>
        /// <param name="graph">the graph to be drawn</param>
        private void DrawGraph(AbstractSEECity city, Graph graph)
        {
            graph = city.RelevantGraph(graph);
            GraphRenderer graphRenderer = new GraphRenderer(city, graph);
            // We assume here that this SEECity instance was added to a game object as
            // a component. The inherited attribute gameObject identifies this game object.
            graphRenderer.Draw(city.gameObject);
        }

        /// <summary>
        /// Shows and sets the attributes of the SEECity managed here.
        /// This method should be overridden by subclasses if they have additional
        /// attributes to manage.
        /// </summary>
        protected void Attributes()
        {
            SEECityEvolution city = target as SEECityEvolution;
            city.GXLDirectory = GetDataPath("GXL directory", city.GXLDirectory, fileDialogue: false);
        }
    }
}

#endif
