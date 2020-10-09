using SEE.DataModel;
using SEE.GO;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    /// <summary>
    /// Super class of the behaviours of game objects the player interacts with.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public abstract class InteractableObject : MonoBehaviour
    {
        /// <summary>
        /// The next available ID to be assigned.
        /// </summary>
        private static uint nextID = 0;

        /// <summary>
        /// The interactable objects.
        /// </summary>
        private static readonly Dictionary<uint, InteractableObject> interactableObjects = new Dictionary<uint, InteractableObject>();

        /// <summary>
        /// The unique id of the interactable object.
        /// </summary>
        public uint id;

        // Tutorial on grabbing objects:
        // https://www.youtube.com/watch?v=MKOc8J877tI&t=15s

        // These are the messages the hand sends to objects that it is interacting with:
        //
        // OnHandHoverBegin: Sent when the hand first starts hovering over the object
        // HandHoverUpdate: Sent every frame that the hand is hovering over the object
        // OnHandHoverEnd: Sent when the hand stops hovering over the object
        // OnAttachedToHand: Sent when the object gets attached to the hand
        // HandAttachedUpdate: Sent every frame while the object is attached to the hand
        // OnDetachedFromHand: Sent when the object gets detached from the hand
        // OnHandFocusLost: Sent when an attached object loses focus because something else 
        //                  has been attached to the hand
        // OnHandFocusAcquired: Sent when an attached object gains focus because the previous 
        //                      focus object has been detached from the hand
        //
        // See https://valvesoftware.github.io/steamvr_unity_plugin/articles/Interaction-System.html

        /// <summary>
        /// SteamVR component required for interactions. We assume the gameObject has it as 
        /// component attached. It will be set in Start().
        /// </summary>
        protected Interactable interactable;

        /// <summary>
        /// The graph node represented by the gameObject. We assume that gameObject has a 
        /// NodeRef component by which it can be retrieved. It will be set in Start().
        /// </summary>
        protected Node graphNode;

        /// <summary>
        /// Sets up graphNode and interactable.
        /// 
        /// The following assumptions are made:
        /// * gameObject has an Interactable component attached to it
        /// * gameObject has a NodeRef component attached to it
        /// * that NodeRef refers to a valid graph node with a valid information that can
        ///    be retrieved and shown when the user hovers over the object
        /// </summary>
        protected virtual void Awake()
        {
            NodeRef nodeRef = gameObject.GetComponent<NodeRef>();
            if (nodeRef != null)
            {
                graphNode = nodeRef.node;
                if (graphNode == null)
                {
                    Debug.LogWarningFormat("The node reference in game object {0} is undefined.\n", gameObject.name);
                }
            }
            else
            {
                Debug.LogWarningFormat("The game object {0} has no node reference.\n", gameObject.name);
            }
            interactable = this.GetComponent<Interactable>();
            if (interactable == null)
            {
                Debug.LogErrorFormat("Game object {0} has no component Interactable attached to it.\n", gameObject.name);
            }
            id = nextID++;
            interactableObjects.Add(id, this);
        }

        /// <summary>
        /// Returns the interactable object of given id or <code>null</code>, if it does
        /// not exist.
        /// </summary>
        /// <param name="id">The id of the interactable object.</param>
        /// <returns></returns>
        public static InteractableObject Get(uint id)
        {
            bool result = interactableObjects.TryGetValue(id, out InteractableObject interactableObject);
            return interactableObject;
        }
    }
}