/*
MIT License 
Copyright(c) 2017 MarekMarchlewicz

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.GO.Whiteboard
{
    [System.Obsolete("Experimental code. Do not use it. May be removed soon.")]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Interactable))]
    public class DraggableObject : MonoBehaviour
    {
        [SerializeField]
        private readonly Transform wallTransform, offsetTransform;

        [SerializeField]
        private readonly float followingSpeed = 50f;

        protected Rigidbody mRigidbody;

        private const int MaxNumberOfPositions = 8;
        private readonly Queue<Vector3> velocities = new Queue<Vector3>(MaxNumberOfPositions);
        private readonly Queue<Vector3> angularVelocities = new Queue<Vector3>(MaxNumberOfPositions);

        private Vector3 startPosition;
        private Quaternion starRotation;

        private Vector3? lastPosition = null;

        private Transform controllerTransform = null;

        public bool IsDragged
        {
            get
            {
                return controllerTransform != null;
            }
        }

        /// <summary>
        /// SteamVR component required for interactions. We assume the gameObject has it as 
        /// component attached. It will be set in Start().
        /// </summary>
        private Interactable interactable;

        protected virtual void Awake()
        {
            mRigidbody = GetComponent<Rigidbody>();

            startPosition = mRigidbody.position;
            starRotation = mRigidbody.rotation;

            interactable = GetComponent<Interactable>();
            if (interactable == null)
            {
                Debug.LogErrorFormat("Game object {0} has no component Interactable attached to it.\n", gameObject.name);
                return;
            }
        }

        private void FixedUpdate()
        {
            if (controllerTransform != null)
            {
                if (lastPosition.HasValue)
                {
                    UpdatePosition(controllerTransform.position, followingSpeed);
                    UpdateRotation(controllerTransform.rotation * Quaternion.Euler(Vector3.right * 90f), followingSpeed);

                    Vector3 velocity = (mRigidbody.position - lastPosition.Value) / Time.deltaTime;

                    velocities.Enqueue(velocity);
                    if (velocities.Count > MaxNumberOfPositions)
                    {
                        velocities.Dequeue();
                    }

                    // FIXME:
                    Vector3 angularVelocity = Vector3.zero;
                    //Vector3 angularVelocity = SteamVR_Controller.Input((int)controllerTransform.GetComponent<SteamVR_TrackedObject>().index).angularVelocity;

                    angularVelocities.Enqueue(angularVelocity);
                    if (angularVelocities.Count > MaxNumberOfPositions)
                    {
                        angularVelocities.Dequeue();
                    }
                }

                lastPosition = mRigidbody.position;
            }
        }

        protected virtual void UpdatePosition(Vector3 targetPosition, float followingSpeed)
        {
            float zPositionOffset = offsetTransform.position.z - transform.position.z;
            if (controllerTransform.position.z + zPositionOffset > wallTransform.position.z)
            {
                targetPosition.z = wallTransform.position.z - zPositionOffset;
            }

            mRigidbody.position = Vector3.Lerp(mRigidbody.position, targetPosition, Time.deltaTime * followingSpeed);
        }

        protected virtual void UpdateRotation(Quaternion targetRotation, float followingSpeed)
        {
            mRigidbody.rotation = Quaternion.Lerp(mRigidbody.rotation, targetRotation, Time.deltaTime * followingSpeed);
        }

        public void StartDragging(Transform targetTransform)
        {
            GetComponent<Rigidbody>().isKinematic = true;

            velocities.Clear();

            controllerTransform = targetTransform;
        }

        public void StopDragging()
        {
            controllerTransform = null;

            Vector3 releaseVelocity = Vector3.zero;

            foreach (Vector3 velocity in velocities)
            {
                releaseVelocity += velocity;
            }

            if (velocities.Count > 0)
            {
                releaseVelocity /= velocities.Count;
            }

            Vector3 releaseAngularVelocity = Vector3.zero;

            foreach (Vector3 angularVelocity in angularVelocities)
            {
                releaseAngularVelocity += angularVelocity;
            }

            if (angularVelocities.Count > 0)
            {
                releaseAngularVelocity /= angularVelocities.Count;
            }

            mRigidbody.isKinematic = false;
            mRigidbody.velocity = releaseVelocity;
            mRigidbody.angularVelocity = releaseAngularVelocity;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.tag == "Floor")
            {
                ResetPosition();
            }
        }

        private void ResetPosition()
        {
            mRigidbody.position = startPosition;
            mRigidbody.rotation = starRotation;

            mRigidbody.velocity = Vector3.zero;
            mRigidbody.angularVelocity = Vector3.zero;
        }

        //---------------------------------------------------------------
        // Private actions called by the hand when the object is hovered.
        //---------------------------------------------------------------

        /// <summary>
        /// Called by the Hand when that Hand starts hovering over this object.
        /// 
        /// Activates the source name and detail text and highlights the object by
        /// material with a different color.
        /// </summary>
        /// <param name="hand">the hand hovering over the object</param>
        private void OnHandHoverBegin(Hand hand)
        {
            Debug.Log("OnHandHoverEnd");
            //hand.ShowGrabHint();
        }

        /// <summary>
        /// Called by the Hand when that Hand stops hovering over this object
        /// 
        /// Deactivates the source name and detail text and restores the original material.
        /// </summary>
        /// <param name="hand">the hand hovering over the object</param>
        private void OnHandHoverEnd(Hand hand)
        {
            Debug.Log("OnHandHoverEnd");
            //hand.HideGrabHint();
        }

        private readonly Hand.AttachmentFlags attachmentFlags
               = Hand.defaultAttachmentFlags
                 & (~Hand.AttachmentFlags.SnapOnAttach)
                 & (~Hand.AttachmentFlags.DetachOthers)
                 & (~Hand.AttachmentFlags.VelocityMovement);

        /// <summary>
        /// Called every Update() by a Hand while that Hand is hovering over this object.
        /// </summary>
        /// <param name="hand">the hand hovering over the object</param>
        private void HandHoverUpdate(Hand hand)
        {
            //Debug.Log("HandHoverUpdate");
            GrabTypes startingGrabType = hand.GetGrabStarting();
            bool isGrabEnding = hand.IsGrabEnding(gameObject);

            if (interactable.attachedToHand == null && startingGrabType != GrabTypes.None)
            {
                // The hand is grabbing the object
                Debug.LogFormat("grabbed object {0}\n", hand.gameObject.name);

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
                Debug.LogFormat("released object {0}\n", hand.gameObject.name);
            }
        }
    }
}
