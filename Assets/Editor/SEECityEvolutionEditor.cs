#if UNITY_EDITOR

using SEE.DataModel.DG;
using SEE.Utils;
using SEE.Game.City;
using UnityEditor;
using UnityEngine;
using SEE.Game;

namespace SEEEditor
{
    /// <summary>
    /// A custom editor for instances of SEECityEvolution as an extension
    /// of the AbstractSEECityEditor.
    /// </summary>
    //[CustomEditor(typeof(SEECityEvolution))]
    [CanEditMultipleObjects]
    public class SEECityEvolutionEditor : StoredSEECityEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            SEECityEvolution cityEvolution = city as SEECityEvolution;
            EditorGUILayout.Separator();
            Attributes();
            EditorGUILayout.Separator();
            EvolutionMarkerAttributes(cityEvolution);
            EditorGUILayout.Separator();
            ShowNodeTypes(cityEvolution);
            Buttons(cityEvolution);
        }

        /// <summary>
        /// The loaded graph. It is the first one in the series of graphs.
        /// </summary>
        private Graph firstGraph = null;

        /// <summary>
        /// Whether the animation foldout should be expanded.
        /// </summary>
        private bool evolutionMarkerFoldOut = false;

        /// <summary>
        /// Creates the buttons for loading the first graph of the evolution series.
        /// </summary>
        private void Buttons(SEECityEvolution city)
        {
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
            graphRenderer.DrawGraph(graph, city.gameObject);
        }

        /// <summary>
        /// Renders the GUI for attributes of animations.
        /// </summary>
        private void EvolutionMarkerAttributes(SEECityEvolution city)
        {
            evolutionMarkerFoldOut = EditorGUILayout.Foldout(evolutionMarkerFoldOut, "Evolution Markers", true, EditorStyles.foldoutHeader);
            if (evolutionMarkerFoldOut)
            {
                city.MaxRevisionsToLoad = EditorGUILayout.IntField("Maximal revisions", city.MaxRevisionsToLoad);
                city.MarkerWidth = Mathf.Max(0, EditorGUILayout.FloatField("Width", city.MarkerWidth));
                city.MarkerHeight = Mathf.Max(0, EditorGUILayout.FloatField("Height", city.MarkerHeight));
                city.AdditionBeamColor = EditorGUILayout.ColorField("Color of additions", city.AdditionBeamColor);
                city.ChangeBeamColor = EditorGUILayout.ColorField("Color of changes", city.ChangeBeamColor);
                city.DeletionBeamColor = EditorGUILayout.ColorField("Color of deletions", city.DeletionBeamColor);
            }
        }

        /// <summary>
        /// Shows and sets the attributes of the <see cref="SEECityEvolution"/> managed here.
        /// This method should be overridden by subclasses if they have additional
        /// attributes to manage.
        /// </summary>
        protected void Attributes()
        {
            SEECityEvolution city = target as SEECityEvolution;
            city.GXLDirectory = DataPathEditor.GetDataPath("GXL directory", city.GXLDirectory, fileDialogue: false) as DirectoryPath;
        }
    }
}

#endif
