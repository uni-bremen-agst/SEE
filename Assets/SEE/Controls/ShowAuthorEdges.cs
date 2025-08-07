using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using SEE.Game;
using SEE.Game.City;
using SEE.GameObjects;
using SEE.GO;
using UnityEngine;

namespace SEE.Controls.Actions
{
    internal class ShowAuthorEdges : InteractableObjectAction
    {

        private CancellationTokenSource edgeToggleToken;

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

        private async UniTaskVoid ToggleAuthorEdges(bool show, EdgeAnimationKind animationKind, CancellationToken token, BranchCity branchCity)
        {
            // When the user hovers over a graph node
            if (gameObject.TryGetComponent(out AuthorRef authorRef))
            {
                // When the author threashold is reached for the node, we do not show the edges.
                if (authorRef.Edges.Count >= branchCity.AuthorThreshold && branchCity.ShowEdgesStrategy == ShowAuthorEdgeStrategy.ShowOnHoverOrWithMultipleAuthors)
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

            // When the user hovers over an AuthorSphere.
            else if (gameObject.TryGetComponent(out AuthorSphere sphere))
            {
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
        }

        private async void OnHoverIn(InteractableObject interactableObject, bool isInitiator)
        {
            if (City() is BranchCity branchCity)
            {
                edgeToggleToken?.Cancel();
                edgeToggleToken = new CancellationTokenSource();

                ToggleAuthorEdges(true, branchCity.EdgeLayoutSettings.AnimationKind, edgeToggleToken.Token, branchCity).Forget();
            }
        }

        private void OnHoverOut(InteractableObject interactableObject, bool isInitiator)
        {
            if (City() is BranchCity branchCity)
            {
                edgeToggleToken?.Cancel();
                edgeToggleToken = new CancellationTokenSource();

                ToggleAuthorEdges(false, branchCity.EdgeLayoutSettings.AnimationKind, edgeToggleToken.Token, branchCity).Forget();
            }
        }
    }
}
