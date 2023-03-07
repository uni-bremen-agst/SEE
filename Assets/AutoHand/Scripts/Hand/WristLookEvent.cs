using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo{
    public class WristLookEvent : MonoBehaviour{
        public Hand hand;
        public Camera head;

        [Tooltip("The minimum head->wrist distance required to activate")]
        public float maxDistance = 0.75f;
        [Tooltip("The angle precisness required to activate; 0 is any angle, 1 is exactly pointed at the face")]
        [Range(0, 1)]
        public float anglePreciseness = 0.75f;
        public bool disableWhileHolding = true;

        [Header("Events")]
        public UnityHandEvent OnShow;
        public UnityHandEvent OnHide;


        bool showing = false;

        void Update(){
            if (hand == null || head == null)
                return;

            var handPos = hand.transform.position;
            var headPos = head.transform.position;

            float lookness = Vector3.Dot((headPos - handPos).normalized, -hand.palmTransform.forward);
            float distance = Vector3.Distance(headPos, hand.palmTransform.position);
            bool found = lookness >= anglePreciseness && distance < maxDistance && hand.holdingObj == null;

            if (!showing && found){
                OnShow?.Invoke(hand);
                showing = true;
            }
            else if(showing && !found){
                OnHide?.Invoke(hand);
                showing = false;
            }
        }
    }
}