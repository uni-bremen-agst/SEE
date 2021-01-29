using UnityEngine;
using OdinSerializer;

namespace SEE.GO
{

    /// <summary>
    /// Saves the control points and line points of an edge.
    /// </summary>
    public class Points : SerializedMonoBehaviour
    {
        ///The control points of the edge
        public Vector3[] controlPoints;

        ///The line points of the edge
        public Vector3[] linePoints;
    }
}

