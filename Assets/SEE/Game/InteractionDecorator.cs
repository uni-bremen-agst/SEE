using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    public class InteractionDecorator
    {
        private static void PrepareForInteraction(GameObject go)
        {
            go.isStatic = false; // we want to move the object during the game
            Interactable interactable = go.AddComponent<Interactable>(); // enable interactions
            interactable.highlightOnHover = false;
            GrabbableObject grabbableObject = go.AddComponent<GrabbableObject>(); // our customized reactions to hovering and grabbing events
        }

        public static void PrepareForInteraction(ICollection<GameObject> gameNodes)
        {
            foreach (GameObject go in gameNodes)
            {
                PrepareForInteraction(go);
            }
        }
    }
}