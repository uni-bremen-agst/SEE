using System;
using System.Collections.Generic;

namespace SEE.Game.UI
{
    /// <summary>
    /// Represents a menu of various actions the user can choose from.
    /// The Menu can consists of multiple MenuEntries of the type <paramref name="T"/>
    /// and can have multiple representations depending on the platform used.
    /// </summary>
    /// <typeparam name="T">the type of entries used. Must be derived from <see cref="MenuEntry"/>.</typeparam>
    /// <seealso cref="MenuEntry"/>
    public partial class Menu<T>: PlatformDependentComponent where T : MenuEntry
    {

        /// <summary>
        /// Whether the menu is currently being shown.
        /// </summary>
        protected bool MenuShown;

        /// <summary>
        /// Displays or hides the menu, depending on <paramref name="show"/>.
        /// </summary>
        /// <param name="show">Whether the menu should be shown.</param>
        public void ShowMenu(bool show)
        {
            MenuShown = show;
        }

        /// <summary>
        /// Displays the menu when it's hidden, and vice versa.
        /// </summary>
        public void ToggleMenu()
        {
            ShowMenu(!MenuShown);
        }
        
        /// <summary>
        /// A list of menu entries for this menu.
        /// </summary>
        /// <seealso cref="MenuEntry"/>
        public readonly IList<T> Entries = new List<T>();

        /// <summary>
        /// Adds an <paramref name="entry"/> to this menu's <see cref="Entries"/>.
        /// This method must be called <i>before</i> this component's Start() method has been called.
        /// </summary>
        /// <param name="entry">The entry to add to this menu.</param>
        public void AddEntry(T entry)
        {
            Entries.Add(entry);
        }

        /// <summary>
        /// Selects the entry at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index in <see cref="Entries"/> of the selected entry.</param>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="index"/> is above the size of
        /// <see cref="Entries"/></exception>
        public void SelectEntry(int index)
        {
            if (index > Entries.Count)
            {
                throw new ArgumentOutOfRangeException($"Entry index {index} doesn't exist in "
                                                   + $"{Entries.Count}-element array entries.");
            }
            OnEntrySelected(Entries[index]);
        }

        /// <summary>
        /// Called when an entry in the menu is selected.
        /// </summary>
        /// <param name="entry">The entry which was selected.</param>
        protected virtual void OnEntrySelected(T entry)
        {
            entry.DoAction();
        }

        // TODO: Implement TouchGamepad UI (same as Desktop?)
        protected override void StartTouchGamepad() => StartDesktop();

        // TODO: Implement VR UI (same as Desktop, but Curved?)
        protected override void StartVR() => StartDesktop();


        protected override void UpdateTouchGamepad() => UpdateDesktop();

        protected override void UpdateVR() => UpdateDesktop();
    }
}