using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand {
    [RequireComponent(typeof(Grabbable))]
    public class GrabbableExtraEvents : MonoBehaviour {
        public UnityHandGrabEvent OnFirstGrab;
        public UnityHandGrabEvent OnLastRelease;
        public UnityHandGrabEvent OnTwoHandedGrab;
        public UnityHandGrabEvent OnTwoHandedRelease;

        Grabbable grab;

        void OnEnable() {
            grab = GetComponent<Grabbable>();
            grab.OnGrabEvent += Grab;
            grab.OnReleaseEvent += Release;
        }

        void OnDisable() {
            grab = grab ?? GetComponent<Grabbable>();
            grab.OnGrabEvent -= Grab;
            grab.OnReleaseEvent -= Release;

        }

        public void Grab(Hand hand, Grabbable grab) {
            if(grab.HeldCount() == 1) {
                OnFirstGrab?.Invoke(hand, grab);
            }
            if(grab.HeldCount() == 2) {
                OnTwoHandedGrab?.Invoke(hand, grab);
            }
        }

        public void Release(Hand hand, Grabbable grab) {
            if(grab.HeldCount() == 0) {
                OnLastRelease?.Invoke(hand, grab);
            }
            if(grab.HeldCount() == 1) {
                OnTwoHandedRelease?.Invoke(hand, grab);
            }
        }
    }

}