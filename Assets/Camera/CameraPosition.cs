using System.Collections.Generic;
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
        /// The list of data points on the path captured and to be stored.
        /// </summary>
        private List<string> data = new List<string>();

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

        /// <summary>
        /// If true, only the position and the time is recorded. Otherwise rotation is 
        /// recorded, too.
        /// </summary>
        public bool PositionOnly = false;

        void Start()
        {
            //This gets the Main Camera from the Scene
            mainCamera = Camera.main;
        }

        /// <summary>
        /// The delimiter to separate data points within the same line.
        /// </summary>
        private const string delimiter = ";";

        /// <summary>
        /// Converts a float value to a string with two digits and a period as a 
        /// decimal separator.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>the float as a string in the requested format</returns>
        private string FloatToString(float value)
        {
            return value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns the position data. If not only the position is requested,
        /// the result includes the rotation as a Quaternion.
        /// </summary>
        /// <returns>position data to be output</returns>
        private string Output()
        {
            Vector3 position = mainCamera.transform.position;
            string output = FloatToString(position.x)
                            + delimiter + FloatToString(position.y)
                            + delimiter + FloatToString(position.z);
            if (!PositionOnly)
            {
                Quaternion rotation = mainCamera.transform.rotation;
                output += delimiter + FloatToString(rotation.x)
                            + delimiter + FloatToString(rotation.y)
                            + delimiter + FloatToString(rotation.z)
                            + delimiter + FloatToString(rotation.w);
            }
            output += delimiter + (Interactive ? Mathf.RoundToInt(Time.realtimeSinceStartup).ToString() 
                                               : FloatToString(Time.realtimeSinceStartup));
            return output;
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
                data.Add(Output());
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
            if (data.Count == 0)
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
            // WriteAllLines creates a file, writes a collection of strings to the file,
            // and then closes the file.  You do NOT need to call Flush() or Close().
            string path = Filename();
            System.IO.File.WriteAllLines(path, data);
            Debug.LogFormat("Saved camera path to {0}\n", path);
        }
    }
}
