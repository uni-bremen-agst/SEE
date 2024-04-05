using HSVPicker;
using SEE.Game.Drawable;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.UI.Drawable
{
    /// <summary>
    /// This class holds the presets and a image button to create new presets.
    /// 
    /// The file comes from the HSVPicker package.
    /// https://github.com/judah4/HSV-Color-Picker-Unity/blob/master/Packages/com.judahperez.hsvcolorpicker/UI/ColorPresets.cs
    /// It has been extended to include the functionality of removing and modifying colors, 
    /// as well as loading and saving colors from the previous session.
    /// </summary>
    public class ColorPresets : MonoBehaviour
    {
        /// <summary>
        /// The picker will be needed for update the selected color to the HSVPicker.
        /// </summary>
        public ColorPicker picker;

        /// <summary>
        /// This array holds the GameObjects for the up to eleven presets.
        /// </summary>
        public GameObject[] presets;

        /// <summary>
        /// Is used as a button, to create new color presets.
        /// </summary>
        public Image createPresetImage;

        /// <summary>
        /// Is the used ColorPresetList.
        /// </summary>
        private ColorPresetList _colors;

        /// <summary>
        /// Adds a new onValueChangs Listener to the picker.
        /// </summary>
        void Awake()
        {
            picker.onValueChanged.AddListener(ColorChanged);
        }

        /// <summary>
        /// Loads the colors from the file, if the file exists. 
        /// Otherwise it loads the default colors.
        /// It adds the OnColorsUpdate method to the ColorPresetList action onColorsUpdated.
        /// Then this method will be called with the given colors.
        /// </summary>
        void Start()
        {
            if (ColorPresetsConfigManager.IsFileExists())
            {
                _colors = ColorPresetManager.Get(ColorPresetsConfigManager.LoadColors().Colors);
            }
            else
            {
                _colors = ColorPresetManager.Get(picker.Setup.DefaultPresetColors, "default");
            }

            _colors.OnColorsUpdated += OnColorsUpdate;
            OnColorsUpdate(_colors.Colors);
        }

        /// <summary>
        /// Assigns each color element from the <paramref name="colors"/> to a preset box. 
        /// The respective preset box is made active and is assigned the respective color.
        /// Once the eleventh preset box has been assigned, the Create button will be hidden.
        /// In addition, each time this method is called, the list of color presets is saved to a file.
        /// </summary>
        /// <param name="colors">are the passed list of colors.</param>
        private void OnColorsUpdate(List<Color> colors)
        {
            for (int cnt = 0; cnt < presets.Length; cnt++)
            {
                /// Disables the unassigned presets.
                if (colors.Count <= cnt)
                {
                    presets[cnt].SetActive(false);
                    continue;
                }
                /// Enables the assigned preset and assigns colors to them.
                presets[cnt].SetActive(true);
                presets[cnt].GetComponent<Image>().color = colors[cnt];

            }
            /// Enables the button for adding a color, or disables it if all eleven preset slots are filled.
            createPresetImage.gameObject.SetActive(colors.Count < presets.Length);

            /// Saves the presets.
            ColorPresetsConfigManager.SaveColors(colors.ToArray());
        }

        /// <summary>
        /// Add a new color to the list.
        /// Will be used from the create preset button in the line configuration menu.
        /// </summary>
        public void CreatePresetButton()
        {
            _colors.AddColor(picker.CurrentColor);
        }

        /// <summary>
        /// Assigns the chosen color to the color picker.
        /// Will be used from the preset boxes in the line configuration menu.
        /// </summary>
        public void PresetSelect(Image sender)
        {
            picker.CurrentColor = sender.color;
        }

        /// <summary>
        /// Assigns the color picker color to the create preset button.
        /// Will be used for the create preset button in the line configuration menu.
        /// </summary>
        private void ColorChanged(Color color)
        {
            createPresetImage.color = color;
        }

        /// <summary>
        /// Removes the chosen color from the list.
        /// </summary>
        public void RemoveColor(Image sender)
        {
            _colors.RemoveColor(sender.color);
        }

        /// <summary>
        /// Changes the chosen color from the list to the current color of the color picker.
        /// </summary>
        public void ChangeColor(int index)
        {
            _colors.ChangeColor(index, picker.CurrentColor);
        }
    }
}