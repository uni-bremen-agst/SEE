using SEE.Controls;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SEE.CameraPaths
{
    /// <summary>
    /// This scripts logs the position, rotation, and time in seconds since game start
    /// (rounded to integer) of a user-selected tracked object at any point in time when
    ///  the user presses the key KeyBindings.SavePathPosition or periodically.
    /// </summary>
    public class CameraRecorder : MonoBehaviour
    {        
        /// <summary>
        /// The directory in which to store path files.
        /// </summary>
        [Tooltip("The directory in which to store path files.")]
        public string Directory;

        /// <summary>
        /// Base name of the file where to store the captured data points (Take will be appended).
        /// </summary>
        [Tooltip("Base name of the file where to store the captured data points (Take will be appended).")]
        public string Basename = "path";

        /// <summary>
        /// The number of recording to be used for the filename.
        /// </summary>
        [Tooltip("The number of recording to be used for the filename (will be appended to Path).")]
        public int Take = 1;

        /// <summary>
        /// The length of a period in which to record a path position automatically (in seconds).
        /// Not used if Interactive.
        /// </summary>
        [Tooltip("The length of a period in which to record a path position automatically (in seconds)"
                 + "Not used if Interactive.")]
        public float Period = 0.5f;

        /// <summary>
        /// Whether the recording is interactive. If true, a position is recorded only if
        /// the user presses the recording key (see Keybindings).
        /// </summary>
        [Tooltip("Whether the recording is interactive. If true, a position is recorded only if "
                 + "the user presses the recording key (see Keybindings).")]
        public bool Interactive = false;

        /// <summary>
        /// This is the list of object to be tracked in the scene. They will be retrieved
        /// from names in <see cref="TrackedObjects"/>. The main camera will be added implicitly.
        /// </summary>
        private GameObject[] trackedObjects;

        /// <summary>
        /// This is the list of names of the game objects to be tracked in the scene.
        /// The main camera will be tracked implicitly.
        /// </summary>
        [Tooltip("Names of the objects to be tracked in the scene. The main camera will be tracked implicitly.")]
        public string[] TrackedObjects;

        /// <summary>
        /// The recorded paths. paths[i] is the path of trackedObjects[i].
        /// </summary>
        private CameraPath[] paths;

        /// <summary>
        /// Returns the filename that does not currently exist taking into
        /// account the path, basename, take, and extension.
        /// </summary>
        /// <param name="objectName">the name of the tracked object to be added to the filename</param>
        /// <returns>filename for the recording</returns>
        private string Filename(string objectName)
        {
            string result = NewName(Directory, Basename, objectName, Take, CameraPath.DotPathFileExtension);
            while (File.Exists(result))
            {
                Take++;
                result = NewName(Directory, Basename, objectName, Take, CameraPath.DotPathFileExtension);
            }
            return result;
        }

        /// <summary>
        /// Returns the name of the file in which to store that path taking into
        /// account the path, basename, take, and extension. This filename may or
        /// may not already exist.
        /// </summary>
        /// <param name="path">leading path</param>
        /// <param name="basename">base name of the file</param>
        /// <param name="objectName">the name of the tracked object to be added to the filename</param>
        /// <param name="take">the number of the take</param>
        /// <param name="extension">file extension</param>
        /// <returns>filename for the recording as a concatenation of all given input parameters</returns>
        private string NewName(string path, string basename, string objectName, int take, string extension)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                path += Path.DirectorySeparatorChar;
            }
            return path + basename + objectName + take + extension;
        }

        /// <summary>
        /// The passed time of the current period.
        /// </summary>
        private float accumulatedTime = 0.0f;

        /// <summary>
        /// Collects the <see cref="trackedObjects"/>, sets up the <see cref="paths"/>, and <see cref="Directory"/>.
        /// </summary>
        private void Start()
        {
            trackedObjects = GetTrackedObjects(TrackedObjects);
            paths = new CameraPath[trackedObjects.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = new CameraPath();
            }
            if (string.IsNullOrEmpty(Directory))
            {
                Directory = UnityProject.GetPath();
            }
        }

        /// <summary>
        /// Retrieves all objects to be tracked from the scene including the main camera.
        /// </summary>
        /// <param name="trackedObjects">the name of the objects to be tracked</param>
        /// <returns>all objects to be tracked</returns>
        private static GameObject[] GetTrackedObjects(string[] trackedObjects)
        {
            if (trackedObjects == null || trackedObjects.Length == 0)
            {
                return new GameObject[] { MainCamera.Camera.gameObject };
            }
            else
            {
                // Retrieve the objects from the scene by name.
                IList<GameObject> objects = new List<GameObject>();
                foreach (string name in trackedObjects)
                {
                    GameObject gameObject = GameObject.Find(name);
                    if (gameObject == null)
                    {
                        Debug.LogError($"No such trackable object {name}.\n");
                    }
                    else
                    {
                        objects.Add(gameObject);
                    }
                }
                // Add all retrieved objects to the result.
                GameObject[] result = new GameObject[objects.Count + 1];
                int i = 0;
                foreach (GameObject gameObject in objects)
                {
                    result[i] = gameObject;
                    i++;
                }
                // Finally, add the main camera.
                result[result.Length - 1] = MainCamera.Camera.gameObject;
                return result;
            }
        }

        private void Update()
        {
            accumulatedTime += Time.deltaTime;
            if (accumulatedTime >= Period)
            {
                accumulatedTime = 0.0f;
            }

            // Press the KeyBindings.SavePathPosition key to save position on user demand. If the period has
            // been completed, the position is saved, too, if recording is not interactive.
            if (SEEInput.SavePathPosition() || (!Interactive && accumulatedTime == 0.0f))
            {
                float trackingTime = Interactive ? Mathf.RoundToInt(Time.realtimeSinceStartup) : Time.realtimeSinceStartup;
                for (int i = 0; i < trackedObjects.Length; i++)
                {
                    Vector3 position = trackedObjects[i].transform.position;
                    Quaternion rotation = trackedObjects[i].transform.rotation;
                    paths[i].Add(position, rotation, trackingTime);
                }
            }
        }

        /// <summary>
        /// Saves the recorded points in a file.
        /// 
        /// This function is called on all game objects before the application is quit. 
        /// In the editor it is called when the user stops playmode.
        /// </summary>
        private void OnApplicationQuit()
        {
            if (paths == null || paths.Length == 0)
            {
                Debug.Log("No paths have been recorded.\n");
            }
            else
            {
                SaveFile();
            }
        }

        /// <summary>
        /// Saves the content of data in a file.
        /// </summary>
        private void SaveFile()
        {
            for (int i = 0; i < trackedObjects.Length; i++)
            {
                string objectName = trackedObjects[i].name;
                string filename = Filename(objectName);
                paths[i].Save(filename);
                Debug.Log($"Saved path of '{objectName}' to {filename}\n");
            }
        }
    }
}
