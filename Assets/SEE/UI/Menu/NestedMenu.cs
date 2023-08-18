using System;
using System.Collections.Generic;
using System.Linq;
using FuzzySharp;
using SEE.Controls;
using SEE.GO;
using SEE.GO.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.Windows.Speech;

namespace SEE.UI.Menu
{
    /// <summary>
    /// A nested menu containing a list of <see cref="MenuEntry"/> items.
    /// The difference between this and the generic menu class <see cref="NestedListMenu{T}"/>
    /// is that the type parameter doesn't have to be specified here.
    /// </summary>
    public class NestedListMenu : NestedListMenu<MenuEntry>
    {
        // Intentionally empty, see class documentation.
    }

    /// <summary>
    /// A menu similar to the <see cref="SimpleListMenu"/> with buttons which can open
    /// other menus, i.e., in other words, a container for other menus. In addition
    /// to nested submenus, this class also offers a search over all buttons.
    /// </summary>
    public class NestedListMenu<T> : SimpleListMenu<T> where T : MenuEntry
    {
        /// <summary>
        /// The menu levels we have ascended through.
        /// </summary>
        private readonly Stack<MenuLevel> Levels = new();

        /// <summary>
        /// Path to the NestedMenu prefab.
        /// </summary>
        protected override string MenuPrefab => "Prefabs/UI/NestedMenu";

        /// <summary>
        /// The keyword to be used to step back in the menu verbally.
        /// </summary>
        private const string BackMenuCommand = "go back";

        /// <summary>
        /// The input-field for the fuzzy-search of the nestedMenu.
        /// The prefabs for menus of subclasses may or may not have
        /// this field. If they don't, this field will be null.
        /// </summary>
        private TMP_InputField searchInput;

        /// <summary>
        /// Whether to reset the level of the menu when clicking on the close button.
        /// Can only be changed before this component has been started.
        /// </summary>
        public bool ResetLevelOnClose = true;

        /// <summary>
        /// All leaf-entries of the nestedMenu.
        /// </summary>
        private IDictionary<string, T> AllEntries;

        /// <summary>
        /// True, if the fuzzy-search is active, else false.
        /// </summary>
        private bool searchActive;

        public override void SelectEntry(T entry)
        {
            if (entry is NestedMenuEntry<T> nestedEntry)
            {
                // If this contains another menu level, repopulate list with new level after saving the current one
                AscendLevel(nestedEntry);
            }
            else
            {
                // Otherwise, we do the same we'd do normally
                base.SelectEntry(entry);
            }
        }

        /// <summary>
        /// Appends the <see cref="BackMenuCommand"/> to the keywords.
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<string> GetKeywords()
        {
            return base.GetKeywords().Append(BackMenuCommand);
        }

        protected override void HandleKeyword(PhraseRecognizedEventArgs args)
        {
            if (args.text == BackMenuCommand)
            {
                DescendLevel();
            }
            else
            {
                base.HandleKeyword(args);
            }
        }

        /// <summary>
        /// Ascends up in the menu hierarchy by creating a new level from the given <paramref name="nestedEntry"/>.
        /// </summary>
        /// <param name="nestedEntry">The entry from which to construct the new level</param>
        private void AscendLevel(NestedMenuEntry<T> nestedEntry)
        {
            Levels.Push(new MenuLevel(Title, Description, Icon, Entries));
            while (Entries.Count != 0)
            {
                RemoveEntry(Entries[0]); // Remove all entries
            }
            Title = nestedEntry.Title;
            // TODO: Instead of abusing the description for this, use a proper individual text object
            // (Maybe displaying it above the title in a different color or something would work,
            // as the title is technically the last element in the breadcrumb)
            string breadcrumb = GetBreadcrumb();
            Description = nestedEntry.Description + (breadcrumb.Length > 0 ? $"\n{GetBreadcrumb()}" : "");
            Icon = nestedEntry.Icon;
            nestedEntry.InnerEntries.ForEach(AddEntry);
            KeywordListener.Unregister(HandleKeyword);
            KeywordListener.Register(HandleKeyword);
            MenuTooltip.Hide();
        }

        /// <summary>
        /// Returns a "breadcrumb" for the current level, displaying our current position in the menu hierarchy.
        /// </summary>
        /// <returns>breadcrumb for the current level</returns>
        private string GetBreadcrumb() => string.Join(" / ", Levels.Reverse().Select(x => x.Title));

        /// <summary>
        /// Descends down a level in the menu hierarchy and removes the entry from the <see cref="Levels"/>.
        /// </summary>
        private void DescendLevel()
        {
            if (Levels.Count != 0)
            {
                MenuLevel level = Levels.Pop();
                Title = level.Title;
                Description = level.Description;
                Icon = level.Icon;
                while (Entries.Count != 0)
                {
                    RemoveEntry(Entries[0]); // Remove all entries
                }
                level.Entries.ForEach(AddEntry);
            }
            else
            {
                ShowMenu = false;
            }
            MenuTooltip.Hide();
        }

