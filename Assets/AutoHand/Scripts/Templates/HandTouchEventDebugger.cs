using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo {
    public class HandTouchEventDebugger : MonoBehaviour {
        public HandTouchEvent touchEvent;

        private void OnEnable() {
            touchEvent.HandStartTouchEvent += StartTouch;
            touchEvent.HandStopTouchEvent += StopTouch;
        }

        private void OnDisable() {
            touchEvent.HandStartTouchEvent -= StartTouch;
            touchEvent.HandStopTouchEvent -= StopTouch;
        }

        void StartTouch(Hand hand) {
            Debug.Log("Start Touch: " + hand.name);
        }
        void StopTouch(Hand hand) {
            Debug.Log("Stop Touch: " + hand.name);
        }
    }

}