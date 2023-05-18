using UnityEngine;
using UnityEngine.Events;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// This class differs from a normal <see cref="MenuEntry"/> in that the entries represent states
    /// which can be on or off.
    /// </summary>
    public class ToggleMenuEntry : MenuEntry
    {
        /// <summary>
        /// Instantiates and returns a new ToggleMenuEntry.
        /// </summary>
        /// <param name="selectAction">What action to take when the entry is selected.</param>
        /// <param name="unselectAction">What action to take when the entry is deselected. May be <c>null</c>.</param>
        /// <param name="title">The title of the entry.</param>
        /// <param name="description">A description of the entry.</param>
        /// <param name="entryColor">The color with which this entry shall be displayed.</param>
        /// <param name="icon">The icon which will be displayed alongside this entry.</param>
        /// <param name="enabled">Whether this entry should be enabled on creation.</param>
        public ToggleMenuEntry(UnityAction selectAction, UnityAction unselectAction, string title,
                               string description = null, Color entryColor = default, Sprite icon = null,
                               bool enabled = true)
            : base(selectAction, unselectAction, title, description, entryColor, enabled, icon)
        {
        }
    }
}
