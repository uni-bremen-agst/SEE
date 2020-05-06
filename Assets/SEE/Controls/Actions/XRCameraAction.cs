using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// A camera action for virtual reality.
    /// </summary>
    public class XRCameraAction : CameraAction
    {
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
            Vector3 translation = DirectionDevice.Value * ThrottleDevice.Value * heightFactor;
            gameObject.transform.Translate(translation);
        }
    }
}