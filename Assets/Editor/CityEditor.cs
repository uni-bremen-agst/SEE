using UnityEngine;
using UnityEditor;
using SEE.DataModel;
using SEE;
using SEE.Layout;
using System.Collections.Generic;
using UnityEngine.XR;
using System.Collections;
using System;

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
        private bool tagGroupEnabled = false;

        private Graph graph = null;

        private SEE.GraphSettings editorSettings = new SEE.GraphSettings();

        private SEE.Layout.ILayout layout;

        private string ProjectPath()
        {
            string result = Application.dataPath;
            // Unity uses Unix directory separator; we need Windows here
            
            return result.Replace('/', '\\') + '\\';
        }

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
                }
                else if (camera.transform.parent.name == "Leap Rig")
                {
                    // The camera of the Leap Rig is nested in a game object named accordingly.
                    camera.SetActive(enableVR);
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
        /// Creates a new window offering the city editor commands.
        /// </summary>
        void OnGUI()
        {
            GUILayout.Label("Graph", EditorStyles.boldLabel);
            editorSettings.pathPrefix = EditorGUILayout.TextField("Project path prefix", ProjectPath());
            editorSettings.gxlPath = EditorGUILayout.TextField("GXL file", editorSettings.gxlPath);
            editorSettings.csvPath = EditorGUILayout.TextField("CSV file", editorSettings.csvPath);
            editorSettings.animatedPath = EditorGUILayout.TextField("Revisions folder", editorSettings.animatedPath);

            GUILayout.Label("Lengths of buildings", EditorStyles.boldLabel);
            editorSettings.WidthMetric = EditorGUILayout.TextField("Width", editorSettings.WidthMetric);
            editorSettings.HeightMetric = EditorGUILayout.TextField("Height", editorSettings.HeightMetric);
            editorSettings.DepthMetric = EditorGUILayout.TextField("Depth", editorSettings.DepthMetric);

            GUILayout.Label("VR settings", EditorStyles.boldLabel);
            VRenabled = EditorGUILayout.Toggle("Enable VR", VRenabled);
            EnableVR(VRenabled);

            GUILayout.Label("Visual attributes", EditorStyles.boldLabel);
            editorSettings.BallonLayout = EditorGUILayout.Toggle("Balloon Layout", editorSettings.BallonLayout);
            editorSettings.CScapeBuildings = EditorGUILayout.Toggle("CScape buildings", editorSettings.CScapeBuildings);
            editorSettings.ZScoreScale = EditorGUILayout.Toggle("Z-score scaling", editorSettings.ZScoreScale);
            editorSettings.EdgeWidth = EditorGUILayout.FloatField("Edge width", editorSettings.EdgeWidth);
            editorSettings.ShowEdges = EditorGUILayout.Toggle("Show edges", editorSettings.ShowEdges);
            editorSettings.ShowErosions = EditorGUILayout.Toggle("Show erosions", editorSettings.ShowErosions);
            editorSettings.ShowDonuts = EditorGUILayout.Toggle("Show Donut charts", editorSettings.ShowDonuts);

            // TODO: We may want to allow a user to define all edge types to be considered hierarchical.
            // TODO: We may want to allow a user to define which node attributes should be mapped onto which icons

            //groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
            //myBool = EditorGUILayout.Toggle("Toggle", myBool);
            //myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
            //EditorGUILayout.EndToggleGroup();

            float width = position.width - 5;
            const float height = 30;
            string[] actionLabels = new string[] { "Load City", "Delete City" };
            int selectedAction = GUILayout.SelectionGrid(-1, actionLabels, actionLabels.Length, GUILayout.Width(width), GUILayout.Height(height));

            BlockFactory blockFactory;
            if (editorSettings.CScapeBuildings)
            {
                blockFactory = new BuildingFactory();
            }
            else
            {
                blockFactory = new CubeFactory();
            }
            // If CScape buildings are used, the scale of the world is larger and, hence, the camera needs to move faster.
            AdjustCameraSpeed(blockFactory.Unit());

            switch (selectedAction)
            {
                case 0:
                    graph = SceneGraphs.Add(editorSettings);
                    int numberOfErrors = MetricImporter.Load(graph, editorSettings.CSVPath());
                    if (numberOfErrors > 0)
                    {
                        Debug.LogErrorFormat("CSV file {0} has {1} many errors.\n", editorSettings.CSVPath(), numberOfErrors);
                    }

                    if (graph != null)
                    {
                        //CubeFactory.Reset();            
                        IScale scaler;
                        {
                            List<string> nodeMetrics = new List<string>() { editorSettings.WidthMetric, editorSettings.HeightMetric, editorSettings.DepthMetric };
                            nodeMetrics.AddRange(editorSettings.IssueMap().Keys);
                            if (editorSettings.ZScoreScale)
                            {
                                scaler = new ZScoreScale(graph, editorSettings.MinimalBlockLength, editorSettings.MaximalBlockLength, nodeMetrics);
                            }
                            else
                            {
                                scaler = new LinearScale(graph, editorSettings.MinimalBlockLength, editorSettings.MaximalBlockLength, nodeMetrics);
                            }
                        }

                        if (editorSettings.BallonLayout)
                        {
                            layout = new SEE.Layout.BalloonLayout(editorSettings.ShowEdges,
                                                                  editorSettings.WidthMetric, editorSettings.HeightMetric, editorSettings.DepthMetric, 
                                                                  editorSettings.IssueMap(),
                                                                  editorSettings.InnerNodeMetrics,
                                                                  blockFactory,
                                                                  scaler,
                                                                  editorSettings.EdgeWidth,
                                                                  editorSettings.ShowErosions,
                                                                  editorSettings.ShowDonuts);
                        }
                        else
                        {
                            layout = new SEE.Layout.ManhattenLayout(editorSettings.ShowEdges,
                                                                    editorSettings.WidthMetric, editorSettings.HeightMetric, editorSettings.DepthMetric, 
                                                                    editorSettings.IssueMap(),
                                                                    blockFactory,
                                                                    scaler,
                                                                    editorSettings.EdgeWidth);
                        }
                        layout.Draw(graph);
                    }
                    else
                    {
                        Debug.LogError("No graph loaded.\n");
                    }
                    break;
                case 1:
                    Reset();

                    break;
                default:
                    break;
            }

            if (GUILayout.Button("Load revisions", GUILayout.Width(position.width), GUILayout.Height(height)))
            {
                graph = SceneGraphs.AddAnimated(editorSettings);
                /* TODO flo: enable loading metrics from csv
                int numberOfErrors = MetricImporter.Load(graph, editorSettings.CSVPath());
                if (numberOfErrors > 0)
                {
                    Debug.LogErrorFormat("CSV file {0} has {1} many errors.\n", editorSettings.CSVPath(), numberOfErrors);
                }
                */

                layoutAndRenderGraph();
            }
            if (GUILayout.Button("Delete rev.", GUILayout.Width(position.width), GUILayout.Height(height)))
            {
                Reset();
            }
            if (GUILayout.Button("Next rev.", GUILayout.Width(position.width), GUILayout.Height(height)))
            {
                graph = null;
                resetLayoutAndObjects();
                graph = SceneGraphs.getNextAnimatedGraph();
                layoutAndRenderGraph();
            }
            if (GUILayout.Button("Previous rev.", GUILayout.Width(position.width), GUILayout.Height(height)))
            {
                graph = null;
                resetLayoutAndObjects();
                graph = SceneGraphs.getPreviousAnimatedGraph();
                layoutAndRenderGraph();
            }
            this.Repaint();
        }

        /// <summary>
        /// Resets only the objects generated for a graph, not the graph data itself.
        /// </summary>
        private void resetLayoutAndObjects()
        {
            //CubeFactory.Reset();
            if (layout != null)
            {
                layout = null;
            }
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
        /// Layouts the Graph in graph und creates its objects in scene.
        /// </summary>
        private void layoutAndRenderGraph()
        {
            BlockFactory blockFactory;
            if (editorSettings.CScapeBuildings)
            {
                blockFactory = new BuildingFactory();
            }
            else
            {
                blockFactory = new CubeFactory();
            }
            if (graph != null)
            {
                //CubeFactory.Reset();            
                IScale scaler;
                {
                    List<string> nodeMetrics = new List<string>() { editorSettings.WidthMetric, editorSettings.HeightMetric, editorSettings.DepthMetric };
                    nodeMetrics.AddRange(editorSettings.IssueMap().Keys);
                    if (editorSettings.ZScoreScale)
                    {
                        scaler = new ZScoreScale(graph, editorSettings.MinimalBlockLength, editorSettings.MaximalBlockLength, nodeMetrics);
                    }
                    else
                    {
                        scaler = new LinearScale(graph, editorSettings.MinimalBlockLength, editorSettings.MaximalBlockLength, nodeMetrics);
                    }
                }

                if (editorSettings.BallonLayout)
                {
                    layout = new SEE.Layout.BalloonLayout(editorSettings.ShowEdges,
                                                          editorSettings.WidthMetric, editorSettings.HeightMetric, editorSettings.DepthMetric,
                                                          editorSettings.IssueMap(),
                                                          editorSettings.InnerNodeMetrics,
                                                          blockFactory,
                                                          scaler,
                                                          editorSettings.EdgeWidth,
                                                          editorSettings.ShowErosions,
                                                          editorSettings.ShowDonuts);
                }
                else
                {
                    layout = new SEE.Layout.ManhattenLayout(editorSettings.ShowEdges,
                                                            editorSettings.WidthMetric, editorSettings.HeightMetric, editorSettings.DepthMetric,
                                                            editorSettings.IssueMap(),
                                                            blockFactory,
                                                            scaler,
                                                            editorSettings.EdgeWidth);
                }
                layout.Draw(graph);
            }
            else
            {
                Debug.LogError("No graph loaded.\n");
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
            //CubeFactory.Reset();
            if (layout != null)
            {
                layout = null;
            }
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
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
            Debug.Log("Deleting objects: " + objects.Length + "\n");
            foreach (GameObject o in objects)
            {
                DestroyImmediate(o);
            }
        }
    }
}