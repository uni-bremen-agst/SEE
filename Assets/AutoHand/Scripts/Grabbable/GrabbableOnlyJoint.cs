using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand {
    //This component will freeze an attached grabbable joint with a fixed joint while not being held 
    [RequireComponent(typeof(Grabbable))]
    public class GrabbableOnlyJoint : MonoBehaviour {
        public Grabbable jointedGrabbable;
        public bool resetOnRelease = true;

        Grabbable localGrabbable;

        Joint freezeJoint;
        Vector3 localStartPosition;
        Quaternion localStartRotation;

        void Start() {
            localGrabbable = GetComponent<Grabbable>();
            localGrabbable.OnGrabEvent += OnGrab;
            localGrabbable.OnReleaseEvent += OnRelease;
            localStartPosition = jointedGrabbable.transform.InverseTransformPoint(transform.position);
            localStartRotation = Quaternion.Inverse(jointedGrabbable.transform.rotation) * transform.rotation;

            freezeJoint = localGrabbable.gameObject.AddComponent<FixedJoint>().GetCopyOf(Resources.Load<FixedJoint>("DefaultJoint"));
            freezeJoint.anchor = Vector3.zero;
            freezeJoint.breakForce = float.PositiveInfinity;
            freezeJoint.breakTorque = float.PositiveInfinity;
            freezeJoint.connectedBody = jointedGrabbable.body;
        }

        void OnGrab(Hand hand, Grabbable grab) {
            if(grab.GetHeldBy().Count == 1) {
                Destroy(freezeJoint);
                freezeJoint = null;
            }
        }
        void OnRelease(Hand hand, Grabbable grab) {
            if(grab.GetHeldBy().Count == 0) {
                transform.position = jointedGrabbable.transform.TransformPoint(localStartPosition);
                transform.rotation = jointedGrabbable.transform.rotation * localStartRotation;
                localGrabbable.body.position = transform.position;
                localGrabbable.body.rotation = transform.rotation;

                Invoke("CreateJoint", Time.fixedDeltaTime + Time.deltaTime);
            }
        }

        private void LateUpdate() {
            if(freezeJoint != null) {
                transform.position = jointedGrabbable.transform.TransformPoint(localStartPosition);
                transform.rotation = jointedGrabbable.transform.rotation * localStartRotation;
            }
        }

        void CreateJoint() {
            freezeJoint = localGrabbable.gameObject.AddComponent<FixedJoint>().GetCopyOf(Resources.Load<FixedJoint>("DefaultJoint"));
            freezeJoint.anchor = Vector3.zero;
            freezeJoint.breakForce = float.PositiveInfinity;
            freezeJoint.breakTorque = float.PositiveInfinity;
            freezeJoint.connectedBody = jointedGrabbable.body;
        }
    }
}