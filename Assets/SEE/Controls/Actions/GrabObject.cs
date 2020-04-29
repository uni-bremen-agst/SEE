using SEE.DataModel;
using SEE.GO;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    /// <summary>
    /// Implements interactions with a grabbed game object.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class GrabObject : MonoBehaviour
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
        private Interactable interactable;

        /// <summary>
        /// The graph node visualized. We assume that gameNode has a node ref by 
        /// which it can be retrieved.
        /// </summary>
        private Node graphNode;

        /// <summary>
        /// Sets up interactable and graphNode
        /// 
        /// The following assumptions are made:
        /// 1) gameObject has a component Interactable attached to it
        /// 2) gameObject has a NodeRef component attached to it
        /// 3) this NodeRef refers to a valid graph node with a valid information that can
        ///    be retrieved and shown when the user hovers over the object
        /// </summary>
        void Start()
        {
            interactable = this.GetComponent<Interactable>();
            if (interactable == null)
            {
                Debug.LogErrorFormat("Game object {0} has no component Interactable attached to it.\n", gameObject.name);
                return;
            }

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
        }

        //-------------------------------------------------
        // Showing additional information when hovered over
        //-------------------------------------------------

        /// <summary>
        /// A text field showing information on the graphNode when it is hovered over.
        /// This component will be created on demand.
        /// </summary>
        private TextMesh textField = null;
        /// <summary>
        /// A game object holding the textField.
        /// </summary>
        private GameObject textFieldObject = null;
        /// <summary>
        /// The local position of the textFieldObject relative to gameObject.
        /// </summary>
        private readonly Vector3 localPositionOfTextFieldObject = new Vector3(0.075f, 0.025f, 0.0f);

        /// <summary>
        /// Shows information about the currently grabbed object using the textField.
        /// If the textField and its container textFieldObject do not exist, they will
        /// be created first.
        /// </summary>
        private void ShowInformation()
        {
            if (textFieldObject == null)
            {
                textFieldObject = new GameObject("Hovering Text Field");
                textFieldObject.transform.SetParent(gameObject.transform);
                textFieldObject.transform.localPosition = localPositionOfTextFieldObject;
                textField = AddTextMesh(textFieldObject);
            }
            textField.text = GetAttributes(graphNode);
        }

        /// <summary>
        /// Adds the necessary components MeshRenderer and TextMesh to <paramref name="textField"/>
        /// </summary>
        /// <param name="textField">the object holding the text</param>
        /// <returns>the TextMash that was added to <paramref name="textField"/> as a component</returns>
        private TextMesh AddTextMesh(GameObject textField)
        {
            MeshRenderer rendender = textField.AddComponent<MeshRenderer>();

            rendender.receiveShadows = false;
            rendender.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            TextMesh textMesh = textField.AddComponent<TextMesh>();
            textMesh.characterSize = 0.0025f;
            textMesh.fontSize = 120;

            return textMesh;
        }

        /// <summary>
        /// Resets the currently shown text in textField to the empty string.
        /// </summary>
        private void HideInformation()
        {
            textField.text = "";
            // We keep textFieldObject for later use. Chances are that an object is selected
            // when it was selected once.
        }

        //-----------------------------------------------
        // Restoring the original position and rotation
        //-----------------------------------------------

        /// <summary>
        /// The position before the object was grabbed so that it can be restored
        /// when the object is no longer grabbed.
        /// </summary>
        private Vector3 oldPosition;
        /// <summary>
        /// The rotation before the object was grabbed so that it can be restored
        /// when the object is no longer grabbed.
        /// </summary>
        private Quaternion oldRotation;

        /// <summary>
        /// The local scale before the object was grabbed so that it can be restored
        /// when the object is no longer grabbed.
        /// </summary>
        private Vector3 oldLocalScale;

        /// <summary>
        /// How long the animation to restore the grabbed object to is original position
        /// and rotation should last in seconds.
        /// </summary>
        private const float ResetAnimationTime = 1.0f;

        /// <summary>
        /// Save our position/rotation/scale so that we can restore it when we detach.
        /// </summary>
        private void SaveCurrentPosition()
        {
            oldPosition = transform.position;
            oldRotation = transform.rotation;
            oldLocalScale = transform.localScale;
        }

        /// <summary>
        /// Restores the grabbed object to is original scale, position and rotation by animation.
        /// </summary>
        private void Reset()
        {
            HideInformation();
            gameObject.transform.rotation = oldRotation;
            iTween.ScaleTo(gameObject, oldLocalScale, ResetAnimationTime);
            iTween.MoveTo(gameObject, iTween.Hash(
                                          "position", oldPosition,
                                          //"islocal", true,
                                          //"rotation", oldRotation,
                                          "time", ResetAnimationTime
                ));
            
        }

        // -------------------------------
        // Highlighting the grabbed object
        // -------------------------------

        /// <summary>
        /// The material before the object was grabbed so that it can be restored
        /// when the object is no longer grabbed. While hovering, a highlighting
        /// material will be used.
        /// </summary>
        private Material oldMaterial;

        /// <summary>
        /// Highlights the grabbed gameNode by assigning a particular highlight material.
        /// Stores the original material in oldMaterial.
        /// </summary>
        private void HighlightMaterial()
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            oldMaterial = renderer.sharedMaterial;
            renderer.sharedMaterial = Materials.HighlightMaterial();
        }

        /// <summary>
        /// Resets the original material of gameNode using the material stored in oldMaterial.
        /// </summary>
        private void ResetMaterial()
        {
            gameObject.GetComponent<Renderer>().sharedMaterial = oldMaterial;
        }

        //------------------------------------
        // Actions when the object is hovered.
        //------------------------------------

        /// <summary>
        /// Called when a Hand starts hovering over this object.
        /// 
        /// Activates the source name and detail text and highlights the object by
        /// material with a different color.
        /// </summary>
        /// <param name="hand">the hand hovering over the object</param>
        private void OnHandHoverBegin(Hand hand)
        {
            //Debug.Log("OnHandHoverEnd");
            ShowInformation();
            HighlightMaterial();
            //hand.ShowGrabHint();
        }

        /// <summary>
        /// Called when a Hand stops hovering over this object
        /// 
        /// Deactivates the source name and detail text and restores the original material.
        /// </summary>
        /// <param name="hand">the hand hovering over the object</param>
        private void OnHandHoverEnd(Hand hand)
        {
            //Debug.Log("OnHandHoverEnd");
            HideInformation();
            ResetMaterial();
            //hand.HideGrabHint();
        }

        private Hand.AttachmentFlags attachmentFlags
               = Hand.defaultAttachmentFlags
                 & (~Hand.AttachmentFlags.SnapOnAttach)
                 & (~Hand.AttachmentFlags.DetachOthers)
                 & (~Hand.AttachmentFlags.VelocityMovement);

        /// <summary>
        /// Called every Update() while a Hand is hovering over this object
        /// </summary>
        /// <param name="hand">the hand hovering over the object</param>
        private void HandHoverUpdate(Hand hand)
        {
            //Debug.Log("HandHoverUpdate");
            GrabTypes startingGrabType = hand.GetGrabStarting();
            bool isGrabEnding = hand.IsGrabEnding(this.gameObject);

            if (interactable.attachedToHand == null && startingGrabType != GrabTypes.None)
            {
                // The hand is grabbing the object
                SaveCurrentPosition();

                // Call this to continue receiving HandHoverUpdate messages,
                // and prevent the hand from hovering over anything else
                hand.HoverLock(interactable);

                // Attach this object to the hand
                hand.AttachObject(gameObject, startingGrabType, attachmentFlags);

                //hand.HideGrabHint();
            }
            else if (isGrabEnding)
            {
                // The hand is no longer grabbing the object.

                // Detach this object from the hand
                hand.DetachObject(gameObject);

                // Call this to undo HoverLock
                hand.HoverUnlock(interactable);

                Reset();
            }
        }

        //-----------------
        // Helper functions
        //-----------------

        /// <summary>
        /// Retrieves the attributes and their values of <paramref name="graphNode"/>.
        /// </summary>
        /// <param name="graphNode">graph node whose attributes are requested</param>
        /// <returns>attribute names and values</returns>
        private string GetAttributes(Node graphNode)
        {
            if (graphNode == null)
            {
                return gameObject.name;
            }
            string result = "";
            result += "ID" + graphNode.ID + "\n";
            result += "Type" + graphNode.Type + "\n";

            foreach (var entry in graphNode.StringAttributes)
            {
                result += string.Format("{0}: {1}\n", entry.Key, entry.Value);
            }
            foreach (var entry in graphNode.FloatAttributes)
            {
                result += string.Format("{0}: {1}\n", entry.Key, entry.Value);
            }
            foreach (var entry in graphNode.IntAttributes)
            {
                result += string.Format("{0}: {1}\n", entry.Key, entry.Value);
            }
            foreach (var entry in graphNode.ToggleAttributes)
            {
                result += entry + "\n";
            }
            return result;
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