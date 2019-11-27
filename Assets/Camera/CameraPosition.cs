using System.Collections.Generic;
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
        /// Name of the file where to store the captured data points.
        /// </summary>
        public string filename = "path.csv";

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
        /// <returns></returns>
        private string FloatToString(float value)
        {
            return value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        void Update()
        {
            //Press the L Button to switch cameras
            if (Input.GetKeyDown(KeyCode.P))
            {
                Vector3 position = mainCamera.transform.position;
                string output = FloatToString(position.x) 
                                + delimiter + FloatToString(position.y)
                                + delimiter + FloatToString(position.z)
                                + delimiter + Mathf.RoundToInt(Time.realtimeSinceStartup);
                data.Add(output);
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
            string path = Application.persistentDataPath + "/" + filename;
            System.IO.File.WriteAllLines(path, data);
            Debug.LogFormat("Saved camera path to {0}\n", path);
        }
    }
}
