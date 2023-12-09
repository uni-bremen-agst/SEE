using System;
using System.Collections.Generic;
using System.Linq;
using Michsky.UI.ModernUIPack;
using SEE.DataModel.DG;
using SEE.DataModel.GraphSearch;
using SEE.Game.City;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI.PopupMenu;
using SEE.Utils;
using UnityEngine;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;
using State = SEE.Tools.ReflexionAnalysis.State;

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
        /// The grouper that is used to group the elements in the tree window.
        /// </summary>
        private readonly TreeWindowGrouper Grouper;

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
        /// The button that opens the group menu.
        /// </summary>
        private readonly ButtonManagerBasic GroupButton;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="contextMenu">The context menu that this class manages.</param>
        /// <param name="searcher">The graph search associated with the tree window.</param>
        /// <param name="grouper">The grouper that is used to group the elements in the tree window.</param>
        /// <param name="rebuild">The function to call to rebuild the tree window.</param>
        /// <param name="filterButton">The button that opens the filter menu.</param>
        /// <param name="sortButton">The button that opens the sort menu.</param>
        /// <param name="groupButton">The button that opens the group menu.</param>
        public TreeWindowContextMenu(PopupMenu.PopupMenu contextMenu, GraphSearch searcher, TreeWindowGrouper grouper,
                                     Action rebuild, ButtonManagerBasic filterButton, ButtonManagerBasic sortButton,
                                     ButtonManagerBasic groupButton)
        {
            ContextMenu = contextMenu;
            Searcher = searcher;
            Grouper = grouper;
            Rebuild = rebuild;
            FilterButton = filterButton;
            SortButton = sortButton;
            GroupButton = groupButton;

            ResetFilter();
            ResetSort();
            ResetGrouping();
            FilterButton.clickEvent.AddListener(ShowFilterMenu);
            SortButton.clickEvent.AddListener(ShowSortMenu);
            GroupButton.clickEvent.AddListener(ShowGroupMenu);
        }

        /// <summary>
        /// Forwards to <see cref="PopupMenu.ShowWith(IEnumerable{PopupMenuEntry},Vector2)"/>.
        /// </summary>
        public void ShowWith(IEnumerable<PopupMenuEntry> entries, Vector2 position) => ContextMenu.ShowWith(entries, position);

        #region Filter menu

        /// <summary>
        /// Displays the filter menu.
        /// </summary>
        private void ShowFilterMenu()
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
        private void ShowSortMenu()
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

            if (Grouper.IsActive)
            {
                entries.Add(new PopupMenuHeading("Grouping is active!"));
                entries.Add(new PopupMenuHeading("Items ordered by group count."));
            }

            // TODO: Any other attributes we want to sort by? Or should we just include all attributes?
            entries.Add(SortActionFor("Source Name", x => x is Node node ? node.SourceName : null, false));
            entries.Add(SortActionFor("Source Line", x => x.SourceLine(), true));
            entries.Add(SortActionFor("Filename", x => x.Filename(), false));
            entries.Add(SortActionFor("Type", x => x.Type, false));

            ContextMenu.ClearEntries();
            ContextMenu.AddEntries(entries);
        }

        /// <summary>
        /// Returns the sort action for the given <paramref name="name"/> and <paramref name="key"/>.
        /// </summary>
        /// <param name="name">The name of the sort attribute.</param>
        /// <param name="key">The key to sort by.</param>
        /// <param name="numeric">Whether this is for a numeric attribute.</param>
        /// <returns>The sort action for the given <paramref name="key"/>.</returns>
        private PopupMenuAction SortActionFor(string name, Func<GraphElement, object> key, bool numeric)
        {
            return new PopupMenuAction(name, ToggleSortAction,
                                       SortIcon(numeric, Searcher.Sorter.IsAttributeDescending(name)),
                                       CloseAfterClick: false);

            void ToggleSortAction()
            {
                // Switch from ascending->descending->none->ascending.
                switch (Searcher.Sorter.IsAttributeDescending(name))
                {
                    case null:
                        Searcher.Sorter.AddSortAttribute(name, key, false);
                        break;
                    case false:
                        Searcher.Sorter.RemoveSortAttribute(name);
                        Searcher.Sorter.AddSortAttribute(name, key, true);
                        break;
                    default:
                        Searcher.Sorter.RemoveSortAttribute(name);
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
            Searcher.Sorter.AddSortAttribute("Source Name", x => x is Node node ? node.SourceName : null, false);
        }

        #endregion

        #region Group menu

        /// <summary>
        /// Displays the group menu.
        /// </summary>
        private void ShowGroupMenu()
        {
            UpdateGroupMenuEntries();
            ContextMenu.ShowWith(position: GroupButton.transform.position);
        }

        /// <summary>
        /// Updates the group menu entries.
        /// </summary>
        private void UpdateGroupMenuEntries()
        {
            ISet<TreeWindowGroup> currentGroups = Grouper.AllGroups.ToHashSet();
            List<PopupMenuEntry> entries = new()
            {
                new PopupMenuAction("None", () =>
                {
                    ResetGrouping();
                    Rebuild();
                    UpdateGroupMenuEntries();
                }, Radio(!Grouper.IsActive), CloseAfterClick: true),
                GroupActionFor("Reflexion State",
                               new TreeWindowGroupAssigment<State?>(Enum.GetValues(typeof(State)).Cast<State?>()
                                                                        .ToDictionary(keySelector: x => x,
                                                                                      elementSelector: ReflexionStateToGroup),
                                                                    element => element is Edge edge ? edge.State() : null)),
                GroupActionFor("Type",
                               new TreeWindowGroupAssigment<string>(Searcher.Graph.AllElementTypes().ToDictionary(x => x, TypeToGroup), element => element.Type)),
                // TODO: Any other useful groups?
            };
            ContextMenu.ClearEntries();
            ContextMenu.AddEntries(entries);
            return;

            // Returns the group action for the given <paramref name="name"/> and <paramref name="assignment"/>.
            PopupMenuAction GroupActionFor(string name, ITreeWindowGroupAssigment assignment)
            {
                return new PopupMenuAction(name, () =>
                                           {
                                               Grouper.Assignment = assignment;
                                               Rebuild();
                                               UpdateGroupMenuEntries();
                                           }, Radio(currentGroups.SetEquals(assignment.AllGroups)),
                                           CloseAfterClick: true);
            }

            // Returns the group for the given <paramref name="state"/>.
            TreeWindowGroup ReflexionStateToGroup(State? state)
            {
                (string text, char icon) = state switch
                {
                    State.Divergent => ("Divergent", Icons.CircleExclamationMark),
                    State.Absent => ("Absent", Icons.CircleMinus),
                    State.Allowed => ("Allowed", Icons.CircleCheckmark),
                    State.Convergent => ("Convergent", Icons.CircleCheckmark),
                    State.ImplicitlyAllowed => ("Implicitly allowed", Icons.CircleCheckmark),
                    State.AllowedAbsent => ("Allowed absent", Icons.CircleCheckmark),
                    State.Specified => ("Specified", Icons.CircleQuestionMark),
                    State.Unmapped => ("Unmapped", Icons.CircleQuestionMark),
                    State.Undefined => ("Undefined", Icons.QuestionMark),
                    null => ("Unknown", Icons.QuestionMark),
                    _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
                };
                (Color start, Color end) = ReflexionVisualization.GetEdgeGradient(state ?? State.Undefined);
                return new TreeWindowGroup(text, icon, start, end);
            }

            TreeWindowGroup TypeToGroup(string type)
            {
                return new TreeWindowGroup(type, Icons.Info, Color.white, Color.white.Darker());
            }
        }

        /// <summary>
        /// Returns a radio button icon for the given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">Whether the radio button is checked.</param>
        /// <returns>A radio button icon for the given <paramref name="value"/>.</returns>
        private static char Radio(bool value) => value ? Icons.CheckedRadio : Icons.EmptyRadio;

        /// <summary>
        /// Resets the grouping to its default state.
        /// </summary>
        private void ResetGrouping()
        {
            Grouper.Reset();
        }

        #endregion
    }
}
