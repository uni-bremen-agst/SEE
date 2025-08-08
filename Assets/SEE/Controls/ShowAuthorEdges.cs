using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using SEE.Game;
using SEE.Game.City;
using SEE.GameObjects;
using SEE.GO;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This action show/animates edges connecting authors spheres and nodes when the user hovers over them.
    /// This script can be added to both AuthorSpheres and nodes.
    /// </summary>
    internal class ShowAuthorEdges : InteractableObjectAction
    {
        /// <summary>
        /// The token used to cancel the edge toggling operation.
        /// </summary>
        private CancellationTokenSource edgeToggleToken;

        /// <summary>
        /// Sets <see cref="Interactable"/> and subscribes to hover events.
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
        /// Returns the code city holding the settings for the visualization of the node.
        /// May be null.
        /// </summary>
        private AbstractSEECity City()
        {
            GameObject codeCityObject = SceneQueries.GetCodeCity(gameObject.transform)?.gameObject;
            if (codeCityObject == null)
            {
                Debug.LogError($"Could not retrieve CodeCity for {gameObject.name}!");
                return null;
            }

            codeCityObject.TryGetComponent(out AbstractSEECity city);
            return city;
        }

        /// <summary>
        /// Toggles the visibility of author edges for a node.
        /// </summary>
        /// <param name="show">Should be set to true if the animation should show the edges and false if it should hides them.</param>
        /// <param name="animationKind">Animation kind to use (will be set by <see cref="BranchCity"/>).</param>
        /// <param name="authorRef"><see cref="AuthorRef"/> instance which the user hovered over.</param>
        /// <param name="branchCity">Configuration of the code city.</param>
        /// <param name="token">Cancelation token.</param>
        /// <returns>Returns an empty task.</returns>
        private async Task ToggleAuthorEdgesForNodeAsync(bool show, EdgeAnimationKind animationKind, AuthorRef authorRef, BranchCity branchCity, CancellationToken token)
        {
            if (branchCity.ShowAuthorEdgesStrategy == ShowAuthorEdgeStrategy.ShowAllways)
            {
                return;
            }

            // When the author threashold is reached for the node, we do not show the edges.
            if (authorRef.Edges.Count >= branchCity.AuthorThreshold && branchCity.ShowAuthorEdgesStrategy == ShowAuthorEdgeStrategy.ShowOnHoverOrWithMultipleAuthors)
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

            foreach (GameObject edge in authorRef.Edges.Select(x => x.Item1))
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

        /// <summary>
        /// Toggles the visibility of author edges for an author sphere.
        /// </summary>
        /// <param name="show">Should be set to true if the animation should show the edges and false if it should hides them.</param>
        /// <param name="animationKind">Animation kind to use (will be set by <see cref="BranchCity"/>).</param>
        /// <param name="sphere">TThe <see cref="AuthorSphere"/> the user hover over.</param>
        /// <param name="branchCity">Configuration of the code city.</param>
        /// <param name="token">Cancelation token.</param>
        /// <returns>Returns an empty task.</returns>
        private async Task ToggleAuthorEdgesForAuthorSphereAsync(bool show, EdgeAnimationKind animationKind, AuthorSphere sphere, BranchCity branchCity, CancellationToken token)
        {
            if (branchCity.ShowAuthorEdgesStrategy == ShowAuthorEdgeStrategy.ShowAllways)
            {
                return;
            }

            foreach (GameObject edge in sphere.Edges.Select(x => x.Item1))
            {
                // Only show edges in which the author threshold is not reached.
                if (edge.TryGetComponent(out AuthorEdge authorEdge) && authorEdge.targetNode.Edges.Count < branchCity.AuthorThreshold)
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
        /// Toggles the visibility of author edges of the node or author sphere the user hovers over.
        /// </summary>
        /// <param name="show">Should be set to true if the animation should show the edges and false if it should hides them.</param>
        /// <param name="animationKind">Animation kind to use (will be set by <see cref="BranchCity"/>).</param>
        /// <param name="token">Cancelation token.</param>
        /// <param name="branchCity">Configuration of the code city.</param>
        /// <returns>Returns an empty task.</returns>
        private async UniTaskVoid ToggleAuthorEdgesAsync(bool show, EdgeAnimationKind animationKind, CancellationToken token, BranchCity branchCity)
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
        /// Handles the hover in event of the interactable object.
        /// Toggles the visibility of author edges for the node or author sphere the user hovers over.
        /// </summary>
        /// <param name="interactableObject">The actual object, the user hovers over.</param>
        /// <param name="isInitiator">Will be ignored here.</param>
        private void OnHoverIn(InteractableObject interactableObject, bool isInitiator)
        {
            if (City() is BranchCity branchCity)
            {
                edgeToggleToken?.Cancel();
                edgeToggleToken = new CancellationTokenSource();

                ToggleAuthorEdgesAsync(true, branchCity.EdgeLayoutSettings.AnimationKind, edgeToggleToken.Token, branchCity).Forget();
            }
        }

        /// <summary>
        /// Handles the hover out event of the interactable object.
        /// Toggles the visibility of author edges for the node or author sphere the user hovers over.
        /// </summary>
        /// <param name="interactableObject">The actual object, the user hovers over.</param>
        /// <param name="isInitiator">Will be ignored here.</param>
        private void OnHoverOut(InteractableObject interactableObject, bool isInitiator)
        {
            if (City() is BranchCity branchCity)
            {
                edgeToggleToken?.Cancel();
                edgeToggleToken = new CancellationTokenSource();

                ToggleAuthorEdgesAsync(false, branchCity.EdgeLayoutSettings.AnimationKind, edgeToggleToken.Token, branchCity).Forget();
            }
        }
    }
}
