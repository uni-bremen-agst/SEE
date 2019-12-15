using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a camera path with position and rotation of the camera in a series of 
/// points in time.
/// </summary>
public class CameraPath
{
    /// <summary>
    /// The number of path data captured so far.
    /// </summary>
    public int Count { get => data.Count; }

    /// <summary>
    /// The data captured about a particular position in a path at a given point in time.
    /// </summary>
    private struct PathData
    {
        public Vector3 position;    // position of the camera
        public Quaternion rotation; // rotation of the camera
        public float time;          // point in time in seconds since game start

        /// <summary>
        /// Sets the data captured about a particular position in a path at a given point in time.
        /// </summary>
        /// <param name="position">position of the camera</param>
        /// <param name="rotation">rotation of the camera</param>
        /// <param name="time">point in time in seconds since game start</param>
        public PathData(Vector3 position, Quaternion rotation, float time)
        {
            this.position = position;
            this.rotation = rotation;
            this.time = time;
        }
    }

    /// <summary>
    /// The path data captured.
    /// </summary>
    private List<PathData> data = new List<PathData>();

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
    private const string delimiter = ";";

}
