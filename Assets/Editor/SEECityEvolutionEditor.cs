#if UNITY_EDITOR

using UnityEditor;
using SEE.Game;
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
            ShowNodeTypes(city);
            Buttons();
        }

        /// <summary>
        /// Creates the buttons for loading the first graph of the evolution series.
        /// </summary>
        protected void Buttons()
        {
            SEECityEvolution city = target as SEECityEvolution;
            if (GUILayout.Button("Load First Graph"))
            {
                city.InspectSchema(city.LoadFirstGraph());
            }
        }
    }
}

#endif
