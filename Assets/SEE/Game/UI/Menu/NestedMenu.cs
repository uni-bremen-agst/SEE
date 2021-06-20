using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// A menu similar to the <see cref="SimpleMenu"/> with buttons which can open other menus.
    /// </summary>
    public class NestedMenu : SimpleMenu<MenuEntry>
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
        /// Whether to reset the level of the menu when clicking on the close button.
        /// Can only be changed before this component has been started.
        /// </summary>
        public bool ResetLevelOnClose = true;

        protected override void OnEntrySelected(MenuEntry entry)
        {
            if (entry is NestedMenuEntry nestedEntry)
            {
                // If this contains another menu level, repopulate list with new level after saving the current one
                Levels.Push(new MenuLevel(Title, Description, Icon, entries));
                while (entries.Count != 0)
                {
                    RemoveEntry(entries[0]); // Remove all entries
                }
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
            if (Levels.Count != 0)
            {
                MenuLevel level = Levels.Pop();
                Title = level.Title;
                Description = level.Description;
                Icon = level.Icon;
                while (entries.Count != 0)
                {
                    RemoveEntry(entries[0]); // Remove all entries
                }
                level.Entries.ForEach(AddEntry);
            }
            else
            {
                ShowMenu(false);
            }
        }

        /// <summary>
        /// Resets to the lowest level, i.e. resets the menu to the state it was in before any
        /// <see cref="NestedMenuEntry"/> was clicked.
        /// </summary>
        private void ResetToBase()
        {
            while (Levels.Count > 0)
            {
                DescendLevel();
            }
        }

        protected override void StartDesktop()
        {
            base.StartDesktop();
            Manager.onCancel.AddListener(DescendLevel); // Go one level higher when clicking "back"
            if (ResetLevelOnClose)
            {
                Manager.onConfirm.AddListener(ResetToBase); // When closing the menu, its level will be reset to the top
            }
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
            public readonly List<MenuEntry> Entries;

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