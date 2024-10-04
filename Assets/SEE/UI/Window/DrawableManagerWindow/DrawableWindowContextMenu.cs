using Michsky.UI.ModernUIPack;
using SEE.UI.PopupMenu;
using System;
using System.Collections.Generic;
using UnityEngine;
using SEE.Utils;
using UnityEngine.Events;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.UI.Menu.Drawable;

namespace SEE.UI.Window.DrawableManagerWindow
{
    /// <summary>
    /// Manages the context menu for the drawable manager view.
    /// </summary>
    public class DrawableWindowContextMenu
    {
        /// <summary>
        /// The context menu that this class manages.
        /// </summary>
        private readonly PopupMenu.PopupMenu contextMenu;

        /// <summary>
        /// The function to call to rebuild the tree window.
        /// </summary>
        private readonly UnityEvent<List<GameObject>> rebuild;

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
        /// The drawable surface filter.
        /// </summary>
        public readonly DrawableSurfaceFilter Filter;

        /// <summary>
        /// The drawable surface grouper.
        /// </summary>
        public readonly DrawableWindowGrouper Grouper;

        /// <summary>
        /// The drawable surface sorter.
        /// </summary>
        public readonly GameObjectSorter Sorter;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="contextMenu">The context menu that this class manages.</param>
        /// <param name="rebuild">The function to call to rebuild the drawable manager window.</param>
        /// <param name="filterButton">The button that opens the filter menu.</param>
        /// <param name="sortButton">The button that opens the sort menu.</param>
        /// <param name="groupButton">The button that opens the group menu.</param>
        public DrawableWindowContextMenu(PopupMenu.PopupMenu contextMenu,
                                     UnityEvent<List<GameObject>> rebuild, ButtonManagerBasic filterButton, ButtonManagerBasic sortButton,
                                     ButtonManagerBasic groupButton)
        {
            this.contextMenu = contextMenu;
            this.rebuild = rebuild;
            this.filterButton = filterButton;
            this.sortButton = sortButton;
            this.groupButton = groupButton;
            Filter = new DrawableSurfaceFilter();
            Grouper = new DrawableWindowGrouper();
            Sorter = new GameObjectSorter();

            ResetFilter();
            ResetSort();
            ResetGrouping();
            this.filterButton.clickEvent.AddListener(ShowFilterMenu);
            this.sortButton.clickEvent.AddListener(ShowSortMenu);
            this.groupButton.clickEvent.AddListener(ShowGroupMenu);
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
                new PopupMenuAction("Reset", () =>
                {
                    ResetFilter();
                    UpdateFilterMenuEntries();
                    rebuild.Invoke(Filter.GetFilteredSurfaces());
                }, Icons.ArrowRotateLeft, CloseAfterClick: false),
                new PopupMenuAction("Whiteboards", () =>
                {
                    Filter.IncludeWhiteboards = !Filter.IncludeWhiteboards;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke(Filter.GetFilteredSurfaces());
                }, Checkbox(Filter.IncludeWhiteboards), CloseAfterClick: false),
                new PopupMenuAction("Sticky Notes", () =>
                {
                    Filter.IncludeStickyNotes = !Filter.IncludeStickyNotes;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke(Filter.GetFilteredSurfaces());
                }, Checkbox(Filter.IncludeStickyNotes), CloseAfterClick: false),
                new PopupMenuAction("Have Description", () =>
                {
                    Filter.IncludeHaveDescription = !Filter.IncludeHaveDescription;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke(Filter.GetFilteredSurfaces());
                }, Checkbox(Filter.IncludeHaveDescription), CloseAfterClick: false),
                new PopupMenuAction("Have no Description", () =>
                {
                    Filter.IncludeHaveNoDescription = !Filter.IncludeHaveNoDescription;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke(Filter.GetFilteredSurfaces());
                }, Checkbox(Filter.IncludeHaveNoDescription), CloseAfterClick: false),
                new PopupMenuAction("Have Lighting", () =>
                {
                    Filter.IncludeHaveLighting = !Filter.IncludeHaveLighting;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke(Filter.GetFilteredSurfaces());
                }, Checkbox(Filter.IncludeHaveLighting), CloseAfterClick: false),
                new PopupMenuAction("Have no Lighting", () =>
                {
                    Filter.IncludeHaveNoLighting = !Filter.IncludeHaveNoLighting;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke(Filter.GetFilteredSurfaces());
                }, Checkbox(Filter.IncludeHaveNoLighting), CloseAfterClick: false),
                new PopupMenuAction("Is Visible", () =>
                {
                    Filter.IncludeIsVisible = !Filter.IncludeIsVisible;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke(Filter.GetFilteredSurfaces());
                }, Checkbox(Filter.IncludeIsVisible), CloseAfterClick: false),
                new PopupMenuAction("Is Invisible", () =>
                {
                    Filter.IncludeIsInvisibile = !Filter.IncludeIsInvisibile;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke(Filter.GetFilteredSurfaces());
                }, Checkbox(Filter.IncludeIsInvisibile), CloseAfterClick: false),
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

        #region Group menu
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
                new PopupMenuAction("None", () =>
                {
                    ResetGrouping();
                    UpdateGroupMenuEntries();
                    rebuild.Invoke(Filter.GetFilteredSurfaces());
                }, Radio(!Grouper.IsActive), CloseAfterClick: false),
                new PopupMenuAction("Surface Type", () =>
                {
                    Grouper.IsActive = true;
                    UpdateGroupMenuEntries();
                    rebuild.Invoke(Filter.GetFilteredSurfaces());
                }, Radio(Grouper.IsActive), CloseAfterClick: false)
            };
            contextMenu.ClearEntries();
            contextMenu.AddEntries(entries);
        }

