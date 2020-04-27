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

        /// <summary>
        /// Moves the game object this action is attached to based on input of the direction
        /// and throttle device. The speed of the movement depends on the throttle and the
        /// distance of the object to the ground level.
        /// </summary>
        public void Update()
        {
            // The factor of speed depending on the height of the moving object.
            float heightFactor = Mathf.Pow(gameObject.transform.position.y, 2) * 0.01f + 1;
            if (heightFactor > 5)
            {
                heightFactor = 5;
            }
            float speed = ThrottleDevice.Value * heightFactor * BoostDevice.Value;
            Vector3 direction = KeepDirectionInPlane ? Vector3.ProjectOnPlane(DirectionDevice.Value, Vector3.up)
                                                     : DirectionDevice.Value;
            gameObject.transform.Translate(direction * speed * Time.deltaTime);
        }
    }
}