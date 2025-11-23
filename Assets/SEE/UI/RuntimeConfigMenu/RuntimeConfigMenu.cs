using Cysharp.Threading.Tasks;
using MoreLinq;
using SEE.Controls;
using SEE.Game;
using SEE.Game.City;
using SEE.GameObjects;
using SEE.Utils;
using System;
using System.Linq;
using UnityEngine;

namespace SEE.UI.RuntimeConfigMenu
{
    /// <summary>
    /// The primary wrapper script for the <see cref="RuntimeTabMenu"/>.
    /// The runtime config menu allows to configure a <see cref="AbstractSEECity"/> at runtime.
    ///
    /// Instantiates the <see cref="RuntimeTabMenu"/>s for each table and handles switching between the tables.
    /// </summary>
    public class RuntimeConfigMenu : MonoBehaviour
    {
        /// <summary>
        /// Contains the menus for each table/city.
        /// </summary>
        private static RuntimeTabMenu[] cityMenus;

        /// <summary>
        /// The index of the currently selected table.
        /// </summary>
        private int currentCity;

        /// <summary>
        /// The list of cities on which the current RuntimeConfigMenu is based.
        /// </summary>
        private AbstractSEECity[] currentMenuCities;

        /// <summary>
        /// Indicator whether the menu has already been built and is ready for use.
        /// </summary>
        private bool menuReady = false;

        /// <summary>
        /// Instantiates the tab menu for each city.
        /// </summary>
        private void Start()
        {
            WaitForLocalPlayerInstantiation().Forget();
            return;

            async UniTask WaitForLocalPlayerInstantiation()
            {
                await UniTask.WaitUntil(() => LocalPlayer.Instance != null
                    && LocalPlayer.TryGetCitiesHolder(out CitiesHolder citiesHolder)
                    && citiesHolder.Cities.Count > 0)
                    .ContinueWith(() => UniTask.DelayFrame(1));
                BuildTabMenus();
            }
        }

        /// <summary>
        /// Builds the menu.
        /// </summary>
        public void BuildTabMenus()
        {
            cityMenus?.ForEach(c => Destroyer.Destroy(c));
            cityMenus = new RuntimeTabMenu[GetCities().Length];
            for (int i = 0; i < GetCities().Length; i++)
            {
                AddCity(i);
            }
            currentMenuCities = GetCities();
            menuReady = true;
        }

        /// <summary>
        /// Rebuild the menu based on the current list of available cities.
        /// Only tabs for new cities will be created; existing ones are preserved and reused.
        /// After reconstruction, the city switcher in each tab is updated.
        /// </summary>
        public async UniTask RebuildMenuAsync()
        {
            // Wait one frame to ensure all pending city deletions are complete to avoid redundant rebuilds.
            await UniTask.DelayFrame(1);
            RuntimeTabMenu currentTab = cityMenus[currentCity];
            int index = currentTab.CityIndex;

            bool isOpen = currentTab.ShowMenu;
            if (isOpen)
            {
                currentTab.ShowMenu = false;
            }

            RuntimeTabMenu[] oldMenus = cityMenus;
            cityMenus = new RuntimeTabMenu[GetCities().Length];
            AbstractSEECity[] newCities = GetCities();

            for (int i = 0; i < newCities.Length; i++)
            {
                int oldIndex = Array.FindIndex(currentMenuCities, city => city.Equals(newCities[i]));

                if (oldIndex >= 0)
                {
                    cityMenus[i] = oldMenus[oldIndex];
                    cityMenus[i].CityIndex = i;
                }
                else
                {
                    AddCity(i);
                }
            }

            foreach (AbstractSEECity oldCity in currentMenuCities)
            {
                if (!newCities.Any(city => city.Equals(oldCity)))
                {
                    int oldIndex = Array.IndexOf(currentMenuCities, oldCity);
                    if (oldIndex == currentCity && currentCity > 0)
                    {
                        currentCity--;
                    }
                    Destroyer.Destroy(oldMenus[oldIndex]);
                }
            }

            currentMenuCities = newCities;
            await UniTask.WaitUntil(() => cityMenus.All(menu => menu != null));
            cityMenus.ForEach(menu => menu.UpdateCitySwitcher());
            if (currentTab != null && index != currentTab.CityIndex)
            {
                currentCity = currentTab.CityIndex;
            }
            menuReady = true;

            if (isOpen)
            {
                if (currentTab != null)
                {
                    currentTab.ShowMenu = true;
                }
                else
                {
                    cityMenus[currentCity].ToggleMenu();
                }
            }
        }

