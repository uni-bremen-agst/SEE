using UnityEngine;
using UnityEditor;
using SEE.DataModel;
using SEE;
using SEE.Layout;
using System.Collections.Generic;
using UnityEngine.XR;
using System.Linq;

namespace SEEEditor
{
    /// <summary>
    /// An editor that allows an Unreal editor user to create a city.
    /// </summary>
    public class CityEditor : EditorWindow
    {
        [MenuItem("Window/City Editor")]
        // This method will be called when the user selects the menu item to create the window.
        // Such methods must be static and void. They can have any name.
        static void Init()
        {
            // We try to open the window by docking it next to the Inspector if possible.
            System.Type desiredDockNextTo = System.Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll");
            CityEditor window;
            if (desiredDockNextTo == null)
            {
                window = (CityEditor)EditorWindow.GetWindow(typeof(CityEditor), false, "City", true);
            }
            else
            {
                window = EditorWindow.GetWindow<CityEditor>("City", false, new System.Type[] { desiredDockNextTo });
            }
            window.Show();
        }

        // As to whether the optional settings for node and edge tags are to be enabled.
        //private bool tagGroupEnabled = false;

        /// <summary>
        /// The graph that is visualized in the scene.
        /// </summary>
        private Graph graph = null;

        /// <summary>
        /// The user settings.
        /// </summary>
        private SEE.GraphSettings editorSettings = new SEE.GraphSettings();

        /// <summary>
        /// Whether VR mode is to be activated for the game.
        /// </summary>
        private bool VRenabled = false;

        /// <summary>
        /// Returns all main cameras (name equals "Main Camera" and tag equals "MainCamera"
        /// no matter whether they are activated or not.
        /// </summary>
        /// <returns></returns>
        private static IList<GameObject> AllMainCameras()
        {
            IList<GameObject> result = new List<GameObject>();
            // FindObjectsOfTypeAll returns also inactive game objects
            foreach (GameObject o in Resources.FindObjectsOfTypeAll(typeof(UnityEngine.GameObject)))
            {
                if (o.name == "Main Camera" && o.tag == "MainCamera")
                {
                    result.Add(o);
                }
            }
            return result;
        }

        /// <summary>
        /// Activates the leap rig for VR and deactivates the main camera for the monitor mode,
        /// if enableVR is true. If enableVR is false, the leap rig for VR is deactivated and 
        /// the main camera for the monitor mode is deactivated.
        /// </summary>
        /// <param name="enableVR">whether the leap rig for the VR mode should be activated</param>
        private static void EnableVR(bool enableVR)
        {
            XRSettings.enabled = enableVR;
            // If VR is to be enabled, we need to disable the main camera for monitor games
            // and active the Leap Rig. If instead VR is to be disabled, we need to disable 
            // the Leap Rig and activate the main camera.
            foreach (GameObject camera in AllMainCameras())
            {
                if (camera.transform.parent == null)
                {
                    // The camera for the monitor game is at top-level.
                    camera.SetActive(!enableVR);
                    //Debug.LogFormat("main camera at top level: {0}\n", !enableVR);
                }
                else if (camera.transform.parent.name == "Leap Rig")
                {
                    // The camera of the Leap Rig is nested in a game object named accordingly.
                    // We set the Leap Rig itself in which the found camera is directly nested.
                    camera.transform.parent.gameObject.SetActive(enableVR);
                    //Debug.LogFormat("Leap rig camera: {0}\n", enableVR);
                }
            }
            EnableCanvas(enableVR);
        }

        /// <summary>
        /// In VR mode, the UI canvas must be disabled because of performance reasons and
        /// it is not used anyhow. The canvas is recognized by its name "Canvas" and the
        /// fact that it is expected to be at top level of the game object hierarchy.
        /// </summary>
        /// <param name="enableVR">whether to disable the canvas</param>
        private static void EnableCanvas(bool enableVR)
        {
            // FindObjectsOfTypeAll returns also inactive game objects
            foreach (GameObject o in Resources.FindObjectsOfTypeAll(typeof(UnityEngine.GameObject)))
            {
                if (o.name == "Canvas" && o.transform.parent == null)
                {
                    o.SetActive(! enableVR);
                }
            }
        }

