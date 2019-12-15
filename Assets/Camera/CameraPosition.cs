using System.IO;
using UnityEngine;

namespace SEE
{
    /// <summary>
    /// This scripts logs the position, rotation, and time in seconds since game start
    /// (rounded to integer) of the main camera at any point in time when the user presses
    /// the key P (for position).
    /// </summary>
    public class CameraPosition : MonoBehaviour
    {
        /// <summary>
        /// This is Main Camera in the Scene.
        /// </summary>
        private Camera mainCamera;

        /// <summary>
        /// The recorded path.
        /// </summary>
        private CameraPath path = new CameraPath();

        /// <summary>
        /// Base name of the file where to store the captured data points.
        /// </summary>
        public string Basename = "path";

        /// <summary>
        /// Filename extension.
        /// </summary>
        public string Extension = ".csv";

        /// <summary>
        /// The number of recording to be used for the filename.
        /// </summary>
        public int Take = 1;

        /// <summary>
        /// Returns the filename that does not currently exist taking into
        /// account the path, basename, take, and extension.
        /// </summary>
        /// <returns>filename for the recording</returns>
        private string Filename()
        {
            string result = NewName(Application.persistentDataPath, Basename, Take, Extension);
            while (File.Exists(result))
            {
                Take++;
                result = NewName(Application.persistentDataPath, Basename, Take, Extension);
            }
            return result;
        }

        /// <summary>
        /// Returns the name of the file in which to store that path taking into
        /// account the path, basename, take, and extension. This filename may or
        /// may not already exist.
        /// </summary>
        /// <param name="path">leading path</param>
        /// <param name="basename">base name of the ile</param>
        /// <param name="take">the number of the take</param>
        /// <param name="extension">file extension</param>
        /// <returns>filename for the recording</returns>
        private string NewName(string path, string basename, int take, string extension)
        {
            return path + "/" + basename + take + extension;
        }

        /// <summary>
        /// The passed time of the current period.
        /// </summary>
        private float accumulatedTime = 0.0f;

        /// <summary>
        /// The length of a period in which to record a path position.
        /// </summary>
        public float Period = 0.5f;

        /// <summary>
        /// Whether the recording is interactive. If true, a position is recorded only if
        /// the user presses key P.
        /// </summary>
        public bool Interactive = false;

        void Start()
        {
            // This gets the Main Camera from the Scene
            mainCamera = Camera.main;
        }

        void Update()
        {
            accumulatedTime += Time.deltaTime;
            if (accumulatedTime >= Period)
            {
                accumulatedTime = 0.0f;
            }

            // Press the P key to save position on user demand. If the period has
            // been completed, the position is saved, too, if recording is not interactive.
            if (Input.GetKeyDown(KeyCode.P) || (! Interactive && accumulatedTime == 0.0f))
            {
                Vector3 position = mainCamera.transform.position;
                Quaternion rotation = mainCamera.transform.rotation;
                path.Add(position, rotation, Interactive ? Mathf.RoundToInt(Time.realtimeSinceStartup) : Time.realtimeSinceStartup);
            }
        }

        /// <summary>
        /// Saves the recorded points in a file.
        /// 
        /// This function is called on all game objects before the application is quit. 
        /// In the editor it is called when the user stops playmode.
        /// </summary>
        void OnApplicationQuit()
        {
            if (path.Count == 0)
            {
                Debug.Log("Empty camera path is not stored.\n");
            }
            else
            {
                SaveFile();
            }
        }

        /// <summary>
        /// Saves the content of data in a file.
        /// </summary>
        public void SaveFile()
        {
            string filename = Filename();
            path.Save(filename);
            Debug.LogFormat("Saved camera path to {0}\n", filename);
        }
    }
}
