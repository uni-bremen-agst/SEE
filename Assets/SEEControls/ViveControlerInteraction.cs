using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Events;

namespace SEE.Controls
{
    /// <summary>
    /// This event is called when a button is pressed.
    /// </summary>
    [System.Serializable]
    public class ButtonEvent : UnityEvent
    {

    }

    /// <summary>
    /// This event is called when an input produces an axis value.
    /// </summary>
    [System.Serializable]
    public class AxisEvent :UnityEvent<float>
    {

    }

    /// <summary>
    /// This event is used when the funktion needs the axis value and the direction.
    /// </summary>
    [System.Serializable]
    public class VectorEvent : UnityEvent<Vector3,float>
    {

    }

    /// <summary>
    /// This class maps functions from other scripts in the editor to the controller events.
    /// It is the central point for processing controller input.
    /// No other script within the level should use controller inputs.
    /// </summary>
    public class ViveControlerInteraction : MonoBehaviour
    {
        [Tooltip("Sensitivity of the left trigger. Only when the trigger is pressed more than this threshold, it will fire.")]
        public float LeftTriggerDepth = 0.5f;
        [Tooltip("Sensitivity of the right trigger. Only when the trigger is pressed more than this threshold, it will fire.")]
        public float RightTriggerDepth = 0.5f;

        [Tooltip("The game object to be used to visualize the left controller.")]
        public GameObject LeftController;
        [Tooltip("The game object to be used to visualize the right controller.")]
        public GameObject RightController;

        [SerializeField]
        [FormerlySerializedAs("OnLeftTrigger")]
        public ButtonEvent _OnLeftTrigger = null;

        [SerializeField]
        [FormerlySerializedAs("OnLeftTriggerAxis")]
        public AxisEvent _OnLeftTriggerAxis = null;

        [SerializeField]
        [FormerlySerializedAs("OnLeftTriggerVector")]
        public VectorEvent _OnLeftTriggerVector = null;

        [SerializeField]
        [FormerlySerializedAs("OnRightTrigger")]
        public ButtonEvent _OnRightTrigger = null;

        [SerializeField]
        [FormerlySerializedAs("OnRightTriggerAxis")]
        public AxisEvent _OnRightTriggerAxis = null;

        [SerializeField]
        [FormerlySerializedAs("OnRightTriggerVector")]
        public VectorEvent _OnRightTriggerVector = null;

        public void Start()
        {
            if (ReferenceEquals(LeftController, null))
            {
                Debug.LogWarning("No game object has been assigned for left controller.\n");
            }
            if (ReferenceEquals(RightController, null))
            {
                Debug.LogWarning("No game object has been assigned for right controller.\n");
            }
        }
        void Update()
        {
            //ShowInput();
            float LeftTriggerAxis = Input.GetAxis("LeftVRTrigger");
            float RightTriggerAxis = Input.GetAxis("RightVRTriggerMovement");

            Debug.LogFormat("LeftTriggerAxis={0} RightTriggerAxis={1}\n", LeftTriggerAxis, RightTriggerAxis);

            if (_OnLeftTrigger != null)
            {
                if (LeftTriggerAxis > LeftTriggerDepth)
                    _OnLeftTrigger.Invoke();
            }
            else
            {
                Debug.LogWarning("_OnLeftTrigger is null.\n");
            }

            if (_OnLeftTriggerAxis != null)
            {
                _OnLeftTriggerAxis.Invoke(LeftTriggerAxis);
            }
            else
            {
                Debug.LogWarning("_OnLeftTriggerAxis is null.\n");
            }

            if (_OnLeftTriggerVector != null)
            {
                _OnLeftTriggerVector.Invoke(LeftController.transform.forward, LeftTriggerAxis);
            }
            else
            {
                Debug.LogWarning("_OnLeftTriggerVector is null.\n");
            }

            if (_OnRightTrigger != null)
            {
                if (RightTriggerAxis > RightTriggerDepth)
                    _OnRightTrigger.Invoke();
            }
            else
            {
                Debug.LogWarning("_OnRightTrigger is null.\n");
            }

            if (_OnRightTriggerAxis != null)
            {
                _OnRightTriggerAxis.Invoke(RightTriggerAxis);
            }
            else
            {
                Debug.LogWarning("_OnRightTriggerAxis is null.\n");
            }

            if (_OnRightTriggerVector != null)
            {
                _OnRightTriggerVector.Invoke(RightController.transform.forward, RightTriggerAxis);
            }
            else
            {
                Debug.LogWarning("_OnRightTriggerVector is null.\n");
            }
        }

        private void ShowInput()
        {
            ShowInput("Mouse X");
            ShowInput("Mouse Y");
            ShowInput("Mouse ScrollWheel");
            ShowInput("Horizontal");
            ShowInput("Vertical");
            ShowInput("Fire1");
            ShowInput("Fire2");
            ShowInput("Fire3");
            ShowInput("Jump");
            ShowInput("Submit");
            ShowInput("Cancel");
            ShowInput("RightVRTriggerMovement");
            ShowInput("RightVRGripButton");
            ShowInput("LeftVRTrigger");
            ShowInput("LeftVRGripButton");
            ShowInput("LeftVRTouchButton");
        }

        private void ShowInput(string inputName)
        {
            Debug.LogFormat("input {0} = {1}\n", inputName, Input.GetAxis(inputName));
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
        /// Returns the GameObject which is assigned in the editor and represents the right controller within the game.
        /// </summary>
        /// <returns>the GameObject of the rigth controller</returns>
        public GameObject GetRightController()
        {
            return RightController;
        }
    }
}
