#if UNITY_EDITOR

using SEE.DataModel.DG;
using SEE.Game;
using UnityEditor;
using UnityEngine;
using SEE.Utils;

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
            city.maxRevisionsToLoad = EditorGUILayout.IntField("Maximal revisions", city.maxRevisionsToLoad);
            city.MarkerWidth = Mathf.Max(0, EditorGUILayout.FloatField("Width of markers", city.MarkerWidth));
            city.MarkerHeight = Mathf.Max(0, EditorGUILayout.FloatField("Height of markers", city.MarkerHeight));
            ShowNodeTypes(city);
            Buttons();
        }

        /// <summary>
        /// True if the underlying graph was successfully loaded.
        /// </summary>
        private bool isGraphLoaded = false;

        /// <summary>
        /// The name of the file where a node-selection will be saved
        /// </summary>
        public string savedProfileName;

        /// <summary>
        /// The loaded graph. It is the first one in the series of graphs.
        /// </summary>
        private Graph firstGraph = null;

        /// <summary>
        /// Creates the buttons for loading the first graph of the evolution series.
        /// </summary>
        protected void Buttons()
        {
            SerializedProperty pathPrefix = serializedObject.FindProperty("pathPrefix");

            SEECityEvolution city = target as SEECityEvolution;
            if (firstGraph == null && GUILayout.Button("Load First Graph"))
            {
                firstGraph = city.LoadFirstGraph();
                city.InspectSchema(firstGraph);
                isGraphLoaded = true;
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
            if (GUILayout.Button("Save Selection") && isGraphLoaded)
            {
                string exportPath = Filenames.OnCurrentPlatform(EditorUtility.OpenFolderPanel("Select saving directory", pathPrefix.stringValue, ""));
                city.SaveSelection(exportPath, savedProfileName);

            }
            savedProfileName = EditorGUILayout.TextField("Name of File: ", savedProfileName);
            if(GUILayout.Button("Load Selection"))
            {
                string importPath = Filenames.OnCurrentPlatform(EditorUtility.OpenFilePanel("Select loading directory", pathPrefix.stringValue, ""));
                 city.RestoreCity(importPath,city);
            }
        }

        /// <summary>
        /// Draws given <paramref name="graph"/> using the settings of <paramref name="city"/>.
        /// </summary>
        /// <param name="city">the city settings for drawing the graph</param>
        /// <param name="graph">the graph to be drawn</param>
        private void DrawGraph(AbstractSEECity city, Graph graph)
        {
            if (graph == null)
            {
                Debug.LogError("No graph loaded.\n");
            }
            else
            {
                graph = city.RelevantGraph(graph);
                GraphRenderer graphRenderer = new GraphRenderer(city, graph);
                // We assume here that this SEECity instance was added to a game object as
                // a component. The inherited attribute gameObject identifies this game object.
                graphRenderer.Draw(city.gameObject);
            }
        }
    }
}

#endif
