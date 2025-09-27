using Michsky.UI.ModernUIPack;
using SEE.GO;
using SEE.UI.PopupMenu;
using SEE.UI.Window.DrawableManagerWindow;
using SEE.Utils;
using SEE.XR;
using System;
using System.Collections.Generic;
using TMPro;
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
        /// The button that opens the group menu.
        /// </summary>
        private readonly ButtonManagerBasic groupButton;

        /// <summary>
        /// The rebuild event.
        /// </summary>
        private readonly UnityEvent rebuild;

        /// <summary>
        /// The property filter.
        /// </summary>
        public readonly PropertyFilter Filter;

        /// <summary>
        /// The property sorter.
        /// </summary>
        public readonly GameObjectSorter Sorter;

        /// <summary>
        /// If true, grouping is done by the names of the attributes.
        /// Otherwise, grouping is done by the type of the attributes.
        /// </summary>
        public bool GroupByName;

        /// <summary>
        /// Construcotr.
        /// </summary>
        /// <param name="contextMenu">The context menu that this class manages.</param>
        /// <param name="rebuild">The rebuild event that should be executed.</param>
        /// <param name="filterButton">The button that opens the filter menu.</param>
        /// <param name="sortButton">The button that opens the sort menu.</param>
        /// <param name="groupButton">The button that opens the group menu.</param>
        public PropertyWindowContextMenu(PopupMenu.PopupMenu contextMenu,
            UnityEvent rebuild, ButtonManagerBasic filterButton,
            ButtonManagerBasic sortButton, ButtonManagerBasic groupButton)
        {
            this.contextMenu = contextMenu;
            this.rebuild = rebuild;
            this.filterButton = filterButton;
            this.sortButton = sortButton;
            this.groupButton = groupButton;

            Filter = new PropertyFilter();
            Sorter = new GameObjectSorter();

            ResetFilter();
            ResetSort();
            ResetGroup();

            this.filterButton.clickEvent.AddListener(ShowFilterMenu);
            this.filterButton.clickEvent.AddListener(() => {
                XRSEEActions.OnSelectToggle = true;
            });
            this.sortButton.clickEvent.AddListener(ShowSortMenu);
            this.sortButton.clickEvent.AddListener(() => {
                XRSEEActions.OnSelectToggle = true;
            });
            this.groupButton.clickEvent.AddListener(ShowGroupMenu);
            this.groupButton.clickEvent.AddListener(() => {
                XRSEEActions.OnSelectToggle = true;
            });
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
                    rebuild.Invoke();
                }, Icons.ArrowRotateLeft, CloseAfterClick: false),
                new PopupMenuAction("Header", () =>
                {
                    Filter.IncludeHeader = !Filter.IncludeHeader;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke();
                }, Checkbox(Filter.IncludeHeader), CloseAfterClick: false),
                new PopupMenuAction(PropertyTypes.ToggleAttributes, () =>
                {
                    Filter.IncludeToggleAttributes = !Filter.IncludeToggleAttributes;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke();
                }, Checkbox(Filter.IncludeToggleAttributes), CloseAfterClick: false),
                new PopupMenuAction(PropertyTypes.StringAttributes, () =>
                {
                    Filter.IncludeStringAttributes = !Filter.IncludeStringAttributes;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke();
                }, Checkbox(Filter.IncludeStringAttributes), CloseAfterClick: false),
                new PopupMenuAction(PropertyTypes.IntAttributes, () =>
                {
                    Filter.IncludeIntAttributes = !Filter.IncludeIntAttributes;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke();
                }, Checkbox(Filter.IncludeIntAttributes), CloseAfterClick: false),
                new PopupMenuAction(PropertyTypes.FloatAttributes, () =>
                {
                    Filter.IncludeFloatAttributes = !Filter.IncludeFloatAttributes;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke();
                }, Checkbox(Filter.IncludeFloatAttributes), CloseAfterClick: false),
            };

            contextMenu.ClearEntries();
            contextMenu.AddEntries(entries);
        }

        /// <summary>
        /// Returns the icon for a checkbox. If <paramref name="value"/> is false,
        /// <see cref="Icons.EmptyCheckbox"/> is returned; otherwise, <see cref="Icons.CheckedCheckbox"/>.
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

        #region Sorter
        /// <summary>
        /// Displays the sort menu.
        /// </summary>
        private void ShowSortMenu()
        {
            UpdateSortMenuEntries();
            contextMenu.ShowWith(position: sortButton.transform.position);
        }

        /// <summary>
        /// Updates the sort menu entries.
        /// </summary>
        private void UpdateSortMenuEntries()
        {
            List<PopupMenuEntry> entries = new()
            {
                new PopupMenuAction("Reset", () =>
                {
                    ResetSort();
                    UpdateSortMenuEntries();
                    rebuild.Invoke();
                }, Icons.ArrowRotateLeft, CloseAfterClick: false),

                new PopupMenuAction("Attribute Name", () =>
                {
                    ToggleSortAction("Name", x => x.FindDescendant("AttributeLine").MustGetComponent<TextMeshProUGUI>().text);
                }, SortIcon(false, Sorter.IsAttributeDescending("Name")), CloseAfterClick: false),

                new PopupMenuAction("Attribute Value", () =>
                {
                    ToggleSortAction("Value", x =>
                    {
                        string text = x.FindDescendant("ValueLine").MustGetComponent<TextMeshProUGUI>().text;
                        if (int.TryParse(text, out int intValue))
                        {
                            return GroupByName ? (float)intValue : intValue;
                        }
                        else if (float.TryParse(text, out float floatValue))
                        {
                            return floatValue;
                        }
                        else
                        {
                            return text;
                        }
                    });
                }, SortIcon(true, Sorter.IsAttributeDescending("Value")), CloseAfterClick: false)
            };

            contextMenu.ClearEntries();
            contextMenu.AddEntries(entries);
            return;

            /// Switch from ascending->descending->none->ascending.
            void ToggleSortAction(string name, Func<GameObject, object> key)
            {
                switch (Sorter.IsAttributeDescending(name))
                {
                    case null:
                        Sorter.AddSortAttribute(name, key, false);
                        break;
                    case false:
                        Sorter.RemoveSortAttribute(name);
                        Sorter.AddSortAttribute(name, key, true);
                        break;
                    default:
                        Sorter.RemoveSortAttribute(name);
                        break;
                }
                UpdateSortMenuEntries();
                rebuild.Invoke();
            }
        }

        /// <summary>
        /// Returns the sort icon depending on whether the attribute is
        /// <paramref name="numeric"/> and whether it is sorted in <paramref name="descending"/> order.
        /// </summary>
        /// <param name="numeric">Whether the attribute is numeric.</param>
        /// <param name="descending">Whether the attribute is sorted in descending order.</param>
        /// <returns>The sort icon depending on the given parameters.</returns>
        private static char SortIcon(bool numeric, bool? descending)
        {
            return (numeric, descending) switch
            {
                (true, null) => Icons.Hashtag,
                (false, null) => Icons.Text,
                (true, true) => Icons.SortNumericDown,
                (true, false) => Icons.SortNumericUp,
                (false, true) => Icons.SortAlphabeticalDown,
                (false, false) => Icons.SortAlphabeticalUp
            };
        }

        /// <summary>
        /// Resets the sorter to its default state.
        /// </summary>
        private void ResetSort()
        {
            Sorter.Reset();
        }
        #endregion

        #region Group
        /// <summary>
        /// Displays the group menu.
        /// </summary>
        private void ShowGroupMenu()
        {
            UpdateGroupMenuEntries();
            contextMenu.ShowWith(position: groupButton.transform.position);
        }

        /// <summary>
        /// Updates the group menu entries.
        /// </summary>
        private void UpdateGroupMenuEntries()
        {
            List<PopupMenuEntry> entries = new()
            {
                new PopupMenuAction("Value Type", () =>
                {
                    ResetGroup();
                    UpdateGroupMenuEntries();
                    rebuild.Invoke();
                }, Radio(!GroupByName), CloseAfterClick: false),
                new PopupMenuAction("Name Type", () =>
                {
                    GroupByName = true;
                    UpdateGroupMenuEntries();
                    rebuild.Invoke();
                }, Radio(GroupByName), CloseAfterClick: false)
            };
            contextMenu.ClearEntries();
            contextMenu.AddEntries(entries);
        }

        /// <summary>
        /// Returns a radio button icon for the given <paramref name="value"/>.
        /// If <paramref name="value"/> is true, <see cref="Icons.CheckedRadio"/> is returned;
        /// otherwise <see cref="Icons.EmptyRadio"/> is returned.
        /// </summary>
        /// <param name="value">Whether the radio button is checked.</param>
        /// <returns>A radio button icon for the given <paramref name="value"/>.</returns>
        private static char Radio(bool value) => value ? Icons.CheckedRadio : Icons.EmptyRadio;

        /// <summary>
        /// Resets the grouper to its default state.
        /// </summary>
        private void ResetGroup()
        {
            GroupByName = false;
        }
        #endregion
    }
}
