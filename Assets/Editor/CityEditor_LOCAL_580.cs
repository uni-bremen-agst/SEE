using UnityEngine;
using UnityEditor;
using SEE.DataModel;
using SEE;
using SEE.Layout;

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

            GUILayout.Label("Width of buildings", EditorStyles.boldLabel);
            editorSettings.WidthMetric = EditorGUILayout.TextField("Width", editorSettings.WidthMetric);

            GUILayout.Label("Height of buildings", EditorStyles.boldLabel);
            editorSettings.HeightMetric = EditorGUILayout.TextField("Width", editorSettings.HeightMetric);

            GUILayout.Label("Breadth of buildings", EditorStyles.boldLabel);
            editorSettings.BreadthMetric = EditorGUILayout.TextField("Breadth", editorSettings.BreadthMetric);

            // TODO: We may want to allow a user to define all edge types to be considered hierarchical.
            // TODO: We may want to allow a user to define which node attributes should be mapped onto which icons

            //groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
            //myBool = EditorGUILayout.Toggle("Toggle", myBool);
            //myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
            //EditorGUILayout.EndToggleGroup();

            tagGroupEnabled = EditorGUILayout.BeginToggleGroup("GameObject Tags", tagGroupEnabled);
            EditorGUILayout.EndToggleGroup();

            float width = position.width - 5;
            const float height = 30;
            string[] actionLabels = new string[] { "Load City", "Delete City" };
            int selectedAction = GUILayout.SelectionGrid(-1, actionLabels, actionLabels.Length, GUILayout.Width(width), GUILayout.Height(height));
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
                        MeshFactory.Reset();
                        //layout = new SEE.Layout.BalloonLayout(editorSettings.WidthMetric, editorSettings.HeightMetric, editorSettings.BreadthMetric, editorSettings.IssueMap());
                        //layout = new SEE.Layout.ManhattenLayout(editorSettings.WidthMetric, editorSettings.HeightMetric, editorSettings.BreadthMetric, editorSettings.IssueMap());
                        layout = new SEE.Layout.CirclePackingLayout(editorSettings.WidthMetric, editorSettings.HeightMetric, editorSettings.BreadthMetric, editorSettings.IssueMap());
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

        /// <summary>
        /// Deletes all scene graph, nodes and edges via their tags.
        /// </summary>
        private void Reset()
        {
            SceneGraphs.DeleteAll();
            graph = null;
            MeshFactory.Reset();
            if (layout != null)
            {
                layout.Reset();
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