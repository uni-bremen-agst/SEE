using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand {
    public class HandPublicEvents : MonoBehaviour {
        public Hand hand;
        public UnityHandGrabEvent OnBeforeGrab = new UnityHandGrabEvent();
        public UnityHandGrabEvent OnGrab = new UnityHandGrabEvent();
        public UnityHandGrabEvent OnRelease = new UnityHandGrabEvent();
        public UnityHandGrabEvent OnForceRelease = new UnityHandGrabEvent();
        public UnityHandGrabEvent OnSqueeze = new UnityHandGrabEvent();
        public UnityHandGrabEvent OnUnsqueeze = new UnityHandGrabEvent();
        public UnityHandGrabEvent OnHighlight = new UnityHandGrabEvent();
        public UnityHandGrabEvent OnStopHighlight = new UnityHandGrabEvent();


        void OnEnable() {
            hand.OnBeforeGrabbed += OnBeforeGrabEvent;
            hand.OnGrabbed += OnGrabEvent;
            hand.OnReleased += OnReleaseEvent;
            hand.OnSqueezed += OnSqueezeEvent;
            hand.OnUnsqueezed += OnUnsqueezeEvent;
            hand.OnHighlight += OnHighlightEvent;
            hand.OnStopHighlight += OnStopHighlightEvent;
        }

        void OnDisable() {
            hand.OnBeforeGrabbed -= OnBeforeGrabEvent;
            hand.OnGrabbed -= OnGrabEvent;
            hand.OnReleased -= OnReleaseEvent;
            hand.OnSqueezed -= OnSqueezeEvent;
            hand.OnUnsqueezed -= OnUnsqueezeEvent;
            hand.OnHighlight -= OnHighlightEvent;
            hand.OnStopHighlight -= OnStopHighlightEvent;
        }

        public void OnBeforeGrabEvent(Hand hand, Grabbable grab) {
            OnBeforeGrab?.Invoke(hand, grab);
        }

        public void OnGrabEvent(Hand hand, Grabbable grab) {
            OnGrab?.Invoke(hand, grab);
        }

        public void OnReleaseEvent(Hand hand, Grabbable grab) {
            OnRelease?.Invoke(hand, grab);
        }

        public void OnSqueezeEvent(Hand hand, Grabbable grab) {
            OnSqueeze?.Invoke(hand, grab);
        }

        public void OnUnsqueezeEvent(Hand hand, Grabbable grab) {
            OnUnsqueeze?.Invoke(hand, grab);
        }
        public void OnHighlightEvent(Hand hand, Grabbable grab) {
            OnHighlight?.Invoke(hand, grab);
        }

        public void OnStopHighlightEvent(Hand hand, Grabbable grab) {
            OnStopHighlight?.Invoke(hand, grab);
        }

        public void OnForceReleaseEvent(Hand hand, Grabbable grab) {
            OnForceRelease?.Invoke(hand, grab);
        }

        private void OnDrawGizmosSelected() {
            if(hand == null && GetComponent<Hand>())
                hand = GetComponent<Hand>();
        }
    }
}
