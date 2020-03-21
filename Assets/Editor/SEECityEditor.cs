using UnityEngine;
using UnityEditor;
using SEE.Game;

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


            city.gxlPath = EditorGUILayout.TextField("GXL file", city.gxlPath);
            city.csvPath = EditorGUILayout.TextField("CSV file", city.csvPath);
            EditorGUI.BeginDisabledGroup(!city.DynamicCallGraph);
            {
                const string dynFileLabel = "DYN file";
                if (city.DynamicCallGraph)
                {
                    city.dynPath = EditorGUILayout.TextField(dynFileLabel, city.dynPath);
                }
                else
                {
                    city.dynPath = EditorGUILayout.TextField(new GUIContent(dynFileLabel, "Enable 'Load dynamic call graph' to edit!"), city.dynPath);
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load City"))
            {
                SetUp(city);
            }
            if (GUILayout.Button("Delete City"))
            {
                Reset(city);
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Loads the graph data and metric data from disk, aggregates the metrics to
        /// inner nodes and renders the graph in the scene.
        /// </summary>
        /// <param name="city">the city to be set up</param>
        private void SetUp(SEECity city)
        {
            city.LoadData();
        }

        /// <summary>
        /// Deletes the underlying graph data of the given city.
        /// </summary>
        private void Reset(SEECity city)
        {
            city.DeleteGraph();   
        }
    }
}