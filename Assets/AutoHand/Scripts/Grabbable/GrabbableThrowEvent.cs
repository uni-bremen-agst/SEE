using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace Autohand{
    [RequireComponent(typeof(Rigidbody), typeof(Grabbable))]
    public class GrabbableThrowEvent : MonoBehaviour {
        [Tooltip("The velocity magnitude required on collision to cause the break event")]
        public float breakVelocity = 1;
        [Tooltip("The layers that will cause this grabbale to break")]
        public LayerMask collisionLayers = ~0;
        public UnityEvent OnBreak;
        Rigidbody rb;
        Grabbable grab;
        bool thrown = false;
        Coroutine resetThrowing;
        float throwTime = 3;

        void Awake() {
            rb = GetComponent<Rigidbody>();
            grab = GetComponent<Grabbable>();
        }

        private void OnEnable() {
            grab.OnReleaseEvent += OnReleased;
        }
        private void OnDisable() {
            grab.OnReleaseEvent -= OnReleased;
        }

        void OnReleased(Hand hand, Grabbable grab) {if(rb.velocity.magnitude >= breakVelocity) 
            thrown = true;
            if(resetThrowing != null)
                StopCoroutine(resetThrowing);
            resetThrowing = StartCoroutine(ResetThrown());
        }

        IEnumerator ResetThrown() {
            yield return new WaitForSeconds(throwTime);
            thrown = false;
            resetThrowing = null;
        }

        

        private void OnCollisionEnter(Collision collision) {
            if(!thrown || grab == null)
                return;

            if(((1 << collision.collider.gameObject.layer) & collisionLayers) == 0)
                return;
        
            if(rb.velocity.magnitude >= breakVelocity) {
                Invoke("Break", Time.fixedDeltaTime);
            }
        }

        void Break() {
            OnBreak.Invoke();
        }
}
}
