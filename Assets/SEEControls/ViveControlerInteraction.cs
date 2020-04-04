using UnityEngine;
using UnityEngine.Serialization;

namespace SEE.Controls
{
    /// <summary>
    /// This class maps functions from other scripts in the editor to the controller events.
    /// It is the central point for processing controller input.
    /// No other script within the level should use controller inputs.
    /// </summary>
    public class ViveControlerInteraction : MonoBehaviour
    {
        [SerializeField]
        [FormerlySerializedAs("OnLeftTrigger")]
        private ButtonEvent _OnLeftTrigger = null;

        public float LeftTriggerDepth = 0.5f;

        [SerializeField]
        [FormerlySerializedAs("OnLeftTriggerAxis")]
        private AxisEvent _OnLeftTriggerAxis = null;

        [SerializeField]
        [FormerlySerializedAs("OnRightTrigger")]
        private ButtonEvent _OnRightTrigger = null;

        public float RightTriggerDepth = 0.5f;

        [SerializeField]
        [FormerlySerializedAs("OnRightTriggerAxis")]
        private AxisEvent _OnRightTriggerAxis = null;

        public GameObject LeftController;
        public GameObject RightController;

        void Update()
        {
            float LeftTriggerAxis = Input.GetAxis("LeftVRTrigger");
            float RightTriggerAxis = Input.GetAxis("RightVRTriggerMovement");

            //Debug.LogFormat("LeftTriggerAxis={0} RightTriggerAxis={0}\n", LeftTriggerAxis, RightTriggerAxis);

            if (_OnLeftTrigger != null)
            {
                if (LeftTriggerAxis > LeftTriggerDepth)
                    _OnLeftTrigger.Invoke();
            }

            if (_OnLeftTriggerAxis != null)
            {
                _OnLeftTriggerAxis.Invoke(LeftTriggerAxis);
            }

            if (_OnRightTrigger != null)
            {
                if (RightTriggerAxis > RightTriggerDepth)
                    _OnRightTrigger.Invoke();
            }

            if (_OnRightTriggerAxis != null)
            {
                _OnRightTriggerAxis.Invoke(RightTriggerAxis);
            }
        }

        /// <summary>
        /// Returns the GameObject which is assigned in the editor and represents the left controller within the game.
        /// </summary>
        /// <returns>the GameObject of the left controller</returns>
        public GameObject GetLeftController()
        {
            return LeftController;
        }

        /// <summary>
        /// Returns the GameObject which is assigned in the editor and represents the rigth controller within the game.
        /// </summary>
        /// <returns>the GameObject of the rigth controller</returns>
        public GameObject GetRightController()
        {
            return RightController;
        }
    }
}
