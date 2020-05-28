using SEE.DataModel;
using SEE.GO;
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
        protected virtual void Start()
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
        }

        //---------------------------------------------------------
        // Called when this GameObject becomes attached to the hand
        //-------------------------------------------------
        //private void OnAttachedToHand(Hand hand)
        //{
        //    sourceText.text = sourceName;
        //    hoveringText.text = detailText;
        //}

        //-------------------------------------------------
        // Called when this GameObject is detached from the hand
        //-------------------------------------------------
        //private void OnDetachedFromHand(Hand hand)
        //{
        //    sourceText.text = sourceName;
        //    hoveringText.text = "";
        //}

        //-------------------------------------------------
        // Called every Update() while this GameObject is attached to the hand
        //-------------------------------------------------
        //private void HandAttachedUpdate(Hand hand)
        //{
        //    sourceText.text = sourceName;
        //    hoveringText.text = detailText;
        //}

        //-------------------------------------------------
        // Called when this attached GameObject becomes the primary attached object
        //-------------------------------------------------
        //private void OnHandFocusAcquired(Hand hand)
        //{
        //}

        //-------------------------------------------------
        // Called when another attached GameObject becomes the primary attached object
        //-------------------------------------------------
        //private void OnHandFocusLost(Hand hand)
        //{
        //}
    }
}