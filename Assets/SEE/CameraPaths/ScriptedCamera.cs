using System.Collections.Generic;
using UnityEngine;
using System;
using TinySpline;

namespace SEE.CameraPaths
{
    /// <summary>
    /// A script to move a camera programmatically along a path. This
    /// script must be added as a component to a camera.
    /// </summary>
    public class ScriptedCamera : MonoBehaviour
    {
        /// <summary>
        /// The path of the camera to be followed.
        /// </summary>
        private CameraPath path;

        /// <summary>
        /// The current location index in the path. It identifies the position and rotation
        /// of the camera.
        ///
        /// Invariant: 0 <= location < path.Count
        /// </summary>
        private int location = 0;

        /// <summary>
        /// Name of the file where to load the captured data camera path points from.
        /// </summary>
        public string Filename = "path" + CameraPath.DotPathFileExtension;

        /// <summary>
        /// As to whether the path should be drawn as a sequence of lines in the game.
        /// </summary>
        public bool ShowPath = false;

        /// <summary>
        /// The interpolated spline.
        /// </summary>
        private BSpline spline;

        /// <summary>
        /// Accumulated time since game start in seconds.
        /// </summary>
        private float time = 0.0f;

        /// <summary>
        /// If enabled is true, the camera follows along the loaded path.
        /// Will be false, if an error occurs when the path is loaded.
        /// </summary>
        private bool pathIsEnabled;

        /// <summary>
        /// Returns the position co-ordinates and the times of given path
        /// as a serialized list of doubles (position.x, position.y,
        /// position.z, time for each entry in the path).
        /// </summary>
        /// <param name="path">path to be serialized</param>
        /// <returns>serialized position and timing data of path</returns>
        private IList<double> VectorsToList(CameraPath path)
        {
            List<double> list = new List<double>();
            foreach (PathData pathData in path)
            {
                list.Add(pathData.position.x);
                list.Add(pathData.position.y);
                list.Add(pathData.position.z);
                list.Add(pathData.time);
            }
            return list;
        }

        /// <summary>
        /// Deserializes the list of doubles as list of Vector4s.
        /// </summary>
        /// <param name="list">list to be deserialized</param>
        /// <returns>list of Vectors4 (x,y,z,w) where x,y,z are
        /// 3D co-ordinates and w is the time deserialized from given list</returns>
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
        /// Reads the path data from disk and sets up the data structures
        /// necessary to move the camera along the path.
        ///
        /// In case the path cannot be loaded from disk, pathIsEnabled is
        /// set to false.
        ///
        /// Start is called before the first frame update.
        /// </summary>
        void Start()
        {
            try
            {
                path = CameraPath.ReadPath(Filename);
                Debug.LogFormat("Read camera path from {0}\n", Filename);
                if (ShowPath)
                {
                    path.Draw();
                }
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("ScriptedCamera: Could not read path from file {0}: {1}\n", Filename, e.ToString());
                pathIsEnabled = false;
                return;
            }
            if (path.Count < 1)
            {
                Debug.LogWarning("ScriptedCamera: Requiring at least one location.\n");
                pathIsEnabled = false;
                return;
            }
            try
            {
                spline = TinySpline.BSpline.InterpolateCatmullRom(VectorsToList(path), 4);
                pathIsEnabled = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ScriptedCamera: Interpolation failed with error '{e.Message}'\n");
                Debug.LogWarning($"ScriptedCamera: Creating spline with default location\n");
                spline = new BSpline(1, 4, 0);
                spline.ControlPoints = new List<double> { 0, 0, 0, 0 };
                pathIsEnabled = false;
            }
            transform.position = ListToVectors(spline.ControlPointAt(0))[0];
            transform.rotation = path[0].rotation;
        }

        /// <summary>
        /// Update is called once per frame and moves and rotates the camera along the
        /// timed path.
        /// </summary>
        void Update()
        {
            if (pathIsEnabled)
            {
                // Time.deltaTime is the time since the last Update() in seconds.
                time += Time.deltaTime;
                transform.position = ListToVectors(spline.Bisect(time, 0.001, false, 3).Result)[0];
                transform.rotation = Forward(ref location, time);
            }
        }

        /// <summary>
        /// Yields the interpolated rotation on the way between two subsequent points
        /// P1 and P2 on the path, where P2 is the point path[I] at the smallest index I in path
        /// where current <= I and time <= path[I].time and P1 is path[I-1].
        ///
        /// Special cases: if current = 0, path[0].rotation is returned (the initial rotation
        /// of the camera) and if current = path.Count, the interpolated rotation between
        /// path[path.Count-2].time and path[path.Count-1].time is returned (the output current
        /// will then be path.Count-1).
        ///
        /// Increases the current index to the next point reached (but not farther than path.Count-1).
        /// </summary>
        /// <param name="current">the current index into the path; will be increased to the
        /// next index we need to reach at the given point in time</param>
        /// <param name="time">current point in time since game start</param>
        /// <returns>the interpolated rotation on the way between two subsequent path points</returns>
        private Quaternion Forward(ref int current, float time)
        {
            while (current < path.Count && path[current].time < time)
            {
                current++;
            }
            if (current == 0)
            {
                return path[0].rotation;
            }
            if (current == path.Count)
            {
                current -= 1;
            }
            // Assert: There are at least two path entries and current > 1. Thus, path[current - 1] exists.
            // Assert: path[current].time >= path[current - 1].time
            // Assert: path[current].time >= time

            // Normalize the time window so that the left time is 0.
            float previousTime = path[current - 1].time;
            float rightTime = path[current].time - previousTime;
            time -= previousTime;
            float relativeTime = time / rightTime;
            return Quaternion.Slerp(path[current - 1].rotation, path[current].rotation, relativeTime);
        }
    }
}
