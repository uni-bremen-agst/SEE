using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// A selection menu containing <see cref="ToggleMenuEntry"/> entries.
    /// </summary>
    public class SelectionMenu : SelectionMenu<ToggleMenuEntry> {}

    /// <summary>
    /// A menu in which the user can choose one active selection out of a menu.
    /// It is assumed that only one selection can be active at a time.
    /// </summary>
    public class SelectionMenu<T>: SimpleListMenu<T> where T : ToggleMenuEntry
    {
        /// <summary>
        /// The active entry.
        /// </summary>
        private T activeEntry;

        /// <summary>
        /// The active entry.
        /// Assigning a new value unselects the old value.
        /// </summary>
        public T ActiveEntry
        {
            get => activeEntry;
            set
            {
                T oldActiveEntry = ActiveEntry;
                activeEntry = value;
                if (oldActiveEntry != null)
                {
                    oldActiveEntry.UnselectAction();
                    OnEntryUnselected?.Invoke(oldActiveEntry);
                }
                OnActiveEntryChanged?.Invoke();
            }
        }

        /// <summary>
        /// Updates the menu and adds listeners.
        /// </summary>
        protected override void OnStartFinished()
        {
            base.OnStartFinished();
            // updates menu
            if (ActiveEntry != null) ActivateButton(ActiveEntry);
            // adds listeners
            OnEntrySelected += entry => ActiveEntry = entry;
            OnEntrySelected += ActivateButton;
            OnEntryUnselected += DeactivateButton;
            OnEntryRemoved += entry => { if (entry == ActiveEntry) ActiveEntry = null; };
        }

        /// <summary>
        /// Activates a button.
        /// It is assumed that the entry is the active entry.
        /// </summary>
        /// <param name="entry">The menu entry.</param>
        private void ActivateButton(T entry)
        {
            GameObject button = EntryGameObject(entry);
            ButtonManagerBasicWithIcon manager = button.GetComponent<ButtonManagerBasicWithIcon>();
            manager.buttonText = $"[{entry.Title}]";
            manager.normalText.fontStyle = FontStyles.UpperCase;
            manager.normalText.text = manager.buttonText;
        }

        /// <summary>
        /// Deactivates a button.
        /// It is assumed that the entry was the previously active entry.
        /// </summary>
        /// <param name="entry">The menu entry.</param>
        private void DeactivateButton(T entry)
        {
            GameObject button = EntryGameObject(entry);
            ButtonManagerBasicWithIcon manager = button.GetComponent<ButtonManagerBasicWithIcon>();
            manager.buttonText = entry.Title;
            manager.normalText.fontStyle = FontStyles.Normal;
            manager.normalText.text = manager.buttonText;
        }

        /// <summary>
        /// Triggers when the <see cref="ActiveEntry"/> was unselected.
        /// </summary>
        public event UnityAction<T> OnEntryUnselected;

        /// <summary>
        /// Triggers when <see cref="ActiveEntry"/> was changed.
        /// </summary>
        public event UnityAction OnActiveEntryChanged;
    }
}
