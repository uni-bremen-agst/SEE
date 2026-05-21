using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Windows.Speech;

namespace SEE.UI.Menu
{
    /// <summary>
    /// A simple list menu containing <see cref="MenuEntry"/> items.
    /// </summary>
    public class SimpleListMenu : SimpleListMenu<MenuEntry> { }

    /// <summary>
    /// Represents a menu of various actions the user can choose from.
    /// The Menu consists of multiple MenuEntries of the type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of entries used. Must be derived from <see cref="MenuEntry"/>.</typeparam>
    /// <seealso cref="MenuEntry"/>
    public partial class SimpleListMenu<T> : SimpleMenu<T> where T : MenuEntry
    {
        /// <summary>
        /// Whether the menu can be closed by not making any selection.
        /// </summary>
        private bool allowNoSelection = true;

        /// <summary>
        /// Whether the menu can be closed by not making any selection.
        /// </summary>
        public bool AllowNoSelection
        {
            get => allowNoSelection;
            set
            {
                allowNoSelection = value;
                OnAllowNoSelectionChanged?.Invoke();
            }
        }

        /// <summary>
        /// Whether to hide the menu after selection.
        /// </summary>
        private bool hideAfterSelection = true;

        /// <summary>
        /// Whether to hide the menu after selection.
        /// </summary>
        public bool HideAfterSelection
        {
            get => hideAfterSelection;
            set
            {
                hideAfterSelection = value;
                OnHideAfterSelectionChanged?.Invoke();
            }
        }

        /// <summary>
        /// The menu entries.
        /// </summary>
        private readonly List<T> entries = new();

        /// <summary>
        /// A read-only wrapper around the menu entries.
        /// </summary>
        /// <see cref="entries"/>
        public IList<T> Entries => entries.AsReadOnly();

        /// <summary>
        /// Adds a menu entry.
        /// </summary>
        /// <param name="entry">The menu entry to be added.</param>
        public virtual void AddEntry(T entry)
        {
            entries.Add(entry);
            OnEntryAdded?.Invoke(entry);
        }

        /// <summary>
        /// Adds all given menu <paramref name="entries"/>.
        /// </summary>
        /// <param name="entries">Entries to be added.</param>
        public void AddEntries(IEnumerable<T> entries)
        {
            foreach (T entry in entries)
            {
                AddEntry(entry);
            }
        }

        /// <summary>
        /// Removes a menu entry.
        /// It is assumed that the menu contains the entry.
        /// </summary>
        /// <param name="entry">The menu entry to be removed.</param>
        public void RemoveEntry(T entry)
        {
            entries.Remove(entry);
            OnEntryRemoved?.Invoke(entry);
        }

        /// <summary>
        /// Removes all menu entries.
        /// </summary>
        public void ClearEntries() => entries.ToList().ForEach(RemoveEntry);

        /// <summary>
        /// Selects a menu entry.
        /// It is assumed that the menu contains the entry.
        /// </summary>
        /// <param name="entry">The menu entry to be selected.</param>
        public virtual void SelectEntry(T entry)
        {
            entry.SelectAction();
            OnEntrySelected?.Invoke(entry);
        }

        /// <summary>
        /// The keywords the menu should listen to.
        /// Adds the titles of the menu entries.
        /// Removes the CloseMenuKeyword if <see cref="AllowNoSelection"/> isn't true.
        /// </summary>
        /// <returns>The keywords.</returns>
        protected override IEnumerable<string> GetKeywords()
        {
            IEnumerable<string> keywords = base.GetKeywords();
            // removes the CloseMenuKeyword if no selection isn't allowed.
            if (!AllowNoSelection)
            {
                keywords = keywords.Where(keyword => keyword != CloseMenuKeyword);
            }
            // adds the entry titles as keywords
            keywords = keywords.Concat(Entries.Select(entry => entry.Title));
            return keywords;
        }

        /// <summary>
        /// Selects an entry if the keyword is the entry title.
        /// </summary>
        /// <param name="args">The phrase recognized.</param>
        protected override void HandleKeyword(PhraseRecognizedEventArgs args)
        {
            T entry = entries.FirstOrDefault(entry => entry.Title == args.text);
            if (entry != null)
            {
                SelectEntry(entry);
            }
            base.HandleKeyword(args);
        }

        /// <summary>
        /// Triggers when <see cref="AllowNoSelection"/> was changed.
        /// </summary>
        public event Action OnAllowNoSelectionChanged;

        /// <summary>
        /// Triggers when <see cref="HideAfterSelection"/> was changed.
        /// </summary>
        public event Action OnHideAfterSelectionChanged;

        /// <summary>
        /// Triggers when an entry was added. (<see cref="AddEntry"/>)
        /// </summary>
        public event Action<T> OnEntryAdded;

        /// <summary>
        /// Triggers when an entry was removed. (<see cref="RemoveEntry"/>)
        /// </summary>
        public event Action<T> OnEntryRemoved;

        /// <summary>
        /// Triggers when an entry was selected. (<see cref="SelectEntry"/>)
        /// </summary>
        public event Action<T> OnEntrySelected;
    }
}
