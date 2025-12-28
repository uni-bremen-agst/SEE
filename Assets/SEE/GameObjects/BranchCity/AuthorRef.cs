using SEE.Game.Operator;
using SEE.GO;
using Sirenix.Serialization;
using System.Collections;
using System.Collections.Generic;

namespace SEE.GameObjects.BranchCity
{
    /// <summary>
    /// Holds the connections of a file to all its authors.
    /// This component will be attached to all file game nodes in a <see cref="BranchCity"/>.
    /// </summary>
    public class AuthorRef : VCSDecorator, IEnumerable<AuthorEdge>
    {
        /// <summary>
        /// The edges to the authors of this specific file.
        /// </summary>
        [OdinSerialize]
        private readonly HashSet<AuthorEdge> edges = new();

        /// <summary>
        /// The number of edges to authors of this specific file.
        /// </summary>
        public int Count => edges.Count;

        /// <summary>
        /// Creates the <see cref="effect"/> used for potential edit conflicts
        /// and updates all <see cref="edges"/> via <see cref="UpdateEdges"/>.
        /// </summary>
        private void Start()
        {
            // We may already have edges from the serialization and might
            // need to update these.
            UpdateEdges();
        }

        /// <summary>
        /// Updates the visibility of all edges and the highlighting for
        /// potential edit conflicts.
        /// </summary>
        private void UpdateEdges()
        {
            UpdateEdgeVisibility();
            UpdateConflictVisibility();
        }

        /// <summary>
        /// If there is more than one edge and conflict markers are enabled,
        /// the highlight effect for a potential edit conflict is turned on;
        /// otherwise it is turned off.
        /// </summary>
        /// <remarks>The update takes place only if we are in play mode.
        /// Our <see cref="NodeOperator"/>s are not prepared to run in the
        /// editor mode.</remarks>
        public void UpdateConflictVisibility()
        {
            if (City.EnableConflictMarkers && edges.Count > 1)
            {
                gameObject.AddOrGetComponent<NodeOperator>().EnableDynamicMark();
            }
            else
            {
                gameObject.AddOrGetComponent<NodeOperator>().DisableDynamicMark();
            }
        }

        /// <summary>
        /// Updates the visibility of all edges to authors of this specific file.
        /// </summary>
        private void UpdateEdgeVisibility()
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
        /// Adds an edge to the list of edges to authors of this specific file.
        /// </summary>
        /// <param name="authorEdge">Edge to be added (must not be null).</param>
        /// <exception cref="System.ArgumentNullException">Thrown in case
        /// <paramref name="authorEdge"/> is null.</exception>
        internal void Add(AuthorEdge authorEdge)
        {
            if (authorEdge == null)
            {
                throw new System.ArgumentNullException(nameof(authorEdge));
            }
            if (edges.Add(authorEdge))
            {
                UpdateEdges();
            }
        }

        /// <summary>
        /// Removes an edge from the list of edges to authors of this specific file.
        /// </summary>
        /// <param name="authorEdge">Edge to be removed.</param>
        internal void Remove(AuthorEdge authorEdge)
        {
            if (authorEdge == null)
            {
                throw new System.ArgumentNullException(nameof(authorEdge));
            }
            if (edges.Remove(authorEdge))
            {
                UpdateEdges();
            }
        }

        /// <summary>
        /// Allows to iterate over all <see cref="AuthorEdge"/>s of this file node.
        /// </summary>
        /// <returns>Iterator for all edges.</returns>
        public IEnumerator<AuthorEdge> GetEnumerator()
        {
            return edges.GetEnumerator();
        }

        /// <summary>
        /// Allows to iterate over all <see cref="AuthorEdge"/>s of this file node.
        /// </summary>
        /// <returns>Iterator for all edges.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return edges.GetEnumerator();
        }
    }
}
