using SEE.Controls.KeyActions;
using SEE.Extensions;
using SEE.Tools.OpenTelemetry;
using UnityEngine;
using Plane = SEE.Cities.Plane;

namespace SEE.Controls.Players
{
    /// <summary>
    /// Moves a player in a desktop environment (based on keyboard and mouse input).
    /// </summary>
    /// <remarks>This component is expected to be attached to the game object representing
    /// the local player.</remarks>
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
        public Plane FocusedObject;

        /// <summary>
        /// Sets up the <see cref="CharacterController"/> and the <see cref="cameraState"/>.
        /// </summary>
        private void Start()
        {
            // The player is spawned standing on the floor.
            floorY = transform.position.y;

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
        /// Indicates whether any movement has occurred.
        /// </summary>
        private bool moved;
        /// <summary>
        /// True if a movement key was just released in the last frame.
        /// </summary>
        private bool keyReleased = false;
        /// <summary>
        /// The time when the current movement started in seconds.
        /// </summary>
        private float movementStartTime;
        /// <summary>
        /// Tracks the position at the start of a movement.
        /// </summary>
        private Vector3 movementStartPosition;

        /// <summary>
        /// Initializes movement tracking if it hasn't started yet.
        /// </summary>
        private void StartMovementIfNeeded()
        {
            if (!moved)
            {
                movementStartTime = Time.time;
                movementStartPosition = transform.position;
                moved = true;
                keyReleased = false;
            }
        }

        /// <summary>
        /// Reacts to the user input.
        /// </summary>
        private void Update()
        {
            if (AnyMovementInput() || SEEInput.RotateCamera())
            {
                LastUserInputTime = Time.realtimeSinceStartup;
            }

            if (flight != null)
            {
                if (AnyMovementInput() || SEEInput.RotateCamera() || SEEInput.ToggleCameraLock())
                {
                    // The user takes over; stay where the flight got to.
                    EndFlight();
                }
                else
                {
                    AdvanceFlight();
                    return;
                }
            }

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
                transform.SetPositionAndRotation(FocusedObject.CenterTop,
                    Quaternion.Euler(cameraState.Pitch, cameraState.Yaw, 0.0f));
                transform.position -= transform.forward * cameraState.DistanceToFocusedObject;
            }
            else // cameraState.freeMode == true
            {
                // The directed distance the player should be moved in this frame.
                Vector3 step = Vector3.zero;

                // Determine the direction of the movement.
                if (SEEInput.MoveForward())
                {
                    StartMovementIfNeeded();
                    step += transform.forward;
                }

                if (SEEInput.MoveBackward())
                {
                    StartMovementIfNeeded();
                    step -= transform.forward;
                }

                if (SEEInput.MoveRight())
                {
                    StartMovementIfNeeded();
                    step += transform.right;
                }

                if (SEEInput.MoveLeft())
                {
                    StartMovementIfNeeded();
                    step -= transform.right;
                }

                if (SEEInput.MoveUp())
                {
                    StartMovementIfNeeded();
                    step += Vector3.up;
                }

                if (SEEInput.MoveDown())
                {
                    StartMovementIfNeeded();
                    step += Vector3.down;
                }

                step.Normalize();
                if (step == Vector3.zero)
                {
                    movingTime = 0f;

                    if (moved && !keyReleased && !AnyMovementInput())
                    {
                        float movementDuration = Time.time - movementStartTime;
                        TracingHelperService.Instance?.TrackDesktopMovement(movementStartPosition, transform.position,
                            movementDuration);
                        movementStartPosition = transform.position;
                        moved = false;
                        keyReleased = true;
                    }
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
                float distance = Mathf.Lerp(minimalInitialSpeedFactor * Speed, Speed,
                    Mathf.Min(movingTime / timeToReachSpeed, 1f)) * Time.deltaTime;
                if (SEEInput.BoostCameraSpeed())
                {
                    distance *= BoostFactor;
                }

                return distance;
            }
        }

        /// <summary>
        /// Checks whether any movement input key is currently being held down by the player.
        /// This includes inputs for moving forward, backward, right, left, up, or down.
        ///
        /// It returns a boolean value indicating if any of the movement inputs are active.
        /// If any of the directional keys or controls are being pressed, it will return true,
        /// otherwise it will return false.
        /// </summary>
        /// <returns>
        /// True if any movement input (forward, backward, right, left, up, or down) is held down,
        /// false otherwise.
        /// </returns>
        private bool AnyMovementInput()
        {
            return SEEInput.MoveForward() || SEEInput.MoveBackward()
                   || SEEInput.MoveRight() || SEEInput.MoveLeft()
                   || SEEInput.MoveUp() || SEEInput.MoveDown();
        }

        /// <summary>
        /// The mouse position of the last frame.
        /// </summary>
        private Vector2 lastAxis;
        /// <summary>
        /// True if the user was rotating the camera in the last frame.
        /// </summary>
        private bool wasRotating = false;
        /// <summary>
        /// Represents the initial yaw angle of the rotation, measured in degrees.
        /// </summary>
        /// <remarks>This field is used to store the starting yaw angle for
        /// a rotation operation. The value is typically set when the rotation
        /// begins and may be used to calculate  relative changes in yaw during
        /// the operation.</remarks>
        private float rotationStartYaw;

        /// <summary>
        /// If the user wants us, we rotate the gameobject according to mouse input.
        /// Modifies <see cref="cameraState.yaw"/> and <see cref="cameraState.pitch"/>.
        ///
        /// Note: This is a workaround of issues with the correct mouse position
        /// in a remote-desktop session.
        /// </summary>
        private void HandleRotation()
        {
            bool isRotating = SEEInput.RotateCamera();
            if (isRotating)
            {
                if (!wasRotating)
                {
                    rotationStartYaw = cameraState.Yaw;
                    wasRotating = true;
                }

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
            else if (wasRotating)
            {
                // Rotation has just ended
                wasRotating = false;
                TracingHelperService.Instance?.TrackRotation(rotationStartYaw, cameraState.Yaw);
            }

            lastAxis.x = Input.mousePosition.x;
            lastAxis.y = Input.mousePosition.y;
        }

        #region Automatic camera flight

        /// <summary>
        /// The state of a running camera flight started by <see cref="FlyToLookAt"/>.
        /// </summary>
        private class Flight
        {
            /// <summary>
            /// World position of the player (in Unity units) when the flight started.
            /// </summary>
            internal Vector3 StartPosition;

            /// <summary>
            /// World position of the player (in Unity units) at the end of the flight.
            /// </summary>
            internal Vector3 EndPosition;

            /// <summary>
            /// Yaw (rotation around the vertical axis) of the camera when the flight started,
            /// in degrees.
            /// </summary>
            internal float StartYaw;

            /// <summary>
            /// Yaw (rotation around the vertical axis) of the camera at the end of the flight,
            /// in degrees.
            /// </summary>
            internal float EndYaw;

            /// <summary>
            /// Pitch (looking up or down) of the camera when the flight started, in degrees
            /// within [-90, 90]; positive values look down.
            /// </summary>
            internal float StartPitch;

            /// <summary>
            /// Pitch (looking up or down) of the camera at the end of the flight, in degrees
            /// within [-90, 90]; positive values look down.
            /// </summary>
            internal float EndPitch;

            /// <summary>
            /// Total duration of the flight in seconds; 0 means the camera jumps immediately.
            /// </summary>
            internal float Duration;

            /// <summary>
            /// Time that has passed since the flight started, in seconds.
            /// </summary>
            internal float Elapsed;
        }

        /// <summary>
        /// The running camera flight, or null if there is none.
        /// </summary>
        private Flight flight;

        /// <summary>
        /// Height of the floor: the player's Y position when it was spawned.
        /// A camera flight never puts the player's feet below it.
        /// </summary>
        private float floorY = float.NegativeInfinity;

        /// <summary>
        /// The local player's movement component, or null if there is no desktop player.
        /// Only the local player gets this component (see <c>AvatarAdapter</c>).
        /// </summary>
        internal static DesktopPlayerMovement Local { get; private set; }

        /// <summary>
        /// When the user last moved or turned the camera (realtime seconds; -1 if never).
        /// Automatic camera tours stop once the user takes over.
        /// </summary>
        internal float LastUserInputTime { get; private set; } = -1f;

        /// <summary>
        /// Registers this component as <see cref="Local"/>.
        /// </summary>
        private void OnEnable()
        {
            Local = this;
        }

        /// <summary>
        /// Ends a running camera flight and unregisters this component as <see cref="Local"/>.
        /// </summary>
        private void OnDisable()
        {
            if (flight != null)
            {
                EndFlight();
            }
            if (Local == this)
            {
                Local = null;
            }
        }

        /// <summary>
        /// Smoothly flies the camera so that it looks down at <paramref name="target"/>
        /// from <paramref name="distance"/> under <paramref name="pitch"/> degrees.
        /// The horizontal viewing direction is the direction from the camera to the target,
        /// so the camera turns towards the target instead of circling around it.
        /// The player's feet never go below the floor (<see cref="floorY"/>); if the camera
        /// would have to be lower than eye height, it looks down more steeply instead.
        ///
        /// The camera is switched to free mode. Any movement, rotation or camera-lock input
        /// by the user ends the flight where it is. A new call replaces a running flight.
        /// </summary>
        /// <param name="target">World position to look at.</param>
        /// <param name="distance">Distance between camera and target at the end, in world units.</param>
        /// <param name="pitch">Downward viewing angle at the end, in degrees.</param>
        /// <param name="duration">Duration of the flight in seconds; 0 jumps immediately.</param>
        /// <returns>The world position the camera will have at the end of the flight.</returns>
        internal Vector3 FlyToLookAt(Vector3 target, float distance, float pitch, float duration)
        {
            Transform cameraTransform = Camera.main != null ? Camera.main.transform : transform;

            // The camera is a child of the player. The player itself only rotates around
            // the y axis (see Update), so the camera's offset is fixed in the player's yaw frame.
            Vector3 offsetInYawFrame = Quaternion.Inverse(transform.rotation)
                                     * (cameraTransform.position - transform.position);

            Vector3 horizontal = target - cameraTransform.position;
            horizontal.y = 0;
            float endYaw = horizontal.sqrMagnitude > 1e-8f
                ? Quaternion.LookRotation(horizontal).eulerAngles.y
                : cameraState.Yaw;
            float endPitch = Mathf.Clamp(pitch, -90, 90);

            Quaternion endRotation = Quaternion.Euler(endPitch, endYaw, 0.0f);
            Vector3 endCameraPosition = target - endRotation * Vector3.forward * distance;
            Vector3 endPlayerPosition = endCameraPosition - Quaternion.Euler(0.0f, endYaw, 0.0f) * offsetInYawFrame;

            // The camera sits at the eyes of a standing avatar. If the wanted camera position
            // is lower than eye height, the avatar would sink into the floor. Instead it stays
            // on the floor and looks down at the target more steeply, keeping the target centred.
            if (endPlayerPosition.y < floorY)
            {
                endPlayerPosition.y = floorY;
                endCameraPosition = endPlayerPosition + Quaternion.Euler(0.0f, endYaw, 0.0f) * offsetInYawFrame;
                Vector3 toTarget = target - endCameraPosition;
                float horizontalDistance = new Vector2(toTarget.x, toTarget.z).magnitude;
                endPitch = Mathf.Clamp(Mathf.Atan2(-toTarget.y, horizontalDistance) * Mathf.Rad2Deg, -90, 90);
            }

            // A flight ends in free mode: locked mode only orbits around FocusedObject.
            cameraState.FreeMode = true;

            flight = new()
            {
                StartPosition = transform.position,
                EndPosition = endPlayerPosition,
                StartYaw = cameraState.Yaw,
                EndYaw = endYaw,
                StartPitch = cameraState.Pitch,
                EndPitch = endPitch,
                Duration = Mathf.Max(0f, duration),
                Elapsed = 0f
            };

            // The CharacterController would block the flight at buildings and may
            // override positions set directly, so it is off while flying.
            if (controller != null)
            {
                controller.enabled = false;
            }
            AdvanceFlight();
            return endCameraPosition;
        }

        /// <summary>
        /// Moves the camera one frame further along <see cref="flight"/>.
        /// </summary>
        private void AdvanceFlight()
        {
            flight.Elapsed += Time.deltaTime;
            float t = flight.Duration > 0 ? Mathf.Clamp01(flight.Elapsed / flight.Duration) : 1f;
            float eased = Mathf.SmoothStep(0f, 1f, t);

            cameraState.Yaw = Mathf.LerpAngle(flight.StartYaw, flight.EndYaw, eased);
            cameraState.Pitch = Mathf.Lerp(flight.StartPitch, flight.EndPitch, eased);
            transform.position = Vector3.Lerp(flight.StartPosition, flight.EndPosition, eased);
            transform.rotation = Quaternion.Euler(0.0f, cameraState.Yaw, 0.0f);
            if (Camera.main != null)
            {
                Camera.main.transform.rotation = Quaternion.Euler(cameraState.Pitch, cameraState.Yaw, 0.0f);
            }

            // Keep the mouse position current, so rotating right after the flight does not jump.
            lastAxis = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

            if (t >= 1f)
            {
                EndFlight();
            }
        }

        /// <summary>
        /// Ends <see cref="flight"/> at the current position and gives control back to the user.
        /// </summary>
        private void EndFlight()
        {
            flight = null;
            if (controller != null)
            {
                controller.enabled = true;
            }
        }

        #endregion
    }
}
