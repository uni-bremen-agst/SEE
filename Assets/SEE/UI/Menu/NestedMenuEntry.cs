using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Menu
{
    /// <summary>
    /// A button which opens another menu when clicking on it.
    /// Must be used inside a <see cref="NestedListMenu"/>.
    /// </summary>
    public record NestedMenuEntry<T> : MenuEntry where T : MenuEntry
    {
        /// <summary>
        /// The menu entries which shall fill the menu when selecting this entry.
        /// </summary>
        public readonly List<T> InnerEntries;

        /// <summary>
        /// The sprite of the icon of the menu itself.
        /// </summary>
        public readonly Sprite MenuIconSprite;

        /// <summary>
        /// Instantiates and returns a new <see cref="NestedMenuEntry{T}"/>.
        /// </summary>
        /// <param name="innerEntries">The entries of this <see cref="NestedMenuEntry{T}"/></param>
        /// <param name="title">The title of the entry.</param>
        /// <param name="description">A description of the entry.</param>
        /// <param name="entryColor">The color with which this entry shall be displayed.</param>
        /// <param name="enabled">Whether this entry should be enabled on creation.</param>
        /// <param name="icon">The FontAwesome icon which shall be displayed alongside this entry.</param>
        /// <param name="menuIconSprite">The sprite of the icon of the menu itself.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="title"/> is <c>null</c>.</exception>
        public NestedMenuEntry(IEnumerable<T> innerEntries, string title, string description = null,
                               Color entryColor = default, bool enabled = true,
                               char icon = Icons.Bars, Sprite menuIconSprite = null)
            : base(() => { }, title, () => { }, description, entryColor, enabled, icon)
        {
            InnerEntries = innerEntries?.ToList() ?? throw new ArgumentNullException(nameof(innerEntries));
            MenuIconSprite = menuIconSprite;
        }
    }
}
