using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

// File from the hsvpicker package 
// https://github.com/judah4/HSV-Color-Picker-Unity/blob/master/Packages/com.judahperez.hsvcolorpicker/UI/ColorPresetManager.cs
// Added methods for remove and change color.
// Also added an method to load colors at the initalization.

/// <summary>
/// This class holds the color preset list with a name.
/// </summary>
public static class ColorPresetManager
{
    /// <summary>
    /// The dictionary that currently holds the color preset list.
    /// </summary>
    private static Dictionary<string, ColorPresetList> _presets = new Dictionary<string, ColorPresetList>();

    /// <summary>
    /// This method adds the created color preset list to the dictionary and returned the list.
    /// </summary>
    /// <param name="colors">are the colors that should be load in the list.</param>
    /// <param name="listId">is the name of the list.</param>
    /// <returns></returns>
    public static ColorPresetList Get(Color[] colors, string listId = "loaded")
    {
        ColorPresetList preset;
        if (!_presets.TryGetValue(listId, out preset))
        {
            preset = new ColorPresetList(listId, colors.ToList());
            _presets.Add(listId, preset);
        }

        return preset;
    }
}

/// <summary>
/// This class holds the colors for the preset boxes.
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
        if (colors == null)
        {
            colors = new List<Color>();
        }

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
        if (OnColorsUpdated != null)
        {
            OnColorsUpdated.Invoke(Colors);
        }
    }

    /// <summary>
    /// Removes the given color from the list.
    /// </summary>
    /// <param name="color">is the color that should be removed.</param>
    public void RemoveColor(Color color)
    {
        Colors.Remove(color);
        if (OnColorsUpdated != null)
        {
            OnColorsUpdated.Invoke(Colors);
        }
    }

    /// <summary>
    /// This method change the color of an already existing color preset.
    /// </summary>
    /// <param name="index">is the index of the color preset that should be changed.</param>
    /// <param name="newColor">is the new color for the color preset.</param>
    public void ChangeColor(int index, Color newColor)
    {
        Colors[index] = newColor;
        if (OnColorsUpdated != null)
        {
            OnColorsUpdated.Invoke(Colors);
        }
    }

    /// <summary>
    /// Updates the list.
    /// </summary>
    /// <param name="colors">are the new colors for the list.</param>
    public void UpdateList(IEnumerable<Color> colors)
    {
        Colors.Clear();
        Colors.AddRange(colors);

        if (OnColorsUpdated != null)
        {
            OnColorsUpdated.Invoke(Colors);
        }
    }


}

