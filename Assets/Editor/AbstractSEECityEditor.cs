#if UNITY_EDITOR

using SEE.Game;
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

            if (city.nodeLayoutSettings.kind == NodeLayoutKind.CompoundSpringEmbedder)
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
            showGlobalAttributes = EditorGUILayout.Foldout(showGlobalAttributes, "Global attributes", true, EditorStyles.foldoutHeader);
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
                city.globalCityAttributes.lodCulling = EditorGUILayout.Slider(city.globalCityAttributes.lodCulling, 0.0f, 1.0f);
                GUILayout.EndHorizontal();
            }

            GUILayout.Label("Data", EditorStyles.boldLabel);

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
                city.coseGraphSettings.EdgeLength = EditorGUILayout.IntField("Edge length", city.coseGraphSettings.EdgeLength);
                city.coseGraphSettings.UseSmartIdealEdgeCalculation = EditorGUILayout.Toggle("Smart ideal edge length", city.coseGraphSettings.UseSmartIdealEdgeCalculation);
                city.coseGraphSettings.PerLevelIdealEdgeLengthFactor = EditorGUILayout.FloatField("Level edge length factor", city.coseGraphSettings.PerLevelIdealEdgeLengthFactor);
                city.coseGraphSettings.MultiLevelScaling = EditorGUILayout.Toggle("MultiLevel-Scaling", city.coseGraphSettings.MultiLevelScaling);
                city.coseGraphSettings.UseSmartMultilevelScaling = EditorGUILayout.Toggle("Smart multilevel-scaling", city.coseGraphSettings.UseSmartMultilevelScaling);
                city.coseGraphSettings.UseSmartRepulsionRangeCalculation = EditorGUILayout.Toggle("Smart repulsion range", city.coseGraphSettings.UseSmartRepulsionRangeCalculation);
                city.coseGraphSettings.RepulsionStrength = EditorGUILayout.FloatField("Repulsion strength", city.coseGraphSettings.RepulsionStrength);
                city.coseGraphSettings.GravityStrength = EditorGUILayout.FloatField("Gravity", city.coseGraphSettings.GravityStrength);
                city.coseGraphSettings.CompoundGravityStrength = EditorGUILayout.FloatField("Compound gravity", city.coseGraphSettings.CompoundGravityStrength);
                city.coseGraphSettings.UseCalculationParameter = EditorGUILayout.Toggle("Calc parameters automatically", city.coseGraphSettings.UseCalculationParameter);
                city.coseGraphSettings.UseIterativeCalculation = EditorGUILayout.Toggle("Find parameters iteratively", city.coseGraphSettings.UseIterativeCalculation);
                if (city.coseGraphSettings.UseCalculationParameter || city.coseGraphSettings.UseIterativeCalculation)
                {
                    city.nodeLayoutSettings.zScoreScale = true;
                    city.nodeLayoutSettings.ScaleOnlyLeafMetrics = true;

                    city.coseGraphSettings.MultiLevelScaling = false;
                    city.coseGraphSettings.UseSmartMultilevelScaling = false;
                    city.coseGraphSettings.UseSmartIdealEdgeCalculation = false;
                    city.coseGraphSettings.UseSmartRepulsionRangeCalculation = false;
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
                EdgeLayoutSettings settings = city.edgeLayoutSettings;
                Assert.IsTrue(settings.GetType().IsClass); // Note: This may change to a struct, which may force us to use 'ref' above.

                settings.kind = (EdgeLayoutKind)EditorGUILayout.EnumPopup("Edge layout", settings.kind);
                settings.edgeWidth = EditorGUILayout.FloatField("Edge width", settings.edgeWidth);
                settings.edgesAboveBlocks = EditorGUILayout.Toggle("Edges above blocks", settings.edgesAboveBlocks);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Bundling tension");
                settings.tension = EditorGUILayout.Slider(settings.tension, 0.0f, 1.0f);
                EditorGUILayout.EndHorizontal();
                settings.rdp = EditorGUILayout.FloatField("RDP", settings.rdp);
                settings.tubularSegments = EditorGUILayout.IntField("Tubular Segments", settings.tubularSegments);
                settings.radius = EditorGUILayout.FloatField("Radius", settings.radius);
                settings.radialSegments = EditorGUILayout.IntField("Radial Segments", settings.radialSegments);
                settings.isEdgeSelectable = EditorGUILayout.Toggle("Edges selectable", settings.isEdgeSelectable);
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
                NodeLayoutSettings settings = city.nodeLayoutSettings;
                Assert.IsTrue(settings.GetType().IsClass); // Note: This may change to a struct, which may force us to use 'ref' above.

                settings.kind = (NodeLayoutKind)EditorGUILayout.EnumPopup("Node layout", settings.kind);
                city.globalCityAttributes.layoutPath = GetDataPath("Layout file", city.globalCityAttributes.layoutPath, Filenames.ExtensionWithoutPeriod(Filenames.GVLExtension));
                settings.zScoreScale = EditorGUILayout.Toggle("Z-score scaling", settings.zScoreScale);
                settings.ScaleOnlyLeafMetrics = EditorGUILayout.Toggle("Scale only leaf metrics", settings.ScaleOnlyLeafMetrics);
                settings.showInnerErosions = EditorGUILayout.Toggle("Show inner erosions", settings.showInnerErosions);
                settings.showLeafErosions = EditorGUILayout.Toggle("Show leaf erosions", settings.showLeafErosions);
                settings.loadDashboardMetrics = EditorGUILayout.Toggle("Load metrics from Dashboard", settings.loadDashboardMetrics);
                if (settings.loadDashboardMetrics)
                {
                    settings.issuesAddedFromVersion = EditorGUILayout.TextField("Only issues added from version",
                                                                                settings.issuesAddedFromVersion);
                    settings.overrideMetrics = EditorGUILayout.Toggle("Override existing metrics", settings.overrideMetrics);
                }
                settings.erosionScalingFactor = EditorGUILayout.FloatField("Scaling factor of erosions",
                                                                           settings.erosionScalingFactor);
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

                InnerNodeAttributes settings = city.innerNodeAttributesPerKind;
                Assert.IsTrue(settings.GetType().IsClass); // Note: This may change to a struct, which may force us to use 'ref' above.

                settings.kind = (InnerNodeKinds)EditorGUILayout.EnumPopup("Type", settings.kind);
                settings.heightMetric = EditorGUILayout.TextField("Height", settings.heightMetric);
                settings.styleMetric = EditorGUILayout.TextField("Style", settings.styleMetric);
                settings.colorRange.lower = EditorGUILayout.ColorField("Lower color", settings.colorRange.lower);
                settings.colorRange.upper = EditorGUILayout.ColorField("Upper color", settings.colorRange.upper);
                settings.colorRange.NumberOfColors = (uint)EditorGUILayout.IntSlider("# Colors", (int)settings.colorRange.NumberOfColors, 1, 15);
                EditorGUI.EndDisabledGroup();
                LabelSettings(ref settings.labelSettings);

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

                LeafNodeAttributes settings = city.leafNodeAttributesPerKind;
                Assert.IsTrue(settings.GetType().IsClass); // Note: This may change to a struct, which may force us to use 'ref' above.

                settings.kind = (LeafNodeKinds)EditorGUILayout.EnumPopup("Type", settings.kind);
                settings.widthMetric = EditorGUILayout.TextField("Width", settings.widthMetric);
                settings.heightMetric = EditorGUILayout.TextField("Height", settings.heightMetric);
                settings.depthMetric = EditorGUILayout.TextField("Depth", settings.depthMetric);
                settings.styleMetric = EditorGUILayout.TextField("Style", settings.styleMetric);
                settings.colorRange.lower = EditorGUILayout.ColorField("Lower color", settings.colorRange.lower);
                settings.colorRange.upper = EditorGUILayout.ColorField("Upper color", settings.colorRange.upper);
                settings.colorRange.NumberOfColors = (uint)EditorGUILayout.IntSlider("# Colors", (int)settings.colorRange.NumberOfColors, 1, 15);
                EditorGUI.EndDisabledGroup();
                LabelSettings(ref settings.labelSettings);

                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Allows the user to set the attributes of <paramref name="labelSettings"/>.
        /// </summary>
        /// <param name="labelSettings">settings to be retrieved from the user</param>
        private static void LabelSettings(ref LabelSettings labelSettings)
        {
            labelSettings.Show = EditorGUILayout.Toggle("Show labels", labelSettings.Show);
            labelSettings.Distance = EditorGUILayout.FloatField("Label distance", labelSettings.Distance);
            labelSettings.FontSize = EditorGUILayout.FloatField("Label font size", labelSettings.FontSize);
            labelSettings.AnimationDuration = EditorGUILayout.FloatField("Label animation duration (in seconds)", labelSettings.AnimationDuration);
        }
    }
}

#endif
