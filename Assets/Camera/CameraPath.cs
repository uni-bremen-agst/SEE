using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.Collections;
using SEE.Layout;

namespace SEE
{
    /// <summary>
    /// Represents a camera path with position and rotation of the camera in a series of 
    /// points in time.
    /// </summary>
    public class CameraPath : IEnumerable
    {
        /// <summary>
        /// The file extension of files storing path data.
        /// </summary>
        public const string PathFileExtension    = "csv";
        public const string DotPathFileExtension = ".csv";

        /// <summary>
        /// The number of path data captured so far.
        /// </summary>
        public int Count { get => data.Count; }

        public CameraPath()
        {
            path = "";
        }

        private CameraPath(string path)
        {
            this.path = path;
        }

        // The default color for the start of the lines of a path.
        public static Color StartDefaultColor = Color.blue;

        // The default color for the end of the lines of a path.
        public static Color EndDefaultColor = Color.blue;

        /// <summary>
        /// The material we use for the line drawing the path. All paths share the same material.
        /// </summary>
        private static Material material; // FIXME = LineFactory.NewLineMaterial();

        /// <summary>
        /// The line width for drawing paths.
        /// </summary>
        private const float lineWidth = 0.5f;

        /// <summary>
        /// The path data captured.
        /// </summary>
        private List<PathData> data = new List<PathData>();

        /// <summary>
        /// Operator[key] yielding the key'th entry in the path. The first
        /// entry has key = 0.
        /// 
        /// Precondition: 0 <= key < Count. 
        /// </summary>
        /// <param name="key">the index of the path entry to be returned</param>
        /// <returns>key'th entry in the path</returns>
        public PathData this[int key]
        {
            get => data[key];
            //set => data[key] = value;
        }

        /// <summary>
        /// Adds the data captured about a particular camera position and rotation in a path 
        /// at a given point in time.
        /// </summary>
        /// <param name="position">position of the camera</param>
        /// <param name="rotation">rotation of the camera</param>
        /// <param name="time">point in time in seconds since game start</param>
        public void Add(Vector3 position, Quaternion rotation, float time)
        {
            data.Add(new PathData(position, rotation, time));
        }

        /// <summary>
        /// Saves the captured path data in a file with given filename. The file will
        /// be overwritten if it exists.
        /// </summary>
        /// <param name="filename">name of the output file</param>
        public void Save(string filename)
        {
            List<string> outputs = new List<string>();

            foreach (PathData d in data)
            {
                string output = FloatToString(d.position.x)
                              + delimiter + FloatToString(d.position.y)
                              + delimiter + FloatToString(d.position.z)
                              + delimiter + FloatToString(d.rotation.x)
                              + delimiter + FloatToString(d.rotation.y)
                              + delimiter + FloatToString(d.rotation.z)
                              + delimiter + FloatToString(d.rotation.w)
                              + delimiter + FloatToString(d.time);
                outputs.Add(output);
            }

            // WriteAllLines creates a file, writes a collection of strings to the file,
            // and then closes the file.  You do NOT need to call Flush() or Close().
            System.IO.File.WriteAllLines(filename, outputs);
        }

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
        /// The delimiter to separate data points within the same line in the output file.
        /// </summary>
        private static readonly char delimiter = ';';

        /// <summary>
        /// The minimal number of columns a CSV file containing path data must have:
        /// 3 (position = Vector3) + 4 (rotation = Quaternion) + 1 (time = float) = 7.
        /// </summary>
        private static readonly int minimalColumns = 7;

        /// <summary>
        /// Name of the file from which the path was loaded. May be the empty string
        /// if the path was not loaded but created differently.
        /// </summary>
        private readonly string path = "";

        /// <summary>
        /// Loads the path data from a the file.
        /// </summary>
        public static CameraPath ReadPath(string path)
        {
            string[] data = System.IO.File.ReadAllLines(path);
            CameraPath result = new CameraPath(path);

            int i = 0;
            foreach (string line in data)
            {
                string[] coordinates = line.Split(delimiter);

                if (coordinates.Length < minimalColumns)
                {
                    Debug.LogErrorFormat
                        ("Data format error at line {0} in file {1}: expected at least {2} entries separated by {3}. Got: {4} in '{5}'.\n",
                         i + 1, path, minimalColumns, delimiter, coordinates.Length, line);
                }
                else
                {
                    Vector3 position;
                    position.x = float.Parse(coordinates[0], CultureInfo.InvariantCulture);
                    position.y = float.Parse(coordinates[1], CultureInfo.InvariantCulture);
                    position.z = float.Parse(coordinates[2], CultureInfo.InvariantCulture);

                    Quaternion rotation;
                    rotation.x = float.Parse(coordinates[3], CultureInfo.InvariantCulture);
                    rotation.y = float.Parse(coordinates[4], CultureInfo.InvariantCulture);
                    rotation.z = float.Parse(coordinates[5], CultureInfo.InvariantCulture);
                    rotation.w = float.Parse(coordinates[6], CultureInfo.InvariantCulture);

                    float time;
                    time = float.Parse(coordinates[7], CultureInfo.InvariantCulture);

                    result.Add(position, rotation, time);
                    // Note: We ignore all remaining columns if there are any.
                }
                i++;
            }
            return result;
        }

        /// <summary>
        /// Returns an enumerator over all path data entries in the path.
        /// 
        /// Implements interface IEnumerable.
        /// </summary>
        /// <returns>an enumerator over all path data entries in the path</returns>
        public IEnumerator GetEnumerator()
        {
            return (IEnumerator)this.data.GetEnumerator();
        }

        public GameObject Draw()
        {
            GameObject result = new GameObject(string.IsNullOrEmpty(path) ? "anonymous path" : path);

            LineRenderer line = result.AddComponent<LineRenderer>();

            LineFactory.SetDefaults(line);
            LineFactory.SetColors(line, StartDefaultColor, EndDefaultColor);
            LineFactory.SetWidth(line, lineWidth);

            line.useWorldSpace = true;

            // All lines have the same material to reduce the number of drawing calls.
            line.sharedMaterial = material;

            // Set the line positions along the path.
            Vector3[] positions = new Vector3[data.Count];
            int i = 0;
            foreach (PathData d in data)
            {
                positions[i] = d.position;
                i++;
            }

            line.positionCount = positions.Length;
            line.SetPositions(positions);

            return result;
        }
    }
}