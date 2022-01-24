using SEE.Utils;
using UnityEngine;
namespace SEE.Controls
{

    public class MobilePlayerMovement : MonoBehaviour
    {
        [Tooltip("Speed of movements")]
        public float Speed = 0.5f;

        [Tooltip("Handels camera direction")]
        public Joystick joystickRight;

        [Tooltip("Handels player movement")]
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
                //TODO
            }

            float speed = Speed * Time.deltaTime;

            if (!cameraState.freeMode)
            {
                //TODO
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

        /// <summary>
        /// Handels the rotation, repeats at 0 after turning 360°
        /// </summary>
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