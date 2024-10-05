using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// This class holds the colors for the preset boxes.
    ///
    /// The file comes from the HSVPicker package.
    /// https://github.com/judah4/HSV-Color-Picker-Unity/blob/master/Packages/com.judahperez.hsvcolorpicker/UI/ColorPresetManager.cs
    /// It has been extended to include the functionality of removing and modifying colors,
    /// as well as loading and saving colors from the previous session.
    /// </summary>
    public class ColorPresetList
    {
        /// <summary>
        /// The list name.
        /// </summary>
        public string ListId { get; private set; }

        /// <summary>
        /// The colors of the preset boxes.
        /// </summary>
        public List<Color> Colors { get; private set; }

        /// <summary>
        /// The action that will be called when the list were updated.
        /// </summary>
        public event UnityAction<List<Color>> OnColorsUpdated;

        /// <summary>
        /// The constructor of ColorPresetList.
        /// </summary>
        /// <param name="listId">is the id/name of the list.</param>
        /// <param name="colors">are the colors that the list holds.</param>
        public ColorPresetList(string listId, List<Color> colors = null)
        {
            colors ??= new();
            Colors = colors;
            ListId = listId;
        }

        /// <summary>
        /// Adds a new color to the list and updates the UI.
        /// </summary>
        /// <param name="color">is the color that should be added.</param>
        public void AddColor(Color color)
        {
            Colors.Add(color);
            OnColorsUpdated?.Invoke(Colors);
        }

        /// <summary>
        /// Removes the given color from the list.
        /// </summary>
        /// <param name="color">is the color that should be removed.</param>
        public void RemoveColor(Color color)
        {
            Colors.Remove(color);
            OnColorsUpdated?.Invoke(Colors);
        }

        /// <summary>
        /// This method change the color of an already existing color preset.
        /// </summary>
        /// <param name="index">is the index of the color preset that should be changed.</param>
        /// <param name="newColor">is the new color for the color preset.</param>
        public void ChangeColor(int index, Color newColor)
        {
            Colors[index] = newColor;
            OnColorsUpdated?.Invoke(Colors);
        }

        /// <summary>
        /// Updates the list.
        /// </summary>
        /// <param name="colors">are the new colors for the list.</param>
        public void UpdateList(IEnumerable<Color> colors)
        {
            Colors.Clear();
            Colors.AddRange(colors);

            OnColorsUpdated?.Invoke(Colors);
        }
    }
}
