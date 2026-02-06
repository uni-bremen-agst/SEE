using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.Controls.Interactables;
using SEE.GO;
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
        /// Adds the following components to given <paramref name="gameObject"/>:
        /// <see cref="InteractableObject"/>,
        /// <see cref="XRSimpleInteractable"/>,
        /// <see cref="BoxCollider"/>,
        /// <see cref="ShowHovering"/>,
        /// <see cref="ShowSelection"/>,
        /// <see cref="ShowGrabbing"/>.
        ///
        /// If <paramref name="gameObject"/> has a <see cref="NodeRef"/>, then the following
        /// components are added in addition to the ones above:
        /// <see cref="ShowLabel"/>,
        /// <see cref="ShowHoverInfo"/>
        /// <see cref="ShowEdges"/>
        /// <see cref="ShowAuthorEdges"/>
        /// <see cref="HighlightErosion"/>.
        ///
        /// Note: The <paramref name="gameObject"/> is assumed to represent a graph node
        /// or edge.
        /// </summary>
        /// <param name="gameObject">Game object where the components are to be added to.</param>
        public static void PrepareGraphElementForInteraction(GameObject gameObject)
        {
            gameObject.AddOrGetComponent<InteractableGraphElement>();

            gameObject.isStatic = false; // we want to move the object during the game
                                         // The following additions of components must come after the addition of InteractableObject
                                         // because they require the presence of an InteractableObject.
            AddGeneralComponents(gameObject);
            if (gameObject.HasNodeRef())
            {
                gameObject.AddOrGetComponent<ShowLabel>();
                gameObject.AddOrGetComponent<ShowHoverInfo>();
                gameObject.AddOrGetComponent<ShowEdges>();
                gameObject.AddOrGetComponent<ShowAuthorEdges>();
                gameObject.AddOrGetComponent<HighlightErosion>();
            }
        }

        /// <summary>
        /// Addes the following components to given <paramref name="gameObject"/>:
        /// <see cref="InteractableAuthor"/>,
        /// <see cref="ShowAuthorEdges"/>,
        /// <see cref="XRSimpleInteractable"/>,
        /// <see cref="BoxCollider"/>,
        /// <see cref="ShowHovering"/>,
        /// <see cref="ShowSelection"/>,
        /// <see cref="ShowGrabbing"/>.
        /// </summary>
        /// <param name="gameObject">Where the components should be added to.</param>
        public static void PrepareAuthorForInteraction(GameObject gameObject)
        {
            gameObject.AddOrGetComponent<InteractableAuthor>();
            gameObject.AddOrGetComponent<ShowAuthorEdges>();
            AddGeneralComponents(gameObject);
        }

        /// <summary>
        /// Adds the following components to given <paramref name="gameObject"/>:
        /// <see cref="XRSimpleInteractable"/>,
        /// <see cref="BoxCollider"/>,
        /// <see cref="ShowHovering"/>,
        /// <see cref="ShowSelection"/>,
        /// <see cref="ShowGrabbing"/>.
        /// </summary>
        /// <param name="gameObject">Where the components should be added to.</param>
        private static void AddGeneralComponents(GameObject gameObject)
        {
            gameObject.AddOrGetComponent<XRSimpleInteractable>().colliders.Add(gameObject.GetComponent<BoxCollider>());
            gameObject.AddOrGetComponent<ShowHovering>();
            gameObject.AddOrGetComponent<ShowSelection>();
            gameObject.AddOrGetComponent<ShowGrabbing>();
        }

        /// <summary>
        /// Adds the same components as <see cref="PrepareGraphElementForInteraction"/>
        /// to all <paramref name="gameObjects"/>.
        ///
        /// Note: All <paramref name="gameObjects"/> are assumed to represent a graph node
        /// or edge.
        /// </summary>
        /// <param name="gameObjects">Game objects where the components are to be added to.</param>
        /// <param name="updateProgress">Action that updates the progress of the preparation.</param>
        /// <param name="token">Token with which to cancel the preparation.</param>
        public static async UniTask PrepareForInteractionAsync(ICollection<GameObject> gameObjects,
                                                               Action<float> updateProgress,
                                                               CancellationToken token = default)
        {
            int totalGameObjects = gameObjects.Count;
            // The batch size controls the compromise between FPS and processing speed.
            // In the editor, requirements for FPS are significantly lower than in-game.
            int batchSize = Application.isPlaying ? 200 : 1000;
            float i = 0;
            await foreach (GameObject go in gameObjects.BatchPerFrame(batchSize, token: token))
            {
                PrepareGraphElementForInteraction(go);
                updateProgress(++i / totalGameObjects);
            }
        }
    }
}
