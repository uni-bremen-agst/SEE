using System.Collections.Generic;
using SEE.Controls.Actions;
using SEE.Game;
using SEE.GO;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
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
            Interactable interactable = gameObject.AddComponent<Interactable>(); // enable interactions
            interactable.highlightOnHover = false;
            gameObject.AddComponent<InteractableObject>();
            // The following additions of components must come after the addition of InteractableObject
            // because they require the presence of an InteractableObject.
            gameObject.AddComponent<ShowHovering>();
            gameObject.AddComponent<ShowSelection>();
            gameObject.AddComponent<ShowGrabbing>();
            if (gameObject.HasNodeRef())
            {
                gameObject.AddComponent<GameNodeScaler>();
                gameObject.AddComponent<ShowLabel>();
                gameObject.AddComponent<EyeGazeHandler>();
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
        public static void PrepareForInteraction(ICollection<GameObject> gameObjects)
        {
            foreach (GameObject go in gameObjects)
            {
                PrepareForInteraction(go);
            }
        }
    }
}