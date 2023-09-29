using Assets.SEE.Game.Drawable;
using HSVPicker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.SEE.Game.UI.Drawable
{
    // File from the hsvpicker packages
    // https://github.com/judah4/HSV-Color-Picker-Unity/blob/master/Packages/com.judahperez.hsvcolorpicker/UI/ColorPresets.cs
    // added methods for remove and change color. Also added load and save the color presets to a file.

    /// <summary>
    /// This class holds the presets and a image button to create new presets.
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
        /// The start method loads the colors from the file, if the file exists. Otherwise it loads the default colors.
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
        /// With this method a preset box is assigned for each element of the passed color list. The preset box is made active and is assigned the respective color.
        /// Once the eleventh preset box has been assigned, the Create button will be hidden.
        /// In addition, each time this method is called, the list of color presets is saved to a file.
        /// </summary>
        /// <param name="colors">are the passed list of colors.</param>
        private void OnColorsUpdate(List<Color> colors)
        {
            for (int cnt = 0; cnt < presets.Length; cnt++)
            {
                if (colors.Count <= cnt)
                {
                    presets[cnt].SetActive(false);
                    continue;
                }
                presets[cnt].SetActive(true);
                presets[cnt].GetComponent<Image>().color = colors[cnt];

            }

            createPresetImage.gameObject.SetActive(colors.Count < presets.Length);
            ColorPresetsConfigManager.SaveColors(colors.ToArray());
        }

        /// <summary>
        /// This method will be used from the create preset button in the line configuration menu.
        /// It's add a new color to the list.
        /// </summary>
        public void CreatePresetButton()
        {
            _colors.AddColor(picker.CurrentColor);
        }

        /// <summary>
        /// This method will be used from the preset boxes in the line configuration menu.
        /// It's select the choosen color to the color picker.
        /// </summary>
        public void PresetSelect(Image sender)
        {
            picker.CurrentColor = sender.color;
        }

        /// <summary>
        /// This method will be used for the create preset button in the line configuration menu.
        /// If the color picker changed his color, the create preset button get the same color.
        /// </summary>
        private void ColorChanged(Color color)
        {
            createPresetImage.color = color;
        }

        /// <summary>
        /// This method will be used for the preset boxes in the line configuration menu.
        /// It's removed the choosen color from the list.
        /// </summary>
        public void RemoveColor(Image sender)
        {
            _colors.RemoveColor(sender.color);
        }

        /// <summary>
        /// This method will be used for the preset boxes in the line configuration menu.
        /// It's changed the choosen color from the list to the current color of the color picker.
        /// </summary>
        public void ChangeColor(int index)
        {
            _colors.ChangeColor(index, picker.CurrentColor);
        }
    }
}