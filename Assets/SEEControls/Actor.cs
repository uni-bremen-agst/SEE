using System;
using UnityEngine;

namespace SEE.Controls
{
    public class Actor : MonoBehaviour
    {
        [Tooltip("The first device from which to read the input.")]
        public InputDevice inputDeviceA;

        [Tooltip("The second device from which to read the input.")]
        public InputDevice inputDeviceB;

        [Tooltip("The action applied to move the camera.")]
        public CameraAction cameraAction;

        [Tooltip("The action applied to select an object.")]
        private SelectionAction selectionAction;

        private void Start()
        {
            if (inputDeviceA == null)
            {
                Debug.LogError("No input device A set for actor.\n");
            }
            else
            {
                inputDeviceA.ListenMovemementDirection(OnMoveDirection);
                inputDeviceA.ListenThrottle(OnThrottle);
                inputDeviceA.ListenButtonB(OnButtonB);
            }
            if (inputDeviceB == null)
            {
                Debug.LogError("No input device B set for actor.\n");
            }
            else
            {
                inputDeviceB.ListenPointingDirection(OnPointingDirection);
                inputDeviceB.ListenTrigger(OnTrigger);

                inputDeviceB.ListenMovemementDirection(OnLookDirection);
                inputDeviceB.ListenButtonB(OnButtonB);
                inputDeviceB.ListenScroll(OnScroll);
                
            }
            if (cameraAction == null)
            {
                Debug.LogError("There is no camera action assigned to this actor.\n");
            }
            if (selectionAction == null)
            {
                // In VR, the selectionAction will be put on the pointing hand so that
                // the selection ray starts at the hand rather than at the player.
                // In Non-VR, it will be added to the player.
                // FIXME: Do we have a smarter solution than this one? It introduces
                // too many assumptions.
                XRDevice pointer = gameObject.GetComponent<XRDevice>();
                if (pointer != null)
                {
                    // Virtual reality
                    selectionAction = pointer.PointingHand.gameObject.AddComponent<SelectionAction>();
                    selectionAction.ThreeDimensions = true;
                }
                else
                {
                    // Non-virtual reality
                    selectionAction = gameObject.AddComponent<SelectionAction>();
                    selectionAction.ThreeDimensions = false;
                }
                selectionAction.SetCamera(GetCamera());
            }
        }

        private Camera GetCamera()
        {
            return gameObject.GetComponentInChildren<Camera>();
        }

        protected void OnThrottle(float speed)
        {
            //Debug.LogFormat("Actor speed={0}\n", speed);
            cameraAction.SetSpeed(speed);
        }

        protected void OnMoveDirection(Vector3 direction)
        {
            //Debug.LogFormat("Actor direction={0}\n", direction);
            cameraAction.MoveToward(direction);
        }

        protected void OnLookDirection(Vector3 direction)
        {
            cameraAction.LookAt(direction);
        }

        protected void OnButtonB(bool activated)
        {
            cameraAction.Look(activated);
        }

        protected void OnScroll(float value)
        {
            cameraAction.SetBoost(value);
        }

        protected void OnPointingDirection(Vector3 direction)
        {
            selectionAction.SelectAt(direction);
        }

        protected void OnTrigger(float value)
        {
            selectionAction.RayOnOff(value > 0);
        }
    }
}