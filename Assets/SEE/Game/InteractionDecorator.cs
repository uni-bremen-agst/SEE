using System.Collections.Generic;
using SEE.Controls.Actions;
using SEE.Game;
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
        /// Adds the following components to given <paramref name="gameNode"/>:
        /// Interactable, InteractableObject, and ShowLabel.
        /// </summary>
        /// <param name="gameNode">game object where the components are to be added to</param>
        public static void PrepareForInteraction(GameObject gameNode)
        {
            gameNode.isStatic = false; // we want to move the object during the game
            Interactable interactable = gameNode.AddComponent<Interactable>(); // enable interactions
            interactable.highlightOnHover = false;
            gameNode.AddComponent<InteractableObject>();
            // The following additions of components must come after the addition of InteractableObject
            // because they require the presence of an InteractableObject.
            gameNode.AddComponent<EyeGazeHandler>();
            gameNode.AddComponent<ShowLabel>();
            gameNode.AddComponent<ShowHovering>();
            gameNode.AddComponent<ShowSelection>();
            gameNode.AddComponent<ShowGrabbing>();
            gameNode.AddComponent<GameNodeScaler>();
        }

        /// <summary>
        /// Adds an Interactable and InteractableObject component to all given <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes">game object where the components are to be added to</param>
        public static void PrepareForInteraction(ICollection<GameObject> gameNodes)
        {
            foreach (GameObject go in gameNodes)
            {
                PrepareForInteraction(go);
            }
        }
    }
}