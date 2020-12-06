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
    }
}

#endif
