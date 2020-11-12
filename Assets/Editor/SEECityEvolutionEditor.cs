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
            if (isGraphLoaded)
            {
                ShowNodeTypes(city);
            }
            Buttons();
        }

        /// <summary>
        /// True if the underlying graph was successfully loaded.
        /// </summary>
        private bool isGraphLoaded = false;

        /// <summary>
        /// The loaded graph. The value is different from null only if isGraphLoaded is true.
        /// </summary>
        private Graph graph = null;

        /// <summary>
        /// Creates the buttons for loading the first graph of the evolution series.
        /// </summary>
        protected void Buttons()
        {
            SEECityEvolution city = target as SEECityEvolution;
            if (GUILayout.Button("Load First Graph") && !isGraphLoaded)
            {
                graph = city.LoadFirstGraph();
                city.InspectSchema(graph);
                isGraphLoaded = true;
            }
            if (GUILayout.Button("Draw"))
            {
                if (isGraphLoaded)
                {
                    if (graph != null)
                    {
                        graph = city.LoadFirstGraph();
                        DrawGraph(city, graph);
                    }
                    else
                    {
                        Debug.LogError("No valid graph loaded.\n");
                    }
                }
                else
                {
                    Debug.LogError("No valid graph loaded.\n");
                }
            }
                if (GUILayout.Button("Delete") && isGraphLoaded)
                {
                    isGraphLoaded = false;
                    city.Reset();
                }                    
        }

        private void DrawGraph(AbstractSEECity city, Graph graph)
        {                  
                GraphRenderer graphRenderer = new GraphRenderer(city, graph);
                // We assume here that this SEECity instance was added to a game object as
                // a component. The inherited attribute gameObject identifies this game object.
                graphRenderer.Draw(city.gameObject);
                     
        }
    }
}

#endif
