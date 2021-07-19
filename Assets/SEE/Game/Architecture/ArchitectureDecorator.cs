using System.Collections.Generic;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.Controls.Actions.Architecture;
using SEE.Controls.Architecture;
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
        public static void DecorateForInteraction(GameObject gameObject, PenInteractionController penInteractionController)
        {
            gameObject.isStatic = false;
            gameObject.AddComponent<PenInteraction>().controller = penInteractionController;
            gameObject.AddComponent<ElementOutline>();
            gameObject.AddComponent<ElementTooltip>();
        }

        /// <summary>
        /// Decorates a list of game objects with components that are needed for interaction.
        /// </summary>
        /// <param name="gameObjects"></param>
        public static void DecorateForInteraction(ICollection<GameObject> gameObjects, PenInteractionController penInteractionController)
        {
            foreach (GameObject o in gameObjects)
            {
                DecorateForInteraction(o, penInteractionController);
            }
        }


        
    }
}