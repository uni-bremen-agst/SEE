using System;
using System.Collections.Generic;
using System.Linq;
using Michsky.UI.ModernUIPack;
using SEE.DataModel.DG;
using SEE.DataModel.GraphSearch;
using SEE.UI.PopupMenu;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Window.TreeWindow
{
    /// <summary>
    /// Manages the context menu for the tree window.
    /// </summary>
    public class TreeWindowContextMenu
    {
        /// <summary>
        /// The context menu that this class manages.
        /// </summary>
        private readonly PopupMenu.PopupMenu ContextMenu;

        /// <summary>
        /// The graph search associated with the tree window.
        /// We also retrieve the graph from this.
        /// </summary>
        private readonly GraphSearch Searcher;

        /// <summary>
        /// The function to call to rebuild the tree window.
        /// </summary>
        private readonly Action Rebuild;

        /// <summary>
        /// The button that opens the filter menu.
        /// </summary>
        private readonly ButtonManagerBasic FilterButton;

        /// <summary>
        /// The button that opens the sort menu.
        /// </summary>
        private readonly ButtonManagerBasic SortButton;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="contextMenu">The context menu that this class manages.</param>
        /// <param name="searcher">The graph search associated with the tree window.</param>
        /// <param name="rebuild">The function to call to rebuild the tree window.</param>
        /// <param name="filterButton">The button that opens the filter menu.</param>
        /// <param name="sortButton">The button that opens the sort menu.</param>
        public TreeWindowContextMenu(PopupMenu.PopupMenu contextMenu, GraphSearch searcher, Action rebuild,
                                     ButtonManagerBasic filterButton, ButtonManagerBasic sortButton)
        {
            ContextMenu = contextMenu;
            Searcher = searcher;
            Rebuild = rebuild;
            FilterButton = filterButton;
            SortButton = sortButton;

            ResetFilter();
            ResetSort();
            FilterButton.clickEvent.AddListener(ShowFilterMenu);
            SortButton.clickEvent.AddListener(ShowSortMenu);
        }

        /// <summary>
        /// Forwards to <see cref="PopupMenu.ShowWith(IEnumerable{PopupMenuEntry},Vector2)"/>.
        /// </summary>
        public void ShowWith(IEnumerable<PopupMenuEntry> entries, Vector2 position) => ContextMenu.ShowWith(entries, position);

        #region Filter menu

        /// <summary>
        /// Displays the filter menu.
        /// </summary>
        public void ShowFilterMenu()
        {
            UpdateFilterMenuEntries();
            ContextMenu.ShowWith(position: FilterButton.transform.position);
        }

        /// <summary>
        /// Updates the filter menu entries.
        /// </summary>
        private void UpdateFilterMenuEntries()
        {
            ISet<string> nodeToggles = Searcher.Graph.AllToggleNodeAttributes();
            ISet<string> edgeToggles = Searcher.Graph.AllToggleEdgeAttributes();
            ISet<string> commonToggles = nodeToggles.Intersect(edgeToggles).ToHashSet();
            // Don't include common toggles in node/edge toggles.
            nodeToggles.ExceptWith(commonToggles);
            edgeToggles.ExceptWith(commonToggles);
            // TODO: Allow filtering by node type.

            List<PopupMenuEntry> entries = new()
            {
                new PopupMenuAction("Reset", () =>
                {
                    ResetFilter();
                    UpdateFilterMenuEntries();
                    Rebuild();
                }, Icons.ArrowRotateLeft, CloseAfterClick: false),
                new PopupMenuAction("Edges",
                                    () =>
                                    {
                                        Searcher.Filter.IncludeEdges = !Searcher.Filter.IncludeEdges;
                                        UpdateFilterMenuEntries();
                                        Rebuild();
                                    },
                                    Checkbox(Searcher.Filter.IncludeEdges), CloseAfterClick: false),
            };

            if (Searcher.Filter.ExcludeElements.Count > 0)
            {
                entries.Insert(0, new PopupMenuAction("Show hidden elements",
                                                      () =>
                                                      {
                                                          Searcher.Filter.ExcludeElements.Clear();
                                                          Rebuild();
                                                      },
                                                      Icons.Show));
            }

            if (commonToggles.Count > 0)
            {
                entries.Add(new PopupMenuHeading("Common properties"));
                entries.AddRange(commonToggles.Select(FilterActionFor));
            }
            if (nodeToggles.Count > 0)
            {
                entries.Add(new PopupMenuHeading("Node properties"));
                entries.AddRange(nodeToggles.Select(FilterActionFor));
            }
            if (edgeToggles.Count > 0)
            {
                entries.Add(new PopupMenuHeading("Edge properties"));
                entries.AddRange(edgeToggles.Select(FilterActionFor));
            }

            ContextMenu.ClearEntries();
            ContextMenu.AddEntries(entries);
        }

        /// <summary>
        /// Returns the filter action for the given <paramref name="toggleAttribute"/>.
        /// </summary>
        /// <param name="toggleAttribute">The toggle attribute to create a filter action for.</param>
        /// <returns>The filter action for the given <paramref name="toggleAttribute"/>.</returns>
        private PopupMenuAction FilterActionFor(string toggleAttribute)
        {
            return new PopupMenuAction(toggleAttribute, ToggleFilterAction,
                                       Searcher.Filter.ExcludeToggleAttributes.Contains(toggleAttribute)
                                           ? Icons.MinusCheckbox
                                           : Checkbox(Searcher.Filter.IncludeToggleAttributes.Contains(toggleAttribute)),
                                       CloseAfterClick: false);

            void ToggleFilterAction()
            {
                // Toggle from include->exclude->none->include.
                if (Searcher.Filter.IncludeToggleAttributes.Contains(toggleAttribute))
                {
                    Searcher.Filter.IncludeToggleAttributes.Remove(toggleAttribute);
                    Searcher.Filter.ExcludeToggleAttributes.Add(toggleAttribute);
                }
                else if (Searcher.Filter.ExcludeToggleAttributes.Contains(toggleAttribute))
                {
                    Searcher.Filter.ExcludeToggleAttributes.Remove(toggleAttribute);
                }
                else
                {
                    Searcher.Filter.IncludeToggleAttributes.Add(toggleAttribute);
                }
                UpdateFilterMenuEntries();
                Rebuild();
            }
        }

        /// <summary>
        /// Resets the filter to its default state.
        /// </summary>
        private void ResetFilter()
        {
            Searcher.Filter.Reset();
        }

        /// <summary>
        /// Returns the icon for a checkbox.
        /// </summary>
        /// <param name="value">Whether the checkbox is checked.</param>
        /// <returns>The icon for a checkbox.</returns>
        private static char Checkbox(bool value) => value ? Icons.CheckedCheckbox : Icons.EmptyCheckbox;

        #endregion

        #region Sort menu

        /// <summary>
        /// Displays the sort menu.
        /// </summary>
        public void ShowSortMenu()
        {
            UpdateSortMenuEntries();
            ContextMenu.ShowWith(position: SortButton.transform.position);
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
                    Rebuild();
                }, Icons.ArrowRotateLeft, CloseAfterClick: false)
            };

            // TODO: Add all attributes, or only pre-selected common ones?
            entries.AddRange(Searcher.Graph.AllNumericAttributes().Select(attribute => SortActionFor(attribute, numeric: true)));
            entries.AddRange(Searcher.Graph.AllStringAttributes().Select(attribute => SortActionFor(attribute, numeric: false)));

            ContextMenu.ClearEntries();
            ContextMenu.AddEntries(entries);
        }

        /// <summary>
        /// Returns the sort action for the given <paramref name="attribute"/>.
        /// </summary>
        /// <param name="attribute">The attribute to create a sort action for.</param>
        /// <param name="numeric">Whether the attribute name is for a numeric attribute.</param>
        /// <returns>The sort action for the given <paramref name="attribute"/>.</returns>
        private PopupMenuAction SortActionFor(string attribute, bool numeric)
        {
            return new PopupMenuAction(attribute, ToggleSortAction,
                                       SortIcon(numeric, Searcher.Sorter.IsAttributeDescending(attribute)),
                                       CloseAfterClick: false);

            void ToggleSortAction()
            {
                // Switch from ascending->descending->none->ascending.
                switch (Searcher.Sorter.IsAttributeDescending(attribute))
                {
                    case null: Searcher.Sorter.SortAttributes.Add((attribute, false));
                        break;
                    case false:
                        Searcher.Sorter.SortAttributes.Remove((attribute, false));
                        Searcher.Sorter.SortAttributes.Add((attribute, true));
                        break;
                    default: Searcher.Sorter.SortAttributes.Remove((attribute, true));
                        break;
                }
                UpdateSortMenuEntries();
                Rebuild();
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
                (_, null) => ' ',
                (true, true) => Icons.SortNumericDown,
                (true, false) => Icons.SortNumericUp,
                (false, true) => Icons.SortAlphabeticalDown,
                (false, false) => Icons.SortAlphabeticalUp
            };
        }

        /// <summary>
        /// Resets the sort to its default state.
        /// </summary>
        private void ResetSort()
        {
            Searcher.Sorter.Reset();
            Searcher.Sorter.SortAttributes.Add((Node.SourceNameAttribute, false));
        }

        #endregion
    }
}
