#if UNITY_EDITOR

using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
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
            SEECityEvolution cityEvolution = city as SEECityEvolution;

            EditorGUILayout.Separator();

            Attributes();

            EditorGUILayout.Separator();

            AnimationAttributes(cityEvolution);

            EditorGUILayout.Separator();

            MarkerAttributes(cityEvolution);

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
        private bool showAnimationFoldOut = false;

        /// <summary>
        /// Whether the marker attribute foldout should be expanded.
        /// </summary>
        private bool showMarkerAttributes = false;

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
            graphRenderer.DrawGraph(city.gameObject);
        }

        /// <summary>
        /// Renders the GUI for attributes of animations.
        /// </summary>
        private void AnimationAttributes(SEECityEvolution city)
        {
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
        }

        /// <summary>
        /// Renders the GUI for attributes of markers.
        /// </summary>
        private void MarkerAttributes(SEECityEvolution city)
        {
            showMarkerAttributes = EditorGUILayout.Foldout(showMarkerAttributes, "Attributes of markers", true, EditorStyles.foldoutHeader);
            if (showMarkerAttributes)
            {
                MarkerAttributes settings = city.MarkerSettings;

                settings.Kind = (MarkerKinds)EditorGUILayout.EnumPopup("Type", settings.Kind);

                EditorGUI.indentLevel++;

                SerializedProperty sections = serializedObject.FindProperty("MarkerSettings.MarkerSections");
                EditorGUILayout.PropertyField(sections, new GUIContent("Marker sections"), true);

                EditorGUI.indentLevel--;

            }
        }

        /// <summary>
        /// Shows and sets the attributes of the SEECity managed here.
        /// This method should be overridden by subclasses if they have additional
        /// attributes to manage.
        /// </summary>
        protected void Attributes()
        {
            SEECityEvolution city = target as SEECityEvolution;
            city.GXLDirectory = DataPathEditor.GetDataPath("GXL directory", city.GXLDirectory, fileDialogue: false);
        }
    }
}

#endif
