using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand {
    [HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/extras/stickies")]
    public class Stickable : MonoBehaviour{
        [Header("Sticky Settings")]
        public Rigidbody body;
        [Tooltip("How strong the joint is between the stickable and this")]
        public float stickStrength = 1;
        [Tooltip("Multiplyer for required stick speed to activate")]
        public float stickSpeedMultiplyer = 1;
        [Tooltip("This index must match the sticky object to stick")]
        public int stickIndex = 0;


        [Header("Event")]
        public UnityEvent OnStick;
        public UnityEvent EndStick;

        Sticky stickSource;

        private void OnDrawGizmosSelected() {
            if(!body && GetComponent<Rigidbody>())
                body = GetComponent<Rigidbody>();
        }

        public void Stick(Sticky source) {
            stickSource = source;
            OnStick?.Invoke();
        }


        public void Unstick(Sticky source) {
            stickSource = null;
            EndStick?.Invoke();

        }

        public void ForceReleaseStick() {
            stickSource?.ForceRelease(this);
        }
    }
}
