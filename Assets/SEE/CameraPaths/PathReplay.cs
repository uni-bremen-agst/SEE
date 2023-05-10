using SEE.Controls;
using SEE.Utils;
using System;
using System.Collections.Generic;
using TinySpline;
using UnityEngine;

namespace SEE.CameraPaths
{
    /// <summary>
    /// A script to move a game object, e.g., a camera, programmatically along a recorded path. 
    /// </summary>
    public class PathReplay : MonoBehaviour
    {
        /// <summary>
        /// The object to be moved along the recorded path.
        /// </summary>
        [Tooltip("The object to be moved along the recorded path. If not set, the main camera will be moved.")]
        public GameObject MovedObject;

        /// <summary>
        /// Name of the file where to load the captured data camera path points from.
        /// </summary>
        [Tooltip("Name of the file where to load the path data.")]
        public string Filename = "path" + CameraPath.DotPathFileExtension;

        /// <summary>
        /// As to whether the path should be drawn as a sequence of lines in the game.
        /// </summary>
        [Tooltip("Whether the path should be drawn as a sequence of lines in the game.")]
        public bool ShowPath = false;

        /// <summary>
        /// If true, the moved object and all its ancestors will be activated.
        /// This may be useful if there are game objects not activated by default.
        /// </summary>
        [Tooltip("If true, the moved object and all its ancestors will be activated. "
                  + "This may be useful if there are game objects not activated by default.")]
        public bool ActivateOnStart = false;

        /// <summary>
        /// If true, the path is replayed, otherwise stopped.
        /// </summary>
        private bool isRunning = true;

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
        /// The interpolated spline.
        /// </summary>
        private BSpline spline;

        /// <summary>
        /// Accumulated time since game start in seconds.
        /// </summary>
        private float time = 0.0f;

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
        private void Start()
        {
            if (MovedObject == null)
            {               
                // We are using the main camera.
                MovedObject = MainCamera.Camera.gameObject;
                if (MovedObject == null)
                {
                    Debug.LogError($"No game object to be moved was assigned. No camera was found. The movement will be disabled.\n");
                    enabled = false;
                }
                else
                {
                    Debug.Log($"No game object to be moved was assigned. We will be using the camera at {MovedObject.name}.\n");
                }
            }
            else if (ActivateOnStart)
            {
                // A MovedObject was assigned and we are to activate it on start.
                Activate(MovedObject, true);
            }
            try
            {
                path = CameraPath.ReadPath(Filename);
                Debug.Log($"Read path for {MovedObject.name} from {Filename}\n");
                if (ShowPath)
                {
                    path.Draw();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"PathReplay: Could not read path from file {Filename} for {MovedObject.name}: {e.Message}\n");
                enabled = false;
                return;
            }
            if (path.Count < 1)
            {
                Debug.LogWarning("PathReplay: Requiring at least one location.\n");
                enabled = false;
                return;
            }
            try
            {
                spline = BSpline.InterpolateCatmullRom(VectorsToList(path), 4);
                enabled = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"PathReplay: Interpolation failed with error '{e.Message}'\n");
                Debug.LogWarning($"PathReplay: Creating spline with default location\n");
                spline = new BSpline(1, 4, 0)
                {
                    ControlPoints = new List<double> { 0, 0, 0, 0 }
                };
                enabled = false;
            }
            MovedObject.transform.position = ListToVectors(spline.ControlPoints)[0];
            MovedObject.transform.rotation = path[0].rotation;
        }

        /// <summary>
        /// Activate (true)/deactivates (false) <paramref name="parent"/> and all its descendants.
        /// </summary>
        /// <param name="parent">root of the game-object tree</param>
        /// <param name="activate">whether the game objects should be activated or deactivated</param>
        private static void Activate(GameObject parent, bool activate)
        {
            parent.SetActive(activate);
            foreach (Transform child in parent.transform)
            {
                Activate(child.gameObject, activate);
            }
        }

        /// <summary>
        /// Update is called once per frame and moves and rotates the camera along the
        /// timed path.
        /// </summary>
        private void Update()
        {
            if (SEEInput.TogglePathPlaying())
            {
                isRunning = !isRunning;
            }
            if (isRunning)
            {
                time += Time.deltaTime;
                MovedObject.transform.position = ListToVectors(spline.Bisect(time, 0.001, false, 3).Result)[0];
                MovedObject.transform.rotation = Forward(ref location, time);
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