        /// <summary>
        /// Returns a radio button icon for the given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">Whether the radio button is checked.</param>
        /// <returns>A radio button icon for the given <paramref name="value"/>.</returns>
        private static char Radio(bool value) => value ? Icons.CheckedRadio : Icons.EmptyRadio;

        /// <summary>
        /// Resets <see cref="Grouper"/>.
        /// </summary>
        private void ResetGrouping()
        {
            Grouper.Reset();
        }
        #endregion

        #region Sort menu
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
                    rebuild.Invoke(Filter.GetFilteredSurfaces());
                }, Icons.ArrowRotateLeft, CloseAfterClick: false)
            };
            if (Grouper.IsActive)
            {
                entries.Add(new PopupMenuHeading("Grouping is active!"));
                entries.Add(new PopupMenuHeading("Items ordered by group count."));
            }
            else
            {
                entries.Add(new PopupMenuAction("Surface Type", () =>
                {
                    ToggleSortAction("Type", x => GameFinder.IsStickyNote(x) ? 0 : GameFinder.IsWhiteboard(x) ? 1 : 2);
                }, SortIcon(false, Sorter.IsAttributeDescending("Type")), CloseAfterClick: false));

                entries.Add(new PopupMenuAction("Name", () =>
                {
                    ToggleSortAction("Name", x => GameFinder.GetUniqueID(x));
                }, SortIcon(false, Sorter.IsAttributeDescending("Name")), CloseAfterClick: false));

                entries.Add(new PopupMenuAction("Description", () =>
                {
                    ToggleSortAction("Description", x => GameDrawableManager.GetDescription(x));
                }, SortIcon(false, Sorter.IsAttributeDescending("Description")), CloseAfterClick: false));

                entries.Add(new PopupMenuAction("Lighting", () =>
                {
                    ToggleSortAction("Lighting", x => GameDrawableManager.IsLighting(x));
                }, SortIcon(false, Sorter.IsAttributeDescending("Lighting")), CloseAfterClick: false));

                entries.Add(new PopupMenuAction("Visibility", () =>
                {
                    ToggleSortAction("Visibility", x => GameDrawableManager.IsVisible(x));
                }, SortIcon(false, Sorter.IsAttributeDescending("Visibility")), CloseAfterClick: false));
            }

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
                rebuild.Invoke(Filter.GetFilteredSurfaces());
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
        /// Resets the <see cref="Sorter"/> to its default state.
        /// </summary>
        private void ResetSort()
        {
            Sorter.Reset();
        }
        #endregion

        #region Page
        /// <summary>
        /// Displays the selection and add-page menu.
        /// </summary>
        /// <param name="surface">The surface whose pages are to be managed.</param>
        /// <param name="position">The position where the popup menu should be displayed.</param>
        public void ShowSelectionAddPageMenu(GameObject surface, Vector3 position)
        {
            UpdatePageMenuEntries(surface);
            contextMenu.ShowWith(position: position);
        }

        /// <summary>
        /// Displays the remove-page menu.
        /// </summary>
        /// <param name="surface">The surface whose pages are to be managed.</param>
        /// <param name="position">The position where the popup menu should be displayed.</param>
        public void ShowRemovePageMenu(GameObject surface, Vector3 position)
        {
            UpdatePageMenuEntries(surface, true);
            contextMenu.ShowWith(position: position);
        }

        /// <summary>
        /// Updates the selection and add-menu entries.
        /// </summary>
        /// <param name="surface">The surface whose pages are to be managed.</param>
        /// <param name="removeIndicator">Wheter the remove option should be displayed.</param>
        private void UpdatePageMenuEntries(GameObject surface, bool removeIndicator = false)
        {
            DrawableHolder holder = surface.GetComponent<DrawableHolder>();
            List<PopupMenuEntry> entries = new();

            List<int> pages = new();
            for (int i = 0; i < holder.MaxPageSize; i++)
            {
                pages.Add(i);
            }
            PopupMenuHeading header = removeIndicator ?
                new PopupMenuHeading("Remove") : new PopupMenuHeading("Select / Add");
            entries.Add(header);
            entries.AddRange(pages.Select(CreatePopupEntries));

            if (!removeIndicator)
            {
                entries.Add(new PopupMenuAction("+", () =>
                {
                    holder.MaxPageSize++;
                    new SynchronizeSurface(DrawableConfigManager.GetDrawableConfig(surface)).Execute();
                    UpdatePageMenuEntries(surface);
                }, ' ', CloseAfterClick: false));
            }
            contextMenu.ClearEntries();
            contextMenu.AddEntries(entries);

            return;

            /// <summary>
            /// Creates a <see cref="PopupMenuAction"/> for each page.
            /// </summary>
            /// <param name="pageNumber">The page number</param>
            /// <returns>The created <see cref="PopupMenuAction"/> for the page number entry.</returns>
            PopupMenuAction CreatePopupEntries(int pageNumber)
            {
                return new PopupMenuAction(pageNumber.ToString(),
                    () =>
                    {
                        if (removeIndicator)
                        {
                            RemovePage(pageNumber);
                        }
                        else
                        {
                            SetPage(pageNumber);
                        }
                    },
                    GetIcon(pageNumber), CloseAfterClick: true); ;
            }

            /// <summary>
            /// Gets the depending icon for the entry.
            /// </summary>
            /// <param name="i">The entry number.</param>
            /// <returns>The corresponding icon.</returns>
            char GetIcon(int i)
            {
                return removeIndicator ? Icons.Trash
                    : i == holder.CurrentPage ? Icons.CheckedRadio : Icons.EmptyRadio;
            }

            /// Sets the page.
            void SetPage(int page)
            {
                GameDrawableManager.ChangeCurrentPage(surface, page);
                new SynchronizeSurface(DrawableConfigManager.GetDrawableConfig(surface)).Execute();
            }

            /// Removes the page.
            void RemovePage(int page)
            {
                if (GameFinder.GetDrawableTypesOfPage(surface, page).Count > 0)
                {
                    ConfirmDialogMenu confirm = new($"Do you really want to delete the page {page}?\r\nThis action cannot be undone.");
                    confirm.ExecuteAfterConfirmAsync(() =>
                    {
                        GameDrawableManager.RemovePage(surface, page);
                        new SurfaceRemovePageNetAction(DrawableConfigManager.GetDrawableConfig(surface), page).Execute();
                    }).Forget();
                }
                else
                {
                    GameDrawableManager.RemovePage(surface, page);
                    new SurfaceRemovePageNetAction(DrawableConfigManager.GetDrawableConfig(surface), page).Execute();
                }
            }
        }
        #endregion
    }
}