        /// <summary>
        /// Adds a <see cref="RuntimeTabMenu"/> for the city with the given <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The city index.</param>
        private void AddCity(int index)
        {
            cityMenus[index] = gameObject.AddComponent<RuntimeTabMenu>();
            cityMenus[index].Title = "City Configuration";
            cityMenus[index].HideAfterSelection = false;
            cityMenus[index].CityIndex = index;
            cityMenus[index].OnSwitchCity += SwitchCity;
        }

        /// <summary>
        /// Opens and closes the menu.
        ///
        /// <seealso cref="SEEInput"/>
        /// </summary>
        private void Update()
        {
            if (menuReady && !currentMenuCities.SequenceEqual(GetCities()))
            {
                menuReady = false;
                RebuildMenuAsync().Forget();
            }
            if (SEEInput.ToggleConfigMenu())
            {
                if (cityMenus.Length <= currentCity)
                {
                    currentCity = cityMenus.Length - 1;
                }
                cityMenus[currentCity].ToggleMenu();
            }
        }

        /// <summary>
        /// Changes the currently selected table/city.
        /// </summary>
        /// <param name="i">index</param>
        internal void SwitchCity(int i)
        {
            if (i == currentCity)
            {
                return;
            }
            cityMenus[currentCity].ShowMenu = false;
            cityMenus[i].ShowMenu = true;
            currentCity = i;
        }

