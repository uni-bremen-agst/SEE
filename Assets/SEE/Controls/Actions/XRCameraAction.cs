using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// A camera movement action for virtual reality.
    /// </summary>
    public class XRCameraAction : CameraAction
    {
        [Tooltip("If true, movements stay in the x/z plane. You cannot go up or down.")]
        public bool KeepDirectionInPlane = true;

        private CharacterController characterController;

        public void Start()
        {
            characterController = GetComponent<CharacterController>();
            ThrottleDevice = GetComponent<Devices.Throttle>();
            DirectionDevice = GetComponent<Devices.Direction>();
            ViewpointDevice = GetComponent<Devices.Viewpoint>();
            BoostDevice = GetComponent<Devices.Boost>();
        }

        /// <summary>
        /// Gravity of the player. This is required to let a player jump down from platforms.
        /// </summary>
        private readonly Vector3 gravity = new Vector3(0, 0.981f, 0);

        /// <summary>
        /// Moves the game object this action is attached to based on input of the direction
        /// and throttle device. The speed of the movement depends on the throttle and the
        /// distance of the object to the ground level.
        /// </summary>
        public void Update()
        {
            // Check for the magnitude of the speed up so that the teleport locomotion
            // system does not interfere with the locomotion based on the VR controller
            // direction and throttle.
            if (ThrottleDevice.Value > 0.1f)
            {
                // The factor of speed depending on the height of the moving object.
                float heightFactor = Mathf.Pow(gameObject.transform.position.y, 2) * 0.01f + 1;
                if (heightFactor > 5)
                {
                    heightFactor = 5;
                }
                float speed = ThrottleDevice.Value * heightFactor * BoostDevice.Value;
                Vector3 direction = KeepDirectionInPlane ? Vector3.ProjectOnPlane(DirectionDevice.Value, Vector3.up) - gravity
                                                         : DirectionDevice.Value;
                characterController.Move(direction * speed * Time.deltaTime);
            }
        }
    }
}