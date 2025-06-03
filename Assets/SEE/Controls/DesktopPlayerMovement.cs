using SEE.GO;
using SEE.Tools.OpenTelemetry;
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

        private Vector3 lastPosition; // For tracking the player's previous position

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

            lastPosition = transform.position; // Initialize last position

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

            // Returns the radius of the player's head.
            float HeadRadius()
            {
                //const string headName = "Root/Global/Position/Hips/LowerBack/Spine/Spine1/Neck/Head/HeadAdjust";
                const string headName =
                    "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_NeckTwist01/CC_Base_NeckTwist02/CC_Base_Head/HeadAdjust";
                Transform head = transform.Find(headName);
                if (head == null)
                {
                    Debug.LogError($"Player {gameObject.name} does not have a child {headName}.\n");
                    return 1.0f;
                }
                // We want to fit the head completely into the CharacterController collider, so we use the maximum value
                // to calculate the radius
                return Mathf.Max(head.transform.localScale.x, head.transform.localScale.y, head.transform.localScale.z) / 2;
            }
        }

        bool moved; // Flag to track if there was any movement
        bool keyReleased = false; // Flag to track if a key was just released
        private float movementStartTime; // Tracks start time

        
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
                    if (!moved) movementStartTime = Time.time;
                    step += transform.forward;
                    moved = true;
                }

                if (SEEInput.MoveBackward())
                {
                    if (!moved) movementStartTime = Time.time;
                    step -= transform.forward;
                    moved = true;
                }

                if (SEEInput.MoveRight())
                {
                    if (!moved) movementStartTime = Time.time;
                    step += transform.right;
                    moved = true;
                }

                if (SEEInput.MoveLeft())
                {
                    if (!moved) movementStartTime = Time.time;
                    step -= transform.right;
                    moved = true;
                }

                if (SEEInput.MoveUp())
                {
                    if (!moved) movementStartTime = Time.time;
                    step += Vector3.up;
                    moved = true;
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
                    if (!moved) movementStartTime = Time.time;
                    step += Vector3.down;
                    moved = true;
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

                // Check for key release to trigger movement tracking
                if (moved && !AnyMovementInput())
                {
                    float movementDuration = Time.time - movementStartTime;
                    TracingHelperService.Instance?.TrackMovement(lastPosition, transform.position, movementDuration);
                    lastPosition = transform.position; // Update last position
                    moved = false;
                }
            }

            // The distance the player should be moved in this frame (in any direction) taking
            // into account the movingTime and potentially the boost factor.
            float GetDistance()
            {
                // Movement should be started at minimalInitialSpeedFactor * Speed and then
                // linearly increased to Speed.
                float distance = Mathf.Lerp(minimalInitialSpeedFactor * Speed, Speed, Mathf.Min(movingTime / timeToReachSpeed, 1f)) * Time.deltaTime;
                if (SEEInput.BoostCameraSpeed())
                {
                    distance *= BoostFactor;
                }
                return distance;
            }
        }

        /// <summary>
        /// Checks whether any movement input is currently being held down by the player.
        /// This includes inputs for moving forward, backward, right, left, up, or down.
        ///
        /// It returns a boolean value indicating if any of the movement inputs are active.
        /// If any of the directional keys or controls are being pressed, it will return true,
        /// otherwise it will return false.
        /// </summary>
        /// <returns>
        /// true if any movement input (forward, backward, right, left, up, or down) is held down,
        /// false otherwise.
        /// </returns>
        private bool AnyMovementInput()
        {
            bool anyInput = SEEInput.MoveForward() || SEEInput.MoveBackward() || SEEInput.MoveRight() ||
                            SEEInput.MoveLeft() ||
                            SEEInput.MoveUp() || SEEInput.MoveDown();
            return anyInput;
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