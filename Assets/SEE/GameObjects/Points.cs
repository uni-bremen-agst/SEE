using UnityEngine;
using OdinSerializer;

namespace SEE.GO
{

    /// <summary>
    /// A reference to a graph edge that can be attached to a game object as a component.
    /// </summary>
    public class Points : SerializedMonoBehaviour
    {
        public Vector3[] controlPoints;

        public Vector3[] linePoints;
    }
}

