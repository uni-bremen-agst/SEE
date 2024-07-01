using Michsky.UI.ModernUIPack;
using SEE.UI.PopupMenu;
using System;
using System.Collections.Generic;
using UnityEngine;
using SEE.Utils;
using UnityEngine.Events;
using SEE.Game.Drawable;
using SEE.UI.Window.TreeWindow;

namespace Assets.SEE.UI.Window.DrawableManagerWindow
{
    /// <summary>
    /// Manages the context menu for the drawable manager view.
    /// </summary>
    public class DrawableWindowContextMenu
    {
        /// <summary>
        /// The context menu that this class manages.
        /// </summary>
        private readonly PopupMenu contextMenu;

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
        public readonly DrawableSurfaceFilter filter;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="contextMenu">The context menu that this class manages.</param>
        /// <param name="rebuild">The function to call to rebuild the drawable manager window.</param>
        /// <param name="filterButton">The button that opens the filter menu.</param>
        /// <param name="sortButton">The button that opens the sort menu.</param>
        /// <param name="groupButton">The button that opens the group menu.</param>
        public DrawableWindowContextMenu(PopupMenu contextMenu,
                                     UnityEvent<List<GameObject>> rebuild, ButtonManagerBasic filterButton, ButtonManagerBasic sortButton,
                                     ButtonManagerBasic groupButton)
        {
            this.contextMenu = contextMenu;
            this.rebuild = rebuild;
            this.filterButton = filterButton;
            this.sortButton = sortButton;
            this.groupButton = groupButton;
            filter = new DrawableSurfaceFilter();

            ResetFilter();
            //ResetSort();
            ResetGrouping();
            this.filterButton.clickEvent.AddListener(ShowFilterMenu);
            //this.sortButton.clickEvent.AddListener(ShowSortMenu);
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
                    rebuild.Invoke(filter.GetFilteredSurfaces());
                }, Icons.ArrowRotateLeft, CloseAfterClick: false),
                new PopupMenuAction("Whiteboards", () =>
                {
                    filter.IncludeWhiteboards = !filter.IncludeWhiteboards;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke(filter.GetFilteredSurfaces());
                }, Checkbox(filter.IncludeWhiteboards), CloseAfterClick: false),
                new PopupMenuAction("Sticky Notes", () =>
                {
                    filter.IncludeStickyNotes = !filter.IncludeStickyNotes;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke(filter.GetFilteredSurfaces());
                }, Checkbox(filter.IncludeStickyNotes), CloseAfterClick: false),
                new PopupMenuAction("Have Description", () =>
                {
                    filter.IncludeHaveDescription = !filter.IncludeHaveDescription;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke(filter.GetFilteredSurfaces());
                }, Checkbox(filter.IncludeHaveDescription), CloseAfterClick: false),
                new PopupMenuAction("Have no Description", () =>
                {
                    filter.IncludeHaveNoDescription = !filter.IncludeHaveNoDescription;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke(filter.GetFilteredSurfaces());
                }, Checkbox(filter.IncludeHaveNoDescription), CloseAfterClick: false),
                new PopupMenuAction("Have Lighting", () =>
                {
                    filter.IncludeHaveLighting = !filter.IncludeHaveLighting;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke(filter.GetFilteredSurfaces());
                }, Checkbox(filter.IncludeHaveLighting), CloseAfterClick: false),
                new PopupMenuAction("Have no Lighting", () =>
                {
                    filter.IncludeHaveNoLighting = !filter.IncludeHaveNoLighting;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke(filter.GetFilteredSurfaces());
                }, Checkbox(filter.IncludeHaveNoLighting), CloseAfterClick: false),
                new PopupMenuAction("Is Visible", () =>
                {
                    filter.IncludeIsVisible = !filter.IncludeIsVisible;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke(filter.GetFilteredSurfaces());
                }, Checkbox(filter.IncludeIsVisible), CloseAfterClick: false),
                new PopupMenuAction("Is Invisible", () =>
                {
                    filter.IncludeIsInvisibile = !filter.IncludeIsInvisibile;
                    UpdateFilterMenuEntries();
                    rebuild.Invoke(filter.GetFilteredSurfaces());
                }, Checkbox(filter.IncludeIsInvisibile), CloseAfterClick: false),
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
            filter.Reset();
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
            //ISet<DrawableWindowGroup> currentGroups = grouper.AllGroups.ToHashSet();
            List<PopupMenuEntry> entries = new()
            {
                new PopupMenuAction("None", () =>
                {
                    ResetGrouping();
                    rebuild.Invoke(filter.GetFilteredSurfaces());
                }, Radio(true/*!grouper.IsActive*/), CloseAfterClick: false),
                new PopupMenuAction("Surface Type", () =>
                {
                    //TODO
                }, Radio(false), CloseAfterClick: false)
            };
            contextMenu.ClearEntries();
            contextMenu.AddEntries(entries);
            return;

            /// Returns the group action for the given 
            ///TODO
            //PopupMenuAction GroupActionFor(string name, )
        }

        /// <summary>
        /// Returns a radio button icon for the given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">Whether the radio button is checked.</param>
        /// <returns>A radio button icon for the given <paramref name="value"/>.</returns>
        private static char Radio(bool value) => value ? Icons.CheckedRadio : Icons.EmptyRadio;

        private void ResetGrouping()
        {
            // TODO Reset
        }
        #endregion

        #region Sort menu

        #endregion
    }
}