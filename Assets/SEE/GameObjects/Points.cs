using UnityEngine;
using OdinSerializer;

namespace SEE.GO
{
    /// <summary>
    /// Saves the control points and line points of an edge.
    /// </summary>
    public class Points : SerializedMonoBehaviour
    {
        /// <summary>
        ///The control points of the edge
        /// </summary>
        public Vector3[] controlPoints;

        /// <summary>
        ///The line points of the edge
        /// </summary>
        public Vector3[] linePoints;
    }
}

