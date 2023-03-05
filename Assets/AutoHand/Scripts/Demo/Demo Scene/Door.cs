using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand.Demo{
    public class Door : PhysicsGadgetHingeAngleReader
    {
        [Header("Door should start closed")]
        public Rigidbody body;
        Vector3 closedPosition;
        Quaternion closedRotation;
        
        [Tooltip("The door needs to reach this level of open before it can be reset")]
        public float minThreshold = 0.05f;
        public float midThreshold = 0.05f;
        [Tooltip("The door needs to reach this level of open before it can be reset")]
        public float maxThreshold = 0.05f;
        [Space]
        public UnityEvent OnMax;
        public UnityEvent OnMid;
        public UnityEvent OnMin;
        
        bool min = false;
        bool max = false;
        bool mid = true;

        private void Awake(){
            if(!body && GetComponent<Rigidbody>())
                body = GetComponent<Rigidbody>();
            
            closedPosition = transform.position;
            closedRotation = transform.rotation;
        }

        protected void FixedUpdate(){
            if(!max && mid && GetValue()+maxThreshold >= 1) {
                Max();
            }

            if(!min && mid && GetValue()-minThreshold <= -1){
                Min();
            }
        
            if (GetValue() <= midThreshold && max && !mid) {
                Mid();
            }

            if (GetValue() >= -midThreshold && min && !mid) {
                Mid();
            }
        }


        void Max(){
            mid = false;
            max = true;
            OnMax?.Invoke();
        }

        void Mid(){
            min = false;
            max = false;
            mid = true;
            OnMid?.Invoke();
        }

        void Min() {
            min = true;
            mid = false;
            OnMin?.Invoke();
        }

        public void ClosedDoor() {
            transform.position = closedPosition;
            transform.rotation = closedRotation;
            body.isKinematic = true;
        }

        private void OnDrawGizmosSelected() {
            if(!body && GetComponent<Rigidbody>())
                body = GetComponent<Rigidbody>();
        }
    }
}
