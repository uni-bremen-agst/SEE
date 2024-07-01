using System.Collections;
using UnityEngine;

namespace Assets.SEE.UI.Window.DrawableManagerWindow
{
    /// <summary>
    /// A group of drawable surfaces in the drawable manager window.
    /// </summary>
    /// <param name="Text">The text to display for the group.</param>
    /// <param name="IconGlyph">The icon to display for this group.</param>
    /// <param name="StartColor">The start color of the gradient to use for this group.</param>
    /// <param name="EndColor">The end color of the gradient to use for this group.</param>
    public record DrawableWindowGroup(string Text, char IconGlyph, Color StartColor, Color EndColor)
    {
        public Color[] Gradient => new[] { StartColor, EndColor };
    }
}