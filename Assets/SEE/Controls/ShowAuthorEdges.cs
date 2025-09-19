using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
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
        /// <param name="animationKind">Animation kind to use (will be set by <see cref="BranchCity"/>).</param>
        /// <param name="authorRef"><see cref="AuthorRef"/> instance which the user hovered over.</param>
        /// <param name="branchCity">Configuration of the code city.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns an empty task.</returns>
        private async Task ToggleAuthorEdgesForNodeAsync
            (bool show,
            EdgeAnimationKind animationKind,
            AuthorRef authorRef,
            BranchCity branchCity,
            CancellationToken token)
        {
            if (branchCity.ShowAuthorEdgesStrategy == ShowAuthorEdgeStrategy.ShowAlways)
            {
                return;
            }

            // When the author threshold is reached for the node, we do not show the edges.
            if (authorRef.Edges.Count >= branchCity.AuthorThreshold
                && branchCity.ShowAuthorEdgesStrategy == ShowAuthorEdgeStrategy.ShowOnHoverOrWithMultipleAuthors)
            {
                if (show)
                {
                    await UniTask.Delay(ShowEdges.TransitiveDelay, cancellationToken: token);
                }
                if (token.IsCancellationRequested)
                {
                    return;
                }
            }
            else
            {
                foreach (GameObject edge in authorRef.Edges)
                {
                    edge.EdgeOperator()?.ShowOrHide(show, animationKind);
                }
                if (show)
                {
                    await UniTask.Delay(ShowEdges.TransitiveDelay, cancellationToken: token);
                }
                if (token.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Toggles the visibility of author edges for an author sphere.
        /// </summary>
        /// <param name="show">Should be set to true if the animation should show the edges and false if it should hide them.</param>
        /// <param name="animationKind">Animation kind to use (will be set by <see cref="BranchCity"/>).</param>
        /// <param name="sphere">The <see cref="AuthorSphere"/> the user hovers over.</param>
        /// <param name="branchCity">Configuration of the code city.</param>
        /// <param name="token">Cancelation token.</param>
        /// <returns>Returns an empty task.</returns>
        private async Task ToggleAuthorEdgesForAuthorSphereAsync
            (bool show,
            EdgeAnimationKind animationKind,
            AuthorSphere sphere,
            BranchCity branchCity,
            CancellationToken token)
        {
            if (branchCity.ShowAuthorEdgesStrategy == ShowAuthorEdgeStrategy.ShowAlways)
            {
                return;
            }

            foreach (GameObject edge in sphere.Edges)
            {
                bool hasAuthorEdge = edge.TryGetComponent(out AuthorEdge authorEdge);
                // Only show edges in which the author threshold is not reached.
                bool belowThreshold = hasAuthorEdge && authorEdge.TargetNode.Edges.Count < branchCity.AuthorThreshold;
                // Or if ShowOnHover was set as strategy.
                bool showOnHover = branchCity.ShowAuthorEdgesStrategy == ShowAuthorEdgeStrategy.ShowOnHover;

                if (belowThreshold || showOnHover)
                {
                    edge.EdgeOperator()?.ShowOrHide(show, animationKind);
                }
            }
            if (show)
            {
                await UniTask.Delay(ShowEdges.TransitiveDelay, cancellationToken: token);
            }
            if (token.IsCancellationRequested)
            {
                return;
            }
        }


        /// <summary>
        /// Toggles the visibility of author edges of the node or the author sphere the user hovers over.
        /// </summary>
        /// <param name="show">Should be set to true if the animation should show the edges and false if it should hide them.</param>
        /// <param name="animationKind">Animation kind to use (will be set by <see cref="BranchCity"/>).</param>
        /// <param name="token">Cancelation token.</param>
        /// <param name="branchCity">Configuration of the code city.</param>
        /// <returns>Returns an empty task.</returns>
        private async UniTask ToggleAuthorEdgesAsync(bool show, EdgeAnimationKind animationKind, CancellationToken token, BranchCity branchCity)
        {
            // When the user hovers over a graph node
            if (gameObject.TryGetComponent(out AuthorRef authorRef))
            {
                await ToggleAuthorEdgesForNodeAsync(show, animationKind, authorRef, branchCity, token);
            }

            // When the user hovers over an AuthorSphere.
            else if (gameObject.TryGetComponent(out AuthorSphere sphere))
            {
                await ToggleAuthorEdgesForAuthorSphereAsync(show, animationKind, sphere, branchCity, token);
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

                ToggleAuthorEdgesAsync(true, branchCity.EdgeLayoutSettings.AnimationKind, edgeToggleToken.Token, branchCity).Forget();
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

                ToggleAuthorEdgesAsync(false, branchCity.EdgeLayoutSettings.AnimationKind, edgeToggleToken.Token, branchCity).Forget();
            }
        }

    }
}
