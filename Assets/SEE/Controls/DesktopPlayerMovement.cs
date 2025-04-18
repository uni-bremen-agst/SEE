using SEE.GO;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Moves a player in a desktop environment (based on keyboard and mouse input).
    /// </summary>
    public class DesktopPlayerMovement : PlayerMovement
    {
        /// <summary>
        /// The time in seconds it takes to reach the speed defined in <see cref="Speed"/>
        /// after the player has initiated a movement.
        /// </summary>
        private const float timeToReachSpeed = 0.5f;

        /// <summary>
        /// The minimal speed factor when the player has just started to move. The initial
        /// speed when the user initiated a movement will be
        /// <see cref="Speed"/> * <see cref="minimalInitialSpeedFactor"/>.
        /// </summary>
        private const float minimalInitialSpeedFactor = 0.1f;

        /// <summary>
        /// The speed of the player movement. This is the distance in Unity units per second.
        /// </summary>
        /// <remarks>The actual speed depends on the point in time where the player
        /// has initiated the movement. We will start slower and then gradually increase
        /// the speed until <see cref="Speed"/> has been reached. This phase takes
        /// <see cref="timeToReachSpeed"/> seconds.</remarks>
        [Tooltip("Speed of movements")]
        public float Speed = 2f;

        /// <summary>
        /// The time (in seconds) past since the player has initiated a movement.
        /// </summary>
        private float movingTime = 0f;

        /// <summary>
        /// The factor by which the speed is multiplied when the shift key is pressed.
        /// </summary>
        [Tooltip("Boost factor of speed, applied when shift is pressed.")]
        public float BoostFactor = 2f;

        /// <summary>
        /// The state of the camera.
        /// </summary>
        private struct CameraState
        {
            /// <summary>
            /// Rotation in Euler degrees around the y-axis (yaw).
            /// </summary>
            internal float Yaw;
            /// <summary>
            /// Rotation in Euler degrees around the x-axis (pitch).
            /// </summary>
            internal float Pitch;
            /// <summary>
            /// If true, the player moves freely in the world; otherwise, the player is
            /// rotating around the <see cref="FocusedObject"/> (the code city the player is conntected to).
            /// </summary>
            internal bool FreeMode;
            /// <summary>
            ///  The distance to the <see cref="FocusedObject"/> (the code city the player
            ///  is conntected to). This attribute is considered only if <see cref="FreeMode"/> is false.
            /// </summary>
            internal float DistanceToFocusedObject;
        }

        /// <summary>
        /// The current state of the camera.
        /// </summary>
        private CameraState cameraState;

        /// <summary>
        /// Unity component that moves the player constrained by collisions.
        /// It moves the player with its own <see cref="CharacterController.Move(Vector3)"/> method.
        /// </summary>
        private CharacterController controller;

        /// <summary>
        /// The code city which the player is focusing on if not in free mode.
        /// </summary>
        [Tooltip("The code city which the player is focusing on.")]
        public GO.Plane FocusedObject;

        /// <summary>
        /// Sets up the <see cref="CharacterController"/> and the <see cref="cameraState"/>.
        /// </summary>
        private void Start()
        {
            controller = gameObject.MustGetComponent<CharacterController>();

            // The default layer should be ignored by the collider.
            // LayerMasks are bit masks, so we need that 1 << shifting to get the right layer.
            controller.excludeLayers = 1 << LayerMask.NameToLayer("Default");

            // Defines the built-in collider of the CharacterController, by default the collider is a capsule.
            // We chose the following values to minimize the collider to roughly fit around the player's head as a sphere.
            controller.center = new Vector3(0.0f, 1.55f, 0.0f);
            controller.radius = 0.4f;
            controller.height = 0.0f;

            if (FocusedObject != null)
            {
                cameraState.DistanceToFocusedObject = 2.0f;
                cameraState.Yaw = 0.0f;
                cameraState.Pitch = 45.0f;
                transform.position = FocusedObject.CenterTop;
                transform.position -= transform.forward * cameraState.DistanceToFocusedObject;
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

        /// <summary>
        /// Reacts to the user input.
        /// </summary>
        private void Update()
        {
            if (FocusedObject != null && SEEInput.ToggleCameraLock())
            {
                cameraState.FreeMode = !cameraState.FreeMode;
                if (!cameraState.FreeMode)
                {
                    Vector3 positionToFocusedObject = FocusedObject.CenterTop - transform.position;
                    cameraState.DistanceToFocusedObject = positionToFocusedObject.magnitude;
                    transform.forward = positionToFocusedObject;
                    Vector3 pitchYawRoll = transform.rotation.eulerAngles;
                    cameraState.Pitch = pitchYawRoll.x;
                    cameraState.Yaw = pitchYawRoll.y;
                }
            }

            if (!cameraState.FreeMode)
            {
                float distance = GetDistance();
                float step = 0.0f;
                if (SEEInput.MoveForward())
                {
                    step += distance;
                }
                if (SEEInput.MoveBackward())
                {
                    step -= distance;
                }
                if (step == 0)
                {
                    // No movement has been initiated, so we reset the moving time.
                    movingTime = 0f;
                }
                else
                {
                    movingTime += Time.deltaTime;
                }
                cameraState.DistanceToFocusedObject -= step;

                HandleRotation();
                transform.SetPositionAndRotation(FocusedObject.CenterTop, Quaternion.Euler(cameraState.Pitch, cameraState.Yaw, 0.0f));
                transform.position -= transform.forward * cameraState.DistanceToFocusedObject;
            }
            else // cameraState.freeMode == true
            {
                // The directed distance the player should be moved in this frame.
                Vector3 step = Vector3.zero;
                // Determine the direction of the movement.
                if (SEEInput.MoveForward())
                {
                    step += transform.forward;
                }
                if (SEEInput.MoveBackward())
                {
                    step -= transform.forward;
                }
                if (SEEInput.MoveRight())
                {
                    step += transform.right;
                }
                if (SEEInput.MoveLeft())
                {
                    step -= transform.right;
                }
                if (SEEInput.MoveUp())
                {
                    step += Vector3.up;
                }
                if (SEEInput.MoveDown())
                {
                    step += Vector3.down;
                }
                step.Normalize();
                if (step == Vector3.zero)
                {
                    // No movement has been initiated, so we reset the moving time.
                    movingTime = 0f;
                }
                else
                {
                    movingTime += Time.deltaTime;
                }
                step *= GetDistance();
                // The following two lines may look strange, yet both are actually needed.
                controller.Move(step); // this is the actual movement
                controller.Move(Vector3.zero); // this prevents the player from sliding without input

                HandleRotation();
                // Players Yaw
                transform.rotation = Quaternion.Euler(0.0f, cameraState.Yaw, 0.0f);
                // Cameras Pitch and Yaw
                Camera.main.transform.rotation = Quaternion.Euler(cameraState.Pitch, cameraState.Yaw, 0.0f);
            }

            // The distance the player should be moved in this frame (in any direction) taking
            // into account the movingTime and potentially the boost factor.
            float GetDistance()
            {
                // Movement should be started at minimalInitialSpeedFactor * Speed and then
                // linearly increased to Speed.
                float distance = Mathf.Lerp(minimalInitialSpeedFactor * Speed, Speed, movingTime / timeToReachSpeed) * Time.deltaTime;
                if (SEEInput.BoostCameraSpeed())
                {
                    distance *= BoostFactor;
                }
                return distance;
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
                cameraState.Pitch = Mathf.Clamp(cameraState.Pitch, -90, 90);
            }
            lastAxis.x = Input.mousePosition.x;
            lastAxis.y = Input.mousePosition.y;
        }
    }
}