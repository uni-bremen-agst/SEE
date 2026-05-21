using UnityEngine;

namespace SEE.CameraPaths
{
    /// <summary>
    /// The data captured about a particular position (including rotation) in a camera path
    /// at a given point in time.
    /// </summary>
    public struct PathData
    {
        public Vector3 Position;    // position of the camera
        public Quaternion Rotation; // rotation of the camera
        public float Time;          // point in time in seconds since game start

        /// <summary>
        /// Sets the data captured about a particular position in a path at a given point in time.
        /// </summary>
        /// <param name="position">Position of the camera.</param>
        /// <param name="rotation">Rotation of the camera.</param>
        /// <param name="time">Point in time in seconds since game start.</param>
        public PathData(Vector3 position, Quaternion rotation, float time)
        {
            this.Position = position;
            this.Rotation = rotation;
            this.Time = time;
        }
    }
}
