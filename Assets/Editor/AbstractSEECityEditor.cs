#if UNITY_EDITOR

using SEE.Game;
using SEE.Game.City;
using SEE.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

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
        /// Whether the foldout for the general attributes of the city should be expanded.
        /// </summary>
        private bool showGeneralAttributes = false;

        /// <summary>
        /// Whether the leaf node attribute foldout should be expanded.
        /// </summary>
        private bool showLeafAttributes = false;

        /// <summary>
        /// Whether the inner node attribute foldout should be expanded.
        /// </summary>
        private bool showInnerAttributes = false;

        /// <summary>
        /// Whether the erosion attribute foldout should be expanded.
        /// </summary>
        private bool showErosionAttributes = false;

        /// <summary>
        /// Whether the "nodes and node layout" foldout should be expanded.
        /// </summary>
        private bool showNodeLayout = false;

        /// <summary>
        /// Whether the "Edges and edge layout" foldout should be expanded.
        /// </summary>
        private bool showEdgeLayout = false;

        /// <summary>
        /// Whether the "Edge Selection" foldout should be expanded.
        /// </summary>
        private bool showEdgeSelection = false;

        /// <summary>
        /// Whether the "Compound spring embedder layout attributes" foldout should be expanded.
        /// </summary>
        private bool showCompoundSpringEmbedder = false;

        /// <summary>
        /// if true, listing of inner nodes with possible nodelayouts and inner node kinds is shown
        /// </summary>
        protected bool showGraphListing = false;

        public override void OnInspectorGUI()
        {
            city = target as AbstractSEECity;

            GlobalAttributes();

            EditorGUILayout.Separator();

            LeafNodeAttributes();

            EditorGUILayout.Separator();

            InnerNodeAttributes();

            EditorGUILayout.Separator();

            ErosionAttributes();

            EditorGUILayout.Separator();

            NodeLayout();

            EditorGUILayout.Separator();

            if (city.NodeLayoutSettings.Kind == NodeLayoutKind.CompoundSpringEmbedder)
            {
                CompoundSpringEmbedderAttributes();
                EditorGUILayout.Separator();
            }

            EdgeLayout();

            EditorGUILayout.Separator();

            EdgeSelection();

            // TODO: We may want to allow a user to define all edge types to be considered hierarchical.
            // TODO: We may want to allow a user to define which node attributes should be mapped onto which icons
        }

        /// <summary>
        /// Foldout for global settings (settings filename, LOD Culling, and the like) and buttons for
        /// loading and saving the settings.
        /// </summary>
        private void GlobalAttributes()
        {
            showGeneralAttributes = EditorGUILayout.Foldout(showGeneralAttributes, "General", true, EditorStyles.foldoutHeader);
            if (showGeneralAttributes)
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

                city.ZScoreScale = EditorGUILayout.Toggle("Z-score scaling", city.ZScoreScale);
                city.ScaleOnlyLeafMetrics = EditorGUILayout.Toggle("Scale only leaf metrics", city.ScaleOnlyLeafMetrics);

                GUILayout.BeginHorizontal();
                city.SolutionPath = DataPathEditor.GetDataPath("Solution file", city.SolutionPath);
                GUILayout.EndHorizontal();
            }

            // TODO: We may want to allow a user to define all edge types to be considered hierarchical.
            // TODO: We may want to allow a user to define which node attributes should be mapped onto which icons
        }

        /// <summary>
        /// Adds controls to set the attributes of <paramref name="dataPath"/>.
        /// </summary>
        /// <param name="label">a label in front of the controls shown in the inspector</param>
        /// <param name="dataPath">the path to be set here</param>
        /// <param name="extension">the extension the selected file should have (used as filter in file panel)</param>
        /// <param name="fileDialogue">if true, a file panel is opened; otherwise a directory panel</param>
        /// <returns>the resulting data specified as selected  by the user</returns>
        protected static DataPath GetDataPath(string label, DataPath dataPath, string extension = "", bool fileDialogue = true)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label);

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            dataPath.Root = (DataPath.RootKind)EditorGUILayout.EnumPopup(dataPath.Root, GUILayout.Width(100));
            if (dataPath.Root == DataPath.RootKind.Absolute)
            {
                dataPath.AbsolutePath = EditorGUILayout.TextField(dataPath.AbsolutePath);
            }
            else
            {
                dataPath.RelativePath = EditorGUILayout.TextField(dataPath.RelativePath);
            }
            if (GUILayout.Button("...", GUILayout.Width(20)))
            {
                string selectedPath = fileDialogue
                    ? EditorUtility.OpenFilePanel("Select file", dataPath.RootPath, extension)
                    : EditorUtility.OpenFolderPanel("Select directory", dataPath.RootPath, extension);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    dataPath.Set(selectedPath);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(dataPath.Path);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            return dataPath;
        }

        /// <summary>
        /// Renders the GUI for Compound spring embedder layout attributes.
        /// </summary>
        private void CompoundSpringEmbedderAttributes()
        {
            showCompoundSpringEmbedder = EditorGUILayout.Foldout(showCompoundSpringEmbedder, "Compound spring embedder layout attributes", true, EditorStyles.foldoutHeader);
            if (showCompoundSpringEmbedder)
            {
                GUILayout.Label("", EditorStyles.boldLabel);
                city.CoseGraphSettings.EdgeLength = EditorGUILayout.IntField("Edge length", city.CoseGraphSettings.EdgeLength);
                city.CoseGraphSettings.UseSmartIdealEdgeCalculation = EditorGUILayout.Toggle("Smart ideal edge length", city.CoseGraphSettings.UseSmartIdealEdgeCalculation);
                city.CoseGraphSettings.PerLevelIdealEdgeLengthFactor = EditorGUILayout.FloatField("Level edge length factor", city.CoseGraphSettings.PerLevelIdealEdgeLengthFactor);
                city.CoseGraphSettings.MultiLevelScaling = EditorGUILayout.Toggle("MultiLevel-Scaling", city.CoseGraphSettings.MultiLevelScaling);
                city.CoseGraphSettings.UseSmartMultilevelScaling = EditorGUILayout.Toggle("Smart multilevel-scaling", city.CoseGraphSettings.UseSmartMultilevelScaling);
                city.CoseGraphSettings.UseSmartRepulsionRangeCalculation = EditorGUILayout.Toggle("Smart repulsion range", city.CoseGraphSettings.UseSmartRepulsionRangeCalculation);
                city.CoseGraphSettings.RepulsionStrength = EditorGUILayout.FloatField("Repulsion strength", city.CoseGraphSettings.RepulsionStrength);
                city.CoseGraphSettings.GravityStrength = EditorGUILayout.FloatField("Gravity", city.CoseGraphSettings.GravityStrength);
                city.CoseGraphSettings.CompoundGravityStrength = EditorGUILayout.FloatField("Compound gravity", city.CoseGraphSettings.CompoundGravityStrength);
                city.CoseGraphSettings.UseCalculationParameter = EditorGUILayout.Toggle("Calc parameters automatically", city.CoseGraphSettings.UseCalculationParameter);
                city.CoseGraphSettings.UseIterativeCalculation = EditorGUILayout.Toggle("Find parameters iteratively", city.CoseGraphSettings.UseIterativeCalculation);
                if (city.CoseGraphSettings.UseCalculationParameter || city.CoseGraphSettings.UseIterativeCalculation)
                {
                    city.ZScoreScale = true;
                    city.ScaleOnlyLeafMetrics = true;

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
            showEdgeLayout = EditorGUILayout.Foldout(showEdgeLayout, "Edges layout", true, EditorStyles.foldoutHeader);
            if (showEdgeLayout)
            {
                EdgeLayoutAttributes settings = city.EdgeLayoutSettings;
                Assert.IsTrue(settings.GetType().IsClass); // Note: This may change to a struct, which may force us to use 'ref' above.

                settings.Kind = (EdgeLayoutKind)EditorGUILayout.EnumPopup("Edge layout", settings.Kind);
                settings.EdgeWidth = EditorGUILayout.FloatField("Edge width", settings.EdgeWidth);
                settings.EdgesAboveBlocks = EditorGUILayout.Toggle("Edges above blocks", settings.EdgesAboveBlocks);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Bundling tension");
                settings.Tension = EditorGUILayout.Slider(settings.Tension, 0.0f, 1.0f);
                EditorGUILayout.EndHorizontal();
                settings.RDP = EditorGUILayout.FloatField("RDP", settings.RDP);
            }
        }

        /// <summary>
        /// Renders the GUI for attributes for the edge selection.
        /// </summary>
        private void EdgeSelection()
        {
            showEdgeSelection = EditorGUILayout.Foldout(showEdgeSelection, "Edge selection", true, EditorStyles.foldoutHeader);
            if (showEdgeSelection)
            {
                EdgeSelectionAttributes settings = city.EdgeSelectionSettings;

                settings.TubularSegments = EditorGUILayout.IntField("Tubular Segments", settings.TubularSegments);
                settings.Radius = EditorGUILayout.FloatField("Radius", settings.Radius);
                settings.RadialSegments = EditorGUILayout.IntField("Radial Segments", settings.RadialSegments);
                settings.AreSelectable = EditorGUILayout.Toggle("Edges selectable", settings.AreSelectable);
            }
        }

        /// <summary>
        /// Renders the GUI for Nodes and node layout.
        /// </summary>
        private void NodeLayout()
        {
            showNodeLayout = EditorGUILayout.Foldout(showNodeLayout, "Nodes layout", true, EditorStyles.foldoutHeader);
            if (showNodeLayout)
            {
                NodeLayoutAttributes settings = city.NodeLayoutSettings;
                Assert.IsTrue(settings.GetType().IsClass); // Note: This may change to a struct, which may force us to use 'ref' above.

                settings.Kind = (NodeLayoutKind)EditorGUILayout.EnumPopup("Node layout", settings.Kind);
                settings.LayoutPath = GetDataPath("Layout file", city.NodeLayoutSettings.LayoutPath, Filenames.ExtensionWithoutPeriod(Filenames.GVLExtension));
            }
        }

        /// <summary>
        /// Renders the GUI for erosion attributes.
        /// </summary>
        private void ErosionAttributes()
        {
            showErosionAttributes = EditorGUILayout.Foldout(showErosionAttributes, "Attributes of software erosions", true, EditorStyles.foldoutHeader);
            if (showErosionAttributes)
            {
                EditorGUI.indentLevel++;

                ErosionAttributes settings = city.ErosionSettings;

                settings.ShowInnerErosions = EditorGUILayout.Toggle("Show inner erosions", settings.ShowInnerErosions);
                settings.ShowLeafErosions = EditorGUILayout.Toggle("Show leaf erosions", settings.ShowLeafErosions);
                settings.LoadDashboardMetrics = EditorGUILayout.Toggle("Load metrics from Dashboard", settings.LoadDashboardMetrics);
                if (settings.LoadDashboardMetrics)
                {
                    settings.IssuesAddedFromVersion = EditorGUILayout.TextField("Only issues added from version",
                                                                                settings.IssuesAddedFromVersion);
                    settings.OverrideMetrics = EditorGUILayout.Toggle("Override existing metrics", settings.OverrideMetrics);
                }
                settings.ErosionScalingFactor = EditorGUILayout.FloatField("Scaling factor of erosions",
                                                                           settings.ErosionScalingFactor);
                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Renders the GUI for inner node attributes.
        /// </summary>
        private void InnerNodeAttributes()
        {
            showInnerAttributes = EditorGUILayout.Foldout(showInnerAttributes, "Attributes of inner nodes", true, EditorStyles.foldoutHeader);
            if (showInnerAttributes)
            {
                EditorGUI.indentLevel++;

                InnerNodeAttributes settings = city.InnerNodeSettings;
                Assert.IsTrue(settings.GetType().IsClass); // Note: This may change to a struct, which may force us to use 'ref' above.

                settings.Kind = (InnerNodeKinds)EditorGUILayout.EnumPopup("Type", settings.Kind);
                settings.HeightMetric = EditorGUILayout.TextField("Height", settings.HeightMetric);
                settings.ColorMetric = EditorGUILayout.TextField("Color", settings.ColorMetric);
                settings.ColorRange.lower = EditorGUILayout.ColorField("Lower color", settings.ColorRange.lower);
                settings.ColorRange.upper = EditorGUILayout.ColorField("Upper color", settings.ColorRange.upper);
                settings.ColorRange.NumberOfColors = (uint)EditorGUILayout.IntSlider("# Colors", (int)settings.ColorRange.NumberOfColors, 1, 15);
                EditorGUI.EndDisabledGroup();
                LabelSettings(ref settings.LabelSettings);

                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Renders the GUI for attributes of leaf nodes.
        /// </summary>
        private void LeafNodeAttributes()
        {
            showLeafAttributes = EditorGUILayout.Foldout(showLeafAttributes, "Attributes of leaf nodes", true, EditorStyles.foldoutHeader);
            if (showLeafAttributes)
            {
                EditorGUI.indentLevel++;

                LeafNodeAttributes settings = city.LeafNodeSettings;
                Assert.IsTrue(settings.GetType().IsClass); // Note: This may change to a struct, which may force us to use 'ref' above.

                settings.Kind = (LeafNodeKinds)EditorGUILayout.EnumPopup("Type", settings.Kind);
                settings.WidthMetric = EditorGUILayout.TextField("Width", settings.WidthMetric);
                settings.HeightMetric = EditorGUILayout.TextField("Height", settings.HeightMetric);
                settings.DepthMetric = EditorGUILayout.TextField("Depth", settings.DepthMetric);
                settings.ColorMetric = EditorGUILayout.TextField("Color", settings.ColorMetric);
                settings.ColorRange.lower = EditorGUILayout.ColorField("Lower color", settings.ColorRange.lower);
                settings.ColorRange.upper = EditorGUILayout.ColorField("Upper color", settings.ColorRange.upper);
                settings.ColorRange.NumberOfColors = (uint)EditorGUILayout.IntSlider("# Colors", (int)settings.ColorRange.NumberOfColors, 1, 15);
                settings.MinimalBlockLength = Mathf.Max(0, EditorGUILayout.FloatField("Minimal lengths", settings.MinimalBlockLength));
                settings.MaximalBlockLength = EditorGUILayout.FloatField("Maximal lengths", settings.MaximalBlockLength);
                EditorGUI.EndDisabledGroup();
                LabelSettings(ref settings.LabelSettings);

                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Allows the user to set the attributes of <paramref name="labelAttributes"/>.
        /// </summary>
        /// <param name="labelAttributes">settings regarding the label above game nodes to be retrieved from the user</param>
        private static void LabelSettings(ref LabelAttributes labelAttributes)
        {
            labelAttributes.Show = EditorGUILayout.Toggle("Show labels", labelAttributes.Show);
            labelAttributes.Distance = EditorGUILayout.FloatField("Label distance", labelAttributes.Distance);
            labelAttributes.FontSize = EditorGUILayout.FloatField("Label font size", labelAttributes.FontSize);
            labelAttributes.AnimationDuration = EditorGUILayout.FloatField("Label animation duration (in seconds)", labelAttributes.AnimationDuration);
        }
    }
}

#endif
