using UnityEngine;
using UnityEditor;

namespace SEEEditor
{
    // An editor that allows an Unreal editor user to create a city.
    // Note: An alternative to an EditorWindow extension could have been a ScriptableWizard.
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
        bool tagGroupEnabled = false;
        [Tooltip("The tag of all nodes")]
        public string nodeTag = "House";

        [Tooltip("The tag of all edges")]
        public string edgeTag = "Edge";

        [Tooltip("The relative path to the building preftab")]
        public string nodePrefabPath = "Assets/Prefabs/House.prefab";

        [Tooltip("The relative path to the connection preftab")]
        public string edgePreftabPath = "Assets/Prefabs/Line.prefab";

        [Tooltip("The path to the graph data")]
        public string graphPath = "C:\\Users\\raine\\develop\\seecity\\data\\gxl\\minimal_clones.gxl";
        //public string graphPath = "C:\\Users\\raine\\develop\\see\\data\\gxl\\linux-clones\\clones.gxl";
        // The following graph will not work because it does not have the necessary metrics.
        // public string graphPath = "C:\\Users\\raine\\Downloads\\codefacts.gxl";

        [Tooltip("The name of the edge type of hierarchical edges")]
        public string hierarchicalEdgeType = "Enclosing";

        /// <summary>
        /// Creates a new window offering the city editor commands.
        /// </summary>
        void OnGUI()
        {
            sceneGraph = GetSceneGraph();

            GUILayout.Label("Graph", EditorStyles.boldLabel);
            graphPath = EditorGUILayout.TextField("Graph", graphPath);
            hierarchicalEdgeType = EditorGUILayout.TextField("Hierarchical Edge", hierarchicalEdgeType);

            GUILayout.Label("Preftabs", EditorStyles.boldLabel);
            nodePrefabPath = EditorGUILayout.TextField("Node", nodePrefabPath);
            edgePreftabPath = EditorGUILayout.TextField("Edge", edgePreftabPath);

            //groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
            //myBool = EditorGUILayout.Toggle("Toggle", myBool);
            //myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
            //EditorGUILayout.EndToggleGroup();

            tagGroupEnabled = EditorGUILayout.BeginToggleGroup("GameObject Tags", tagGroupEnabled);
            nodeTag = EditorGUILayout.TextField("Node Tag", nodeTag);
            edgeTag = EditorGUILayout.TextField("Edge Tag", edgeTag);
            EditorGUILayout.EndToggleGroup();

            float width = position.width - 5;
            const float height = 30;
            string[] actionLabels = new string[] { "Load City", "Delete City" };
            int selectedAction = GUILayout.SelectionGrid(-1, actionLabels, actionLabels.Length, GUILayout.Width(width), GUILayout.Height(height));
            switch (selectedAction)
            {
                case 0:
                    Debug.Log(actionLabels[0] + "\n");
                    LoadCity();
                    break;
                case 1:
                    Debug.Log(actionLabels[1] + "\n");
                    sceneGraph.Delete();
                    // delete any left-over if there is any
                    DeleteAll();
                    break;
                default:
                    break;
            }
            this.Repaint();
        }

        // The scene graph created by this CityEditor.
        private SEE.SceneGraph sceneGraph = null;

        /// <summary>
        /// Returns the scene graph if it exists. Will return null if it does not exist.
        /// </summary>
        /// <returns>the scene graph or null</returns>
        private SEE.SceneGraph GetSceneGraph()
        {
            if (sceneGraph == null)
            {
                sceneGraph = SEE.SceneGraph.GetInstance();
            }
            return sceneGraph;
        }

        /// <summary>
        /// Loads a graph from disk and creates the scene objects representing it.
        /// </summary>
        private void LoadCity()
        {
            SEE.SceneGraph sgraph = GetSceneGraph();
            if (sgraph != null)
            {
                Debug.Log("Loading graph from " + graphPath + "\n");
                sgraph.LoadAndDraw(graphPath);
            }
            else
            {
                Debug.LogError("There is no scene graph.\n");
            }
        }

        /// <summary>
        /// Deletes all scene nodes and edges via the tags defined in sceneGraph.
        /// </summary>
        private void DeleteAll()
        {
            try
            {
                DeleteByTag(sceneGraph.houseTag);
            }
            catch (UnityException e)
            {
                Debug.LogError(e.ToString());
            }
            try
            {
                DeleteByTag(sceneGraph.edgeTag);
            }
            catch (UnityException e)
            {
                Debug.LogError(e.ToString());
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