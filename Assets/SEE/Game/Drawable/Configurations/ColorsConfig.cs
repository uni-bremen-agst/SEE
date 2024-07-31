using SEE.UI.Menu.Drawable;
using SEE.Utils.Config;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// This class can hold all the information that is needed to configure the
    /// color presets of the color picker.
    /// Used for the line configuration menu.
    /// </summary>
    public class ColorsConfig
    {
        /// <summary>
        /// The colors of the color presets boxes.
        /// </summary>
        public Color[] Colors;

        /// <summary>
        /// The label for the first color preset box.
        /// </summary>
        private const string ColorPresets0 = "ColorPresets0";
        /// <summary>
        /// The label for the second color preset box.
        /// </summary>
        private const string ColorPresets1 = "ColorPresets1";
        /// <summary>
        /// The label for the third color preset box.
        /// </summary>
        private const string ColorPresets2 = "ColorPresets2";
        /// <summary>
        /// The label for the fourth color preset box.
        /// </summary>
        private const string ColorPresets3 = "ColorPresets3";
        /// <summary>
        /// The label for the fifth color preset box.
        /// </summary>
        private const string ColorPresets4 = "ColorPresets4";
        /// <summary>
        /// The label for the sixth color preset box.
        /// </summary>
        private const string ColorPresets5 = "ColorPresets5";
        /// <summary>
        /// The label for the seventh color preset box.
        /// </summary>
        private const string ColorPresets6 = "ColorPresets6";
        /// <summary>
        /// The label for the eighth color preset box.
        /// </summary>
        private const string ColorPresets7 = "ColorPresets7";
        /// <summary>
        /// The label for the ninth color preset box.
        /// </summary>
        private const string ColorPresets8 = "ColorPresets8";
        /// <summary>
        /// The label for the tenth color preset box.
        /// </summary>
        private const string ColorPresets9 = "ColorPresets9";
        /// <summary>
        /// The label for the eleventh color preset box.
        /// </summary>
        private const string ColorPresets10 = "ColorPresets10";

        /// <summary>
        /// An array that holds the eleven color-preset labels. It is needed for iterating.
        /// </summary>
        private readonly string[] presets = { ColorPresets0, ColorPresets1, ColorPresets2,
            ColorPresets3, ColorPresets4, ColorPresets5, ColorPresets6, ColorPresets7,
            ColorPresets8, ColorPresets9, ColorPresets10};

        /// <summary>
        /// Saves this instance's attributes using the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to be used to to write the attributes.</param>
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
                Colors = LineMenu.Instance.GameObject.GetComponent<HSVPicker.ColorPicker>()
                    .Setup.DefaultPresetColors;
            }
        }
    }
}