        /// <summary>
        /// Position of the scrollview
        /// </summary>
        Vector2 scrollPos;

        /// <summary>
        /// Creates a new window offering the city editor commands.
        /// </summary>
        void OnGUI()
        {
            // Important note: OnGUI is called whenever the windows gets or looses the focus
            // as well as when any of its widgets are hovered by the mouse cursor. For this
            // reason, do not run any expensive algorithm here unless it is really needed,
            // that is, only when any of its buttons is pressed or any of its entry are updated.

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos,
                                                          false,
                                                          false,
                                                          GUILayout.Width(Screen.width),
                                                          GUILayout.Height(Screen.height));

            GUILayout.Label("Graph", EditorStyles.boldLabel);
            if (editorSettings.pathPrefix == null)
            {
                // Application.dataPath (used within ProjectPath()) must not be called in a 
                // constructor. That is why we need to set it here if it is not yet defined.
                editorSettings.pathPrefix = UnityProject.GetPath();
            }
            editorSettings.pathPrefix = EditorGUILayout.TextField("Project path prefix", editorSettings.pathPrefix);
            editorSettings.gxlPath = EditorGUILayout.TextField("GXL file", editorSettings.gxlPath);
            editorSettings.csvPath = EditorGUILayout.TextField("CSV file", editorSettings.csvPath);
            editorSettings.origin = EditorGUILayout.Vector3Field("Origin", editorSettings.origin);

            GUILayout.Label("Attributes of leaf nodes", EditorStyles.boldLabel);
            editorSettings.WidthMetric = EditorGUILayout.TextField("Width", editorSettings.WidthMetric);
            editorSettings.HeightMetric = EditorGUILayout.TextField("Height", editorSettings.HeightMetric);
            editorSettings.DepthMetric = EditorGUILayout.TextField("Depth", editorSettings.DepthMetric);
            editorSettings.ColorMetric = EditorGUILayout.TextField("Color", editorSettings.ColorMetric);

            GUILayout.Label("VR settings", EditorStyles.boldLabel);
            VRenabled = EditorGUILayout.Toggle("Enable VR", VRenabled);

            GUILayout.Label("Visual node attributes", EditorStyles.boldLabel);
            editorSettings.LeafObjects = (GraphSettings.LeafNodeKinds)EditorGUILayout.EnumPopup("Leaf nodes", editorSettings.LeafObjects);
            editorSettings.InnerNodeObjects = (GraphSettings.InnerNodeKinds)EditorGUILayout.EnumPopup("Inner nodes", editorSettings.InnerNodeObjects);
            editorSettings.NodeLayout = (GraphSettings.NodeLayouts)EditorGUILayout.EnumPopup("Layout", editorSettings.NodeLayout);
            
            editorSettings.ZScoreScale = EditorGUILayout.Toggle("Z-score scaling", editorSettings.ZScoreScale);
            editorSettings.ShowErosions = EditorGUILayout.Toggle("Show erosions", editorSettings.ShowErosions);

