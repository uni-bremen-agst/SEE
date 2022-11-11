using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Moves a player in a desktop environment (keyboard and mouse input).
    /// </summary>
    public class DesktopPlayerMovement : PlayerMovement
    {
        [Tooltip("Speed of movements")]
        public float Speed = 1f;
        [Tooltip("Boost factor of speed, applied when shift is pressed.")]
        public float BoostFactor = 4f;

        private struct CameraState
        {
            internal float Distance;
            internal float Yaw;
            internal float Pitch;
            internal bool FreeMode;
        }

        private CameraState cameraState;

        [Tooltip("The code city which the player is focusing on.")]
        public GO.Plane FocusedObject;

        private void Start()
        {
            if (FocusedObject != null)
            {
                cameraState.Distance = 2.0f;
                cameraState.Yaw = 0.0f;
                cameraState.Pitch = 45.0f;
                transform.position = FocusedObject.CenterTop;
                transform.position -= transform.forward * cameraState.Distance;
                transform.rotation = Quaternion.Euler(cameraState.Pitch, cameraState.Yaw, 0.0f);
            }
            else
            {
                // Use the inital camera rotation.
                Vector3 rotation = transform.rotation.eulerAngles;
                cameraState.Yaw = rotation.y;
                cameraState.Pitch = rotation.x;
                cameraState.FreeMode = true;
            }
            lastAxis = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        }

        private void Update()
        {
            if (FocusedObject != null && SEEInput.ToggleCameraLock())
            {
                cameraState.FreeMode = !cameraState.FreeMode;
                if (!cameraState.FreeMode)
                {
                    Vector3 positionToFocusedObject = FocusedObject.CenterTop - transform.position;
                    cameraState.Distance = positionToFocusedObject.magnitude;
                    transform.forward = positionToFocusedObject;
                    Vector3 pitchYawRoll = transform.rotation.eulerAngles;
                    cameraState.Pitch = pitchYawRoll.x;
                    cameraState.Yaw = pitchYawRoll.y;
                }
            }

            float speed = Speed * Time.deltaTime;
            if (SEEInput.BoostCameraSpeed())
            {
                speed *= BoostFactor;
            }

            if (!cameraState.FreeMode)
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
                cameraState.Distance -= d;

                HandleRotation();
                transform.position = FocusedObject.CenterTop;
                transform.rotation = Quaternion.Euler(cameraState.Pitch, cameraState.Yaw, 0.0f);
                transform.position -= transform.forward * cameraState.Distance;
            }
            else // cameraState.freeMode == true
            {
                Vector3 velocity = Vector3.zero;
                if (SEEInput.MoveForward())
                {
                    velocity += transform.forward;
                }
                if (SEEInput.MoveBackward())
                {
                    velocity -= transform.forward;
                }
                if (SEEInput.MoveRight())
                {
                    velocity += transform.right;
                }
                if (SEEInput.MoveLeft())
                {
                    velocity -= transform.right;
                }
                if (SEEInput.MoveUp())
                {
                    velocity += Vector3.up;
                }
                if (SEEInput.MoveDown())
                {
                    velocity += Vector3.down;
                }
                velocity.Normalize();
                velocity *= speed;
                transform.position += velocity;

                HandleRotation();
                // Players Yaw
                transform.rotation = Quaternion.Euler(0.0f, cameraState.Yaw, 0.0f);
                // Cameras Pitch and Yaw
                Camera.main.transform.rotation = Quaternion.Euler(cameraState.Pitch, cameraState.Yaw, 0.0f);
            }
        }

        /// <summary>
        /// The mouse position of the last frame.
        /// </summary>
        private Vector2 lastAxis;

        /// <summary>
        /// If the user wants us, we rotate the gameobject according to mouse input.
        /// Modifies <see cref="cameraState.yaw"/> and <see cref="cameraState.pitch"/>.
        ///
        /// Note: This is a workaround of issues with the correct mouse position
        /// in a remote-desktop session.
        /// </summary>
        private void HandleRotation()
        {
            if (SEEInput.RotateCamera())
            {
                float x = -(lastAxis.x - Input.mousePosition.x) * 0.1f;
                float y = -(lastAxis.y - Input.mousePosition.y) * 0.1f;

                // These were the original statements which, however, do not work in
                // a remote-desktop session (RDP).
                // float x = Input.GetAxis("Mouse X");
                // float y = Input.GetAxis("Mouse Y");

                cameraState.Yaw += x;
                cameraState.Pitch -= y;
                // locks the camera, so the player can look up and down, but can't fully rotate the camera.
                if (cameraState.Pitch > 90) cameraState.Pitch = 90;
                if (cameraState.Pitch < -90) cameraState.Pitch = -90;
            }
            lastAxis.x = Input.mousePosition.x;
            lastAxis.y = Input.mousePosition.y;
        }
    }
}