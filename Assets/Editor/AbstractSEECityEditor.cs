#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using SEE.Game;
using SEE.Layout.EdgeLayouts;
using SEE.Layout.NodeLayouts;
using SEE.Utils;
using UnityEditor;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// An abstract custom editor for instances of AbstractSEECity.
    /// </summary>
    [CustomEditor(typeof(AbstractSEECity))]
    [CanEditMultipleObjects]
    public class AbstractSEECityEditor : Editor
    {
        /// <summary>
        /// the city to display
        /// </summary>
        private AbstractSEECity city;

        /// <summary>
        /// Whether the foldout for the global attributes of the city should be expanded.
        /// </summary>
        private bool showGlobalAttributes = true;

        /// <summary>
        /// Whether the leaf node attribute foldout should be expanded.
        /// </summary>
        private bool showLeafAttributes = true;

        /// <summary>
        /// Whether the inner node attribute foldout should be expanded.
        /// </summary>
        private bool showInnerAttributes = true;

        /// <summary>
        /// Whether the "nodes and node layout" foldout should be expanded.
        /// </summary>
        private bool showNodeLayout = true;
        
        /// <summary>
        /// Whether the "Edges and edge layout" foldout should be expanded.
        /// </summary>
        private bool showEdgeLayout = true;

        /// <summary>
        /// Whether the "Compound spring embedder layout attributes" foldout should be expanded.
        /// </summary>
        private bool showCompoundSpringEmbedder = true;

        /// <summary>
        /// if true, listing of inner nodes with possible nodelayouts and inner node kinds is shown
        /// </summary>
        protected bool ShowGraphListing = true;

        public override void OnInspectorGUI()
        {
            city = target as AbstractSEECity;

            GlobalAttributes();

            EditorGUILayout.Separator();

            LeafNodeAttributes();

            EditorGUILayout.Separator();

            InnerNodeAttributes();

            EditorGUILayout.Separator();

            NodeLayout();

            EditorGUILayout.Separator();

            if (city.NodeLayout == NodeLayoutKind.CompoundSpringEmbedder)
            {
                CompoundSpringEmbedderAttributes();
                EditorGUILayout.Separator();
            }

            EdgeLayout();

            // TODO: We may want to allow a user to define all edge types to be considered hierarchical.
            // TODO: We may want to allow a user to define which node attributes should be mapped onto which icons
        }

        /// <summary>
        /// Foldout for global settings (settings filename, LOD Culling) and buttons for 
        /// loading and saving the settings.
        /// </summary>
        private void GlobalAttributes()
        {
            showGlobalAttributes = EditorGUILayout.Foldout(showGlobalAttributes,
                                                           "Global attributes", true, EditorStyles.foldoutHeader);
            if (showGlobalAttributes)
            {                
                city.CityPath = DataPathEditor.GetDataPath("Settings file", city.CityPath, Filenames.ExtensionWithoutPeriod(Filenames.ConfigExtension));
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Load", GUILayout.Width(50)))
                {
                    city.Load();
                }
                if (GUILayout.Button("Save", GUILayout.Width(50)))
                {
                    city.Save();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("LOD Culling");
                city.LODCulling = EditorGUILayout.Slider(city.LODCulling, 0.0f, 1.0f);
                GUILayout.EndHorizontal();
            }

            GUILayout.Label("Data", EditorStyles.boldLabel);

            // TODO: We may want to allow a user to define all edge types to be considered hierarchical.
            // TODO: We may want to allow a user to define which node attributes should be mapped onto which icons
        }

        /// <summary>
        /// Renders the GUI for Compound spring embedder layout attributes.
        /// </summary>
        private void CompoundSpringEmbedderAttributes()
        {
            showCompoundSpringEmbedder = EditorGUILayout.Foldout(showCompoundSpringEmbedder,
                "Compound spring embedder layout attributes", true, EditorStyles.foldoutHeader);
            if (showCompoundSpringEmbedder)
            {
                GUILayout.Label("", EditorStyles.boldLabel);
                city.CoseGraphSettings.EdgeLength =
                    EditorGUILayout.IntField("Edge length", city.CoseGraphSettings.EdgeLength);
                city.CoseGraphSettings.UseSmartIdealEdgeCalculation =
                    EditorGUILayout.Toggle("Smart ideal edge length",
                        city.CoseGraphSettings.UseSmartIdealEdgeCalculation);
                city.CoseGraphSettings.PerLevelIdealEdgeLengthFactor =
                    EditorGUILayout.FloatField("Level edge length factor",
                        city.CoseGraphSettings.PerLevelIdealEdgeLengthFactor);
                city.CoseGraphSettings.MultiLevelScaling = EditorGUILayout.Toggle("MultiLevel-Scaling",
                    city.CoseGraphSettings.MultiLevelScaling);
                city.CoseGraphSettings.UseSmartMultilevelScaling =
                    EditorGUILayout.Toggle("Smart multilevel-scaling",
                        city.CoseGraphSettings.UseSmartMultilevelScaling);
                city.CoseGraphSettings.UseSmartRepulsionRangeCalculation =
                    EditorGUILayout.Toggle("Smart repulsion range",
                        city.CoseGraphSettings.UseSmartRepulsionRangeCalculation);
                city.CoseGraphSettings.RepulsionStrength = EditorGUILayout.FloatField("Repulsion strength",
                    city.CoseGraphSettings.RepulsionStrength);
                city.CoseGraphSettings.GravityStrength =
                    EditorGUILayout.FloatField("Gravity", city.CoseGraphSettings.GravityStrength);
                city.CoseGraphSettings.CompoundGravityStrength = EditorGUILayout.FloatField("Compound gravity",
                    city.CoseGraphSettings.CompoundGravityStrength);
                /*city.CoseGraphSettings.useOptAlgorithm = EditorGUILayout.Toggle("Use Opt-Algorithm", city.CoseGraphSettings.useOptAlgorithm);
                    if (city.CoseGraphSettings.useOptAlgorithm)
                    {
                        //city.CoseGraphSettings.useCalculationParameter = false; 
                    }*/
                city.CoseGraphSettings.UseCalculationParameter =
                    EditorGUILayout.Toggle("Calc parameters automatically",
                        city.CoseGraphSettings.UseCalculationParameter);
                city.CoseGraphSettings.UseIterativeCalculation =
                    EditorGUILayout.Toggle("Find parameters iteratively",
                        city.CoseGraphSettings.UseIterativeCalculation);
                if (city.CoseGraphSettings.UseCalculationParameter ||
                    city.CoseGraphSettings.UseIterativeCalculation)
                {
                    city.ZScoreScale = true;

                    city.CoseGraphSettings.MultiLevelScaling = false;
                    city.CoseGraphSettings.UseSmartMultilevelScaling = false;
                    city.CoseGraphSettings.UseSmartIdealEdgeCalculation = false;
                    city.CoseGraphSettings.UseSmartRepulsionRangeCalculation = false;
                }
            }
        }

        /// <summary>
        /// Renders the GUI for Edges and edge layout.
        /// </summary>
        private void EdgeLayout()
        {
            showEdgeLayout = EditorGUILayout.Foldout(showEdgeLayout, "Edges and edge layout", true, EditorStyles.foldoutHeader);
            if (showEdgeLayout)
            {
                city.EdgeLayout = (EdgeLayoutKind) EditorGUILayout.EnumPopup("Edge layout", city.EdgeLayout);
                city.EdgeWidth = EditorGUILayout.FloatField("Edge width", city.EdgeWidth);
                city.EdgesAboveBlocks = EditorGUILayout.Toggle("Edges above blocks", city.EdgesAboveBlocks);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Bundling tension");
                city.Tension = EditorGUILayout.Slider(city.Tension, 0.0f, 1.0f);
                EditorGUILayout.EndHorizontal();
                city.RDP = EditorGUILayout.FloatField("RDP", city.RDP);
                city.TubularSegments = EditorGUILayout.IntField("Tubular Segments", city.TubularSegments);
                city.Radius = EditorGUILayout.FloatField("Radius", city.Radius);
                city.RadialSegments = EditorGUILayout.IntField("Radial Segments", city.RadialSegments);
                city.isEdgeSelectable = EditorGUILayout.Toggle("Edges selectable", city.isEdgeSelectable);
            }
        }

        /// <summary>
        /// Renders the GUI for Nodes and node layout.
        /// </summary>
        private void NodeLayout()
        {
            showNodeLayout = EditorGUILayout.Foldout(showNodeLayout, "Nodes and node layout", true, EditorStyles.foldoutHeader);
            if (showNodeLayout)
            {
                city.LeafObjects = (AbstractSEECity.LeafNodeKinds) EditorGUILayout.EnumPopup("Leaf nodes", city.LeafObjects);
                city.NodeLayout = (NodeLayoutKind) EditorGUILayout.EnumPopup("Node layout", city.NodeLayout);
                city.LayoutPath = DataPathEditor.GetDataPath("Layout file", city.LayoutPath, "gvl");

                GUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Inner nodes");
                Dictionary<AbstractSEECity.InnerNodeKinds, string> shapeKinds = city.NodeLayout.GetInnerNodeKinds()
                    .ToDictionary(kind => kind, kind => kind.ToString());

                if (shapeKinds.ContainsKey(city.InnerNodeObjects))
                {
                    city.InnerNodeObjects = shapeKinds.ElementAt(EditorGUILayout.Popup(
                        shapeKinds.Keys.ToList().IndexOf(city.InnerNodeObjects), shapeKinds.Values.ToArray())).Key;
                }
                else
                {
                    city.InnerNodeObjects = shapeKinds.ElementAt(EditorGUILayout.Popup(
                        shapeKinds.Keys.ToList().IndexOf(shapeKinds.First().Key), shapeKinds.Values.ToArray())).Key;
                }

                GUILayout.EndHorizontal();

                city.ZScoreScale = EditorGUILayout.Toggle("Z-score scaling", city.ZScoreScale);
                city.ShowErosions = EditorGUILayout.Toggle("Show erosions", city.ShowErosions);
                city.MaxErosionWidth = EditorGUILayout.FloatField("Max. width of erosion icon", city.MaxErosionWidth);
            }
        }

        /// <summary>
        /// Renders the GUI for inner node attributes.
        /// </summary>
        private void InnerNodeAttributes()
        {
            showInnerAttributes = EditorGUILayout.Foldout(showInnerAttributes, "Attributes of inner nodes", true,
                EditorStyles.foldoutHeader);
            if (showInnerAttributes)
            {
                city.InnerNodeHeightMetric = EditorGUILayout.TextField("Height", city.InnerNodeHeightMetric);
                city.InnerNodeStyleMetric = EditorGUILayout.TextField("Style", city.InnerNodeStyleMetric);
                city.InnerNodeColorRange.lower = EditorGUILayout.ColorField("Lower color", city.InnerNodeColorRange.lower);
                city.InnerNodeColorRange.upper = EditorGUILayout.ColorField("Upper color", city.InnerNodeColorRange.upper);
                city.InnerNodeColorRange.NumberOfColors =
                    (uint) EditorGUILayout.IntSlider("# Colors", (int) city.InnerNodeColorRange.NumberOfColors, 1, 15);
                LabelSettings(ref city.InnerNodeLabelSettings);
            }
        }

        /// <summary>
        /// Renders the GUI for attributes of leaf nodes.
        /// </summary>
        private void LeafNodeAttributes()
        {
            showLeafAttributes =
                EditorGUILayout.Foldout(showLeafAttributes, "Attributes of leaf nodes", true, EditorStyles.foldoutHeader);
            if (showLeafAttributes)
            {
                city.WidthMetric = EditorGUILayout.TextField("Width", city.WidthMetric);
                city.HeightMetric = EditorGUILayout.TextField("Height", city.HeightMetric);
                city.DepthMetric = EditorGUILayout.TextField("Depth", city.DepthMetric);
                city.LeafStyleMetric = EditorGUILayout.TextField("Style", city.LeafStyleMetric);
                city.LeafNodeColorRange.lower =
                    EditorGUILayout.ColorField("Lower color", city.LeafNodeColorRange.lower);
                city.LeafNodeColorRange.upper =
                    EditorGUILayout.ColorField("Upper color", city.LeafNodeColorRange.upper);
                city.LeafNodeColorRange.NumberOfColors = (uint) EditorGUILayout.IntSlider("# Colors",
                    (int) city.LeafNodeColorRange.NumberOfColors, 1, 15);
                LabelSettings(ref city.LeafLabelSettings);
            }
        }

        /// <summary>
        /// Allows the user to set the attributes of <paramref name="labelSettings"/>.
        /// </summary>
        /// <param name="labelSettings">settings to be retrieved from the user</param>
        private void LabelSettings(ref LabelSettings labelSettings)
        {
            labelSettings.Show = EditorGUILayout.Toggle("Show labels", labelSettings.Show);
            labelSettings.Distance = EditorGUILayout.FloatField("Label distance", labelSettings.Distance);
            labelSettings.FontSize = EditorGUILayout.FloatField("Label font size", labelSettings.FontSize);
            labelSettings.AnimationDuration = EditorGUILayout.FloatField("Label animation duration (in seconds)",
                                                                         labelSettings.AnimationDuration);
        }
    }
}

#endif