            if (editorSettings.NodeLayout == GraphSettings.NodeLayouts.CompoundSpringEmbedder)
            {
                GUILayout.Label("Compound spring embedder layout attributes", EditorStyles.boldLabel);
                editorSettings.CoseGraphSettings.EdgeLength = EditorGUILayout.IntField("Edge length", editorSettings.CoseGraphSettings.EdgeLength);
                editorSettings.CoseGraphSettings.UseSmartIdealEdgeCalculation = EditorGUILayout.Toggle("Smart ideal edge length", editorSettings.CoseGraphSettings.UseSmartIdealEdgeCalculation);
                editorSettings.CoseGraphSettings.PerLevelIdealEdgeLengthFactor = EditorGUILayout.DoubleField("Level edge length factor", editorSettings.CoseGraphSettings.PerLevelIdealEdgeLengthFactor);
                editorSettings.CoseGraphSettings.multiLevelScaling = EditorGUILayout.Toggle("MultiLevelscaling", editorSettings.CoseGraphSettings.multiLevelScaling);
                editorSettings.CoseGraphSettings.UseSmartRepulsionRangeCalculation = EditorGUILayout.Toggle("Smart repulsion range", editorSettings.CoseGraphSettings.UseSmartRepulsionRangeCalculation);
                editorSettings.CoseGraphSettings.RepulsionStrength = EditorGUILayout.DoubleField("Repulsion Strength", editorSettings.CoseGraphSettings.RepulsionStrength);
                editorSettings.CoseGraphSettings.GravityStrength = EditorGUILayout.DoubleField("Gravity", editorSettings.CoseGraphSettings.GravityStrength);
                editorSettings.CoseGraphSettings.CompoundGravityStrength = EditorGUILayout.DoubleField("Compound gravity", editorSettings.CoseGraphSettings.CompoundGravityStrength);
                editorSettings.CoseGraphSettings.useOptAlgorithm = EditorGUILayout.Toggle("Use Opt-Algorithm", editorSettings.CoseGraphSettings.useOptAlgorithm);


                if (editorSettings.CoseGraphSettings.dirs.Count > 0)
                {
                    GUILayout.Label("Choose Sublayouts", EditorStyles.boldLabel);
                    List<Node> roots = editorSettings.CoseGraphSettings.dirs;

                    if (editorSettings.CoseGraphSettings.show.Count == 0)
                    {
                        foreach (Node root in roots)
                        {
                            TraverseThruNodesCounter(root); ;
                        }
                    }

                    foreach (Node root in roots)
                    {
                        TraverseThruNodes(root);
                    }
                }
            }

            GUILayout.Label("Visual edge attributes", EditorStyles.boldLabel);
            editorSettings.EdgeLayout = (GraphSettings.EdgeLayouts)EditorGUILayout.EnumPopup("Layout", editorSettings.EdgeLayout);
            editorSettings.EdgeWidth = EditorGUILayout.FloatField("Edge width", editorSettings.EdgeWidth);
            editorSettings.EdgesAboveBlocks = EditorGUILayout.Toggle("Edges above blocks", editorSettings.EdgesAboveBlocks);

            if (editorSettings.Measurements.Count > 0)
            {
                GUILayout.Label("Measurements", EditorStyles.boldLabel);
                MeasurementsTable(editorSettings.Measurements);
            }

            GUILayout.Label("");

            // TODO: We may want to allow a user to define all edge types to be considered hierarchical.
            // TODO: We may want to allow a user to define which node attributes should be mapped onto which icons

            //groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
            //myBool = EditorGUILayout.Toggle("Toggle", myBool);
            //myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
            //myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
            //EditorGUILayout.EndToggleGroup();

            float width = position.width - 5;
            const float height = 30;
            string[] actionLabels = new string[] { "Load City", "Delete City" };
            int selectedAction = GUILayout.SelectionGrid(-1, actionLabels, actionLabels.Length, GUILayout.Width(width), GUILayout.Height(height));

