using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    /// <summary>
    /// Adds components required for interacting with a game object.
    /// </summary>
    public class InteractionDecorator
    {
        /// <summary>
        /// Adds an Interactable and InteractableObject component to given <paramref name="gameNode"/>.
        /// </summary>
        /// <param name="gameNode">game object where the components are to be added to</param>
        public static void PrepareForInteraction(GameObject gameNode)
        {
            gameNode.isStatic = false; // we want to move the object during the game
            Interactable interactable = gameNode.AddComponent<Interactable>(); // enable interactions
            interactable.highlightOnHover = false;
            gameNode.AddComponent<InteractableObject>();
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