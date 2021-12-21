using SEE.Utils;
using UnityEngine;
namespace SEE.Controls
{

    public class MobilePlayerMovement : MonoBehaviour
    {
        [Tooltip("Speed of movements")]
        public float Speed = 0.5f;
        [Tooltip("Boost factor of speed, applied when shift is pressed.")]
        public float BoostFactor = 2.0f;
        public Joystick joystickRight;
        public Joystick joystickLeft;

        private struct CameraState
        {
            internal float distance;
            internal float yaw;
            internal float pitch;
            internal bool freeMode;
        }

        private CameraState cameraState;

        [Tooltip("The code city which the player is focusing on.")]
        public GO.Plane focusedObject;

        private void Start()
        {
            Camera mainCamera = MainCamera.Camera;
            if (focusedObject != null)
            {
                cameraState.distance = 2.0f;
                cameraState.yaw = 0.0f;
                cameraState.pitch = 45.0f;
                mainCamera.transform.position = focusedObject.CenterTop;
                mainCamera.transform.position -= mainCamera.transform.forward * cameraState.distance;
                mainCamera.transform.rotation = Quaternion.Euler(cameraState.pitch, cameraState.yaw, 0.0f);
            }
            else
            {
                // Use the inital camera rotation.
                Vector3 rotation = mainCamera.transform.rotation.eulerAngles;
                cameraState.yaw = rotation.y;
                cameraState.pitch = rotation.x;
                cameraState.freeMode = true;
            }
        }

        private void Update()
        {
            Camera mainCamera = MainCamera.Camera;
            if (SEEInput.ToggleCameraLock())
            {
                cameraState.freeMode = !cameraState.freeMode;
                if (!cameraState.freeMode)
                {
                    Vector3 positionToFocusedObject = focusedObject.CenterTop - transform.position;
                    cameraState.distance = positionToFocusedObject.magnitude;
                    transform.forward = positionToFocusedObject;
                    Vector3 pitchYawRoll = transform.rotation.eulerAngles;
                    cameraState.pitch = pitchYawRoll.x;
                    cameraState.yaw = pitchYawRoll.y;
                }
            }

            float speed = Speed * Time.deltaTime;
            if (SEEInput.BoostCameraSpeed())
            {
                speed *= BoostFactor;
            }

            if (!cameraState.freeMode)
            {
                float d = 0.0f;
                if (SEEInput.MoveForward())
                {
                    d += speed;
                }
                if (SEEInput.MoveBackward())
                {
                    d -= speed;
                }
                cameraState.distance -= d;

                HandleRotation();
                mainCamera.transform.position = focusedObject.CenterTop;
                mainCamera.transform.rotation = Quaternion.Euler(cameraState.pitch, cameraState.yaw, 0.0f);
                mainCamera.transform.position -= mainCamera.transform.forward * cameraState.distance;
            }
            else // cameraState.freeMode == true
            {
                
                //Taking the joystick inputs
                float _xMovementInput = joystickLeft.Horizontal;
                float _yMovementInput = joystickLeft.Vertical;

                //calculating velocity vectors
                Vector3 _movementHorizontal = transform.right * _xMovementInput;
                Vector3 _movementVertical = transform.forward * _yMovementInput;

                //calculate final movement velocity vector
                Vector3 v = (_movementHorizontal + _movementVertical);
              
                v.Normalize();
                v *= speed; 
                mainCamera.transform.position += v;

                HandleRotation();
                mainCamera.transform.rotation = Quaternion.Euler(cameraState.pitch, cameraState.yaw, 0.0f);
            }
        }


        private void HandleRotation()
        {
            //Taking the joystick inputs
            float _xMovementInput = joystickRight.Horizontal;
            float _yMovementInput = joystickRight.Vertical;

            //roatation of the camera - repeat sets the value back to 0 after hitting 360 degrees 
            cameraState.pitch = Mathf.Repeat(cameraState.pitch - _yMovementInput * Speed, 360f);
            cameraState.yaw = Mathf.Repeat(cameraState.yaw + _xMovementInput * Speed, 360f);

        }
    }
}