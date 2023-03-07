using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand.Demo {
    public class Grenade : MonoBehaviour {
        public Grabbable grenade;
        public Grabbable pin;
        public ConfigurableJoint pinJoint;
        public float explosionDelay = 2;
        public bool startDelayOnRelease = false;
        public float explosionForce = 100;
        public float explosionRadius = 10;
        public float pinJointStrength = 750f;
        public GameObject explosionEffect;
        public UnityEvent pinBreakEvent;
        public UnityEvent explosionEvent;

        private void OnEnable() {
            pin.isGrabbable = false;
            grenade.OnGrabEvent += OnGrenadeGrab;
            grenade.OnReleaseEvent += OnGrenadeRelease;
            pin.OnGrabEvent += OnPinGrab;
            pin.OnReleaseEvent += OnPinRelease;
            if(!grenade.jointedBodies.Contains(pin.body))
                grenade.jointedBodies.Add(pin.body);
            if(!pin.jointedBodies.Contains(grenade.body))
                pin.jointedBodies.Add(grenade.body);
        }

        private void OnDisable() {
            grenade.OnGrabEvent -= OnGrenadeGrab;
            grenade.OnReleaseEvent -= OnGrenadeRelease;
            pin.OnGrabEvent -= OnPinGrab;
            pin.OnReleaseEvent -= OnPinRelease;
        }

        void OnGrenadeGrab(Hand hand, Grabbable grab) {
            if(pinJoint != null) {
                pin.isGrabbable = true;
            }
        }

        void OnGrenadeRelease(Hand hand, Grabbable grab) {
            if(pinJoint != null) {
                pin.isGrabbable = false;
            }
            if(grenade != null && startDelayOnRelease)
                Invoke("CheckJointBreak", explosionDelay + Time.fixedDeltaTime * 3);

        }
        void OnPinGrab(Hand hand, Grabbable grab) {
            if(pinJoint != null) {
                pinJoint.breakForce = pinJointStrength;
            }
        }

        void OnPinRelease(Hand hand, Grabbable grab) {
            if(pinJoint != null) {
                pinJoint.breakForce = 100000;
            }

        }

        private void OnJointBreak(float breakForce) {
            Invoke("CheckJointBreak", Time.fixedDeltaTime*2);
        }

        void CheckJointBreak() {
            if(pinJoint == null) {
                pin.maintainGrabOffset = false;
                pin.RemoveJointedBody(grenade.body);
                grenade.RemoveJointedBody(pin.body);
                if(!startDelayOnRelease)
                    Invoke("Explode", explosionDelay);
            }

        }
        
        void Explode() {
            var hits = Physics.OverlapSphere(grenade.transform.position, explosionRadius);
            foreach(var hit in hits) {
                if(AutoHandPlayer.Instance.body == hit.attachedRigidbody) {
                    AutoHandPlayer.Instance.DisableGrounding(0.05f);
                    var dist = Vector3.Distance(hit.attachedRigidbody.position, grenade.transform.position);
                    explosionForce *= 2;
                    hit.attachedRigidbody.AddExplosionForce(explosionForce - explosionForce * (dist / explosionRadius), grenade.transform.position, explosionRadius);
                    explosionForce /= 2;
                }
                if(hit.attachedRigidbody != null) {
                    var dist = Vector3.Distance(hit.attachedRigidbody.position, grenade.transform.position);
                    hit.attachedRigidbody.AddExplosionForce(explosionForce - explosionForce * (dist / explosionRadius), grenade.transform.position, explosionRadius);
                }
            }
            explosionEvent?.Invoke();
            GameObject.Instantiate(explosionEffect, grenade.transform.position, grenade.transform.rotation);
            GameObject.Destroy(grenade.gameObject);

        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.red;
            if(grenade != null)
            Gizmos.DrawWireSphere(grenade.transform.position, explosionRadius);
        }
    }
}