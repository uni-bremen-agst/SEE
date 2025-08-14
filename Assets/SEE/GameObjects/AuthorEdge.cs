using Sirenix.OdinInspector;

namespace SEE.GameObjects
{
    /// <summary>
    /// Attribute of an author edge connecting an author sphere to a node.
    /// </summary>
    public class AuthorEdge : SerializedMonoBehaviour
    {
        /// <summary>
        /// Reference to the target node this edge connects to.
        /// </summary>
        public AuthorRef targetNode;

        /// <summary>
        /// Reference to the <see cref="AuthorSphere"/> that this edge originates from.
        /// </summary>
        public AuthorSphere authorSphere;
    }
}
