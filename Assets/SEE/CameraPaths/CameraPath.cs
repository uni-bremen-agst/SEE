using SEE.DataModel;
using SEE.GO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SEE.CameraPaths
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
        public const string PathFileExtension = "csv";
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
        public static Color StartDefaultColor = Color.cyan;

        // The default color for the end of the lines of a path.
        public static Color EndDefaultColor = Color.magenta;

        /// <summary>
        /// The material we use for the line drawing the path. All paths share the same material.
        /// </summary>
        private static Material pathMaterial;

        /// <summary>
        /// The material we use for the line drawing the lookout direction. All these lines share the same material.
        /// </summary>
        private static Material lookoutMaterial;

        /// <summary>
        /// The line width for drawing paths.
        /// </summary>
        private const float lineWidth = 0.1f;

        /// <summary>
        /// This factor is one of the parameters for the size of the spheres we draw for
        /// aggregegated locations in the path.
        /// </summary>
        private const float timeFactor = 0.1f;

        /// <summary>
        /// The path data captured.
        /// </summary>
        private readonly List<PathData> data = new List<PathData>();

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
        ///
        /// The file format is as follows. Each line has exactly minimalColumns entries
        /// seperated by the delimiter. The first three entries form the position
        /// vector, the second three entries the rotation in Euler angles, and the
        /// the last entry contains the time. Each value is a float.
        /// </summary>
        /// <param name="filename">name of the output file</param>
        public void Save(string filename)
        {
            List<string> outputs = new List<string>();

            foreach (PathData d in data)
            {
                Vector3 rotation = d.rotation.eulerAngles;
                string output = FloatToString(d.position.x)
                              + delimiter + FloatToString(d.position.y)
                              + delimiter + FloatToString(d.position.z)
                              + delimiter + FloatToString(rotation.x)
                              + delimiter + FloatToString(rotation.y)
                              + delimiter + FloatToString(rotation.z)
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
        /// <param name="value">the value to be converted</param>
        /// <returns>the float as a string in the requested format</returns>
        private static string FloatToString(float value)
        {
            return value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The delimiter to separate data points within the same line in the output file.
        /// </summary>
        private static readonly char delimiter = ';';

        /// <summary>
        /// The minimal number of columns a CSV file containing path data must have:
        /// 3 (position = Vector3) + 3 (rotation = Euler angles) + 1 (time = float) = 6.
        /// </summary>
        private static readonly int minimalColumns = 6;

        /// <summary>
        /// Name of the file from which the path was loaded. May be the empty string
        /// if the path was not loaded but created differently.
        /// </summary>
        private readonly string path = "";

        /// <summary>
        /// Loads the path data from a the file.
        ///
        /// Precondition: Each line in the file has at least minimalColumns entries
        /// seperated by the delimiter. The first three entries form the position
        /// vector, the second three entries the rotation in Euler angles, and the
        /// the last entry contains the time. Each value must be a float. There
        /// may be additional columns, which are ignored.
        ///
        /// May throw any exception that can be thrown by System.IO.File.ReadAllLines.
        /// </summary>
        /// <param name="filename">name of the file containing the path data</param>
        public static CameraPath ReadPath(string filename)
        {
            string[] data = System.IO.File.ReadAllLines(filename);
            CameraPath result = new CameraPath(filename);

            int i = 0;
            foreach (string line in data)
            {
                string[] coordinates = line.Split(delimiter);

                if (coordinates.Length < minimalColumns)
                {
                    Debug.LogErrorFormat
                        ("Data format error at line {0} in file {1}: expected at least {2} entries separated by {3}. Got: {4} in '{5}'.\n",
                         i + 1, filename, minimalColumns, delimiter, coordinates.Length, line);
                }
                else
                {
                    Vector3 position;
                    position.x = float.Parse(coordinates[0], CultureInfo.InvariantCulture);
                    position.y = float.Parse(coordinates[1], CultureInfo.InvariantCulture);
                    position.z = float.Parse(coordinates[2], CultureInfo.InvariantCulture);

                    Quaternion rotation;
                    {
                        Vector3 eulerAngles;
                        eulerAngles.x = float.Parse(coordinates[3], CultureInfo.InvariantCulture);
                        eulerAngles.y = float.Parse(coordinates[4], CultureInfo.InvariantCulture);
                        eulerAngles.z = float.Parse(coordinates[5], CultureInfo.InvariantCulture);
                        rotation = Quaternion.Euler(eulerAngles.x, eulerAngles.y, eulerAngles.z);
                    }

                    float time = float.Parse(coordinates[6], CultureInfo.InvariantCulture);

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
            return data.GetEnumerator();
        }

        /// <summary>
        /// Draws the path in the scene as a sequence of lines along the path locations. At each
        /// recorded location, the lookout is visualized as a single line towards the direction
        /// of the lookout, whose length is proportional to the time the camera has looked into
        /// this direction. The whole path data is created as a single static root game object tagged
        /// by Tags.Path whose children represent the movements and the lookouts.
        /// </summary>
        /// <returns>game object representing the path</returns>
        public GameObject Draw()
        {
            GameObject result = new GameObject(string.IsNullOrEmpty(path) ? "anonymous path" : path)
            {
                tag = Tags.Path,
                isStatic = true
            };

            DrawPath(result);
            DrawLookOuts(result);
            return result;
        }

        /// <summary>
        /// Draws the lookouts along the path as a set of single lines nested within the given
        /// game object. The lookouts are determined through grouping path entries with similar
        /// position and rotation independent of the point in time. Each group is represented
        /// as a single line starting at the position of the path group and directing into
        /// the direction the camera has looked in this path group. The length of this line
        /// is proportional to the sum over all times of the elements of a path group.
        /// </summary>
        /// <param name="pathGameObject">the parent the game objects created here are to become
        /// children of</param>
        private void DrawLookOuts(GameObject pathGameObject)
        {
            int i = 0;
            foreach (PathData d in Aggregate(data))
            {
                /*
                {
                    float sphereRadius = 0.5f;
                    // draw a sphere whose size is proportional to the time the camera has
                    // spent at this location
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.name = "location " + i.ToString();
                    sphere.isStatic = true;
                    Renderer renderer = sphere.GetComponent<Renderer>();

                    // Object should not cast shadows: too expensive and may hide information.
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

                    sphere.transform.localScale = 2 * sphereRadius * Vector3.one;
                    sphere.transform.parent = result.transform;
                    sphere.transform.position = d.position;
                }
                */
                // draw a line towards the look out whose length is proportional to the
                // time the camera has looked into this direction
                {
                    GameObject direction = new GameObject("direction " + i.ToString());
                    direction.isStatic = true;
                    direction.transform.parent = pathGameObject.transform;
                    direction.transform.position = d.position;

                    LineRenderer line = direction.AddComponent<LineRenderer>();

                    LineFactory.SetDefaults(line);
                    LineFactory.SetColor(line, Color.red);
                    LineFactory.SetWidth(line, lineWidth);

                    line.useWorldSpace = true;

                    // All path lines have the same material to reduce the number of drawing calls.
                    if (lookoutMaterial == null)
                    {
                        lookoutMaterial = Materials.New(Materials.ShaderType.TransparentLine, Color.white);
                    }
                    line.sharedMaterial = lookoutMaterial;

                    // Line from the surface of the sphere along the direction of the lookout
                    // proportional to the length of the lookout
                    Vector3[] positions = new Vector3[2];
                    positions[0] = d.position;
                    positions[1] = positions[0] + d.rotation * Vector3.forward * d.time * timeFactor;

                    line.positionCount = positions.Length;
                    line.SetPositions(positions);
                }
                i++;
            }
        }

        /// <summary>
        /// Returns 0 if the two vectors are similar, i.e., if the respective differences
        /// of their co-ordinates are not greater than the given allowed difference.
        /// Returns 1 or -1, respectively, if the vectors are not similar enough.
        /// </summary>
        /// <param name="me">one vector to be compared</param>
        /// <param name="other">other vector to be compared against the first one</param>
        /// <param name="allowedDifference">allowable difference between co-ordinates</param>
        /// <returns></returns>
        private int CompareTo(Vector3 me, Vector3 other, float allowedDifference)
        {
            {
                float delta = me.x - other.x;
                if (Mathf.Abs(delta) > allowedDifference)
                {
                    return delta > 0 ? 1 : -1;
                }
            }
            {
                float delta = me.y - other.y;
                if (Mathf.Abs(delta) > allowedDifference)
                {
                    return delta > 0 ? 1 : -1;
                }
            }
            {
                float delta = me.z - other.z;
                if (Mathf.Abs(delta) > allowedDifference)
                {
                    return delta > 0 ? 1 : -1;
                }
            }
            return 0;
        }

        /// <summary>
        /// Returns a aggregated path of the given path. The aggregation is formed
        /// by grouping path entries with similar location and lookout independent of
        /// their points in time. The resulting time is the sum of all times of the
        /// entries within the same group.
        ///
        /// The order of the resulting aggregated path is arbitrary. It will not be consistent
        /// with the order of the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>path aggregation</returns>
        private List<PathData> Aggregate(List<PathData> path)
        {
            if (path.Count <= 1)
            {
                return path;
            }
            // We copy the data because we do not want to change the order in the input data list.
            List<PathData> copy = new List<PathData>();
            copy.AddRange(path);
            // Sort all data so that we can aggregated them into groups. A group is a set of
            // path data that have a similar position and rotation. Similar means, the difference
            // between their respective position and rotation co-ordinates falls below our
            // threshold.
            copy.Sort(delegate (PathData x, PathData y)
            {
                return CompareTo(x, y);
            });

            List<PathData> result = new List<PathData>();

            PathData current = new PathData();
            bool first = true;
            foreach (PathData d in copy)
            {
                if (first)
                {
                    first = false;
                    current = d;
                }
                else if (CompareTo(d, current) == 0)
                {
                    // d and current are in the same group
                    current.time += d.time;
                }
                else
                {
                    // a new group starts
                    result.Add(current);
                    current = d;
                }
            }
            result.Add(current);
            return result;
        }

        /// <summary>
        /// Compares the first argument against the second one based on the similarity
        /// of their position and rotation (ignoring time).
        /// </summary>
        /// <param name="x">first argument</param>
        /// <param name="y">second argument to be compared to the first argument</param>
        /// <returns>0 if x and y are similar, 1 if x is before y, otherwise -1 (the 'before'
        /// relation is arbitrary)</returns>
        private int CompareTo(PathData x, PathData y)
        {
            // The following value defines the difference below we still consider two
            // corresponding co-ordinates similar enough.
            float allowedDifference = 0.05f;
            // We do not really care about the precise order as we use this order only
            // for the aggregation. The main concern is that all path entries with
            // similar position and rotation are neighbors in the resulting order.
            int positionCompareTo = CompareTo(x.position, y.position, allowedDifference);
            if (positionCompareTo != 0)
            {
                return positionCompareTo;
            }
            return CompareTo(x.rotation.eulerAngles, y.rotation.eulerAngles, allowedDifference);
        }

        /// <summary>
        /// Draws the lines of the path along which the camera moved. The line
        /// is created as a line rendering becoming a component of the given object
        /// representing the path.
        /// </summary>
        /// <param name="pathGameObject"></param>
        private void DrawPath(GameObject pathGameObject)
        {
            LineRenderer line = pathGameObject.AddComponent<LineRenderer>();

            LineFactory.SetDefaults(line);
            LineFactory.SetColors(line, StartDefaultColor, EndDefaultColor);
            LineFactory.SetWidth(line, lineWidth);

            line.useWorldSpace = true;

            // All path lines have the same material to reduce the number of drawing calls.
            if (pathMaterial == null)
            {
                pathMaterial = Materials.New(Materials.ShaderType.TransparentLine, Color.white);
            }
            line.sharedMaterial = pathMaterial;

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
        }

        private string Dump(Vector3 v)
        {
            return ("(" + v.x.ToString("0.00000")
                    + ", " + v.y.ToString("0.00000")
                    + ", " + v.z.ToString("0.00000") + ")");
        }

        public void Dump()
        {
            foreach (PathData d in data)
            {
                Debug.LogFormat("position(x,y,z)={0} rotation={1}, rotation(x, y, z, w)= ({2}, {3}, {4}, {5}), rotation(Euler angles)={6}, time={7})\n",
                                d.position,
                                d.rotation,
                                d.rotation.x.ToString("0.000"), d.rotation.y.ToString("0.000"), d.rotation.z.ToString("0.000"), d.rotation.w.ToString("0.000"),
                                Dump(d.rotation.eulerAngles),
                                d.time);
            }
        }
    }
}