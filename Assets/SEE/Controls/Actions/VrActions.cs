using System;
using Autohand;
using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;
using SEE.Game;

namespace SEE.Controls.Actions
{
    public class VrActions : MonoBehaviour
    {

        private Vector2 leftFront;
        private Vector2 rightBack;


        public void Start()
        {
            GrabbableExtraEvents grabbableExtraEvents = gameObject.AddOrGetComponent<GrabbableExtraEvents>();
            Grabbable grabbable = gameObject.GetComponent<Grabbable>();



            grabbable.onGrab = new UnityHandGrabEvent();
            grabbable.onGrab.AddListener(OnGrab);

            gameObject.AddComponent<GrabbableExtraEvents>();
            grabbableExtraEvents.OnFirstGrab = new UnityHandGrabEvent();
            grabbableExtraEvents.OnLastRelease = new UnityHandGrabEvent();

            grabbableExtraEvents.OnFirstGrab.AddListener(OnFirstGrab);
            grabbableExtraEvents.OnLastRelease.AddListener(OnLastRelease);

        }

        //FIXME
        private void OnFirstGrab(Hand hand, Grabbable grabbable)
        {
            Debug.LogWarning("first grab");
           // VrMoveAction.CreateReversibleAction();

            if (gameObject.TryGetNode(out Node node) && !node.IsRoot())
            {
                gameObject.GetComponent<VrTriggerEvents>().enabled = true;

                Portal.GetPortal(gameObject, out Vector2 leftFront, out Vector2 rightBack);
                this.leftFront = leftFront;
                this.rightBack = rightBack;
                Portal.SetInfinitePortal(gameObject);
                Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
                rigidbody.isKinematic = false;
                grabbable.parentOnGrab = true;
            }
        }

        private void OnLastRelease(Hand hand, Grabbable grabbable)
        {


            Debug.LogWarning("last release");

            if (gameObject.TryGetNode(out Node node) && !node.IsRoot())
            {
                gameObject.GetComponent<VrTriggerEvents>().enabled = true;
                Portal.SetPortal(gameObject, leftFront, rightBack);

                Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
                rigidbody.isKinematic = true;
                grabbable.parentOnGrab = false;
            }

        }


        private void OnGrab(Hand hand, Grabbable grabbable)
        {




        }

    }
}