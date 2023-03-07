using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand {
    [RequireComponent(typeof(Grabbable))]
    public class GrabbableCollisionHaptics : MonoBehaviour {
        [Tooltip("The layers that cause the sound to play")]
        public LayerMask collisionTriggers = ~0;
        public float hapticAmp = 0.8f;
        public float velocityAmp = 0.5f;
        public float repeatDelay = 0.2f;
        public float maxDuration = 0.5f;
        [Tooltip("Source to play sound from")]
        public AnimationCurve velocityAmpCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [Tooltip("Source to play sound from")]
        public AnimationCurve velocityDurationCurve = AnimationCurve.Linear(0, 0, 1, 1);

        Grabbable grab;
        Rigidbody body;
        bool canPlay = true;
        Coroutine playRoutine;

        private void Start() {
            body = GetComponent<Rigidbody>();
            grab = GetComponent<Grabbable>();

            //So the sound doesn't play when falling in place on start
            StartCoroutine(HapticPlayBuffer(1f));
        }

        private void OnDisable() {
            if(playRoutine != null)
                StopCoroutine(playRoutine);
        }

        void OnCollisionEnter(Collision collision) {
            if(canPlay && collisionTriggers == (collisionTriggers | (1 << collision.gameObject.layer))) {
                if(body != null) {
                    if(collision.collider.attachedRigidbody == null || collision.collider.attachedRigidbody.mass > 0.0000001f) {
                        var magnitude = collision.relativeVelocity.magnitude;
                        grab.PlayHapticVibration(Mathf.Clamp(velocityDurationCurve.Evaluate(magnitude), 0, maxDuration), velocityAmpCurve.Evaluate(magnitude * velocityAmp) * hapticAmp);
                        if(playRoutine != null)
                            StopCoroutine(playRoutine);
                        playRoutine = StartCoroutine(PlayBuffer());
                    }
                }
            }
        }

        IEnumerator PlayBuffer() {
            canPlay = false;
            yield return new WaitForSeconds(repeatDelay);
            canPlay = true;
            playRoutine = null;
        }

        IEnumerator HapticPlayBuffer(float time) {
            canPlay = false;
            yield return new WaitForSeconds(time);
            canPlay = true;
            playRoutine = null;
        }
    }
}