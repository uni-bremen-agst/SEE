using Sirenix.OdinInspector;

namespace SEE.GameObjects
{
    /// <summary>
    /// Attribute of an author edge connecting an <see cref="AuthorSphere"/>
    /// to <see cref="AuthorRef"/>.
    /// </summary>
    public class AuthorEdge : SerializedMonoBehaviour
    {
        /// <summary>
        /// Reference to the target node this edge connects to.
        /// </summary>
        public AuthorRef TargetNode;

        /// <summary>
        /// Reference to the <see cref="GameObjects.AuthorSphere"/> that this edge originates from.
        /// </summary>
        public AuthorSphere AuthorSphere;

        /// <summary>
        /// Churn of the author for the specific file this edge connects to.
        /// </summary>
        public int Churn;
    }
}
