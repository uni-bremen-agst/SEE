using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// A button which opens another menu when clicking on it.
    /// Must be used inside a <see cref="NestedMenu"/>.
    /// </summary>
    /// <typeparam name="T">the type of the menu entries which shall fill the menu
    ///  when this button is clicked</typeparam>
    public class NestedMenuEntry<T> : MenuEntry where T : MenuEntry
    {
        /// <summary>
        /// The menu entries which shall fill the menu when selecting this entry.
        /// </summary>
        public readonly List<T> InnerEntries;

        public NestedMenuEntry(IEnumerable<T> innerEntries, string title, string description = null,
                               Color entryColor = default, bool enabled = true, Sprite icon = null) :
            base(() => { }, title, description, entryColor, enabled, icon)
        {
            InnerEntries = innerEntries?.ToList() ?? throw new ArgumentNullException(nameof(innerEntries));
        }
    }
}