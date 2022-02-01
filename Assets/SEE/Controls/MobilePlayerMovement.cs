using SEE.Utils;
using UnityEngine;
using SEE.UI;
using SEE.Game.UI;

namespace SEE.Controls
{
    /// <summary>
    /// Moves a player in a mobile environment (via virtual joysticks).
    /// </summary>
    public class MobilePlayerMovement :PlayerMovement
    {
        [Tooltip("Speed of movements")]
        public float Speed = 1f;

        /// <summary>
        /// Handles the camera movement
        /// </summary>
        private Joystick joystickRight;

        /// <summary>
        /// Handles the player movement
        /// </summary>
        private Joystick joystickLeft;

        /// <summary>
        /// Path to the left Joystick prefab
        /// </summary>
        private const string JOYSTICK_PREFAB_LEFT = "Prefabs/UI/FixedJoystickLeft";

        /// <summary>
        /// Path to the right Joystick prefab
        /// </summary>
        private const string JOYSTICK_PREFAB_RIGHT = "Prefabs/UI/FixedJoystickRight";

        /// <summary>
        /// The canvas on which UI elements are placed.
        /// This GameObject must be named <see cref="CanvasUtils.UI_CANVAS_NAME"/>.
        /// If it doesn't exist yet, it will be created from a prefab.
        /// </summary>
        protected GameObject Canvas;

        /// <summary>
        /// State of the main camera in the scene
        /// </summary>
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

        protected void Start()
        {
            Canvas = CanvasUtils.GetCanvas();
            Joystick joystickRightPrefab = Resources.Load<Joystick>(JOYSTICK_PREFAB_RIGHT);
            Joystick joystickLeftPrefab = Resources.Load<Joystick>(JOYSTICK_PREFAB_LEFT);
            joystickLeft = Instantiate(joystickLeftPrefab, Canvas.transform, false);
            joystickRight = Instantiate(joystickRightPrefab, Canvas.transform, false);
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
                // Use the initial camera rotation.
                Vector3 rotation = mainCamera.transform.rotation.eulerAngles;
                cameraState.yaw = rotation.y;
                cameraState.pitch = rotation.x;
                cameraState.freeMode = true;
            }
        }

        protected void Update()
        {
            Camera mainCamera = MainCamera.Camera;
            if (SEEInput.ToggleCameraLock())
            {
                // TODO
            }

            float distance = Speed * Time.deltaTime;

            if (!cameraState.freeMode)
            {
                // TODO
            }
            else // cameraState.freeMode == true
            {
                
                // Taking the joystick inputs
                float xMovementInput = joystickLeft.Horizontal;
                float yMovementInput = joystickLeft.Vertical;

                // calculating velocity vectors
                Vector3 movementHorizontal = transform.right * xMovementInput;
                Vector3 movementVertical = transform.forward * yMovementInput;

                // calculate final movement velocity vector
                Vector3 velocity = (movementHorizontal + movementVertical);
              
                velocity.Normalize();
                velocity *= distance; 
                transform.position += velocity;

                HandleRotation();
                transform.rotation = Quaternion.Euler(cameraState.pitch, cameraState.yaw, 0.0f);
            }
        }

        /// <summary>
        /// Handles the rotation, repeats at 0 after turning 360°
        /// </summary>
        private void HandleRotation()
        {
            // Taking the joystick inputs
            float xMovementInput = joystickRight.Horizontal;
            float yMovementInput = joystickRight.Vertical;

            // rotation of the camera - repeat sets the value back to 0 after hitting 360 degrees 
            cameraState.pitch = Mathf.Repeat(cameraState.pitch - yMovementInput, 360f);
            cameraState.yaw = Mathf.Repeat(cameraState.yaw + xMovementInput, 360f);
        }
    }
}