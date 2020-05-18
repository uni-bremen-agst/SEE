using InControl;
using UnityEngine;

namespace SEE.Controls.Devices
{ 
    /// <summary>
    /// This class is intended for a selection through the center of the viewport.
    /// Selection devices should generally be suitable for 2D (x/y coordinate on
    /// the screen) or 3D (location in space by way of an XR controller) positional 
    /// selection. There are, however, gamepads wich allow us to move the camera
    /// or the viewpoint, but do not give use positions on the screen or in 3D
    /// space. In this case, we can select an objects that can be hit by a ray 
    /// through the center of the viewport.
    /// </summary>
    public class TouchGamepadSelection : Selection
    {
        [Tooltip("The threshold determining whether a selection trigger value has to be considered."), Range(0.001f, 1f)]
        public float Threshold = 0.3f;

        private readonly Vector3 viewPortCenter = new Vector3(0.5f, 0.5f, 0f);

        public override Vector3 Direction => viewPortCenter;

        public override float Pull => throw new System.NotImplementedException();

        public override Vector3 Position => viewPortCenter;

        // For the standardized layout of gamepads supported by InControl, see:
        // http://www.gallantgames.com/pages/incontrol-standardized-controls

        private bool isSelecting = false;

        /// <summary>
        /// Whether the Action2 button was pressed on the gamepad. This is typically the button labeled "A".
        /// The button works as a toggle.
        /// </summary>
        public override bool IsSelecting => isSelecting;

        /// <summary>
        /// Whether the right trigger has been pressed deeply enough.
        /// </summary>
        public override bool IsGrabbing => InputManager.ActiveDevice.RightTrigger.Value >= Threshold;

        /// <summary>
        /// Whether the Action1 button was pressed on the gamepad. This is typically the button labeled "B".
        /// The button works as a transient event.
        /// </summary>
        public override bool IsCanceling => InputManager.ActiveDevice.Action1.State;

        private void Update()
        {
            if (InputManager.ActiveDevice.Action2.WasPressed)
            {
                isSelecting = !isSelecting;
            }
        }
    }
}