using UnityEngine;
namespace SEE.Game.UI.ConfigMenu
{
    [RequireComponent(typeof(HSVPicker.ColorPicker))]
    public class ColorPickerControl : DynamicUIBehaviour
    {
        private HSVPicker.ColorPicker _colorPickerHost;
        private ColorPicker _registeredPicker;

        private void Start()
        {
            _colorPickerHost = GetComponent<HSVPicker.ColorPicker>();
        }

        public void RequestControl(ColorPicker colorPicker)
        {
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
        public void AskForColorUpdate(ColorPicker supplicant, Color newColor)
        {
            if (_registeredPicker == supplicant)
            {
                _colorPickerHost.AssignColor(newColor);
            }
        }

        public void Reset()
        {
                _colorPickerHost.onValueChanged.RemoveAllListeners();
                _colorPickerHost.AssignColor(Color.white);
                gameObject.SetActive(false);
                _registeredPicker = null;
        }
    }
}
