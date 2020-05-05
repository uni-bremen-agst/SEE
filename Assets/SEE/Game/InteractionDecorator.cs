using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    public class InteractionDecorator
    {
        public static void PrepareForInteraction(GameObject go)
        {
            go.isStatic = false; // we want to move the object during the game
            go.AddComponent<Interactable>(); // enable interactions
            go.AddComponent<GrabbableObject>(); // our customized reactions to grabbing events
            go.AddComponent<HoverableObject>(); // our customized reactions to grabbing events
            //go.AddComponent<Rigidbody>(); // so the object follows the laws of physics
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