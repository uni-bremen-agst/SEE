using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FuzzySharp;
using SEE.Controls;
using SEE.DataModel.DG.GraphSearch;
using SEE.GO;
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
        /// The menu levels we have descended through.
        /// </summary>
        private readonly Stack<MenuLevel> levels = new();

        /// <summary>
        /// The current menu level.
        /// </summary>
        public int CurrentLevel => levels.Count();

        /// <summary>
        /// Path to the NestedMenu prefab.
        /// </summary>
        protected override string MenuPrefab => "Prefabs/UI/NestedMenu";

        /// <summary>
        /// The keyword to be used to step back in the menu verbally.
        /// </summary>
        private const string backMenuCommand = "go back";

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
        public bool ResetLevelOnClose = false;

        /// <summary>
        /// All leaf-entries of the nestedMenu.
        /// </summary>
        private IDictionary<string, T> allEntries;

        /// <summary>
        /// True, if the fuzzy-search is active, else false.
        /// </summary>
        private bool searchActive;

        /// <summary>
        /// A semaphore to prevent multiple searches from happening at the same time.
        /// </summary>
        private readonly SemaphoreSlim searchSemaphore = new(1, 1);

        /// <summary>
        /// True, if the search-input is focused, else false.
        /// </summary>
        public bool IsSearchFocused => searchInput.isFocused;

        public override void SelectEntry(T entry)
        {
            if (entry is NestedMenuEntry<T> nestedEntry)
            {
                // If this contains another menu level, repopulate list with new level after saving the current one
                DescendLevel(nestedEntry);
            }
            else
            {
                // Otherwise, we do the same we'd do normally
                base.SelectEntry(entry);
            }
        }

        /// <summary>
        /// Appends the <see cref="backMenuCommand"/> to the keywords.
        /// </summary>
        /// <returns>The titles the menu should listen to.</returns>
        protected override IEnumerable<string> GetKeywords()
        {
            return base.GetKeywords().Append(backMenuCommand);
        }

        protected override void HandleKeyword(PhraseRecognizedEventArgs args)
        {
            if (args.text == backMenuCommand)
            {
                AscendLevel();
            }
            else
            {
                base.HandleKeyword(args);
            }
        }

        /// <summary>
        /// Descends down in the menu hierarchy by creating a new level from the given <paramref name="nestedEntry"/>.
        /// </summary>
        /// <param name="nestedEntry">The entry from which to construct the new level.</param>
        /// <param name="withBreadcrumb">Whether to include a breadcrumb indicating the hierarchy
        /// in the description of the newly descended level.</param>
        private void DescendLevel(NestedMenuEntry<T> nestedEntry, bool withBreadcrumb = true)
        {
            levels.Push(new MenuLevel(Title, Description, Icon, Entries));
            while (Entries.Count != 0)
            {
                RemoveEntry(Entries[0]); // Remove all entries
            }
            Title = nestedEntry.Title;
            // TODO: Instead of abusing the description for this, use a proper individual text object
            // (Maybe displaying it above the title in a different color or something would work,
            // as the title is technically the last element in the breadcrumb)
            string breadcrumb = withBreadcrumb ? GetBreadcrumb() : string.Empty;
            Description = nestedEntry.Description + (breadcrumb.Length > 0 ? $"\n{GetBreadcrumb()}" : "");
            Icon = nestedEntry.MenuIconSprite;
            nestedEntry.InnerEntries.ForEach(AddEntry);
            /// The null check must be performed due to the <see cref="DrawableActionBar">.
            /// When switching to a drawable action via the bar without opening the menu, the KeywordListener is null.
            if (KeywordListener != null)
            {
                KeywordListener.Unregister(HandleKeyword);
                KeywordListener.Register(HandleKeyword);
            }
            Tooltip.Deactivate();
        }

        /// <summary>
        /// Returns a "breadcrumb" for the current level, displaying our current position in the menu hierarchy.
        /// </summary>
        /// <returns>Breadcrumb for the current level.</returns>
        private string GetBreadcrumb() => string.Join(" / ", levels.Reverse().Select(x => x.Title));

        /// <summary>
        /// Ascends up a level in the menu hierarchy and removes the current level from the <see cref="levels"/>.
        /// </summary>
        /// <param name="exitOnEmpty">Whether to exit the menu when the top level is reached.</param>
        private void AscendLevel(bool exitOnEmpty = true)
        {
            if (levels.Count != 0)
            {
                MenuLevel level = levels.Pop();
                Title = level.Title;
                Description = level.Description;
                Icon = level.Icon;
                while (Entries.Count != 0)
                {
                    RemoveEntry(Entries[0]); // Remove all entries
                }
                level.Entries.ForEach(AddEntry);
            }
            else if (exitOnEmpty)
            {
                ShowMenu = false;
            }
            Tooltip.Deactivate();
        }

        /// <summary>
        /// Resets to the highest level, i.e. resets the menu to the state it was in before any
        /// <see cref="NestedMenuEntry"/> was clicked.
        /// </summary>
        public void ResetToBase()
        {
            if (searchInput)
            {
                searchInput.text = string.Empty;
            }
            while (levels.Count > 0)
            {
                AscendLevel();
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
                    searchInput.onValueChanged.AddListener(_ => SearchTextEnteredAsync().Forget());
                    MenuManager.onCancel.AddListener(() =>
                    {
                        searchActive = false;
                        searchInput.text = string.Empty;
                    });
                }
            }
            else
            {
                Debug.LogWarning("Search field must be present in the prefab for the nested menu to work properly.");
            }
            MenuManager.onCancel.AddListener(() => AscendLevel()); // Go one level higher when clicking "back"
            if (ResetLevelOnClose)
            {
                // When closing the menu, its level will be reset to the top.
                MenuManager.onConfirm.AddListener(ResetToBase);
            }

            OnShowMenuChanged += () =>
            {
                // If the menu is enabled, keyboard shortcuts must be disabled and vice versa.
                SEEInput.KeyboardShortcutsEnabled = !ShowMenu;
                // Additionally, if the menu is disabled, the search input must be cleared
                // and the level must be reset to the top.
                if (!ShowMenu)
                {
                    if (searchInput)
                    {
                        searchInput.text = string.Empty;
                    }
                }
            };
        }

        /// <summary>
        /// Gets all leaf-entries - or rather menuEntries (no nestedMenuEntries) of the nestedMenu.
        /// </summary>
        /// <returns>All leaf-entries of the nestedMenu.</returns>
        private IEnumerable<T> GetAllEntries()
        {
            IList<T> allMenuEntries = levels.LastOrDefault()?.Entries ?? Entries;
            return GetAllEntries(allMenuEntries);
        }

        /// <summary>
        /// Searches through the complete tree of the nestedMenu and selects all MenuEntries.
        /// </summary>
        /// <param name="startingEntries">The entries to research.</param>
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
        /// Displays all results of the fuzzySearch inside of the menu ordered by matching-specificity.
        /// </summary>
        /// <remarks>
        /// This method is async because the menu may need an additional frame to reset properly.
        /// </remarks>
        private async UniTaskVoid SearchTextEnteredAsync()
        {
            if (!await searchSemaphore.WaitAsync(0))
            {
                // If the semaphore is locked, a search is already in progress.
                return;
            }

            try
            {
                if (searchActive)
                {
                    AscendLevel(exitOnEmpty: false);
                    // We need to wait for the next frame to ensure that the level has been reset properly.
                    await UniTask.WaitForEndOfFrame();
                }

                if (!ShowMenu)
                {
                    // If the menu has been hidden in the meantime, we don't want to search.
                    return;
                }

                searchActive = searchInput.text.Length != 0;
                if (searchInput.text.Length == 0)
                {
                    return;
                }

                allEntries ??= GetAllEntries().ToDictionary(x => x.Title, x => x);
                IEnumerable<T> results = Process.ExtractTop(GraphSearch.FilterString(searchInput.text), allEntries.Keys, cutoff: 10)
                                                .OrderByDescending(x => x.Score)
                                                .Select(x => allEntries[x.Value])
                                                .ToList();

                NestedMenuEntry<T> resultEntry = new(results, Title, Description, menuIconSprite: Icon);
                DescendLevel(resultEntry, withBreadcrumb: false);
            }
            finally
            {
                searchSemaphore.Release();
            }
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