        /// <summary>
        /// Resets to the lowest level, i.e. resets the menu to the state it was in before any
        /// <see cref="NestedMenuEntry"/> was clicked.
        /// </summary>
        public void ResetToBase()
        {
            if (searchInput)
            {
                searchInput.text = string.Empty;
            }
            while (Levels.Count > 0)
            {
                DescendLevel();
            }
        }

        /// <summary>
        /// Sets <see cref="searchInput"/> if it exists. Registers necessary listeners
        /// to be called when the menu is cancelled or confirmed or when the user
        /// has changed the content of <see cref="searchInput"/>.
        /// </summary>
        protected override void StartDesktop()
        {
            base.StartDesktop();

            Transform searchField = Content.transform.Find("Search Field");
            if (searchField)
            {
                if (searchField.gameObject.TryGetComponentOrLog(out searchInput))
                {
                    searchInput.onValueChanged.AddListener(SearchTextEntered);
                    MenuManager.onCancel.AddListener(() =>
                    {
                        searchActive = false;
                        searchInput.text = string.Empty;
                    });
                }
            }
            MenuManager.onCancel.AddListener(DescendLevel); // Go one level higher when clicking "back"
            if (ResetLevelOnClose)
            {
                // When closing the menu, its level will be reset to the top.
                MenuManager.onConfirm.AddListener(ResetToBase);
            }

            // If the menu is enabled, keyboard shortcuts must be disabled and vice versa.
            OnShowMenuChanged += () => SEEInput.KeyboardShortcutsEnabled = !ShowMenu;
        }

        /// <summary>
        /// Gets all leaf-entries - or rather menuEntries (no nestedMenuEntries) of the nestedMenu.
        /// </summary>
        /// <returns>All leaf-entries of the nestedMenu.</returns>
        private IEnumerable<T> GetAllEntries()
        {
            IList<T> allEntries = Levels.LastOrDefault()?.Entries ?? Entries;
            return GetAllEntries(allEntries);
        }

        /// <summary>
        /// Searchs through the complete tree of the nestedMenu and selects all MenuEntries.
        /// </summary>
        /// <param name="startingEntries">the entries to research.</param>
        /// <returns>All leafEntries of the nestedMenu.</returns>
        private static IEnumerable<T> GetAllEntries(IEnumerable<T> startingEntries)
        {
            List<T> leafEntries = new();
            foreach (T startingEntry in startingEntries)
            {
                if (startingEntry is NestedMenuEntry<T> nestedMenuEntry)
                {
                    leafEntries.AddRange(GetAllEntries(nestedMenuEntry.InnerEntries));
                }
                else
                {
                    leafEntries.Add(startingEntry);
                }
            }

            return leafEntries;
        }

        /// <summary>
        /// The action which is called by typing inside of the fuzzy-search input field.
        /// Displays all results of the fuzzySearch inside of the menu ordered by matching-specifity.
        /// </summary>
        /// <param name="text">the text inside of the fuzzy-search.</param>
        private void SearchTextEntered(string text)
        {
            if (searchActive)
            {
                DescendLevel();
            }

            searchActive = text.Length != 0;
            if (text.Length == 0)
            {
                return;
            }

            AllEntries ??= GetAllEntries().ToDictionary(x => x.Title, x => x);
            IEnumerable<T> results = Process.ExtractTop(SearchMenu.FilterString(text), AllEntries.Keys, cutoff: 10)
                                            .OrderByDescending(x => x.Score)
                                            .Select(x => AllEntries[x.Value])
                                            .ToList();

            NestedMenuEntry<T> resultEntry = new(results, "Results", $"Found {results.Count()} help pages.",
                                                 default, default, Resources.Load<Sprite>("Materials/Notification/info"));
            AscendLevel(resultEntry);
        }

        /// <summary>
        /// A state of this menu representing a nesting level established by selecting a <see cref="NestedMenuEntry"/>.
        /// Contains everything necessary to restore the menu to this state.
        /// </summary>
        private class MenuLevel
        {
            public readonly string Title;
            public readonly string Description;
            public readonly Sprite Icon;
            public readonly List<T> Entries;

            public MenuLevel(string title, string description, Sprite icon, IList<T> entries)
            {
                Title = title ?? throw new ArgumentNullException(nameof(title));
                Description = description ?? throw new ArgumentNullException(nameof(description));
                Icon = icon;
                Entries = entries != null ? new List<T>(entries) : throw new ArgumentNullException(nameof(entries));
            }
        }
    }
}
