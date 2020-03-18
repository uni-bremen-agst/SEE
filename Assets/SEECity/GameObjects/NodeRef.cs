using UnityEngine;
using SEE.DataModel;

namespace SEE.Layout
{
    /// <summary>
    /// A reference to a graph node that can be attached to a game object as a component.
    /// </summary>
    [System.Serializable]
    public class NodeRef : MonoBehaviour
    {
        [SerializeField]
        public Node node;
    }
}
