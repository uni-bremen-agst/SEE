using System;
using Autohand;
using SEE.GO;
using UnityEngine;

namespace SEE.Controls.Actions
{
    public class VrActions : MonoBehaviour
    {
        public void Start()
        {
            GrabbableExtraEvents grabbableExtraEvents = gameObject.AddOrGetComponent<GrabbableExtraEvents>();

            gameObject.AddComponent<GrabbableExtraEvents>();
            grabbableExtraEvents.OnFirstGrab = new UnityHandGrabEvent();
            grabbableExtraEvents.OnLastRelease = new UnityHandGrabEvent();

            grabbableExtraEvents.OnFirstGrab.AddListener(OnFirstGrab);
            grabbableExtraEvents.OnLastRelease.AddListener(OnLastRelease);
        }

        private void OnFirstGrab(Hand hand, Grabbable grabbable)
        {
            Debug.LogWarning("first grab");
           // VrMoveAction.CreateReversibleAction();
        }

        private void OnLastRelease(Hand hand, Grabbable grabbable)
        {
            Debug.LogWarning("last release");
        }

    }
}