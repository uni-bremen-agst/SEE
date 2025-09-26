using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace SEE.GameObjects
{
    /// <summary>
    /// Holds the connections of a file all its authors.
    /// This component will be attached to all file game nodes.
    /// </summary>
    public class AuthorRef : SerializedMonoBehaviour
    {
        /// <summary>
        /// The edges to the authors of this specific file.
        /// </summary>
        public IList<AuthorEdge> Edges = new List<AuthorEdge>();

        /// <summary>
        /// Updates all <see cref="Edges"/> connected to this file node.
        /// Should be called after the file node moved.
        /// </summary>
        internal void UpdateEdges()
        {
            foreach (AuthorEdge edge in Edges)
            {
                edge.Update();
            }
        }
    }
}
