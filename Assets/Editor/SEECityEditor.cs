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
            Attributes();
            Buttons();
        }

        /// <summary>
        /// Creates the buttons for loading and deleting a city.
        /// </summary>
        protected void Buttons()
        {
            SEECity city = target as SEECity;
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
        /// inner nodes and renders the graph in the scene.
        /// </summary>
        /// <param name="city">the city to be set up</param>
        private void SetUp(SEECity city)
        {
            city.LoadAndDrawGraph();
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