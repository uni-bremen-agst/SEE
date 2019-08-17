using UnityEngine;
using UnityEditor;

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

        /// <summary>
        /// Creates a new window offering the city editor commands.
        /// </summary>
        void OnGUI()
        {
            GUILayout.Label("Graph", EditorStyles.boldLabel);
            SceneGraphCreator.settings.graphPath = EditorGUILayout.TextField("Graph", SceneGraphCreator.settings.graphPath);
            SceneGraphCreator.settings.hierarchicalEdgeType = EditorGUILayout.TextField("Hierarchical Edge", SceneGraphCreator.settings.hierarchicalEdgeType);

            GUILayout.Label("Preftabs", EditorStyles.boldLabel);
            SceneGraphCreator.settings.nodePrefabPath = EditorGUILayout.TextField("Node", SceneGraphCreator.settings.nodePrefabPath);
            SceneGraphCreator.settings.edgePreftabPath = EditorGUILayout.TextField("Edge", SceneGraphCreator.settings.edgePreftabPath);

            //groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
            //myBool = EditorGUILayout.Toggle("Toggle", myBool);
            //myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
            //EditorGUILayout.EndToggleGroup();

            tagGroupEnabled = EditorGUILayout.BeginToggleGroup("GameObject Tags", tagGroupEnabled);
            SceneGraphCreator.settings.nodeTag = EditorGUILayout.TextField("Node Tag", SceneGraphCreator.settings.nodeTag);
            SceneGraphCreator.settings.edgeTag = EditorGUILayout.TextField("Edge Tag", SceneGraphCreator.settings.edgeTag);
            EditorGUILayout.EndToggleGroup();

            float width = position.width - 5;
            const float height = 30;
            string[] actionLabels = new string[] { "Load City", "Delete City" };
            int selectedAction = GUILayout.SelectionGrid(-1, actionLabels, actionLabels.Length, GUILayout.Width(width), GUILayout.Height(height));
            switch (selectedAction)
            {
                case 0:
                    SceneGraphCreator.Load();
                    break;
                case 1:
                    SceneGraphCreator.Delete();
                    // delete all left-overs if there are any
                    DeleteAll();
                    break;
                default:
                    break;
            }
            this.Repaint();
        }

        /// <summary>
        /// Deletes all scene nodes and edges via the tags defined in sceneGraph.
        /// </summary>
        private void DeleteAll()
        {
            try
            {
                DeleteByTag(SceneGraphCreator.settings.nodeTag);
            }
            catch (UnityException e)
            {
                Debug.LogError(e.ToString());
            }
            try
            {
                DeleteByTag(SceneGraphCreator.settings.edgeTag);
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