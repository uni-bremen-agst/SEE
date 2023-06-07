﻿using System;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// This class represents a platform-independent entry in a <see cref="SimpleListMenu"/>,
    /// which performs a designated action when it is chosen there.
    /// </summary>
    public class MenuEntry
    {
        /// <summary>
        /// The title of this entry.
        /// </summary>
        public readonly string Title;

        /// <summary>
        /// A description of this entry.
        /// </summary>
        public readonly string Description;

        /// <summary>
        /// The color of this entry.
        /// </summary>
        public readonly Color EntryColor;

        /// <summary>
        /// An icon for this entry.
        /// </summary>
        public readonly Sprite Icon;

        /// <summary>
        /// The action to be taken when the entry is selected.
        /// </summary>
        public readonly UnityAction SelectAction;

        /// <summary>
        /// The action to be taken when the entry is deselected.
        /// </summary>
        public readonly UnityAction UnselectAction;

        /// <summary>
        /// Whether this entry is currently enabled (i.e. whether it can be selected.)
        /// Defaults to true.
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// The color of this entry when disabled.
        /// </summary>
        public Color DisabledColor => EntryColor.WithAlpha(0.2f);

        /// <summary>
        /// Instantiates and returns a new MenuEntry.
        /// </summary>
        /// <param name="selectAction">What action to take when the entry is selected.</param>
        /// <param name="unselectAction">What action to take when the entry is unselected.</param>
        /// <param name="title">The title of the entry.</param>
        /// <param name="description">A description of the entry.</param>
        /// <param name="entryColor">The color with which this entry shall be displayed.</param>
        /// <param name="enabled">Whether this entry should be enabled on creation.</param>
        /// <param name="icon">The icon which shall be displayed alongside this entry.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="title"/> is <c>null</c>.</exception>
        public MenuEntry(UnityAction selectAction, UnityAction unselectAction, string title, string description = null, Color entryColor = default,
                         bool enabled = true, Sprite icon = null)
        {
            SelectAction = selectAction;
            UnselectAction = unselectAction ?? (() => { });
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description;
            EntryColor = entryColor;
            Enabled = enabled;
            Icon = icon;
        }
    }
}
