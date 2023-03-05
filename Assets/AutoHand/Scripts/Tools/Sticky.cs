using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand{
    [RequireComponent(typeof(Rigidbody))]
    public class Sticky : MonoBehaviour{
        [Header("Sticky Settings")]
        [Tooltip("How strong the joint is between the stickable and this")]
        public float stickStrength = 1;
        [Tooltip("Multiplyer for required stick speed to activate")]
        public float requiredStickSpeed = 1;
        [Tooltip("This index must match the stickable object to stick")]
        public int stickIndex = 0;

        [Header("Event")]
        public UnityEvent OnStick;

        Rigidbody body;
        List<Stickable> stickers;
        List<Joint> joints;

        private void Start() {
            if(body == null)
                body = GetComponent<Rigidbody>();
            stickers = new List<Stickable>();
            joints = new List<Joint>();
        }

        void OnCollisionEnter(Collision collision) {
            Stickable stick;
            if(collision.gameObject.CanGetComponent(out stick)) {
                CreateStick(stick);
            }   
        }

        void CreateStick(Stickable sticker) {
            if(stickers.Contains(sticker) || sticker.stickIndex != stickIndex)
                return;
            if(sticker.body.velocity.sqrMagnitude*sticker.stickSpeedMultiplyer < requiredStickSpeed)
                return;

            var joint = gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = sticker.body;
            joint.breakForce = 1000*stickStrength*sticker.stickStrength;
            joint.breakTorque = 1000*stickStrength*sticker.stickStrength;
                
            joint.connectedMassScale = 1;
            joint.massScale = 1;
            joint.enableCollision = false;
            joint.enablePreprocessing = true;

            sticker.Stick(this);
            OnStick?.Invoke();

            joints.Add(joint);
            stickers.Add(sticker);
        }

        public void ForceRelease(Stickable stuck) {
            Destroy(joints[stickers.IndexOf(stuck)]);
        }

        void OnJointBreak(float breakForce) {
            StartCoroutine(JointBreak());
        }

        IEnumerator JointBreak() {
            yield return new WaitForFixedUpdate();
            for(int i = joints.Count-1; i >= 0; i--) {
                if(!joints[i]) {
                    joints.RemoveAt(i);

                    stickers[i].EndStick?.Invoke();
                    stickers.RemoveAt(i);
                }
            }
        }
    
        private void OnDrawGizmosSelected() {
            if(!body && GetComponent<Rigidbody>())
                body = GetComponent<Rigidbody>();
        }
    }
}
