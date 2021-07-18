using System;

namespace SEE.Game.GestureRecognition
{
    /// <summary>
    /// Representation of a point within the gestures point cloud.
    /// </summary>
    [Serializable]
    public class GesturePoint
    {
        public float X, Y;
        public int StrokeID;
        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="x">The x coordindate.</param>
        /// <param name="y">They coordindate</param>
        /// <param name="strokeID">The index of the stroke when multi stroke detection is used</param>
        public GesturePoint(float x, float y, int strokeID)
        {
            X = x;
            Y = y;
            StrokeID = strokeID;
        }
    }
}