using HSVPicker;
using SEE.Game.Drawable;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// This class holds the presets and an image button to create new presets.
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
        [FormerlySerializedAs("picker")]
        public ColorPicker Picker;

        /// <summary>
        /// This array holds the GameObjects for the up to eleven presets.
        /// </summary>
        [FormerlySerializedAs("presets")]
        public GameObject[] Presets;

        /// <summary>
        /// Is used as a button, to create new color presets.
        /// </summary>
        [FormerlySerializedAs("createPresetImage")]
        public Image CreatePresetImage;

        /// <summary>
        /// Is the used ColorPresetList.
        /// </summary>
        private ColorPresetList colors;

        /// <summary>
        /// Adds a new onValueChangs Listener to the picker.
        /// </summary>
        private void Awake()
        {
            Picker.onValueChanged.AddListener(ColorChanged);
        }

        /// <summary>
        /// Loads the colors from the file, if the file exists.
        /// Otherwise it loads the default colors.
        /// It adds the OnColorsUpdate method to the ColorPresetList action onColorsUpdated.
        /// Then this method will be called with the given colors.
        /// </summary>
        private void Start()
        {
            if (ColorPresetsConfigManager.IsFileExists())
            {
                colors = ColorPresetManager.Get(ColorPresetsConfigManager.LoadColors().Colors);
            }
            else
            {
                colors = ColorPresetManager.Get(Picker.Setup.DefaultPresetColors, "default");
            }

            colors.OnColorsUpdated += OnColorsUpdate;
            OnColorsUpdate(colors.Colors);
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
            for (int cnt = 0; cnt < Presets.Length; cnt++)
            {
                /// Disables the unassigned presets.
                if (colors.Count <= cnt)
                {
                    Presets[cnt].SetActive(false);
                    continue;
                }
                /// Enables the assigned preset and assigns colors to them.
                Presets[cnt].SetActive(true);
                Presets[cnt].GetComponent<Image>().color = colors[cnt];

            }
            /// Enables the button for adding a color, or disables it if all eleven preset slots are filled.
            CreatePresetImage.gameObject.SetActive(colors.Count < Presets.Length);

            /// Saves the presets.
            ColorPresetsConfigManager.SaveColors(colors.ToArray());
        }

        /// <summary>
        /// Add a new color to the list.
        /// Will be used from the create preset button in the line configuration menu.
        /// Warning: This method is used by a prefab (Picker 2.0) and therefore has 0 references!
        /// </summary>
        public void CreatePresetButton()
        {
            colors.AddColor(Picker.CurrentColor);
        }

        /// <summary>
        /// Assigns the chosen color to the color picker.
        /// Will be used from the preset boxes in the line configuration menu.
        /// Warning: This method is used by a prefab (Picker 2.0) and therefore has 0 references!
        /// </summary>
        public void PresetSelect(Image sender)
        {
            Picker.CurrentColor = sender.color;
        }

        /// <summary>
        /// Assigns the color picker color to the create preset button.
        /// Will be used for the create preset button in the line configuration menu.
        /// </summary>
        private void ColorChanged(Color color)
        {
            CreatePresetImage.color = color;
        }

        /// <summary>
        /// Removes the chosen color from the list.
        /// Warning: This method is used by a prefab (Picker 2.0) and therefore has 0 references!
        /// </summary>
        public void RemoveColor(Image sender)
        {
            colors.RemoveColor(sender.color);
        }

        /// <summary>
        /// Changes the chosen color from the list to the current color of the color picker.
        /// Warning: This method is used by a prefab (Picker 2.0) and therefore has 0 references!
        /// </summary>
        public void ChangeColor(int index)
        {
            colors.ChangeColor(index, Picker.CurrentColor);
        }
    }
}