using UnityEngine;
using UnityEditor;
using SEE.Game;
using System.IO;
using SEE;
using SEE.DataModel;
using System.Collections.Generic;
using System.Linq;
using static SEE.Game.AbstractSEECity;

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
        AbstractSEECity city;

        public override void OnInspectorGUI()
        {
            city = target as AbstractSEECity;

            GUILayout.Label("Graph", EditorStyles.boldLabel);
            city.origin = EditorGUILayout.Vector3Field("Origin", city.origin);

            GUILayout.Label("Attributes of leaf nodes", EditorStyles.boldLabel);
            city.WidthMetric = EditorGUILayout.TextField("Width", city.WidthMetric);
            city.HeightMetric = EditorGUILayout.TextField("Height", city.HeightMetric);
            city.DepthMetric = EditorGUILayout.TextField("Depth", city.DepthMetric);
            city.LeafStyleMetric = EditorGUILayout.TextField("Style", city.LeafStyleMetric);

            GUILayout.Label("Attributes of inner nodes", EditorStyles.boldLabel);
            city.InnerNodeStyleMetric = EditorGUILayout.TextField("Style", city.InnerNodeStyleMetric);

            GUILayout.Label("Nodes and Node Layout", EditorStyles.boldLabel);
            city.LeafObjects = (SEECity.LeafNodeKinds)EditorGUILayout.EnumPopup("Leaf nodes", city.LeafObjects);
            city.NodeLayout = (SEECity.NodeLayouts)EditorGUILayout.EnumPopup("Node layout", city.NodeLayout);

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Inner nodes");
            Dictionary<AbstractSEECity.InnerNodeKinds, string> shapeKinds = city.NodeLayout.GetInnerNodeKinds().ToDictionary(kind => kind, kind => kind.ToString());

            if (shapeKinds.ContainsKey(city.InnerNodeObjects))
            {
                city.InnerNodeObjects = shapeKinds.ElementAt(EditorGUILayout.Popup(shapeKinds.Keys.ToList().IndexOf(city.InnerNodeObjects), shapeKinds.Values.ToArray())).Key;
            }
            else
            {
                city.InnerNodeObjects = shapeKinds.ElementAt(EditorGUILayout.Popup(shapeKinds.Keys.ToList().IndexOf(shapeKinds.First().Key), shapeKinds.Values.ToArray())).Key;
            }
            GUILayout.EndHorizontal();

            city.ZScoreScale = EditorGUILayout.Toggle("Z-score scaling", city.ZScoreScale);
            city.ShowErosions = EditorGUILayout.Toggle("Show erosions", city.ShowErosions);

            if (city.NodeLayout == AbstractSEECity.NodeLayouts.CompoundSpringEmbedder)
            {
                GUILayout.Label("Compound spring embedder layout attributes", EditorStyles.boldLabel);
                city.CoseGraphSettings.EdgeLength = EditorGUILayout.IntField("Edge length", city.CoseGraphSettings.EdgeLength);
                city.CoseGraphSettings.UseSmartIdealEdgeCalculation = EditorGUILayout.Toggle("Smart ideal edge length", city.CoseGraphSettings.UseSmartIdealEdgeCalculation);
                city.CoseGraphSettings.PerLevelIdealEdgeLengthFactor = EditorGUILayout.FloatField("Level edge length factor", city.CoseGraphSettings.PerLevelIdealEdgeLengthFactor);
                city.CoseGraphSettings.multiLevelScaling = EditorGUILayout.Toggle("MultiLevelscaling", city.CoseGraphSettings.multiLevelScaling);
                city.CoseGraphSettings.UseSmartRepulsionRangeCalculation = EditorGUILayout.Toggle("Smart repulsion range", city.CoseGraphSettings.UseSmartRepulsionRangeCalculation);
                city.CoseGraphSettings.RepulsionStrength = EditorGUILayout.FloatField("Repulsion Strength", city.CoseGraphSettings.RepulsionStrength);
                city.CoseGraphSettings.GravityStrength = EditorGUILayout.FloatField("Gravity", city.CoseGraphSettings.GravityStrength);
                city.CoseGraphSettings.CompoundGravityStrength = EditorGUILayout.FloatField("Compound gravity", city.CoseGraphSettings.CompoundGravityStrength);
                city.CoseGraphSettings.useOptAlgorithm = EditorGUILayout.Toggle("Use Opt-Algorithm", city.CoseGraphSettings.useOptAlgorithm);
            }

            GUILayout.Label("Edges and Edge Layout", EditorStyles.boldLabel);
            city.EdgeLayout = (SEECity.EdgeLayouts)EditorGUILayout.EnumPopup("Edge layout", city.EdgeLayout);
            city.EdgeWidth = EditorGUILayout.FloatField("Edge width", city.EdgeWidth);
            city.EdgesAboveBlocks = EditorGUILayout.Toggle("Edges above blocks", city.EdgesAboveBlocks);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Bundling tension");
            city.Tension = EditorGUILayout.Slider(city.Tension, 0.0f, 1.0f);
            EditorGUILayout.EndHorizontal();
            city.RDP = EditorGUILayout.FloatField("RDP", city.RDP);

            if (city.NodeLayout == AbstractSEECity.NodeLayouts.CompoundSpringEmbedder)
            {
                if (city.CoseGraphSettings.rootDirs != null && city.CoseGraphSettings.rootDirs.Count > 0)
                {
                    GUILayout.Label("Choose Sublayouts", EditorStyles.boldLabel);
                    List<Node> roots = city.CoseGraphSettings.rootDirs;

                    if (city.CoseGraphSettings.show.Count == 0)
                    {
                        foreach (Node root in roots)
                        {
                            TraverseThruNodesCounter(root);
                        }
                        Debug.Log("show empty");
                    }

                    List<NodeLayouts> parentNodeLayouts = new List<NodeLayouts>();
                    foreach (Node root in roots)
                    {
                        TraverseThruNodes(root, parentNodeLayouts);
                    }
                }
            }

            EditorGUIUtility.labelWidth = 150;
            GUILayout.Label("Measurements", EditorStyles.boldLabel);

            city.calculateMeasurements = EditorGUILayout.Toggle("Calculate Measurements", city.calculateMeasurements);

            if (city.calculateMeasurements)
            {
                MeasurementsTable(city.Measurements);
            }


            GUILayout.Label("Data", EditorStyles.boldLabel);
            if (city.PathPrefix == null)
            {
                // Application.dataPath (used within ProjectPath()) must not be called in a 
                // constructor. That is why we need to set it here if it is not yet defined.
                city.PathPrefix = UnityProject.GetPath();
            }
            EditorGUILayout.BeginHorizontal();
            {
                city.PathPrefix = EditorGUILayout.TextField("Data path prefix", Filenames.OnCurrentPlatform(city.PathPrefix));
                //EditorGUILayout.LabelField("Data path prefix", GUILayout.Width(EditorGUIUtility.labelWidth));
                //EditorGUILayout.SelectableLabel(city.PathPrefix, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                if (GUILayout.Button("Select"))
                {
                    city.PathPrefix = Filenames.OnCurrentPlatform(EditorUtility.OpenFolderPanel("Select GXL graph data directory", city.PathPrefix, ""));
                }
                // city.PathPrefix must end with a directory separator
                if (city.PathPrefix.Length > 0 && city.PathPrefix[city.PathPrefix.Length - 1] != Path.DirectorySeparatorChar)
                {
                    city.PathPrefix = city.PathPrefix + Path.DirectorySeparatorChar;
                }
            }
            EditorGUILayout.EndHorizontal();
            // TODO: We may want to allow a user to define all edge types to be considered hierarchical.
            // TODO: We may want to allow a user to define which node attributes should be mapped onto which icons

            //groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
            //myBool = EditorGUILayout.Toggle("Toggle", myBool);
            //myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
            //myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
            //EditorGUILayout.EndToggleGroup();
        }

        /// <summary>
        /// does the gui layout for the measurements table
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
        /// Displays a horizontal line
        /// </summary>
        /// <param name="color">the color for the line</param>
        static void HorizontalLine(Color color)
        {
            var c = GUI.color;
            GUI.color = color;
            GUILayout.Box(GUIContent.none, SetupHorizontalLine());
            GUI.color = c;
        }

        /// <summary>
        /// returns a horizontal line
        /// </summary>
        /// <returns></returns>
        static GUIStyle SetupHorizontalLine()
        {
            GUIStyle horizontalLine;
            horizontalLine = new GUIStyle();
            horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
            horizontalLine.margin = new RectOffset(0, 0, 4, 4);
            horizontalLine.fixedHeight = 1;
            return horizontalLine;
        }


        /// <summary>
        /// traverses thru the nodes and displays the sublayout hierarchie graph
        /// </summary>
        /// <param name="root"></param>
        private void TraverseThruNodes(Node root, List<NodeLayouts> parentNodelayouts)
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
                        var showPosition = EditorGUILayout.Foldout(city.CoseGraphSettings.show[root.ID], root.ID, true);
                        city.CoseGraphSettings.show[root.ID] = showPosition;

                        if (showPosition)
                        {
                            ShowCheckBox(root, false, parentNodelayouts);

                            GUILayout.EndHorizontal();

                            if (root.Children() != null && root.Children().Count > 0)
                            {
                                foreach (Node child in root.Children())
                                {
                                    TraverseThruNodes(child, new List<NodeLayouts>(parentNodelayouts));
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
                        ShowCheckBox(root, true, parentNodelayouts);
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
        private void ShowCheckBox(Node root, bool childrenAreLeaves, List<NodeLayouts> parentNodeLayouts)
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
                ShowSublayoutEnum(AbstractSEECity.NodeLayouts.CompoundSpringEmbedder, root, childrenAreLeaves, new List<NodeLayouts>());
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
        /// Dropdown for the innernode Kinds 
        /// </summary>
        /// <param name="nodeLayout"></param>
        /// <param name="node"></param>
        private void ShowInnerNodesEnum(AbstractSEECity.NodeLayouts nodeLayout, Node node)
        {
            GUILayoutOption[] guiOptions = { GUILayout.ExpandWidth(false), GUILayout.Width(200) };
            EditorGUIUtility.labelWidth = 80; 
            EditorGUILayout.PrefixLabel("Inner nodes");
            Dictionary<AbstractSEECity.InnerNodeKinds, string> shapeKinds = nodeLayout.GetInnerNodeKinds().ToDictionary(kind => kind, kind => kind.ToString());

            if (shapeKinds.ContainsKey(city.CoseGraphSettings.DirShape[node.ID]))
            {
                city.CoseGraphSettings.DirShape[node.ID] = shapeKinds.ElementAt(EditorGUILayout.Popup(shapeKinds.Keys.ToList().IndexOf(city.CoseGraphSettings.DirShape[node.ID]), shapeKinds.Values.ToArray(), guiOptions)).Key;
            } else
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
        private void ShowSublayoutEnum(AbstractSEECity.NodeLayouts nodeLayout, Node root, bool childrenAreLeaves, List<NodeLayouts> parentNodeLayouts)
        {
            GUILayoutOption[] guiOptions = { GUILayout.ExpandWidth(false), GUILayout.Width(200) };
            EditorGUIUtility.labelWidth = 80;
            EditorGUILayout.PrefixLabel("Sublayouts");
            Dictionary<AbstractSEECity.NodeLayouts, string> subLayoutNodeLayouts = childrenAreLeaves ? city.SubLayoutsLeafNodes : city.SubLayoutsInnerNodes;

            foreach (NodeLayouts layout in parentNodeLayouts)
            {
                List<NodeLayouts> possible = layout.GetPossibleSublayouts();
                subLayoutNodeLayouts = subLayoutNodeLayouts.Where(elem => possible.Contains(elem.Key)).ToDictionary(x=> x.Key, x=> x.Value);
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