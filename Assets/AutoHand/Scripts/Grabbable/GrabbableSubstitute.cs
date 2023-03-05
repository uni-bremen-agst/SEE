using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand{
    /// <summary>Takes a reference for a disabled grabbable, and grabs that instead</summary>
    [RequireComponent(typeof(Grabbable))]
    public class GrabbableSubstitute : MonoBehaviour{
        [Tooltip("Whether or not to disable this gameobject on grab")]
        public bool disableOnGrab = true;
        [Tooltip("If true, the substitute will return to the this local location and turn off and the local grabbable will turn back on")]
        public bool returnOnRelease = false;
        public Grabbable grabbableSubstitute;

        Grabbable localGrabbable;

        private void Start() {
            localGrabbable = GetComponent<Grabbable>();
            localGrabbable.OnGrabEvent += OnGrabOriginal;
            grabbableSubstitute.OnReleaseEvent += OnReleaseSub;
        }

        void OnGrabOriginal(Hand hand, Grabbable grab) {
            hand.Release();

            grabbableSubstitute.gameObject.SetActive(true);
            hand.CreateGrabConnection(grabbableSubstitute, hand.transform.position, hand.transform.rotation, grab.transform.position, grab.transform.rotation, true);

            if(disableOnGrab)
                grab.gameObject.SetActive(false);
        }

        void OnReleaseSub(Hand hand, Grabbable grab) {
            if(returnOnRelease) {
                grabbableSubstitute.transform.position = localGrabbable.transform.position;
                grabbableSubstitute.transform.rotation = localGrabbable.transform.rotation;
                grabbableSubstitute.body.position = localGrabbable.body.position;
                grabbableSubstitute.body.rotation = localGrabbable.body.rotation;

                grabbableSubstitute.gameObject.SetActive(false);
                if(disableOnGrab)
                    grab.gameObject.SetActive(true);

            }
        }

        /// <summary>Disables the local grabbale (if enabled), enables the substitute at the local grabbables positoin)</summary>
        public void LocalSubstitute(Hand hand, Grabbable grab) {
            if(localGrabbable.gameObject.activeInHierarchy) {
                grabbableSubstitute.gameObject.SetActive(true);
                grabbableSubstitute.transform.position = localGrabbable.transform.position;
                grabbableSubstitute.transform.rotation = localGrabbable.transform.rotation;
                grabbableSubstitute.body.position = localGrabbable.body.position;
                grabbableSubstitute.body.rotation = localGrabbable.body.rotation;

                if(disableOnGrab)
                    localGrabbable.gameObject.SetActive(false);
            }
        }
    }
}