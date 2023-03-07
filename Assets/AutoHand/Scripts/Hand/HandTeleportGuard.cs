using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Autohand{
    [DefaultExecutionOrder(1000)]
    public class HandTeleportGuard : MonoBehaviour{
        [Header("Helps prevent hand from passing through static collision boundries")]
        public Hand hand;

        [Header("Guard Settings")]
        [Tooltip("The mask of things the guarding will ignore, if left on default or empty, will default to ignoring recommended Auto Hand layers")]
        public LayerMask ignoreMask;
        [Tooltip("The amount of distance change required in one frame or fixed udpate to activate the teleport guard")]
        public float buffer = 0.1f;
        [Tooltip("Whether this should always run or only run when activated by the teleporter")]
        public bool alwaysRun = false;
        [Tooltip("If true hands wont teleport return when past the max distance if something is in the way"), FormerlySerializedAs("strict")]
        public bool ignoreMaxHandDistance = false;
        
        
        Vector3 deltaHandPos;
        Vector3 deltaHandFixedPos;

        void Awake(){
            if(hand == null && GetComponent<Hand>())
                hand = GetComponent<Hand>();
            
            if(ignoreMask == 0)
                ignoreMask = LayerMask.GetMask(Hand.grabbableLayerNameDefault, Hand.grabbingLayerName, Hand.rightHandLayerName, Hand.leftHandLayerName, AutoHandPlayer.HandPlayerLayer);
            else
                ignoreMask |= LayerMask.GetMask(Hand.rightHandLayerName, Hand.leftHandLayerName);
        }

        void Update() {
            if(hand == null || !hand.gameObject.activeInHierarchy)
                return;

            if(alwaysRun) {
                var distance = Vector3.Distance(hand.palmTransform.position, deltaHandPos);
                if(ignoreMaxHandDistance || (!ignoreMaxHandDistance && distance < hand.maxFollowDistance)) {
                    if(distance > buffer)
                        TeleportProtection(deltaHandPos, hand.palmTransform.position);
                }
                deltaHandPos = hand.palmTransform.position;
            }
        }

        void FixedUpdate() {
            if(hand == null || !hand.gameObject.activeInHierarchy)
                return;

            if(alwaysRun) {
                var distance = Vector3.Distance(hand.palmTransform.position, deltaHandFixedPos);
                if(ignoreMaxHandDistance || (!ignoreMaxHandDistance && distance < hand.maxFollowDistance)) {
                    if(distance > buffer)
                        TeleportProtection(deltaHandFixedPos, hand.palmTransform.position);
                }
                deltaHandFixedPos = hand.palmTransform.position;
            }
        }

        /// <summary>Should be called just after a teleportation</summary>
        public void TeleportProtection(Vector3 fromPos, Vector3 toPos) {
            if (hand == null || hand.transform == null)
                return;

            RaycastHit[] hits = Physics.RaycastAll(fromPos, toPos - fromPos, Vector3.Distance(fromPos, toPos), ~ignoreMask);
            Vector3 handPos = Vector3.zero;
            foreach(var hit in hits) {
                if(hit.transform != hand.transform) {
                    handPos = fromPos;
                    break;
                }
            }
            if(handPos != Vector3.zero)
                hand.SetHandLocation(handPos, hand.transform.rotation);
        }
    }
}
