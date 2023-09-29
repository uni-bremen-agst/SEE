using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    /// <summary>
    /// This class can hold all the information that is needed to configure the color presets of the color picker.
    /// Used for the line configuration menu.
    /// </summary>
    public class ColorsConfig
    {
        /// <summary>
        /// The colors of the color presets boxes.
        /// </summary>
        public Color[] Colors;

        /// <summary>
        /// The labels for the eleven color presets boxes.
        /// </summary>
        private const string ColorPresets0 = "ColorPresets0";
        private const string ColorPresets1 = "ColorPresets1";
        private const string ColorPresets2 = "ColorPresets2";
        private const string ColorPresets3 = "ColorPresets3";
        private const string ColorPresets4 = "ColorPresets4";
        private const string ColorPresets5 = "ColorPresets5";
        private const string ColorPresets6 = "ColorPresets6";
        private const string ColorPresets7 = "ColorPresets7";
        private const string ColorPresets8 = "ColorPresets8";
        private const string ColorPresets9 = "ColorPresets9";
        private const string ColorPresets10 = "ColorPresets10";

        /// <summary>
        /// An array that holdes the eleven color presets labels. It is needed for iterating.
        /// </summary>
        private string[] presets = { ColorPresets0, ColorPresets1, ColorPresets2, ColorPresets3,
            ColorPresets4, ColorPresets5, ColorPresets6, ColorPresets7, ColorPresets8, ColorPresets9, ColorPresets10};

        /// <summary>
        /// Writes this instances' attributes into the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes into.</param>
        internal void Save(ConfigWriter writer)
        {
            for (int i = 0; i < Colors.Length; i++)
            {
                writer.Save(Colors[i], presets[i]);
            }
        }

        /// <summary>
        /// Loads the given attributes into this instance of the class <see cref="ColorPresetsConfigManager"/>.
        /// If the given attributes are empty, then the default preset colors are loaded.
        /// </summary>
        /// <param name="attributes">The attributes to load in the format created by
        /// <see cref="ConfigWriter"/>.</param>
        internal void Restore(Dictionary<string, object> attributes)
        {
            Colors = new Color[attributes.Count];
            for (int i = 0; i < attributes.Count; i++)
            {
                Color color = Color.black;
                if (ConfigIO.Restore(attributes, presets[i], ref color))
                {
                    Colors[i] = color;
                }
            }

            if (Colors.Length == 0)
            {
                Colors = DrawableHelper.drawableMenu.GetComponent<HSVPicker.ColorPicker>().Setup.DefaultPresetColors;
            }
        }
    }
}