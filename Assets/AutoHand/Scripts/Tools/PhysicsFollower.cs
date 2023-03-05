using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand{
    [RequireComponent(typeof(Rigidbody)), DefaultExecutionOrder(-1)]
    public class PhysicsFollower : MonoBehaviour{
        [Header("Follow Settings"), Space]
        [Tooltip("Follow target, the hand will always try to match this transforms rotation and position with rigidbody movements")]
        public Transform follow;

        [Tooltip("Stops hand physics follow - to freeze from all forces change rigidbody to kinematic or change rigidbody constraints")]
        public bool freezePos = false;

        [Tooltip("Stops hand physics follow - to freeze from all forces change rigidbody to kinematic or change rigidbody constraints")]
        public bool freezeRot = false;
        
        [Tooltip("This will offset the position without offsetting the rotation pivot")]
        public Vector3 followPositionOffset;
        public Vector3 rotationOffset;

        [Tooltip("Follow target speed (This will cause jittering if turned too high)"), Min(0)]
        public float followPositionStrength = 30;

        [Tooltip("Follow target rotation speed (This will cause jittering if turned too high)"), Min(0)]
        public float followRotationStrength = 30;

        [Tooltip("The maximum allowed velocity of the hand"), Min(0)]
        public float maxVelocity = 5;
        
        
        internal Rigidbody body;
        Transform moveTo;
        
        public void Start() {
            Set();
        }

        public virtual void Set() {
            if(moveTo == null){
                moveTo = new GameObject().transform;
                moveTo.name = gameObject.name + " FOLLOW POINT";
                moveTo.parent = follow.parent;
                moveTo.position = follow.transform.position;
                moveTo.rotation = follow.transform.rotation;
                body = GetComponent<Rigidbody>();
            }
        }
        
        public void Update() {
            OnUpdate();
        }

        protected virtual void OnUpdate() {
            if(follow == null)
                return;

            //Sets [Move To] Object
            moveTo.position = follow.position + transform.rotation*followPositionOffset;
            moveTo.rotation = follow.rotation * Quaternion.Euler(rotationOffset);
        }


        public void FixedUpdate() {
            OnFixedUpdate();
        }

        protected virtual void OnFixedUpdate() {
            if(follow == null)
                return;
            
            //Sets [Move To] Object
            moveTo.position = follow.position + transform.rotation*followPositionOffset;
            moveTo.rotation = follow.rotation * Quaternion.Euler(rotationOffset);

            //Calls physics movements
            if(!freezePos) MoveTo();
            if(!freezeRot) TorqueTo();

        }


        /// <summary>Moves the hand to the controller position using physics movement</summary>
        internal virtual void MoveTo() {
            if(followPositionStrength <= 0)
                return;

            var movePos = moveTo.position;
            var distance = Vector3.Distance(movePos, transform.position);
            var velocityClamp = maxVelocity;
            
            
            //Sets velocity linearly based on distance from hand
            var vel = (movePos - transform.position).normalized * followPositionStrength * distance;
            vel.x = Mathf.Clamp(vel.x, -velocityClamp, velocityClamp);
            vel.y = Mathf.Clamp(vel.y, -velocityClamp, velocityClamp);
            vel.z = Mathf.Clamp(vel.z, -velocityClamp, velocityClamp);
            body.velocity = vel;
        }


        /// <summary>Rotates the hand to the controller rotation using physics movement</summary>
        internal virtual void TorqueTo() {
            var toRot = moveTo.rotation;
            float angleDist = Quaternion.Angle(body.rotation, toRot);
            Quaternion desiredRotation = Quaternion.Lerp(body.rotation, toRot, Mathf.Clamp(angleDist, 0, 2) / 4f);

            var kp = 90f * followRotationStrength;
            var kd = 60f;
            Vector3 x;
            float xMag;
            Quaternion q = desiredRotation * Quaternion.Inverse(transform.rotation);
            q.ToAngleAxis(out xMag, out x);
            x.Normalize();
            x *= Mathf.Deg2Rad;
            Vector3 pidv = kp * x * xMag - kd * body.angularVelocity;
            Quaternion rotInertia2World = body.inertiaTensorRotation * transform.rotation;
            pidv = Quaternion.Inverse(rotInertia2World) * pidv;
            pidv.Scale(body.inertiaTensor);
            pidv = rotInertia2World * pidv;
            body.AddTorque(pidv);
        }

        private void OnDestroy() {
            Destroy(moveTo.gameObject);
        }
    }

  
}
