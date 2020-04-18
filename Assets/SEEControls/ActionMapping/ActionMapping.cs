using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Parent class for mappings from user input of various different
    /// types of input devices onto actions. Actions are the functions
    /// that are to be executed for a given input.
    /// 
    /// This class inherits from ScriptableObject to make it 
    /// a serializable data container.
    /// </summary>
    public abstract class ActionMapping : ScriptableObject
    {
        /// <summary>
        /// The name of the mapping as assigned by the user in the inspector.
        /// </summary>
        public string MappingName = "";

        // Knuckles:
        //   Left joystick, left touchpad => Horizontal, Vertical
        //   Left B        => Fire3
        //   Left A        => LeftVRGripButton
        //   Left trigger  => LeftVRTrigger
        //   Left grip     => LeftVRGripButton

        //   Right B       => Fire1, Submit
        //   Right A       => RightVRGripButton
        //   Right trigger => RightVRTriggerMovement
        //   Right grip    => RightVRGripButton
        //
        //                 => LeftVRTouchButton

        // See also: https://docs.unity3d.com/2017.3/Documentation/Manual/OpenVRControllers.html

        // The names are defined in ProjectSettings/InputManager.asset
        private static string[] axes = new string[] {
                "Horizontal",
                "Vertical",
                "Fire1",
                "Fire2",
                "Fire3",
                "Jump",
                "Mouse X",
                "Mouse Y",
                "Mouse ScrollWheel",
                "Submit",
                "Cancel",
                "RightVRTriggerMovement",
                "RightVRGripButton",
                "LeftVRTrigger",
                "LeftVRGripButton",
                "LeftVRTouchButton",
                "NewInput",
            };

        private float[] axisValues = new float[axes.Length];

        /// <summary>
        /// Processes every possible input of a device and invokes all 
        /// registered callbacks for this kind of input. 
        /// </summary>
        public virtual void CheckInput()
        {
            for (int i = 0; i < axes.Length; i++)
            {
                PrintAxis(i);
            }
        }

        private void PrintAxis(int index)
        {
            string axis = axes[index];
            float previousValue = axisValues[index];
            float newValue = UnityEngine.Input.GetAxis(axis);
            if (previousValue != newValue)
            {
                Debug.LogFormat("Input axis {0}={1}\n", axis, newValue);
                axisValues[index] = newValue;
            }
        }

        public abstract string GetTypeAsString();

        /// <summary>
        /// The name of the mapping as assigned by the user.
        /// </summary>
        public string Name
        {
            get => MappingName;
            set => MappingName = value;
        }
    }
}
