#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Layout.EdgeLayouts;
using SEE.Layout.NodeLayouts;
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

        public override void OnInspectorGUI()
        {
            city = target as AbstractSEECity;

            GUILayout.BeginHorizontal();
            GUILayout.Label("LOD Culling");
            city.LODCulling = EditorGUILayout.Slider(city.LODCulling, 0.0f, 1.0f);
            //city.LODCulling = Mathf.Clamp(EditorGUILayout.FloatField("LOD Culling", city.LODCulling), 0, 1);
            GUILayout.EndHorizontal();

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
            
            EditorGUILayout.Separator();

            if (city.NodeLayout == NodeLayoutKind.CompoundSpringEmbedder)
            {
                if (city.CoseGraphSettings.rootDirs != null && city.CoseGraphSettings.rootDirs.Count > 0)
                {
                    GUILayout.Label("Choose sublayouts", EditorStyles.boldLabel);
                    List<Node> roots = city.CoseGraphSettings.rootDirs;

                    if (city.CoseGraphSettings.show.Count == 0)
                    {
                        foreach (Node root in roots)
                        {
                            TraverseThruNodesCounter(root);
                        }
                    }

                    if (city.CoseGraphSettings.showGraphListing)
                    {
                        List<NodeLayoutKind> parentNodeLayouts = new List<NodeLayoutKind>();
                        foreach (Node root in roots)
                        {
                            TraverseThruNodes(root, parentNodeLayouts);
                        }
                    }
                }
            }

            EditorGUIUtility.labelWidth = 150;
            GUILayout.Label("Measurements", EditorStyles.boldLabel);

            city.calculateMeasurements = EditorGUILayout.Toggle("Calculate measurements", city.calculateMeasurements);

            if (city.calculateMeasurements)
            {
                MeasurementsTable(city.Measurements);
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
        /// <returns></returns>
        protected DataPath GetDataPath(string label, DataPath dataPath, string extension = "", bool fileDialogue = true)
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
                string selectedPath = fileDialogue ?
                      EditorUtility.OpenFilePanel("Select file", dataPath.RootPath, extension)
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
                city.CoseGraphSettings.multiLevelScaling = EditorGUILayout.Toggle("MultiLevel-Scaling",
                    city.CoseGraphSettings.multiLevelScaling);
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
                city.CoseGraphSettings.useCalculationParameter =
                    EditorGUILayout.Toggle("Calc parameters automatically",
                        city.CoseGraphSettings.useCalculationParameter);
                city.CoseGraphSettings.useIterativeCalculation =
                    EditorGUILayout.Toggle("Find parameters iteratively",
                        city.CoseGraphSettings.useIterativeCalculation);
                if (city.CoseGraphSettings.useCalculationParameter ||
                    city.CoseGraphSettings.useIterativeCalculation)
                {
                    city.ZScoreScale = true;

                    city.CoseGraphSettings.multiLevelScaling = false;
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
                city.LeafObjects = (SEECity.LeafNodeKinds) EditorGUILayout.EnumPopup("Leaf nodes", city.LeafObjects);
                city.NodeLayout = (NodeLayoutKind) EditorGUILayout.EnumPopup("Node layout", city.NodeLayout);
                city.LayoutPath = GetDataPath("Layout file", city.LayoutPath, "");

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
                city.ShowLabelOnEyeGaze = EditorGUILayout.Toggle("Eye Gaze Labels (HoloLens)", city.ShowLabelOnEyeGaze);
                GUI.enabled = city.ShowLabelOnEyeGaze;
                city.EyeStareDelay = EditorGUILayout.Slider(
                    new GUIContent("Eye Stare Delay", 
                                   "The time in seconds after which staring at a node triggers its label to appear."), 
                    city.EyeStareDelay, 0, 20);
                GUI.enabled = true;
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
                city.InnerNodeShowLabel = EditorGUILayout.Toggle("Show labels", city.InnerNodeShowLabel);
                city.InnerNodeLabelDistance = EditorGUILayout.FloatField("Label distance", city.InnerNodeLabelDistance);
                city.InnerNodeLabelFontSize = EditorGUILayout.FloatField("Label font size", city.InnerNodeLabelFontSize);
                city.InnerNodeLabelAnimationDuration = EditorGUILayout.FloatField("Label animation duration (in seconds)",
                                                                                  city.InnerNodeLabelAnimationDuration);
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
                city.ShowLabel = EditorGUILayout.Toggle("Show labels", city.ShowLabel);
                city.LeafLabelDistance = EditorGUILayout.FloatField("Label distance", city.LeafLabelDistance);
                city.LeafLabelFontSize = EditorGUILayout.FloatField("Label font size", city.LeafLabelFontSize);
                city.LeafLabelAnimationDuration = EditorGUILayout.FloatField("Label animation duration (in seconds)",
                                                                                  city.LeafLabelAnimationDuration);
            }
        }

        /// <summary>
        /// Does the GUI layout for the measurements table
        /// </summary>
        /// <param name="measurements"></param>
        private void MeasurementsTable(SortedDictionary<string, string> measurements)
        {
            int i = 0;
            foreach (KeyValuePair<string, string> measure in measurements)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(measure.Key, GUILayout.Width(200));
                GUILayout.Label(measure.Value);
                GUILayout.EndHorizontal();

                if (i != measurements.Count - 1)
                {
                    HorizontalLine(Color.grey);
                }
                i++;
            }
        }

        /// <summary>
        /// Displays a horizontal line.
        /// </summary>
        /// <param name="color">the color for the line</param>
        private static void HorizontalLine(Color color)
        {
            Color c = GUI.color;
            GUI.color = color;
            GUILayout.Box(GUIContent.none, SetupHorizontalLine());
            GUI.color = c;
        }

        /// <summary>
        /// Returns a horizontal line.
        /// </summary>
        /// <returns>a horizontal line</returns>
        private static GUIStyle SetupHorizontalLine()
        {
            GUIStyle horizontalLine;
            horizontalLine = new GUIStyle();
            horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
            horizontalLine.margin = new RectOffset(0, 0, 4, 4);
            horizontalLine.fixedHeight = 1;
            return horizontalLine;
        }


        /// <summary>
        /// Traverses through the nodes and displays the sublayout hierarchy graph
        /// </summary>
        /// <param name="root"></param>
        private void TraverseThruNodes(Node root, List<NodeLayoutKind> parentNodeLayouts)
        {
            EditorGUIUtility.labelWidth = 80;
            if (root.Children() != null && !root.IsLeaf())
            {
                GUILayout.BeginHorizontal();

                GUILayout.Space(20 * root.Level);

                if (root.Children() != null && root.Children().Count > 0)
                {
                    bool allLeaves = true;
                    foreach (Node child in root.Children())
                    {
                        if (!child.IsLeaf())
                        {
                            allLeaves = false;
                        }
                    }

                    if (!allLeaves)
                    {
                        bool showPosition = EditorGUILayout.Foldout(city.CoseGraphSettings.show[root.ID], root.ID, true);
                        city.CoseGraphSettings.show[root.ID] = showPosition;

                        if (showPosition)
                        {
                            ShowCheckBox(root, false, parentNodeLayouts);

                            GUILayout.EndHorizontal();

                            if (root.Children() != null && root.Children().Count > 0)
                            {
                                foreach (Node child in root.Children())
                                {
                                    TraverseThruNodes(child, new List<NodeLayoutKind>(parentNodeLayouts));
                                }
                            }
                        }
                        else
                        {
                            GUILayout.EndHorizontal();
                        }
                    }
                    else
                    {
                        EditorGUIUtility.labelWidth = 80;
                        GUILayout.Label(root.ID, GUILayout.Width(120));
                        ShowCheckBox(root, true, parentNodeLayouts);
                        GUILayout.EndHorizontal();
                    }
                }
            }
        }

        /// <summary>
        /// displays the checkbox and dropdowns for each node
        /// </summary>
        /// <param name="root"></param>
        /// <param name="childrenAreLeaves"></param>
        private void ShowCheckBox(Node root, bool childrenAreLeaves, List<NodeLayoutKind> parentNodeLayouts)
        {
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayoutOption[] guiOptionsToggle = { GUILayout.ExpandWidth(false), GUILayout.Width(20) };
            bool toggle = EditorGUILayout.Toggle("", city.CoseGraphSettings.ListDirToggle[root.ID], guiOptionsToggle);
            city.CoseGraphSettings.ListDirToggle[root.ID] = toggle;
            //var checkedToggle = editorSettings.CoseGraphSettings.ListDirToggle.Where(predicate: kvp => kvp.Value);

            if (toggle)
            {
                ShowSublayoutEnum(city.CoseGraphSettings.DirNodeLayout[root.ID], root, childrenAreLeaves, parentNodeLayouts);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                ShowSublayoutEnum(NodeLayoutKind.CompoundSpringEmbedder, root, childrenAreLeaves, new List<NodeLayoutKind>());
                EditorGUI.EndDisabledGroup();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (toggle)
            {
                ShowInnerNodesEnum(city.CoseGraphSettings.DirNodeLayout[root.ID], root);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                ShowInnerNodesEnum(city.CoseGraphSettings.DirNodeLayout[root.ID], root);
                EditorGUI.EndDisabledGroup();
            }
        }

        /// <summary>
        /// Dropdown for the inner node Kinds 
        /// </summary>
        /// <param name="nodeLayout"></param>
        /// <param name="node"></param>
        private void ShowInnerNodesEnum(NodeLayoutKind nodeLayout, Node node)
        {
            GUILayoutOption[] guiOptions = { GUILayout.ExpandWidth(false), GUILayout.Width(200) };
            EditorGUIUtility.labelWidth = 80;
            EditorGUILayout.PrefixLabel("Inner nodes");
            Dictionary<AbstractSEECity.InnerNodeKinds, string> shapeKinds = nodeLayout.GetInnerNodeKinds().ToDictionary(kind => kind, kind => kind.ToString());

            if (shapeKinds.ContainsKey(city.CoseGraphSettings.DirShape[node.ID]))
            {
                city.CoseGraphSettings.DirShape[node.ID] = shapeKinds.ElementAt(EditorGUILayout.Popup(shapeKinds.Keys.ToList().IndexOf(city.CoseGraphSettings.DirShape[node.ID]), shapeKinds.Values.ToArray(), guiOptions)).Key;
            }
            else
            {
                city.CoseGraphSettings.DirShape[node.ID] = shapeKinds.ElementAt(EditorGUILayout.Popup(shapeKinds.Keys.ToList().IndexOf(shapeKinds.First().Key), shapeKinds.Values.ToArray(), guiOptions)).Key;
            }

            EditorGUIUtility.labelWidth = 150;
        }

        /// <summary>
        /// Dropdown for the sublayout kinds
        /// </summary>
        /// <param name="nodeLayout"></param>
        /// <param name="root"></param>
        /// <param name="childrenAreLeaves"></param>
        private void ShowSublayoutEnum(NodeLayoutKind nodeLayout, Node root, bool childrenAreLeaves, List<NodeLayoutKind> parentNodeLayouts)
        {
            GUILayoutOption[] guiOptions = { GUILayout.ExpandWidth(false), GUILayout.Width(200) };
            EditorGUIUtility.labelWidth = 80;
            EditorGUILayout.PrefixLabel("Sublayouts");
            Dictionary<NodeLayoutKind, string> subLayoutNodeLayouts = childrenAreLeaves ? city.SubLayoutsLeafNodes : city.SubLayoutsInnerNodes;

            foreach (NodeLayoutKind layout in parentNodeLayouts)
            {
                List<NodeLayoutKind> possible = layout.GetPossibleSublayouts();
                subLayoutNodeLayouts = subLayoutNodeLayouts.Where(elem => possible.Contains(elem.Key)).ToDictionary(x => x.Key, x => x.Value);
            }

            if (subLayoutNodeLayouts.ContainsKey(city.CoseGraphSettings.DirNodeLayout[root.ID]))
            {
                city.CoseGraphSettings.DirNodeLayout[root.ID] = subLayoutNodeLayouts.ElementAt(EditorGUILayout.Popup(subLayoutNodeLayouts.Keys.ToList().IndexOf(city.CoseGraphSettings.DirNodeLayout[root.ID]), subLayoutNodeLayouts.Values.ToArray(), guiOptions)).Key;

            }
            else
            {
                city.CoseGraphSettings.DirNodeLayout[root.ID] = subLayoutNodeLayouts.ElementAt(EditorGUILayout.Popup(subLayoutNodeLayouts.Keys.ToList().IndexOf(subLayoutNodeLayouts.First().Key), subLayoutNodeLayouts.Values.ToArray(), guiOptions)).Key;
            }

            parentNodeLayouts.Add(city.CoseGraphSettings.DirNodeLayout[root.ID]);
            EditorGUIUtility.labelWidth = 150;
        }

        /// <summary>
        /// traverses thru the nodes and adds them to a list
        /// </summary>
        /// <param name="root">the root node</param>
        private void TraverseThruNodesCounter(Node root)
        {
            if (root.Children() != null && !root.IsLeaf())
            {
                city.CoseGraphSettings.show.Add(root.ID, true);
                if (root.Children() != null || root.Children().Count > 0)
                {
                    foreach (Node child in root.Children())
                    {
                        TraverseThruNodesCounter(child);
                    }
                }
            }
        }
    }
}

#endif
