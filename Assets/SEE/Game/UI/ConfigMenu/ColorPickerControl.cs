using UnityEngine;
namespace SEE.Game.UI.ConfigMenu
{
    /// <summary>
    /// The control script for the singleton color picker.
    /// This is necessary because I had to monkey patch the color picker from a third party library.
    /// </summary>
    [RequireComponent(typeof(HSVPicker.ColorPicker))]
    public class ColorPickerControl : DynamicUIBehaviour
    {
        private HSVPicker.ColorPicker _colorPickerHost;
        private ColorPicker _registeredPicker;

        private void Awake()
        {
            _colorPickerHost = GetComponent<HSVPicker.ColorPicker>();
        }

        /// <summary>
        /// Requests the control of the color picker object.
        /// </summary>
        /// <param name="colorPicker">The wrapper script that wants to take control.</param>
        public void RequestControl(ColorPicker colorPicker)
        {
            // If the control request comes from the currently controlling wrapper script
            // we assume that the caller wants to hide color picker object.
            if (_registeredPicker == colorPicker)
            {
                Reset();
                return;
            }

            gameObject.SetActive(true);
            _registeredPicker = colorPicker;
            _colorPickerHost.onValueChanged.RemoveAllListeners();
            _colorPickerHost.onValueChanged.AddListener(colorPicker.OnPickerHostColorChange);
            _colorPickerHost.AssignColor(colorPicker.LatestSelectedColor);
        }

        /// <summary>
        /// Changes the currently displayed color of the color picker object only if the caller
        /// has control over the color picker object.
        /// </summary>
        /// <param name="supplicant"></param>
        /// <param name="newColor"></param>
        public void AskForColorUpdate(ColorPicker supplicant, Color newColor)
        {
            if (_registeredPicker == supplicant)
            {
                _colorPickerHost.AssignColor(newColor);
            }
        }

        /// <summary>
        /// Resets the color picker object and this control to its initial state state.
        /// </summary>
        public void Reset()
        {
                _colorPickerHost.onValueChanged.RemoveAllListeners();
                _colorPickerHost.AssignColor(Color.white);
                gameObject.SetActive(false);
                _registeredPicker = null;
        }
    }
}
