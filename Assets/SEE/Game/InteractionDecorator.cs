using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.Controls.Actions;
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
        /// <see cref="Interactable"/>, <see cref="InteractableObject"/>,
        /// <see cref="ShowHovering"/>, <see cref="ShowSelection"/>, <see cref="ShowGrabbing"/>.
        /// If <paramref name="gameObject"/> has a <see cref="NodeRef"/>, then the following
        /// components are added in addition to the ones above:
        /// <see cref="GameNodeScaler"/>, <see cref="ShowLabel"/>, <see cref="EyeGazeHandler"/>,
        /// <see cref="HighlightErosion"/>.
        ///
        /// Note: The <paramref name="gameObject"/> is assumed to represent a graph node
        /// or edge.
        /// </summary>
        /// <param name="gameObject">game object where the components are to be added to</param>
        public static void PrepareForInteraction(GameObject gameObject)
        {
            gameObject.isStatic = false; // we want to move the object during the game
#if INCLUDE_STEAM_VR

            Interactable interactable = gameObject.AddComponentIfNecessary<Interactable>();
            interactable.highlightOnHover = false;
#endif
            gameObject.AddComponentIfNecessary<InteractableObject>();
            // The following additions of components must come after the addition of InteractableObject
            // because they require the presence of an InteractableObject.
            if (SceneSettings.InputType == PlayerInputType.VRPlayer)
            {
                gameObject.AddComponentIfNecessary<XRSimpleInteractable>().colliders.Add(gameObject.GetComponent<BoxCollider>());
            }
            gameObject.AddComponentIfNecessary<ShowHovering>();
            gameObject.AddComponentIfNecessary<ShowSelection>();
            gameObject.AddComponentIfNecessary<ShowGrabbing>();
            if (gameObject.HasNodeRef())
            {
                gameObject.AddComponentIfNecessary<ShowLabel>();
                gameObject.AddComponentIfNecessary<ShowHoverInfo>();
                gameObject.AddComponentIfNecessary<ShowEdges>();
                gameObject.AddComponentIfNecessary<HighlightErosion>();
            }
        }

        /// <summary>
        /// Adds a component of type <typeparamref name="T"/> to <paramref name="gameObject"/>
        /// if <paramref name="gameObject"/> does not have one already. The new or the
        /// existing component, respectively, is returned.
        /// </summary>
        /// <typeparam name="T">component that should be part of <paramref name="gameObject"/></typeparam>
        /// <param name="gameObject">game object that should have a component of <typeparamref name="T"/></param>
        /// <returns>component in <paramref name="gameObject"/></returns>
        private static T AddComponentIfNecessary<T>(this GameObject gameObject) where T : MonoBehaviour
        {
            if (gameObject.TryGetComponent(out T component))
            {
                return component;
            }
            else
            {
                return gameObject.AddComponent<T>();
            }
        }

        /// <summary>
        /// Adds the following components to all <paramref name="gameObjects"/>:
        /// <see cref="Interactable"/>, <see cref="InteractableObject"/>,
        /// <see cref="ShowHovering"/>, <see cref="ShowSelection"/>, <see cref="ShowGrabbing"/>.
        /// If a element in <paramref name="gameObjects"/> has a <see cref="NodeRef"/>, then the following
        /// components are added in addition to the ones above:
        /// <see cref="GameNodeScaler"/>, <see cref="ShowLabel"/>, <see cref="EyeGazeHandler"/>,
        /// <see cref="HighlightErosion"/>.
        ///
        /// Note: All <paramref name="gameObjects"/> are assumed to represent a graph node
        /// or edge.
        /// </summary>
        /// <param name="gameObjects">game objects where the components are to be added to</param>
        /// <param name="updateProgress">action that updates the progress of the preparation</param>
        /// <param name="token">token with which to cancel the preparation</param>
        public static async UniTask PrepareForInteractionAsync(ICollection<GameObject> gameObjects,
                                                               Action<float> updateProgress,
                                                               CancellationToken token = default)
        {
            int totalGameObjects = gameObjects.Count;
            // The batch size controls the compromise between FPS and processing speed.
            // In the editor, requirements for FPS are significantly lower than in-game.
            int batchSize = Application.isPlaying ? 200 : 1000;
            float i = 0;
            await foreach (GameObject go in gameObjects.BatchPerFrame(batchSize, cancellationToken: token))
            {
                PrepareForInteraction(go);
                updateProgress(++i / totalGameObjects);
            }
        }
    }
}
