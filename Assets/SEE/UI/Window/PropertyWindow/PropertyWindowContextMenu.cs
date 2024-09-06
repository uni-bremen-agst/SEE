using Assets.SEE.UI.Window.PropertyWindow;
using Michsky.UI.ModernUIPack;
using SEE.UI.PopupMenu;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Window.PropertyWindow
{
    /// <summary>
    /// Manages the context menu for the property window.
    /// </summary>
    public class PropertyWindowContextMenu
    {
        /// <summary>
        /// The context menu that this class manages.
        /// </summary>
        private readonly PopupMenu.PopupMenu contextMenu;

        /// <summary>
        /// The button that opens the filter menu.
        /// </summary>
        private readonly ButtonManagerBasic filterButton;

        /// <summary>
        /// The button that opens the sort menu.
        /// </summary>
        private readonly ButtonManagerBasic sortButton;

        /// <summary>
        /// the button that opens the group menu.
        /// </summary>
        private readonly ButtonManagerBasic groupButton;

        /// <summary>
        /// The property filter.
        /// </summary>
        public readonly PropertyFilter Filter;

        /// <summary>
        /// The function to call to set the activity of the groups.
        /// </summary>
        private readonly UnityEvent<string, bool> filterEvent;

        /// <summary>
        /// Construcotr.
        /// </summary>
        /// <param name="contextMenu">The context menu that this class manages.</param>
        /// <param name="filterButton">The button that opens the filter menu.</param>
        /// <param name="filterEvent">The event that should be triggered if a filter will be used.</param>
        /// <param name="sortButton">The button that opens the sort menu.</param>
        /// <param name="groupButton">The button that opens the group menu.</param>
        public PropertyWindowContextMenu(PopupMenu.PopupMenu contextMenu,
            ButtonManagerBasic filterButton, UnityEvent<string, bool> filterEvent,
            ButtonManagerBasic sortButton,
            ButtonManagerBasic groupButton)
        {
            this.contextMenu = contextMenu;
            this.filterButton = filterButton;
            this.filterEvent = filterEvent;
            this.sortButton = sortButton;
            this.groupButton = groupButton;
            Filter = new PropertyFilter();
            ResetFilter();

            this.filterButton.clickEvent.AddListener(ShowFilterMenu);
        }

        #region Filter menu
        /// <summary>
        /// Displays the filter menu.
        /// </summary>
        private void ShowFilterMenu()
        {
            UpdateFilterMenuEntries();
            contextMenu.ShowWith(position: filterButton.transform.position);
        }

        /// <summary>
        /// Updates the filter menu entries.
        /// </summary>
        private void UpdateFilterMenuEntries()
        {
            List<PopupMenuEntry> entries = new()
            {
                new PopupMenuAction("Reset", ()=>
                {
                    ResetFilter();
                    UpdateFilterMenuEntries();
                    filterEvent.Invoke("Header", true);
                    filterEvent.Invoke("Toggle Attributes", true);
                    filterEvent.Invoke("String Attributes", true);
                    filterEvent.Invoke("Int Attributes", true);
                    filterEvent.Invoke("Float Attributes", true);
                }, Icons.ArrowRotateLeft, CloseAfterClick: false),
                new PopupMenuAction("Header", () =>
                {
                    Filter.IncludeHeader = !Filter.IncludeHeader;
                    UpdateFilterMenuEntries();
                    filterEvent.Invoke("Header", Filter.IncludeHeader);
                }, Checkbox(Filter.IncludeHeader), CloseAfterClick: false),
                new PopupMenuAction("Toggle Attributes", () =>
                {
                    Filter.IncludeToggleAttributes = !Filter.IncludeToggleAttributes;
                    UpdateFilterMenuEntries();
                    filterEvent.Invoke("Toggle Attributes", Filter.IncludeToggleAttributes);
                }, Checkbox(Filter.IncludeToggleAttributes), CloseAfterClick: false),
                new PopupMenuAction("String Attributes", () =>
                {
                    Filter.IncludeStringAttributes = !Filter.IncludeStringAttributes;
                    UpdateFilterMenuEntries();
                    filterEvent.Invoke("String Attributes", Filter.IncludeStringAttributes);
                }, Checkbox(Filter.IncludeStringAttributes), CloseAfterClick: false),
                new PopupMenuAction("Int Attributes", () =>
                {
                    Filter.IncludeIntAttributes = !Filter.IncludeIntAttributes;
                    UpdateFilterMenuEntries();
                    filterEvent.Invoke("Int Attributes", Filter.IncludeIntAttributes);
                }, Checkbox(Filter.IncludeIntAttributes), CloseAfterClick: false),
                new PopupMenuAction("Float Attributes", () =>
                {
                    Filter.IncludeFloatAttributes = !Filter.IncludeFloatAttributes;
                    UpdateFilterMenuEntries();
                    filterEvent.Invoke("Float Attributes", Filter.IncludeFloatAttributes);
                }, Checkbox(Filter.IncludeFloatAttributes), CloseAfterClick: false),
            };

            contextMenu.ClearEntries();
            contextMenu.AddEntries(entries);
        }

        /// <summary>
        /// Returns the icon for a checkbox.
        /// </summary>
        /// <param name="value">Whether the checkbox is checked.</param>
        /// <returns>The icon for a checkbox.</returns>
        private static char Checkbox(bool value) => value ? Icons.CheckedCheckbox : Icons.EmptyCheckbox;

        /// <summary>
        /// Resets the filter to its default state.
        /// </summary>
        private void ResetFilter()
        {
            Filter.Reset();
        }
        #endregion
    }
}