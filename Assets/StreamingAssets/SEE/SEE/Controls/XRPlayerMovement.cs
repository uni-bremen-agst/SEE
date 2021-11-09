using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    /// <summary>
    /// A camera movement action for virtual reality.
    /// </summary>
    public class XRPlayerMovement : MonoBehaviour
    {
        /// <summary>
        /// Gravity of the player. This is required to let a player jump down from platforms.
        /// </summary>
        private static readonly Vector3 gravity = new Vector3(0, 0.981f, 0);

        [Tooltip("The VR controller for directing")]
        public Hand DirectingHand;

        [Tooltip("If true, movements stay in the x/z plane. You cannot go up or down.")]
        public bool KeepDirectionInPlane = false;

        public CharacterController characterController;

        private readonly SteamVR_Action_Single throttleAction = SteamVR_Input.GetSingleAction(XRInput.DefaultActionSetName, XRInput.ThrottleActionName);

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
            if (throttleAction.axis > 0.1f)
            {
                // The factor of speed depending on the height of the moving object.
                float heightFactor = Mathf.Pow(gameObject.transform.position.y, 2) * 0.01f + 1.0f;
                if (heightFactor > 5.0f)
                {
                    heightFactor = 5.0f;
                }
                float speed = throttleAction.axis * heightFactor;

                Vector3 direction = SteamVR_Actions.default_Pose.GetLocalRotation(DirectingHand.handType) * Vector3.forward;
                if (KeepDirectionInPlane)
                {
                    direction = Vector3.ProjectOnPlane(direction, Vector3.up) - gravity; // TODO: gravity is currently handled as velocity, not as acceleration
                }
                characterController.Move(direction * speed * Time.deltaTime);
            }
        }
    }
}