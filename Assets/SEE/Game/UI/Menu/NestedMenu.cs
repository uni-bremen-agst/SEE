using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// A menu similar to the <see cref="SimpleMenu"/> with buttons which can open other menus.
    /// </summary>
    public class NestedMenu: SimpleMenu<MenuEntry>
    {
        /// <summary>
        /// The menu levels we have ascended through.
        /// </summary>
        private readonly Stack<MenuLevel> Levels = new Stack<MenuLevel>();

        /// <summary>
        /// Path to the NestedMenu prefab.
        /// </summary>
        protected override string MENU_PREFAB => "Prefabs/UI/NestedMenu";

        /// <summary>
        /// The parent of the
        /// </summary>
        public SimpleMenu<MenuEntry> parent;
        
        protected override void OnEntrySelected(MenuEntry entry)
        {
            if (entry is NestedMenuEntry nestedEntry)
            {
                // If this contains another menu level, repopulate list with new level after saving the current one
                Levels.Push(new MenuLevel(Title, Description, Icon, entries));
                entries.ForEach(RemoveEntry);
                Title = nestedEntry.Title;
                Description = nestedEntry.Description;
                Icon = nestedEntry.Icon;
                nestedEntry.InnerEntries.ForEach(AddEntry);
            }
            else
            {
                // Otherwise, we do the same we'd do normally
                base.OnEntrySelected(entry);
            }
        }

        /// <summary>
        /// Descends down a level in the menu hierarchy and removes the entry from the <see cref="Levels"/>.
        /// </summary>
        private void DescendLevel()
        {
            MenuLevel level = Levels.Pop();
            Title = level.Title;
            Description = level.Description;
            Icon = level.Icon;
            entries.ForEach(RemoveEntry);
            entries.AddRange(level.Entries);
        }

        /// <summary>
        /// Resets to the lowest level, i.e. resets the menu to the state it was in before any
        /// <see cref="NestedMenuEntry"/> was clicked.
        /// </summary>
        public void ResetToBase()
        {
            while (Levels.Count > 0)
            {
                DescendLevel();
            }
        }

        protected override void StartDesktop()
        {
            base.StartDesktop();
            Manager.onCancel.AddListener(GoBack); // Go one level higher when clicking "back"
        }

        private void GoBack()
        {
            this.ToggleMenu();
            parent.ToggleMenu();
        }

        /// <summary>
        /// A state of this menu representing a nesting level established by selecting a <see cref="NestedMenuEntry"/>.
        /// Contains everything necessary to restore the menu to this state.
        /// </summary>
        private class MenuLevel
        {
            public readonly string Title;
            public readonly string Description;
            public readonly Sprite Icon;
            public readonly IList<MenuEntry> Entries;

            public MenuLevel(string title, string description, Sprite icon, IList<MenuEntry> entries)
            {
                Title = title ?? throw new ArgumentNullException(nameof(title));
                Description = description ?? throw new ArgumentNullException(nameof(description));
                Icon = icon;
                Entries = entries != null ? new List<MenuEntry>(entries) : throw new ArgumentNullException(nameof(entries));
            }
        }
    }
}