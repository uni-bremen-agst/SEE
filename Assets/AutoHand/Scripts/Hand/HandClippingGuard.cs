using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand{
    public class HandClippingGuard : MonoBehaviour{
        //This script is designed to help prevent the hand from clipping through thin grabbables on release.
        //Also recommeneded that the grabbable has 0 `ignore release time`

        public Hand hand;
        [Tooltip("This should be a sphere collider that covers the hand (similar, but seperate from the recommended trigger sphere collider)")]
        public SphereCollider collisionGuard;
        public Transform body;
        public float guardTime = 0.02f;

        Vector3 grabPoint;
        bool runProtection = false;
        Coroutine guardRoutine;

        // Start is called before the first frame update
        void Start(){
            collisionGuard.enabled = false;
            hand.OnGrabJointBreak += OnRelease;
            hand.OnBeforeGrabbed += BeforeGrab;
        }

        void BeforeGrab(Hand hand, Grabbable grab) {
            if(body == null)
                body = hand.transform.parent;

            if (grab.ignoreReleaseTime == 0)
                runProtection = true;
            else
                runProtection = false;

            grabPoint = hand.transform.position;
            if(guardRoutine != null){
                StopCoroutine(guardRoutine);
                collisionGuard.enabled = false;
            }
        }

        void OnRelease(Hand hand, Grabbable grab) {
            if (runProtection) {
                guardRoutine = StartCoroutine(Guard(hand));
                runProtection = false;
            }
        }

        IEnumerator Guard(Hand hand) {
            hand.body.position = grabPoint;
            hand.transform.position = grabPoint;
            hand.transform.position = Vector3.MoveTowards(hand.transform.position, body.position, collisionGuard.radius);
            collisionGuard.enabled = true;
            yield return new WaitForSeconds(guardTime);
            collisionGuard.enabled = false;
        }
}
}
