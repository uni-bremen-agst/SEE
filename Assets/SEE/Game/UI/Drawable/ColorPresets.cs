using HSVPicker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// File from the hsvpicker packages, add remove color
    /// </summary>
    public class ColorPresets : MonoBehaviour
    {
        public ColorPicker picker;
        public GameObject[] presets;
        public Image createPresetImage;

        private ColorPresetList _colors;

        void Awake()
        {
            picker.onValueChanged.AddListener(ColorChanged);
        }

        void Start()
        {
            _colors = ColorPresetManager.Get(picker.Setup.PresetColorsId);

            if (_colors.Colors.Count < picker.Setup.DefaultPresetColors.Length)
            {
                _colors.UpdateList(picker.Setup.DefaultPresetColors);
            }

            _colors.OnColorsUpdated += OnColorsUpdate;
            OnColorsUpdate(_colors.Colors);
        }

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

        }

        public void CreatePresetButton()
        {
            _colors.AddColor(picker.CurrentColor);
        }

        public void PresetSelect(Image sender)
        {
            picker.CurrentColor = sender.color;
        }

        private void ColorChanged(Color color)
        {
            createPresetImage.color = color;
        }

        public void RemoveColor(Image sender)
        {
            _colors.RemoveColor(sender.color);
        }
    }
}