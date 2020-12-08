#if UNITY_EDITOR

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

        /// <summary>
        /// An attribute which is true, if the Button "Load First Graph" was activated, else false. Important for saving json-node-type-collection.
        /// </summary>
        private bool loaded = false;

        public override void OnInspectorGUI()
        {
            
            base.OnInspectorGUI();
            SEECityEvolution city = target as SEECityEvolution;
            Attributes();
            city.maxRevisionsToLoad = EditorGUILayout.IntField("Maximal revisions", city.maxRevisionsToLoad);
            city.MarkerWidth = Mathf.Max(0, EditorGUILayout.FloatField("Width of markers", city.MarkerWidth));
            city.MarkerHeight = Mathf.Max(0, EditorGUILayout.FloatField("Height of markers", city.MarkerHeight));
            ShowNodeTypes(city);
            Buttons();
        }

        /// <summary>
        /// The loaded graph. It is the first one in the series of graphs.
        /// </summary>
        private Graph firstGraph = null;

        /// <summary>
        /// Creates the buttons for loading the first graph of the evolution series.
        /// </summary>
        private void Buttons()
        {
            SEECityEvolution city = target as SEECityEvolution;
          
            if (firstGraph == null && GUILayout.Button("Load First Graph"))
            {
                city.NodeTypesTemp = city.SelectedNodeTypes;
                firstGraph = city.LoadFirstGraph();
                city.InspectSchema(firstGraph);
                loaded = true;
            }
            if (firstGraph != null && GUILayout.Button("Draw"))
            {
                DrawGraph(city, firstGraph);
            }
            if (firstGraph != null && GUILayout.Button("Delete Graph"))
            {
                city.Reset(); // will not clear the selected node types
                city.SetNodeTypesTemp(city.NodeTypesTemp);
                firstGraph = null;
                loaded = false;
            }
            EditorGUILayout.BeginHorizontal();
            if (!loaded)
            {
                if (GUILayout.Button("Save Settings"))
                {
                    SaveCityInJSON(city);
                }
            }
            EditorGUILayout.EndHorizontal();
            if(GUILayout.Button("Load Settings"))
            {
                LoadCityFromJSON(city);
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
