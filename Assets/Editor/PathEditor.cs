#if UNITY_EDITOR

using SEE.CameraPaths;
using SEE.Utils;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// Implements an editor for camera path data to be used at game-design Time.
    /// Allows a user to load and visualize recordered camera paths.
    /// </summary>
    [Serializable]
    public class PathEditor : EditorWindow
    {
        [MenuItem("Window/Path Editor")]
        private
        // This method will be called when the user selects the menu item to create the window.
        // Such methods must be static and void. They can have any name.
        static void Init()
        {
            // We try to open the window by docking it next to the Inspector if possible.
            System.Type desiredDockNextTo = System.Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll");
            PathEditor window;
            if (desiredDockNextTo == null)
            {
                window = (PathEditor)EditorWindow.GetWindow(typeof(PathEditor), false, "Camera Paths", true);
            }
            else
            {
                window = EditorWindow.GetWindow<PathEditor>("Camera Paths", false, new System.Type[] { desiredDockNextTo });
            }
            window.Show();
        }

        /// <summary>
        /// The last selected paths directory.
        /// </summary>
        private string pathsDirectory;

        /// <summary>
        /// The list of paths added.
        /// </summary>
        public SerializableDictionary<string, GameObject> files = new SerializableDictionary<string, GameObject>();

        /// <summary>
        /// Creates a new window offering the path editor commands.
        /// </summary>
        private void OnGUI()
        {
            // Important note: OnGUI is called whenever the windows gets or looses the focus
            // as well as when any of its widgets are hovered by the mouse cursor. For this
            // reason, do not run any expensive algorithm here unless it is really needed,
            // that is, only when any of its buttons is pressed or any of its entry are updated.

            if (string.IsNullOrEmpty(pathsDirectory))
            {
                pathsDirectory = Filenames.OnCurrentPlatform(UnityProject.GetPath());
            }
            ShowFiles();

            // EnableMouseMenu();

            ReactToUser();
        }

        private void EnableMouseMenu()
        {
            Rect clickArea = EditorGUILayout.GetControlRect();

            Event current = Event.current;

            if (current.type == EventType.ContextClick)
            {
                Debug.LogFormat("mouse Position: {0}\n", current.mousePosition);
            }

            if (clickArea.Contains(current.mousePosition) && current.type == EventType.ContextClick)
            {
                //Do a thing, in this case a drop down menu

                GenericMenu menu = new GenericMenu();

                menu.AddDisabledItem(new GUIContent("I clicked on a thing"));
                menu.AddItem(new GUIContent("Do a thing"), false, YourCallback);
                menu.ShowAsContext();

                current.Use();
            }
        }

        private void YourCallback()
        {
            Debug.Log("Hi there\n");
        }

        /// <summary>
        /// Draws the table of paths.
        /// </summary>
        private void ShowFiles()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox); //Declaring our first part of our layout, and adding a bit of flare with EditorStyles.

            GUILayout.Label("List of paths", EditorStyles.boldLabel); //Making a label in our vertical view, declaring its contents, and adding editor flare.
            GUIContent content = EditorGUIUtility.IconContent("d_Toolbar Minus");

            string selectedIndex = "";
            int i = 0;
            foreach (System.Collections.Generic.KeyValuePair<string, GameObject> fileData in files)
            {
                if (!string.IsNullOrEmpty(fileData.Key))
                {
                    EditorGUILayout.BeginHorizontal();
                    string file = EditorGUILayout.TextField("Path[" + i + "]", fileData.Key);
                    if (!file.Equals(fileData.Key))
                    {
                        // FIXME: Update
                    }
                    bool pressed = GUILayout.Button(content, GUILayout.Width(20));
                    EditorGUILayout.EndHorizontal();
                    if (pressed)
                    {
                        selectedIndex = file;
                    }
                }
                i++;
            }
            EditorGUILayout.EndVertical(); // And closing our last area.
            if (!string.IsNullOrEmpty(selectedIndex))
            {
                RemovePath(selectedIndex);
            }
        }

        /// <summary>
        /// Offers buttons to the user to make her/his decision on adding the content of a folder,
        /// adding a single path file, or remove all paths altogether.
        /// </summary>
        private void ReactToUser()
        {
            float width = position.width - 5;
            const float height = 30;
            string[] actionLabels = new string[] { "Add Folder Content", "Add File", "Remove All" };
            int selectedAction = GUILayout.SelectionGrid(-1, actionLabels, actionLabels.Length, GUILayout.Width(width), GUILayout.Height(height));

            switch (selectedAction)
            {
                case 0: // Choose path folder  
                    SelectPathsFolder();
                    ShowFiles();
                    break;
                case 1:
                    SelectFile();
                    ShowFiles();
                    break;
                case 2:
                    RemoveAll();
                    ShowFiles();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Lets a user select a path file to be added.
        /// </summary>
        private void SelectFile()
        {
            string file = Filenames.OnCurrentPlatform(EditorUtility.OpenFilePanel("Select path file", pathsDirectory, CameraPath.PathFileExtension));
            if (!string.IsNullOrEmpty(file) && file.EndsWith(CameraPath.DotPathFileExtension) && !files.ContainsKey(file))
            {
                AddPath(file);
            }
        }

        /// <summary>
        /// Lets a user select a folder whose paths files are all to be added.
        /// </summary>
        private void SelectPathsFolder()
        {
            string path = EditorUtility.OpenFolderPanel("Select path files", pathsDirectory, "");

            if (path.Length != 0)
            {
                pathsDirectory = Filenames.OnCurrentPlatform(path);
                foreach (string file in Directory.GetFiles(pathsDirectory))
                {
                    string normalizedFile = Filenames.OnCurrentPlatform(file);
                    if (normalizedFile.EndsWith(CameraPath.DotPathFileExtension) && !files.ContainsKey(normalizedFile))
                    {
                        AddPath(normalizedFile);
                    }
                }
            }
        }

        /// <summary>
        /// Adds given file to the list of paths.
        /// </summary>
        /// <param name="file">path file to be added</param>
        private void AddPath(string file)
        {
            try
            {
                CameraPath path = CameraPath.ReadPath(file);
                GameObject o = path.Draw();
                files.Add(file, o);
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Failure in loading camera path {0}: {1}\n", file, e.ToString());
            }
        }

        /// <summary>
        /// Removes the path with given index in the list of paths loaded.
        /// </summary>
        /// <param name="selectedIndex">index of the path file to be removed</param>
        private void RemovePath(string selectedIndex)
        {
            ClearPath(files[selectedIndex]);
            files.Remove(selectedIndex);
        }

        /// <summary>
        /// Removes all paths.
        /// </summary>
        private void RemoveAll()
        {
            foreach (System.Collections.Generic.KeyValuePair<string, GameObject> file in files)
            {
                ClearPath(file.Value);
            }
            files.Clear();
        }

        /// <summary>
        /// Last clean up operations when a path game object is to be removed.
        /// </summary>
        /// <param name="pathObject">path game object that is to be removed</param>
        private void ClearPath(GameObject pathObject)
        {
            Destroyer.Destroy(pathObject);
        }
    }
}

#endif
