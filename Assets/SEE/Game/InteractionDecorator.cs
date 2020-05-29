using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    public class InteractionDecorator
    {
        private static uint nextInteractableObjectID;

        private static void PrepareForInteraction(GameObject go)
        {
            go.isStatic = false; // we want to move the object during the game
            go.AddComponent<Interactable>(); // enable interactions
            GrabbableObject grabbableObject = go.AddComponent<GrabbableObject>(); // our customized reactions to hovering and grabbing events
            grabbableObject.id = nextInteractableObjectID++;
            //go.AddComponent<Rigidbody>(); // so the object follows the laws of physics
        }

        public static void PrepareForInteraction(ICollection<GameObject> gameNodes)
        {
            nextInteractableObjectID = 0;
            foreach (GameObject go in gameNodes)
            {
                PrepareForInteraction(go);
            }
        }
    }
}