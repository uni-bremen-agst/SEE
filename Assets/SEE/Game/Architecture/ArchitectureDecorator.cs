using SEE.Controls;
using SEE.Controls.Actions;
using SEE.GO;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Game.Architecture
{
    /// <summary>
    /// Decorator for architecture graph elements. Used by <see cref="ArchitectureRenderer"/>.
    /// </summary>
    public static class ArchitectureDecorator
    {


        /// <summary>
        /// Decorates the given game object with components that are needed for interaction.
        /// </summary>
        /// <param name="gameObject">The game object to decorate.</param>
        public static void DecorateForInteraction(GameObject gameObject)
        {
            gameObject.isStatic = false;
            gameObject.AddComponent<Interactable>();
            gameObject.AddComponent<InteractableObject>();
            gameObject.AddComponent<ShowArchitectureHovering>();
        }


        
    }
}