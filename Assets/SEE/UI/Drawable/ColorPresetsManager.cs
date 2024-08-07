using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// This class holds the color preset list with a name.
    ///
    /// The file comes from the HSVPicker package.
    /// https://github.com/judah4/HSV-Color-Picker-Unity/blob/master/Packages/com.judahperez.hsvcolorpicker/UI/ColorPresetManager.cs
    /// It has been extended to include the functionality of removing and modifying colors,
    /// as well as loading and saving colors from the previous session.
    /// </summary>
    public static class ColorPresetManager
    {
        /// <summary>
        /// The dictionary that currently holds the color preset list.
        /// </summary>
        private static Dictionary<string, ColorPresetList> presets = new();

        /// <summary>
        /// Adds the created color preset list to the dictionary and returned the list.
        /// </summary>
        /// <param name="colors">are the colors that should be load in the list.</param>
        /// <param name="listId">is the name of the list.</param>
        /// <returns>The preset list</returns>
        public static ColorPresetList Get(Color[] colors, string listId = "loaded")
        {
            if (!presets.TryGetValue(listId, out ColorPresetList preset))
            {
                preset = new ColorPresetList(listId, colors.ToList());
                presets.Add(listId, preset);
            }
            return preset;
        }
    }
}
