using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// Represents a menu of various actions the user can choose from.
    /// The Menu consists of multiple MenuEntries of the type <typeparamref name="T"/>
    /// and can have multiple representations depending on the platform used.
    /// </summary>
    /// <typeparam name="T">the type of entries used. Must be derived from <see cref="MenuEntry"/>.</typeparam>
    /// <seealso cref="MenuEntry"/>
    public partial class Menu<T>: PlatformDependentComponent where T : MenuEntry
    {
        
        /// <summary>
        /// Event type which is used for the <see cref="OnMenuEntrySelected"/> event.
        /// Has the <see cref="MenuEntry"/> type <typeparamref name="T"/> as a parameter.
        /// </summary>
        [Serializable]
        public class MenuEntrySelectedEvent : UnityEvent<T> {}

        /// <summary>
        /// The name of this menu. Displayed to the user.
        /// </summary>
        public string Title = "Unnamed Menu";

        /// <summary>
        /// Brief description of what this menu controls.
        /// Will be displayed to the user above the choices.
        /// The text may <i>not be longer than 3 lines!</i>
        /// </summary>
        public string Description = "No description added.";

        /// <summary>
        /// Icon for this menu. Displayed along the title.
        /// Default is a generic settings (gear) icon.
        /// </summary>
        public Sprite Icon;

        /// <summary>
        /// Whether the menu shall be shown.
        /// </summary>
        private bool MenuShown;

        /// <summary>
        /// Whether the menu is currently shown or not.
        /// If this does not match <see cref="MenuShown"/>,
        /// the <see cref="Update"/> method will update the UI accordingly.
        /// </summary>
        private bool CurrentMenuShown = false;

        /// <summary>
        /// This event will be called whenever an entry in the menu is chosen.
        /// Its parameter will be the chosen <see cref="MenuEntry"/> with type <typeparamref name="T"/>.
        /// </summary>
        public readonly MenuEntrySelectedEvent OnMenuEntrySelected = new MenuEntrySelectedEvent();
        
        /// <summary>
        /// A list of menu entries for this menu.
        /// </summary>
        /// <seealso cref="MenuEntry"/>
        protected readonly List<T> entries = new List<T>();
        
        /// <summary>
        /// A read-only wrapper around the list of menu entries for this menu.
        /// </summary>
        /// <seealso cref="MenuEntry"/>
        public IList<T> Entries => entries.AsReadOnly();

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
        /// Adds an <paramref name="entry"/> to this menu's <see cref="entries"/>.
        /// </summary>
        /// <param name="entry">The entry to add to this menu.</param>
        public void AddEntry(T entry)
        {
            if (entries.Any(x => x.Title == entry.Title))
            {
                throw new InvalidOperationException($"Button with the given title '{entry.Title}' already exists!\n");
            }
            entries.Add(entry);
            if (HasStarted)
            {
                AddDesktopButtons(new []{entry});
            }
        }

        /// <summary>
        /// Removes the given <paramref name="entry"/> from the menu.
        /// If the <paramref name="entry"/> is not present in the menu, nothing will happen.
        /// </summary>
        /// <param name="entry">The entry to remove from the menu</param>
        public void RemoveEntry(T entry)
        {
            Entries.Remove(entry);
            if (HasStarted)
            {
                RemoveDesktopButton(entry);
            }
        }

        /// <summary>
        /// Selects the entry at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index in <see cref="Entries"/> of the selected entry.</param>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="index"/> is above the size of
        /// <see cref="Entries"/></exception>
        public void SelectEntry(int index)
        {
            if (index >= Entries.Count)
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
            OnMenuEntrySelected.Invoke(entry);
            entry.DoAction();
        }

        private void Awake()
        {
            // Load default icon (can't be done during instantiation, only in Awake() or Start())
            Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Settings");
        }
        
        protected override void StartTouchGamepad() => StartDesktop();

        protected override void StartVR() => StartDesktop();

        protected override void UpdateTouchGamepad() => UpdateDesktop();

        protected override void UpdateVR() => UpdateDesktop();
    }
}
