using SEE.Utils;
using UnityEngine;

namespace SEE.Controls
{

    public class DesktopPlayerMovement : MonoBehaviour
    {
        [Tooltip("Speed of movements")]
        public float Speed = 2.0f;
        [Tooltip("Boost factor of speed, applied when shift is pressed.")]
        public float BoostFactor = 2.0f;

        private struct CameraState
        {
            internal float distance;
            internal float yaw;
            internal float pitch;
            internal bool freeMode;
        }

        private CameraState cameraState;
        private const int RightMouseButton = 1;

        [Tooltip("The code city which the player is focusing on.")]
        public GO.Plane focusedObject;

        private void Start()
        {
            Camera mainCamera = MainCamera.Camera;
            if (focusedObject != null)
            {                
                mainCamera.transform.position = focusedObject.CenterTop;
            }
            else
            {
                cameraState.freeMode = true;
                Debug.Log($"Player {name} has no focus object assigned.\n");
            }
            cameraState.distance = 2.0f;
            cameraState.yaw = 0.0f;
            cameraState.pitch = 45.0f;
            mainCamera.transform.rotation = Quaternion.Euler(cameraState.pitch, cameraState.yaw, 0.0f);
            mainCamera.transform.position -= mainCamera.transform.forward * cameraState.distance;
        }

        private void Update()
        {
            Camera mainCamera = MainCamera.Camera;
            if (Input.GetKeyDown(KeyBindings.ToggleCameraLock))
            {
                if (cameraState.freeMode)
                {
                    Vector3 positionToFocusedObject = focusedObject.CenterTop - transform.position;
                    cameraState.distance = positionToFocusedObject.magnitude;
                    transform.forward = positionToFocusedObject;
                    Vector3 pitchYawRoll = transform.rotation.eulerAngles;
                    cameraState.pitch = pitchYawRoll.x;
                    cameraState.yaw = pitchYawRoll.y;
                }
                cameraState.freeMode = !cameraState.freeMode;
            }

            float speed = Speed * Time.deltaTime;
            if (Input.GetKey(KeyBindings.BoostCameraSpeed))
            {
                speed *= BoostFactor;
            }

            if (!cameraState.freeMode)
            {
                float d = 0.0f;
                if (Input.GetKey(KeyBindings.MoveForward))
                {
                    d += speed;
                }
                if (Input.GetKey(KeyBindings.MoveBackward))
                {
                    d -= speed;
                }
                cameraState.distance -= d;

                if (Input.GetMouseButton(RightMouseButton))
                {
                    float x = Input.GetAxis("mouse x");
                    float y = Input.GetAxis("mouse y");
                    cameraState.yaw += x;
                    cameraState.pitch -= y;
                }
                mainCamera.transform.position = focusedObject.CenterTop;
                mainCamera.transform.rotation = Quaternion.Euler(cameraState.pitch, cameraState.yaw, 0.0f);
                mainCamera.transform.position -= mainCamera.transform.forward * cameraState.distance;
            }
            else // cameraState.freeMode == true
            {
                Vector3 v = Vector3.zero;
                if (Input.GetKey(KeyBindings.MoveForward))
                {
                    v += mainCamera.transform.forward;
                }
                if (Input.GetKey(KeyBindings.MoveBackward))
                {
                    v -= mainCamera.transform.forward;
                }
                if (Input.GetKey(KeyBindings.MoveRight))
                {
                    v += mainCamera.transform.right;
                }
                if (Input.GetKey(KeyBindings.MoveLeft))
                {
                    v -= mainCamera.transform.right;
                }
                if (Input.GetKey(KeyBindings.MoveUp))
                {
                    v += Vector3.up;
                }
                if (Input.GetKey(KeyBindings.MoveDown))
                {
                    v += Vector3.down;
                }
                v.Normalize();
                v *= speed;
                mainCamera.transform.position += v;

                if (Input.GetMouseButton(RightMouseButton))
                {
                    float x = Input.GetAxis("mouse x");
                    float y = Input.GetAxis("mouse y");
                    cameraState.yaw += x;
                    cameraState.pitch -= y;
                }
                mainCamera.transform.rotation = Quaternion.Euler(cameraState.pitch, cameraState.yaw, 0.0f);
            }
        }
    }
}