using System;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Game.UI
{
    /// <summary>
    /// This class represents a platform-independent entry in a <see cref="Menu"/>,
    /// which performs a designated action when it is chosen there.
    /// </summary>
    public class MenuEntry
    {
        /// <summary>
        /// The title of this entry.
        /// </summary>
        private readonly string Title;
        
        /// <summary>
        /// A description of this entry.
        /// </summary>
        private readonly string Description;
        
        /// <summary>
        /// The color of this entry.
        /// </summary>
        protected readonly Color EntryColor;
        
        /// <summary>
        /// The action to be taken when the entry is selected.
        /// </summary>
        public readonly Action DoAction;
        
        /// <summary>
        /// Whether this entry is currently enabled (i.e. whether it can be selected.)
        /// </summary>
        protected bool Enabled;

        /// <summary>
        /// The color of this entry when disabled.
        /// </summary>
        private Color DisabledColor => EntryColor.ColorWithAlpha(0.2f);

        /// <summary>
        /// Instantiates and returns a new MenuEntry.
        /// </summary>
        /// <param name="action">What action to take when the entry is selected.</param>
        /// <param name="title">The title of the entry.</param>
        /// <param name="description">A description of the entry.</param>
        /// <param name="entryColor">The color with which this entry shall be displayed.</param>
        /// <param name="enabled">Whether this entry should be enabled on creation.</param>
        public MenuEntry(Action action, string title, string description = null, Color entryColor = default, bool enabled = default)
        {
            DoAction = action;
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description;
            EntryColor = entryColor;
            Enabled = enabled;
        }
    }
}