            switch (selectedAction)
            {
                case 0: // Load City  
                    EnableVR(VRenabled);
  
                    graph = SceneGraphs.Add(editorSettings);
                    if (ReferenceEquals(graph, null))
                    {
                        Debug.LogError("No graph loaded.\n");
                    }
                    else
                    {
                        int numberOfErrors = MetricImporter.Load(graph, editorSettings.CSVPath());
                        if (numberOfErrors > 0)
                        {
                            Debug.LogErrorFormat("CSV file {0} has {1} many errors.\n", editorSettings.CSVPath(), numberOfErrors);
                        }
                        {
                            MetricAggregator.AggregateSum(graph, editorSettings.AllLeafIssues().ToArray<string>());
                            // Note: We do not want to compute the derived metric editorSettings.InnerDonutMetric
                            // when we have a single root node in the graph. This metric will be used to define the color
                            // of inner circles of Donut charts. Because the color is a linear interpolation of the whole
                            // metric value range, the inner circle would always have the maximal value (it is the total
                            // sum over all) and hence the maximal color gradient. The color of the other nodes would be
                            // hardly distinguishable. 
                            // FIXME: We need a better solution. This is a kind of hack.
                            MetricAggregator.DeriveSum(graph, editorSettings.AllInnerNodeIssues().ToArray<string>(), editorSettings.InnerDonutMetric, true);
                        }
                        GraphRenderer renderer = new GraphRenderer(editorSettings);
                        renderer.Draw(graph);
                        // If CScape buildings are used, the scale of the world is larger and, hence, the camera needs to move faster.
                        AdjustCameraSpeed(renderer.Unit());
                    }
                    break;

                case 1:  // Delete City
                    Reset();

                    break;
                default:
                    break;
            }

