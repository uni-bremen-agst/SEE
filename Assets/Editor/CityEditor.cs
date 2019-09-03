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
        // This method will be called when the user selects the menu item.
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

        /// <summary>
        /// Creates a new window offering the city editor commands.
        /// </summary>
        void OnGUI()
        {
            GUILayout.Label("Graph", EditorStyles.boldLabel);
            editorSettings.graphPath = EditorGUILayout.TextField("Graph", editorSettings.graphPath);
            editorSettings.hierarchicalEdgeType = EditorGUILayout.TextField("Hierarchical Edge", editorSettings.hierarchicalEdgeType);

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

                    if (graph != null)
                    {
                        const string widthMetric = "Metric.Number_of_Tokens";
                        const string heightMetric = "Metric.Clone_Rate";
                        const string breadthMetric = "Metric.LOC";
                        MeshFactory.Reset();
                        if (true)
                        {
                            layout = new SEE.Layout.BalloonLayout(widthMetric, heightMetric, breadthMetric);
                        }
                        else
                        {
                            layout = new SEE.Layout.ManhattenLayout(widthMetric, heightMetric, breadthMetric);
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