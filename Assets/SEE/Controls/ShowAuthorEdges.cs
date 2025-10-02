using System;
using SEE.GameObjects;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This action shows/animates edges connecting authors spheres and nodes when
    /// the user hovers over them.
    /// This script can be added to both <see cref="AuthorSphere"/>s and game objects
    /// representing graph nodes (aka game nodes).
    /// </summary>
    internal class ShowAuthorEdges : InteractableObjectAction, IDisposable
    {
        /// <summary>
        /// Disposes the CancellationTokenSource.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Subscribes to hover events of the game object.
        /// </summary>
        void OnEnable()
        {
            if (Interactable != null)
            {
                Interactable.HoverIn += OnHoverIn;
                Interactable.HoverOut += OnHoverOut;
            }
        }

        /// <summary>
        /// Unsubscribes from hover events of the game object.
        /// </summary>
        void OnDisable()
        {
            if (Interactable != null)
            {
                Interactable.HoverIn -= OnHoverIn;
                Interactable.HoverOut -= OnHoverOut;
            }
        }

        /// <summary>
        /// Toggles the visibility of author edges for a node.
        /// </summary>
        /// <param name="isHovered">Whether the game object is currently hovered.</param>
        /// <param name="authorRef"><see cref="AuthorRef"/> instance which the user hovered over.</param>
        /// <remarks>This will be executed at the hovering of file nodes.</remarks>
        private void ToggleAuthorEdgesForNode(bool isHovered, AuthorRef authorRef)
        {
            foreach (AuthorEdge authorEdge in authorRef)
            {
                authorEdge.ShowOrHide(isHovered);
            }
        }

        /// <summary>
        /// Toggles the visibility of author edges for an author sphere.
        /// </summary>
        /// <param name="isHovered">Whether the game object is currently hovered.</param>
        /// <param name="sphere">The <see cref="AuthorSphere"/> the user hovers over.</param>
        /// <remarks>This will be executed at the hovering of authors.</remarks>
        private void ToggleAuthorEdgesForAuthorSphere(bool isHovered, AuthorSphere sphere)
        {
            foreach (AuthorEdge authorEdge in sphere.Edges)
            {
                authorEdge.ShowOrHide(isHovered);
            }
        }

        /// <summary>
        /// Toggles the visibility of author edges of the node or the author sphere the user hovers over.
        /// </summary>
        /// <param name="isHovered">Whether the game object is currently hovered.</param>
        /// <remarks>This <see cref="ShowAuthorEdges"/> component could be added to authors as
        /// well as file nodes.</remarks>
        private void ToggleAuthorEdges(bool isHovered)
        {
            if (gameObject.TryGetComponent(out AuthorRef authorRef))
            {
                // When the user hovers over a graph game node
                ToggleAuthorEdgesForNode(isHovered, authorRef);
            }
            else if (gameObject.TryGetComponent(out AuthorSphere sphere))
            {
                // When the user hovers over an AuthorSphere.
                ToggleAuthorEdgesForAuthorSphere(isHovered, sphere);
            }
        }

        /// <summary>
        /// Handles the hover-in event of the <paramref name="interactableObject"/>.
        /// Toggles the visibility of author edges for the node or author sphere the user hovers over.
        /// </summary>
        /// <param name="interactableObject">The actual object the user hovers over.</param>
        private void OnHoverIn(InteractableObject interactableObject, bool _)
        {
            ToggleAuthorEdges(true);
        }

        /// <summary>
        /// Handles the hover-out event of the interactable object.
        /// Toggles the visibility of author edges for the node or author sphere the user hovers over.
        /// </summary>
        /// <param name="interactableObject">The actual object, the user hovers over.</param>
        private void OnHoverOut(InteractableObject interactableObject, bool _)
        {
            ToggleAuthorEdges(false);
        }
    }
}
