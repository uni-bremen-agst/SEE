using System.Collections;
using System.Collections.Generic;
using HighlightPlus;
using SEE.GO;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

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
        /// The number of edges to authors of this specific file.
        /// </summary>
        public int Count => edges.Count;

        /// <summary>
        /// Creates the <see cref="effect"/> used for potential edit conflicts
        /// and updates all <see cref="edges"/> via <see cref="UpdateEdges"/>.
        /// </summary>
        private void Awake()
        {
            effect = gameObject.GetComponent<HighlightEffect>();
            if (effect == null)
            {
                effect = gameObject.AddComponent<HighlightEffect>();
                /// By default, an outline is set, which we do not want.
                /// We reset it to 0 only if we created the <see cref="HighlightEffect"/>;
                /// otherwise we assume whoever set it chose a meaningful value, which
                /// we do not want to disable.
                effect.outline = 0;
            }
            // FIXME: The icon's mesh must respect the portal.
            effect.iconFXLightColor = Color.yellow;
            effect.iconFXDarkColor = Color.yellow;
            // The iconFXOffset is relative to the object, that is, not world space.
            effect.iconFXOffset = Vector3.zero;
            effect.iconFXRotationSpeed = 0f;
            effect.iconFXAnimationOption = IconAnimationOption.VerticalBounce;
            effect.iconFXAnimationAmount = 0.1f;
            effect.iconFXAnimationSpeed = 1f;
            effect.iconFXScale = 0.2f;
            effect.iconFXStayDuration = float.PositiveInfinity;

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
        /// Backing field for the <see cref="HighlightEffect"/>
        /// attached to this game object.
        /// </summary>
        private HighlightEffect effect;

        /// <summary>
        /// If there are more than one edge, the highlight effect for a potential
        /// edit conflict is turned on; otherwise it is turned off.
        /// </summary>
        private void UpdateConflictVisibility()
        {
            if (edges.Count > 1)
            {
                // It is not suffient to only activate iconFX. This field means
                // only that the icon should be used when the object is highlighted;
                // it doesn't mean that it is actually highlighted. That is why
                // we also need to set highlighted to true here.
                effect.highlighted = true;
                effect.iconFX = true;
            }
            else
            {
                // We do not set highlighted to false here because there might be
                // other reasons why the object is highlighted.
                effect.iconFX = false;
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
        /// <param name="authorEdge">edge to be added (must not be null)</param>
        /// <exception cref="System.ArgumentNullException">thrown in case
        /// <paramref name="authorEdge"/> is null</exception>
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
        /// <param name="authorEdge">edge to be removed</param>
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
