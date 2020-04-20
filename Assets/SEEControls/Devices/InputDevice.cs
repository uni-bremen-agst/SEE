using UnityEngine;

namespace SEE.Controls
{
    public abstract class InputDevice : MonoBehaviour
    {
        [Tooltip("Name of the device")]
        public string Name;

        /// <summary>
        /// Name of the action set defined by VR Steam Input.
        /// </summary>
        protected const string defaultActionSet = "default";

        /// <summary>
        /// Name of the throttle action defined by VR Steam Input.
        /// </summary>
        protected const string ThrottleActionName = "Throttle";

        /// <summary>
        /// Name of the mouse X axis as defined in the Unity Input Manager.
        /// </summary>
        protected const string MouseXActionName = "mouse x"; // "Mouse X"; // "mouse x"
        /// <summary>
        /// Name of the mouse Y axis as defined in the Unity Input Manager.
        /// </summary>
        protected const string MouseYActionName = "mouse y"; // "Mouse Y"; // "mouse y"
    }
}