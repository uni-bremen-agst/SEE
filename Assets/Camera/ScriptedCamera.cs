using System.Collections.Generic;
using UnityEngine;
using System;
using TinySpline;
using System.Globalization;

/// <summary>
/// A script to move a camera programmatically along a path. This
/// script must be added as a component to a camera.
/// </summary>
public class ScriptedCamera : MonoBehaviour
{
    /// <summary>
    /// The coordinates to interpolate. W stores the time.
    /// </summary>
    private Vector4[] locations = new Vector4[2];

    /// <summary>
    /// Name of the file where to load the captured data camera path points from.
    /// </summary>
    public string filename = "path.csv";

    /// <summary>
    /// The interpolated spline.
    /// </summary>
    private BSpline spline;

    /// <summary>
    /// Accumulated time since game start in seconds.
    /// </summary>
    private float time = 0.0f;

    private IList<double> VectorsToList(Vector4[] vectors)
    {
        List<double> list = new List<double>();
        foreach(Vector4 vec in vectors)
        {
            list.Add(vec.x);
            list.Add(vec.y);
            list.Add(vec.z);
            list.Add(vec.w);
        }
        return list;
    }

    private Vector4[] ListToVectors(IList<double> list)
    {
        int num = list.Count / 4;
        Vector4[] vectors = new Vector4[num];
        for (int i = 0; i < num; i++)
        {
            vectors[i] = new Vector4(
                (float)list[i * 4],
                (float)list[i * 4 + 1],
                (float)list[i * 4 + 2],
                (float)list[i * 4 + 3]
                );
        }
        return vectors;
    }

    /// <summary>
    /// The delimiter to separate data points within the same line.
    /// </summary>
    private const char delimiter = ';';

    /// <summary>
    /// The minimal number of columns a CSV file containing path data must have.
    /// </summary>
    private const int minimalColumns = 4;

    /// <summary>
    /// Returns the name of the file from which to read the path data.
    /// </summary>
    /// <returns>name of the file from which to read the path data</returns>
    private string Filename()
    {
        return Application.persistentDataPath + "/" + filename;
    }

    /// <summary>
    /// Loads the path data from a the file.
    /// </summary>
    private void ReadPath()
    {
        // WriteAllLines creates a file, writes a collection of strings to the file,
        // and then closes the file. You do NOT need to call Flush() or Close().
        string path = Filename();
        string [] data = System.IO.File.ReadAllLines(path);
        locations = new Vector4[data.Length];

        int i = 0;
        foreach (string line in data)
        {
            string[] coordinates = line.Split(delimiter);
            Vector4 coordinate = Vector4.zero;
            if (coordinates.Length < minimalColumns)
            {
                Debug.LogErrorFormat
                    ("Data format error at line {0} in file {1}: expected at least {2} entries separated by {3}. Got: {4} in '{5}'.\n",
                     i+1, path, minimalColumns, delimiter, coordinates.Length, line);
            }
            else
            {
                coordinate.x = float.Parse(coordinates[0], CultureInfo.InvariantCulture);
                coordinate.y = float.Parse(coordinates[1], CultureInfo.InvariantCulture);
                coordinate.z = float.Parse(coordinates[2], CultureInfo.InvariantCulture);
                coordinate.w = float.Parse(coordinates[3], CultureInfo.InvariantCulture);
                // Note: We ignore all remaining columns.
            }
            locations[i] = coordinate;
            i++;
        }
        Debug.LogFormat("Read camera path from {0}\n", path);
    }

    // Start is called before the first frame update.
    void Start()
    {
        try
        {
            ReadPath();
        }
        catch (Exception e)
        {
            Debug.LogErrorFormat("ScriptedCamera: Could not read path from file {0}: {1}\n", Filename(), e.ToString());
            return;
        }
        if (locations.Length < 2)
        {
            Debug.LogWarning("ScriptedCamera: Requiring at least two locations.\n");
            return;
        }
        try
        {
            spline = TinySpline.Utils.interpolateCubic(VectorsToList(locations), 4);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"ScriptedCamera: Interpolation failed with error '{e.Message}'\n");
            Debug.LogWarning($"ScriptedCamera: Creating spline with default location\n");
            spline = new BSpline(1, 4, 0);
            spline.controlPoints = new List<double> { 0, 0, 0, 0 };
        }
        transform.position = ListToVectors(spline.controlPointAt(0))[0];
    }

    /// <summary>
    /// Update is called once per frame and moves the camera along the
    /// timed path.
    /// </summary>
    void Update()
    {
        // Time.deltaTime is the time since the last Update() in seconds.
        time += Time.deltaTime;
        transform.position = ListToVectors(spline.bisect(time, 0.001, false, 3).result)[0];
    }
}
