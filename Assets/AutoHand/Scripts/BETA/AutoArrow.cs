using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo
{
    public class AutoArrow : MonoBehaviour
    {
        public float minPenetrationVelocity = 4f;
        public float maxPenetrationVelocity = 20f;
        public float minPenetrationDistance = 0.1f;
        public float maxPenetrationDistance = 0.2f;
        public float impactForceMultiplier = 1f;
        public Grabbable grabbable;
        Grabbable hitGrabbable;

        public AutoBow firedBow { get; internal set; }

        Vector3 direction;
        float currforce;

        public void FireArrow(float force, Grabbable arrowGrab, AutoBow firedBow)
        {
            grabbable = arrowGrab;
            this.firedBow = firedBow;
            impactForceMultiplier = firedBow.arrowImpactForceMultiplier;

            currforce = force;
            direction = transform.TransformDirection(firedBow.arrowForceDirection);
        }

        private void OnEnable() {
            grabbable.OnGrabEvent += OnGrabbed;
        }

        private void OnDisable() {
            grabbable.OnGrabEvent -= OnGrabbed;
        }
        void OnGrabbed(Hand hand, Grabbable grab) {
            grabbable.ActivateRigidbody();
            hitGrabbable?.RemoveChildGrabbable(grab);
            hitGrabbable = null;
            firedBow = null;
        }

        public void FixedUpdate()
        {
            if (firedBow != null)
            {
                var currVel = direction * currforce;
                currVel += Physics.gravity * Time.fixedDeltaTime;
                direction = currVel.normalized;
                grabbable.rootTransform.position += direction * Time.fixedDeltaTime * currforce;
                grabbable.rootTransform.rotation = Quaternion.FromToRotation(firedBow.arrowForceDirection, direction);
                grabbable.body.velocity = Vector3.zero;
                grabbable.body.angularVelocity = Vector3.zero;
            }
        }




        public void OnCollisionEnter(Collision collision)
        {
            if (firedBow != null)
            {
                if (collision.rigidbody == null || collision.rigidbody != firedBow.bowHandleGrabbable.body)
                {
                    if (currforce > minPenetrationVelocity)
                    {
                        GrabbableChild hitGrabbableChild;
                        if(collision.collider.CanGetComponent<Grabbable>(out hitGrabbable)) {
                            hitGrabbable.AddChildGrabbable(grabbable);

                        }
                        if (collision.collider.CanGetComponent<GrabbableChild>(out hitGrabbableChild)) {

                            hitGrabbable = hitGrabbableChild.grabParent;
                            hitGrabbable.AddChildGrabbable(grabbable);

                        }

                        grabbable.rootTransform.position += grabbable.body.velocity * 1 / 50f;
                        grabbable.rootTransform.parent = collision.collider.transform;
                        grabbable.DeactivateRigidbody();
                        
                    }

                    //grabbable.body.isKinematic = false;
                    firedBow = null;

                    if (collision.rigidbody != null)
                        collision.rigidbody.AddForceAtPosition(impactForceMultiplier * direction * currforce, collision.contacts[0].point, ForceMode.Impulse);
                }

            }
        }
    }
}
