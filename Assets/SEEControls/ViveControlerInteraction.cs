using SEE.Controls;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SEE.Controls
{
    /// <summary>
    /// This class maps functions from other scripts in the editor to the controler events.
    /// Its the central point for processing controler input.
    /// No other script within the level should use controler inputs.
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

        public GameObject LeftControler;
        public GameObject RightControler;

        void Start()
        {

        }

        void Update()
        {
            float LeftTriggerAxis = Input.GetAxis("LeftVRTrigger");
            float RightTriggerAxis = Input.GetAxis("RightVRTriggerMovement");

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
        /// Returns the GameObject which is assigned in the editor and represents the left controler within the level.
        /// </summary>
        /// <returns>the GameObject of the left controler</returns>
        public GameObject GetLeftControler()
        {
            return LeftControler;
        }

        /// <summary>
        /// Returns the GameObject which is assigned in the editor and represents the rigth controler within the level.
        /// </summary>
        /// <returns>the GameObject of the rigth controler</returns>
        public GameObject GetRightControler()
        {
            return RightControler;
        }
    }
}
