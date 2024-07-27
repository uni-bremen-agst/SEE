using System;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Menu
{
    /// <summary>
    /// This class represents a platform-independent entry in a <see cref="SimpleListMenu"/>,
    /// which performs a designated action when it is chosen there.
    /// </summary>
    /// <param name="selectAction">The action to take when the entry is selected.</param>
    /// <param name="unselectAction">The action to take when the entry is unselected. May be null if nothing
    /// should happen when it is unselected.</param>
    /// <param name="title">The title of the entry.</param>
    /// <param name="description">A description of the entry.</param>
    /// <param name="entryColor">The color with which this entry shall be displayed.</param>
    /// <param name="enabled">Whether this entry should be enabled (i.e., whether it can be selected).</param>
    /// <param name="icon">The icon which shall be displayed alongside this entry,
    /// given as a FontAwesome codepoint. See <see cref="Icons"/> for more information.</param>
    public record MenuEntry(Action SelectAction, string Title, Action UnselectAction = null, string Description = null,
                            Color EntryColor = default, bool Enabled = true, char Icon = ' ')
    {
        /// <summary>
        /// The color of this entry when disabled.
        /// </summary>
        public Color DisabledColor => EntryColor.WithAlpha(0.2f);
    }
}
