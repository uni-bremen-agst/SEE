using UnityEngine;
using SEE.DataModel;

namespace SEE.GO
{
    /// <summary>
    /// A reference to a graph edge that can be attached to a game object as a component.
    /// </summary>
    [System.Serializable]
    public class EdgeRef : MonoBehaviour
    {
        [SerializeField]
        public Edge edge;
    }
}
