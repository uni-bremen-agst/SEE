using SEE.GO;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Moves a player in a desktop environment (based on keyboard and mouse input).
    /// </summary>
    public class DesktopPlayerMovement : PlayerMovement
    {
        [Tooltip("Speed of movements")]
        public float Speed = 2f;
        [Tooltip("Boost factor of speed, applied when shift is pressed.")]
        public float BoostFactor = 2f;

        private struct CameraState
        {
            internal float Distance;
            internal float Yaw;
            internal float Pitch;
            internal bool FreeMode;
        }

        private CameraState cameraState;

        /// <summary>
        /// Unity component that moves the player constrained by collisions.
        /// It moves the player with its own <see cref="CharacterController.Move(Vector3)"/> method.
        /// </summary>
        private CharacterController controller;

        [Tooltip("The code city which the player is focusing on.")]
        public GO.Plane FocusedObject;

        private Vector3 lastPosition; // For tracking the player's previous position

        private void Start()
        {
            controller = gameObject.MustGetComponent<CharacterController>();

            // The default layer should be ignored by the collider.
            // LayerMasks are bit masks, so we need that 1 << shifting to get the right layer.
            controller.excludeLayers = 1 << LayerMask.NameToLayer("Default");

            // Defines the built-in collider of the CharacterController, by default the collider is a capsule.
            // We chose the following values to minimize the collider to roughly fit around the player's head as a sphere.
            controller.center = new Vector3(0.0f, 1.55f, 0.21f);
            controller.radius = HeadRadius();
            controller.height = 0.0f;

            lastPosition = transform.position; // Initialize last position

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
            // Handle movement logic and check for key release
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
                transform.SetPositionAndRotation(FocusedObject.CenterTop,
                    Quaternion.Euler(cameraState.Pitch, cameraState.Yaw, 0.0f));
                transform.position -= transform.forward * cameraState.Distance;
            }
            else // cameraState.freeMode == true
            {
                Vector3 velocity = Vector3.zero;
                if (SEEInput.MoveForward())
                {
                    velocity += transform.forward;
                    moved = true;
                }

                if (SEEInput.MoveBackward())
                {
                    velocity -= transform.forward;
                    moved = true;
                }

                if (SEEInput.MoveRight())
                {
                    velocity += transform.right;
                    moved = true;
                }

                if (SEEInput.MoveLeft())
                {
                    velocity -= transform.right;
                    moved = true;
                }

                if (SEEInput.MoveUp())
                {
                    velocity += Vector3.up;
                    moved = true;
                }

                if (SEEInput.MoveDown())
                {
                    velocity += Vector3.down;
                    moved = true;
                }

                velocity.Normalize();
                velocity *= speed;
                // The following two lines may look strange, yet both are actually needed.
                controller.Move(velocity); // this is the actual movement
                controller.Move(Vector3.zero); // this prevents the player from sliding without input

                HandleRotation();
                // Players Yaw
                transform.rotation = Quaternion.Euler(0.0f, cameraState.Yaw, 0.0f);
                // Cameras Pitch and Yaw
                Camera.main.transform.rotation = Quaternion.Euler(cameraState.Pitch, cameraState.Yaw, 0.0f);

                // Check for key release to trigger movement tracking
                if (moved && !AnyMovementInput())
                {
                    TracingHelper.TrackMovement(lastPosition, transform.position);
                    lastPosition = transform.position; // Update last position
                    moved = false;
                }
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