using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace SEE.GameObjects
{
    /// <summary>
    /// Holds the connections of a file all its authors.
    /// This component will be attached to all file game nodes.
    /// </summary>
    public class AuthorRef : SerializedMonoBehaviour, IEnumerable<AuthorEdge>
    {
        /// <summary>
        /// The edges to the authors of this specific file.
        /// </summary>
        [OdinSerialize]
        private readonly HashSet<AuthorEdge> edges = new();

        /// <summary>
        /// Adds an edge to the list of edges to authors of this specific file.
        /// </summary>
        /// <param name="authorEdge">edge to be added</param>
        internal void Add(AuthorEdge authorEdge)
        {
            if (edges.Add(authorEdge))
            {
                UpdateVisibility();
            }
        }

        /// <summary>
        /// Updates the visibility of all edges to authors of this specific file.
        /// </summary>
        private void UpdateVisibility()
        {
            // If the visibility of one edge changes, all edges must be updated.
            foreach (AuthorEdge authorEdge in edges)
            {
                if (!authorEdge.UpdateVisibility(edges.Count))
                {
                    // No change in visibility, that is, none of the others
                    // can have changes, so we can stop here.
                    break;
                }
            }
        }

        /// <summary>
        /// Removes an edge from the list of edges to authors of this specific file.
        /// </summary>
        /// <param name="authorEdge">edge to be added</param>
        internal void Remove(AuthorEdge authorEdge)
        {
            if (edges.Remove(authorEdge))
            {
                UpdateVisibility();
            }
        }

        /// <summary>
        /// The number of edges to authors of this specific file.
        /// </summary>
        public int Count => edges.Count;

        /// <summary>
        /// Updates the layout of all edges to authors of this specific file.
        /// </summary>
        /// <remarks>This method should be called whenever the position of this file node changed.</remarks>
        internal void UpdateLayout()
        {
            foreach (AuthorEdge edge in edges)
            {
                edge.UpdateLayout();
            }
        }

        /// <summary>
        /// Allows to iterate over all <see cref="AuthorEdge"/>s of this file node.
        /// </summary>
        /// <returns>iterator for all edges</returns>
        public IEnumerator<AuthorEdge> GetEnumerator()
        {
            return edges.GetEnumerator();
        }

        /// <summary>
        /// Allows to iterate over all <see cref="AuthorEdge"/>s of this file node.
        /// </summary>
        /// <returns>iterator for all edges</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return edges.GetEnumerator();
        }
    }
}
