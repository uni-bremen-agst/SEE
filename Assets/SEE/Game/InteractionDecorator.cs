using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.Controls.Interactables;
using SEE.Controls.Modifiers;
using SEE.Extensions;
using SEE.Utils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SEE.Game
{
    /// <summary>
    /// Adds components required for interacting with a game object.
    /// </summary>
    internal static class InteractionDecorator
    {
        /// <summary>
        /// Adds the following components to given <paramref name="gameNodeOrEdge"/>:
        /// <see cref="InteractableObject"/>,
        /// <see cref="XRSimpleInteractable"/>,
        /// <see cref="BoxCollider"/>,
        /// <see cref="ShowHovering"/>,
        /// <see cref="ShowSelection"/>,
        /// <see cref="ShowGrabbing"/>.
        ///
        /// If <paramref name="gameNodeOrEdge"/> has a <see cref="NodeRef"/>, then the following
        /// components are added in addition to the ones above:
        /// <see cref="ShowLabel"/>,
        /// <see cref="ShowHoverInfo"/>
        /// <see cref="ShowEdges"/>
        /// <see cref="ShowAuthorEdges"/>
        /// <see cref="ShowErosions"/>.
        ///
        /// Note: The <paramref name="gameNodeOrEdge"/> is assumed to represent a graph node
        /// or edge.
        /// </summary>
        /// <param name="gameNodeOrEdge">Game node or edge where the components are to be added to.</param>
        public static void PrepareGraphElementForInteraction(GameObject gameNodeOrEdge)
        {
            gameNodeOrEdge.AddOrGetComponent<InteractableGraphElement>();

            gameNodeOrEdge.isStatic = false; // we want to move the object during the game
                                         // The following additions of components must come after the addition of InteractableObject
                                         // because they require the presence of an InteractableObject.
            AddGeneralComponents(gameNodeOrEdge);
            if (gameNodeOrEdge.HasNodeRef())
            {
                gameNodeOrEdge.AddOrGetComponent<ShowLabel>();
                gameNodeOrEdge.AddOrGetComponent<ShowHoverInfo>();
                gameNodeOrEdge.AddOrGetComponent<ShowEdges>();
                gameNodeOrEdge.AddOrGetComponent<ShowAuthorEdges>();
                gameNodeOrEdge.AddOrGetComponent<ShowErosions>();
            }
        }

        /// <summary>
        /// Addes the following components to given <paramref name="authorSphere"/>:
        /// <see cref="InteractableAuthor"/>,
        /// <see cref="ShowAuthorEdges"/>,
        /// <see cref="XRSimpleInteractable"/>,
        /// <see cref="BoxCollider"/>,
        /// <see cref="ShowHovering"/>,
        /// <see cref="ShowSelection"/>,
        /// <see cref="ShowGrabbing"/>.
        ///
        /// <paramref name="authorSphere"/> is assumed to be a game object representing
        /// an author in a <see cref="SEE.Game.City.BranchCity"/>.
        /// </summary>
        /// <param name="authorSphere">Where the components should be added to.</param>
        public static void PrepareAuthorForInteraction(GameObject authorSphere)
        {
            authorSphere.AddOrGetComponent<InteractableAuthor>();
            authorSphere.AddOrGetComponent<ShowAuthorEdges>();
            AddGeneralComponents(authorSphere);
        }

        /// <summary>
        /// Adds the following components to given <paramref name="gameNodeOrEdge"/>:
        /// <see cref="XRSimpleInteractable"/>,
        /// <see cref="BoxCollider"/>,
        /// <see cref="ShowHovering"/>,
        /// <see cref="ShowSelection"/>,
        /// <see cref="ShowGrabbing"/>.
        /// </summary>
        /// <param name="gameNodeOrEdge">A game node or edge where the components should be added to.</param>
        private static void AddGeneralComponents(GameObject gameNodeOrEdge)
        {
            gameNodeOrEdge.AddOrGetComponent<XRSimpleInteractable>().colliders.Add(gameNodeOrEdge.GetComponent<BoxCollider>());
            gameNodeOrEdge.AddOrGetComponent<ShowHovering>();
            gameNodeOrEdge.AddOrGetComponent<ShowSelection>();
            gameNodeOrEdge.AddOrGetComponent<ShowGrabbing>();
        }

        /// <summary>
        /// Adds the same components as <see cref="PrepareGraphElementForInteraction"/>
        /// to all <paramref name="gameNodesOrEdges"/>.
        ///
        /// Note: All <paramref name="gameNodesOrEdges"/> are assumed to represent a graph node
        /// or edge.
        /// </summary>
        /// <param name="gameNodesOrEdges">Game objects where the components are to be added to.</param>
        /// <param name="updateProgress">Action that updates the progress of the preparation.</param>
        /// <param name="token">Token with which to cancel the preparation.</param>
        public static async UniTask PrepareForInteractionAsync(ICollection<GameObject> gameNodesOrEdges,
                                                               Action<float> updateProgress,
                                                               CancellationToken token = default)
        {
            int totalGameObjects = gameNodesOrEdges.Count;
            // The batch size controls the compromise between FPS and processing speed.
            // In the editor, requirements for FPS are significantly lower than in-game.
            int batchSize = Application.isPlaying ? 200 : 1000;
            float i = 0;
            await foreach (GameObject go in gameNodesOrEdges.BatchPerFrame(batchSize, token: token))
            {
                PrepareGraphElementForInteraction(go);
                updateProgress(++i / totalGameObjects);
            }
        }
    }
}
