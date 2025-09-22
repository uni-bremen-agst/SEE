using System;
using System.Threading;
using SEE.Game.City;
using SEE.GameObjects;
using SEE.GO;
using UnityEngine;

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
        /// The token used to cancel the edge toggling operation.
        /// </summary>
        private CancellationTokenSource edgeToggleToken;

        /// <summary>
        /// Disposes the CancellationTokenSource.
        /// </summary>
        public void Dispose()
        {
            edgeToggleToken?.Dispose();
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
        /// Toggles the visibility of author edges for a node.
        /// </summary>
        /// <param name="show">Should be set to true if the animation should show the edges and false if it should hide them.</param>
        /// <param name="authorRef"><see cref="AuthorRef"/> instance which the user hovered over.</param>
        /// <param name="branchCity">Configuration of the code city.</param>
        private void ToggleAuthorEdgesForNode
            (bool show,
            AuthorRef authorRef,
            BranchCity branchCity)
        {
            if (branchCity.ShowAuthorEdgesStrategy == ShowAuthorEdgeStrategy.ShowAlways)
            {
                return;
            }

            // When the author threshold is reached for the node, we do not show the edges.
            if (authorRef.Edges.Count < branchCity.AuthorThreshold
                || branchCity.ShowAuthorEdgesStrategy != ShowAuthorEdgeStrategy.ShowOnHoverOrWithMultipleAuthors)
            {
                foreach (GameObject edge in authorRef.Edges)
                {
                    if (edge.TryGetComponent(out AuthorEdge authorEdge))
                    {
                        authorEdge.ShowOrHide(show);
                    }

                }
            }
        }

        /// <summary>
        /// Toggles the visibility of author edges for an author sphere.
        /// </summary>
        /// <param name="show">Should be set to true if the animation should show the edges and false if it should hide them.</param>
        /// <param name="sphere">The <see cref="AuthorSphere"/> the user hovers over.</param>
        /// <param name="branchCity">Configuration of the code city.</param>
        private void ToggleAuthorEdgesForAuthorSphere(bool show, AuthorSphere sphere, BranchCity branchCity)
        {
            if (branchCity.ShowAuthorEdgesStrategy == ShowAuthorEdgeStrategy.ShowAlways)
            {
                return;
            }

            foreach (GameObject edge in sphere.Edges)
            {
                if (edge.TryGetComponent(out AuthorEdge authorEdge))
                {
                    // Only show edges if ShowOnHover was set as strategy or if the author threshold is not reached.
                    if (branchCity.ShowAuthorEdgesStrategy == ShowAuthorEdgeStrategy.ShowOnHover
                        || authorEdge.FileNode.Edges.Count < branchCity.AuthorThreshold)
                    {
                        authorEdge.ShowOrHide(show);
                    }
                }
            }
        }

        /// <summary>
        /// Toggles the visibility of author edges of the node or the author sphere the user hovers over.
        /// </summary>
        /// <param name="show">Should be set to true if the animation should show the edges and false if it should hide them.</param>
        /// <param name="branchCity">Configuration of the code city.</param>
        private void ToggleAuthorEdges(bool show, BranchCity branchCity)
        {
            if (gameObject.TryGetComponent(out AuthorRef authorRef))
            {
                // When the user hovers over a graph game node
                ToggleAuthorEdgesForNode(show, authorRef, branchCity);
            }
            else if (gameObject.TryGetComponent(out AuthorSphere sphere))
            {
                // When the user hovers over an AuthorSphere.
                ToggleAuthorEdgesForAuthorSphere(show, sphere, branchCity);
            }
        }

        /// <summary>
        /// Handles the hover-in event of the <paramref name="interactableObject"/>.
        /// Toggles the visibility of author edges for the node or author sphere the user hovers over.
        /// </summary>
        /// <param name="interactableObject">The actual object the user hovers over.</param>
        private void OnHoverIn(InteractableObject interactableObject, bool _)
        {
            if (gameObject.ContainingCity() is BranchCity branchCity)
            {
                edgeToggleToken?.Cancel();
                edgeToggleToken = new CancellationTokenSource();

                ToggleAuthorEdges(true, branchCity);
            }
        }

        /// <summary>
        /// Handles the hover-out event of the interactable object.
        /// Toggles the visibility of author edges for the node or author sphere the user hovers over.
        /// </summary>
        /// <param name="interactableObject">The actual object, the user hovers over.</param>
        private void OnHoverOut(InteractableObject interactableObject, bool _)
        {
            if (gameObject.ContainingCity() is BranchCity branchCity)
            {
                edgeToggleToken?.Cancel();
                edgeToggleToken = new CancellationTokenSource();

                ToggleAuthorEdges(false, branchCity);
            }
        }
    }
}
