using UnityEngine;
using UnityEditor;
using UnityEngine.XR;
using SEE;
using SEE.Game;

namespace SEEEditor
{
    /// <summary>
    /// An editor that allows an Unity editor user to set VR parameters for a SEE city.
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

        /// <summary>
        /// Whether VR mode is to be activated for the game.
        /// </summary>
        private bool VRenabled = false;

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
            foreach (GameObject camera in Cameras.AllMainCameras())
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
        /// Creates a new window offering the city editor commands.
        /// </summary>
        void OnGUI()
        {
            // Important note: OnGUI is called whenever the windows gets or looses the focus
            // as well as when any of its widgets are hovered by the mouse cursor. For this
            // reason, do not run any expensive algorithm here unless it is really needed,
            // that is, only when any of its buttons is pressed or any of its entry are updated.

            GUILayout.Label("VR settings", EditorStyles.boldLabel);
            VRenabled = EditorGUILayout.Toggle("Enable VR", VRenabled);

            float width = position.width - 5;
            const float height = 30;
            string[] actionLabels = new string[] { "Delete Everything" };
            int selectedAction = GUILayout.SelectionGrid(-1, actionLabels, actionLabels.Length, GUILayout.Width(width), GUILayout.Height(height));
            switch (selectedAction)
            {
                case 0: // Delete Everything
                    DeleteEverything();
                break;
            }
        }

        /// <summary>
        /// Deletes the underlying graph of every game object having a component SEECity.
        /// Deletes all game objects tagged with one of the tags SEE.DataModel.Tags.All.
        /// </summary>
        private void DeleteEverything()
        {
            // Deletes the underlying graph of every game object having a component SEECity.
            // FindObjectsOfTypeAll returns also inactive game objects
            foreach (GameObject o in Resources.FindObjectsOfTypeAll(typeof(UnityEngine.GameObject)))
            {
                SEECity city = o.GetComponent<SEECity>();
                if (city != null)
                {
                    city.DeleteGraph();
                }
            }
            // Deletes all left-over game objects tagged by any of the tags in SEE.DataModel.Tags.All.
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