            GUILayout.Space(20);
            EditorGUILayout.EndScrollView();
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
        private void TraverseThruNodes(Node root)
        {
            EditorGUIUtility.labelWidth = 80;
            if (!root.IsLeaf())
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
                        bool showPosition = EditorGUILayout.Foldout(editorSettings.CoseGraphSettings.show[root], root.LinkName);
                        editorSettings.CoseGraphSettings.show[root] = showPosition;
                        if (showPosition)
                        {

                            ShowCheckBox(root, false);

                            GUILayout.EndHorizontal();

                            if (root.Children() != null && root.Children().Count > 0)
                            {
                                foreach (Node child in root.Children())
                                {
                                    TraverseThruNodes(child);
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
                        GUILayout.Label(root.LinkName, GUILayout.Width(120));
                        ShowCheckBox(root, true);
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
        private void ShowCheckBox(Node root, bool childrenAreLeaves)
        {
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayoutOption[] guiOptionsToggle = { GUILayout.ExpandWidth(false), GUILayout.Width(20) };
            bool toggle = EditorGUILayout.Toggle("", editorSettings.CoseGraphSettings.ListDirToggle[root.LinkName], guiOptionsToggle);
            editorSettings.CoseGraphSettings.ListDirToggle[root.LinkName] = toggle;
            //var checkedToggle = editorSettings.CoseGraphSettings.ListDirToggle.Where(predicate: kvp => kvp.Value);

            if (toggle)
            {
                ShowSublayoutEnum(editorSettings.CoseGraphSettings.DirNodeLayout[root.LinkName], root, childrenAreLeaves);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                ShowSublayoutEnum(GraphSettings.NodeLayouts.CompoundSpringEmbedder, root, childrenAreLeaves);
                EditorGUI.EndDisabledGroup();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (toggle)
            {
                ShowShapeEnum(editorSettings.CoseGraphSettings.DirShape[root.LinkName], root);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                ShowShapeEnum(editorSettings.CoseGraphSettings.DirShape[root.LinkName], root);
                EditorGUI.EndDisabledGroup();
            }
        }

        /// <summary>
        /// Dropdown for shape of the inner nodes
        /// </summary>
        /// <param name="innerNodeKinds">the inner node kinds</param>
        /// <param name="root">the node</param>
        private void ShowShapeEnum(GraphSettings.InnerNodeKinds innerNodeKinds, Node root)
        {
            GUILayoutOption[] guiOptions = { GUILayout.ExpandWidth(false), GUILayout.Width(200) };
            EditorGUIUtility.labelWidth = 80;
            EditorGUILayout.PrefixLabel("Inner nodes");
            editorSettings.CoseGraphSettings.DirShape[root.LinkName] = (GraphSettings.InnerNodeKinds)EditorGUILayout.EnumPopup("", innerNodeKinds, guiOptions);
            EditorGUIUtility.labelWidth = 150;
        }

        /// <summary>
        /// Dropdown for the sublayout kinds
        /// </summary>
        /// <param name="nodeLayout"></param>
        /// <param name="root"></param>
        /// <param name="childrenAreLeaves"></param>
        private void ShowSublayoutEnum(GraphSettings.NodeLayouts nodeLayout, Node root, bool childrenAreLeaves)
        {
            GUILayoutOption[] guiOptions = { GUILayout.ExpandWidth(false), GUILayout.Width(200) };
            EditorGUIUtility.labelWidth = 80;
            EditorGUILayout.PrefixLabel("Sublayouts");
            Dictionary<GraphSettings.NodeLayouts, string> subLayoutNodeLayouts = new Dictionary<GraphSettings.NodeLayouts, string>();
            subLayoutNodeLayouts = childrenAreLeaves ? editorSettings.SubLayoutsLeafNodes : editorSettings.SubLayoutsInnerNodes;
            editorSettings.CoseGraphSettings.DirNodeLayout[root.LinkName] = subLayoutNodeLayouts.ElementAt(EditorGUILayout.Popup(subLayoutNodeLayouts.Keys.ToList().IndexOf(editorSettings.CoseGraphSettings.DirNodeLayout[root.LinkName]), subLayoutNodeLayouts.Values.ToArray(), guiOptions)).Key;
            EditorGUIUtility.labelWidth = 150;
        }

        /// <summary>
        /// traverses thru the nodes and adds them to a list
        /// </summary>
        /// <param name="root">the root node</param>
        private void TraverseThruNodesCounter(Node root)
        {
            if (!root.IsLeaf())
            {
                editorSettings.CoseGraphSettings.show.Add(root, true);
                if (root.Children() != null || root.Children().Count > 0)
                {
                    foreach (Node child in root.Children())
                    {
                        TraverseThruNodesCounter(child);
                    }
                }
            }
        }


        /// <summary>
        /// Adjusts the spead of the camera according to the space unit. If we use simple
        /// cubes for the buildings, the unit is the normal Unity unit. If we use CScape
        /// buildings, the unit is larger than the normal Unity unit and, hence, camera
        /// speed must be adjusted accordingly.
        /// </summary>
        /// <param name="unit">the factor by which to multiply the camera speed</param>
        private void AdjustCameraSpeed(float unit)
        {
            foreach (GameObject camera in AllMainCameras())
            {
                FlyCamera flightControl = camera.GetComponent<FlyCamera>();
                if (flightControl != null)
                {
                    flightControl.SetDefaults();
                    flightControl.AdjustSettings(unit);
                }
                // TODO: Adjust speed setting for Leap Rig camera
            }
        }

        /// <summary>
        /// Deletes all scene graph, nodes and edges via their tags.
        /// </summary>
        private void Reset()
        {
            SceneGraphs.DeleteAll();
            graph = null;
            // delete all left-overs if there are any
            foreach (string tag in SEE.DataModel.Tags.All)
            {
                try
                {
                    DeleteByTag(tag);
                }
                catch (UnityException e)
                {
                    Debug.LogError(e.ToString());
                }
            }
        }

        /// <summary>
        /// Destroys immediately all game objects with given tag.
        /// </summary>
        /// <param name="tag">tag of the game objects to be destroyed.</param>
        private void DeleteByTag(string tag)
        {
            int count = 0;
            // Note: FindObjectsOfTypeAll retrieves all objects including non-active ones, which is
            // necessary for prefabs serving as prototypes for active game objects.
            foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
            {
                if (go.tag == tag)
                {
                    Destroyer.DestroyGameObject(go);
                    count++;
                }
            }
            Debug.LogFormat("Deleted {0} objects tagged {1}.\n", count, tag);
        }
    }
}