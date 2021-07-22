// Copyright 2021 Ruben Smidt
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

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
        private HSVPicker.ColorPicker colorPickerHost;
        private ColorPicker registeredPicker;

        private void Awake()
        {
            colorPickerHost = GetComponent<HSVPicker.ColorPicker>();
        }

        /// <summary>
        /// Requests the control of the color picker object.
        /// </summary>
        /// <param name="colorPicker">The wrapper script that wants to take control.</param>
        public void RequestControl(ColorPicker colorPicker)
        {
            // If the control request comes from the currently controlling wrapper script
            // we assume that the caller wants to hide color picker object.
            if (registeredPicker == colorPicker)
            {
                Reset();
                return;
            }

            gameObject.SetActive(true);
            registeredPicker = colorPicker;
            colorPickerHost.onValueChanged.RemoveAllListeners();
            colorPickerHost.onValueChanged.AddListener(colorPicker.OnPickerHostColorChange);
            colorPickerHost.AssignColor(colorPicker.LatestSelectedColor);
        }

        /// <summary>
        /// Changes the currently displayed color of the color picker object only if the caller
        /// has control over the color picker object.
        /// </summary>
        /// <param name="supplicant"></param>
        /// <param name="newColor"></param>
        public void AskForColorUpdate(ColorPicker supplicant, Color newColor)
        {
            if (registeredPicker == supplicant)
            {
                colorPickerHost.AssignColor(newColor);
            }
        }

        /// <summary>
        /// Resets the color picker object and this control to its initial state state.
        /// </summary>
        public void Reset()
        {
            colorPickerHost.onValueChanged.RemoveAllListeners();
            colorPickerHost.AssignColor(Color.white);
            gameObject.SetActive(false);
            registeredPicker = null;
        }
    }
}