        /// <summary>
        /// Returns a sorted list of all tables/cities.
        ///
        /// Sorted by the game object name.
        /// </summary>
        /// <returns>the sorted cities or null if this menu does not have a
        /// <see cref="CitiesHolder"/> attached to it</returns>
        public static AbstractSEECity[] GetCities()
        {
            if (LocalPlayer.TryGetCitiesHolder(out CitiesHolder citiesHolder))
            {
                return citiesHolder.Cities
                    .Select(pair => pair.Value.GetComponent<AbstractSEECity>())
                    .Where(component => component != null)
                    .OrderBy(go => go.name).ToArray();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the menu of a table/city by index.
        /// </summary>
        /// <param name="i">city index</param>
        /// <returns>table menu</returns>
        public static RuntimeTabMenu GetMenuForCity(int i)
        {
            return cityMenus[i];
        }

        /// <summary>
        /// Returns the tab menu index of the given <paramref name="city"/>.
        /// </summary>
        /// <param name="city">The city whose tab menu index to return.</param>
        /// <returns>The index of the city if found; otherwise, -1.</returns>
        public int GetIndexForCity(AbstractSEECity city)
        {
            return Array.FindIndex(currentMenuCities, c => c.Equals(city));
        }

        /// <summary>
        /// Performs a tab rebuild for the given <paramref name="city"/>.
        /// </summary>
        /// <param name="city">The city whose tab should be rebuilt.</param>
        public void PerformTabRebuild(AbstractSEECity city)
        {
            RebuildTabAsync(GetIndexForCity(city)).Forget();
        }

        /// <summary>
        /// Simple record to capture the state of an active tab.
        /// This is used to preserve and restore the tab state during a rebuild.
        /// </summary>
        /// <param name="ActiveEntry">The title of the currently active entry.</param>
        /// <param name="Scroll">The current scroll position.</param>
        private record TabStateSnapshot(string ActiveEntry, float Scroll);

        /// <summary>
        /// Rebuilds the tab at the specified <paramref name="index"/>
        /// by destroying and recreating it.
        /// If the tab is currently active, the method preserves its state by:
        /// <list type="bullet">
        ///   <item><description>storing and restoring the current scroll position,</description></item>
        ///   <item><description>reopening the menu if it was previously open,</description></item>
        ///   <item><description>and reselecting the previously active entry.</description></item>
        /// </list>
        /// </summary>
        /// <param name="index">The index of the tab to rebuild.</param>
        public async UniTask RebuildTabAsync(int index)
        {
            // Ensure index is valid
            if (index < 0 || index >= cityMenus.Length)
            {
                return;
            }

            // Check if the tab to rebuild is the currently active city tab
            bool isCurrentCity = index == currentCity;

            // If this is the current city tab but the menu is not open,
            // hide any small editor that is currently shown.
            if (isCurrentCity && !cityMenus[currentCity].ShowMenu && cityMenus[currentCity].IsSmallEditorWindowOpen)
            {
                cityMenus[currentCity].SmallEditorOpener.ShowMenu = false;
            }

            // Capture the current tab state if it's active
            TabStateSnapshot state = null;
            if (isCurrentCity && cityMenus[currentCity].ShowMenu)
            {
                // Save scroll position and active entry
                state = CaptureTabState(cityMenus[currentCity]);
                cityMenus[currentCity].ToggleMenu();
            }

            // Destroy and recreate the tab
            Destroyer.Destroy(cityMenus[index]);
            AddCity(index);

            // Restore previous state if tab was active
            if (state != null)
            {
                await UniTask.Yield();
                cityMenus[currentCity].ToggleMenu();
                // Restore selected entry and scroll position
                await RestoreTabState(cityMenus[currentCity], state);
            }
            return;

            static TabStateSnapshot CaptureTabState(RuntimeTabMenu menu)
            {
                float scrollValue = menu.Content
                                .GetComponentInChildren<ContentSizeWatcher>()
                                .CurrentScrollValue;
                string active = menu.ActiveEntry.Title;
                return new TabStateSnapshot(active, scrollValue);
            }

            static async UniTask RestoreTabState(RuntimeTabMenu menu, TabStateSnapshot state)
            {
                // Select the previously active entry
                menu.SelectEntry(menu.Entries.FirstOrDefault(e => e.Title.Equals(state.ActiveEntry)));

                // Restore the scroll position asynchronously
                await menu.Content
                    .GetComponentInChildren<ContentSizeWatcher>()
                    .ApplyPreviousScrollPositionAsync(state.Scroll);
            }
        }

        /// <summary>
        /// Performs an immediate update for the specified <paramref name="city"/>,
        /// if it exists within the valid index range.
        /// If a small editor window is currently open for this city, it is temporarily closed
        /// and reopened after the update, but only if the corresponding object still exists.
        /// </summary>
        /// <param name="city">The city for which the update should be performed.</param>
        public void PerformUpdate(AbstractSEECity city)
        {
            int index = GetIndexForCity(city);
            if (index < 0 || index >= cityMenus.Length)
            {
                return;
            }

            RuntimeSmallEditorButton smallEditorToReopen = null;
            if (index == currentCity && cityMenus[index].IsSmallEditorWindowOpen)
            {
                smallEditorToReopen = cityMenus[index].SmallEditorOpener;
                cityMenus[index].SmallEditorOpener.ShowMenu = false;
            }
            cityMenus[index].ImmediateUpdate();
            if (smallEditorToReopen != null)
            {
                TryReopenSmallEditor(smallEditorToReopen).Forget();
            }

            static async UniTask TryReopenSmallEditor(RuntimeSmallEditorButton smallBtn)
            {
                await UniTask.DelayFrame(1);
                if (smallBtn != null)
                {
                    smallBtn.ShowMenu = true;
                }
            }
        }
    }
}
