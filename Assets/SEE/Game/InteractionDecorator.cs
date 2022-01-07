using System.Collections.Generic;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.GO;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Game
{
    /// <summary>
    /// Adds components required for interacting with a game object.
    /// </summary>
    public static class InteractionDecorator
    {
        /// <summary>
        /// Adds the following components to given <paramref name="gameObject"/>:
        /// Interactable, InteractableObject, ShowHovering, ShowSelection, ShowGrabbing.
        /// If <paramref name="gameObject"/> represents a graph node, it will also
        /// receive the following components additionally: GameNodeScaler,
        /// ShowLabel, EyeGazeHandler.
        ///
        /// Note: The <paramref name="gameObject"/> is assumed to represent a graph node
        /// or edge.
        /// </summary>
        /// <param name="gameObject">game object where the components are to be added to</param>
        public static void PrepareForInteraction(GameObject gameObject)
        {
            gameObject.isStatic = false; // we want to move the object during the game
            Interactable interactable = gameObject.AddComponentIfNecessary<Interactable>();
            interactable.highlightOnHover = false;
            gameObject.AddComponentIfNecessary<InteractableObject>();
            // The following additions of components must come after the addition of InteractableObject
            // because they require the presence of an InteractableObject.
            gameObject.AddComponentIfNecessary<ShowHovering>();
            gameObject.AddComponentIfNecessary<ShowSelection>();
            gameObject.AddComponentIfNecessary<ShowGrabbing>();
            if (gameObject.HasNodeRef())
            {
                gameObject.AddComponentIfNecessary<GameNodeScaler>();
                gameObject.AddComponentIfNecessary<ShowLabel>();
                gameObject.AddComponentIfNecessary<EyeGazeHandler>();
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
        /// Adds the following components to all given <paramref name="gameObjects"/>:
        /// Interactable, InteractableObject, ShowHovering, ShowSelection, ShowGrabbing.
        /// If <paramref name="gameNode"/> represents a graph node, it will also
        /// receive the following components additionally: GameNodeScaler,
        /// ShowLabel, EyeGazeHandler.
        ///
        /// Note: The <paramref name="gameObject"/> is assumed to represent a graph node
        /// or edge.
        /// </summary>
        /// <param name="gameObjects">game object where the components are to be added to</param>
        public static void PrepareForInteraction(IEnumerable<GameObject> gameObjects)
        {
            foreach (GameObject go in gameObjects)
            {
                PrepareForInteraction(go);
            }
        }
    }
}