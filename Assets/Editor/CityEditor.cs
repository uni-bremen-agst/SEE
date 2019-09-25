using UnityEngine;
using UnityEditor;
using SEE.DataModel;
using SEE;
using SEE.Layout;
using System.Collections.Generic;
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
        /// Creates a new window offering the city editor commands.
        /// </summary>
        void OnGUI()
        {
            GUILayout.Label("Graph", EditorStyles.boldLabel);
            editorSettings.pathPrefix = EditorGUILayout.TextField("Project path prefix", ProjectPath());
            editorSettings.gxlPath = EditorGUILayout.TextField("GXL file", editorSettings.gxlPath);
            editorSettings.csvPath = EditorGUILayout.TextField("CSV file", editorSettings.csvPath);

            GUILayout.Label("Lengths of buildings", EditorStyles.boldLabel);
            editorSettings.WidthMetric = EditorGUILayout.TextField("Width", editorSettings.WidthMetric);
            editorSettings.HeightMetric = EditorGUILayout.TextField("Height", editorSettings.HeightMetric);
            editorSettings.DepthMetric = EditorGUILayout.TextField("Depth", editorSettings.DepthMetric);

            GUILayout.Label("Visual attributes", EditorStyles.boldLabel);
            editorSettings.BallonLayout = EditorGUILayout.Toggle("Balloon Layout", editorSettings.BallonLayout);
            editorSettings.CScapeBuildings = EditorGUILayout.Toggle("CScape buildings", editorSettings.CScapeBuildings);
            editorSettings.ZScoreScale = EditorGUILayout.Toggle("Z-score scaling", editorSettings.ZScoreScale);
            editorSettings.EdgeWidth = EditorGUILayout.FloatField("Edge width", editorSettings.EdgeWidth);

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
                            layout = new SEE.Layout.BalloonLayout(editorSettings.WidthMetric, editorSettings.HeightMetric, editorSettings.DepthMetric, 
                                                                  editorSettings.IssueMap(),
                                                                  blockFactory,
                                                                  scaler,
                                                                  editorSettings.EdgeWidth);
                        }
                        else
                        {
                            layout = new SEE.Layout.ManhattenLayout(editorSettings.WidthMetric, editorSettings.HeightMetric, editorSettings.DepthMetric, 
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
            this.Repaint();
        }

        private void AdjustCameraSpeed(float unit)
        {
            foreach (GameObject camera in GameObject.FindGameObjectsWithTag("MainCamera"))
            {
                FlyCamera flightControl = camera.GetComponent<FlyCamera>();
                if (flightControl != null)
                {
                    flightControl.SetDefaults();
                    flightControl.AdjustSettings(unit);
                }

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