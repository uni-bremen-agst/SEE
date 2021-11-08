#if UNITY_EDITOR

using SEE.Game.City;
using SEE.Utils;
using UnityEditor;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// An editor that allows an Unity editor user to set VR parameters for a SEE city.
    /// </summary>
    public class CityEditor : EditorWindow
    {
        [MenuItem("Window/City Editor")]
        private
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
        /// Creates a new window offering the city editor commands.
        /// </summary>
        private void OnGUI()
        {
            // Important note: OnGUI is called whenever the windows gets or looses the focus
            // as well as when any of its widgets are hovered by the mouse cursor. For this
            // reason, do not run any expensive algorithm here unless it is really needed,
            // that is, only when any of its buttons is pressed or any of its entry are updated.

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
                    city.Reset();
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
                if (go.CompareTag(tag))
                {
                    Destroyer.DestroyGameObject(go);
                    count++;
                }
            }
            Debug.LogFormat("Deleted {0} objects tagged {1}.\n", count, tag);
        }
    }
}

#endif
