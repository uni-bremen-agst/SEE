using Cysharp.Threading.Tasks;
using MoreLinq;
using SEE.Controls;
using SEE.Game;
using SEE.Game.City;
using SEE.GameObjects;
using SEE.Net.Actions.RuntimeConfig;
using SEE.UI.Notification;
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
        /// Whether the menu needs a rebuild.
        /// </summary>
        private bool needsRebuild;

        /// <summary>
        /// Indicator of whether opening the menu should be blocked.
        /// This is only the case during the deletion process.
        /// </summary>
        private bool blockOpening;

        /// <summary>
        /// Indicator of whether the menu was built before.
        /// </summary>
        private bool wasBuiltBefore = false;

        /// <summary>
        /// Instantiates the tab menu for each city.
        /// </summary>
        private void Start()
        {
            WaitForLocalPlayerInstantiation().Forget();
            return;

            async UniTask WaitForLocalPlayerInstantiation()
            {
                await UniTask.WaitUntil(() => LocalPlayer.Instance != null)
                    .ContinueWith(() => UniTask.Yield());
                BuildTabMenus();
            }
        }

        /// <summary>
        /// Builds the menu.
        /// </summary>
        public void BuildTabMenus()
        {
            if (wasBuiltBefore)
            {
                ShowNotification.Info("Rebuilding Runtime Config Menu",
                    "The runtime config menu is being rebuilt. This may take a moment. Please wait.");
            }
            wasBuiltBefore = true;

            cityMenus?.ForEach(c => Destroyer.Destroy(c));
            cityMenus = new RuntimeTabMenu[GetCities().Length];
            for (int i = 0; i < GetCities().Length; i++)
            {
                AddCity(i);
            }
            currentMenuCities = GetCities();
            needsRebuild = false;
        }

        /// <summary>
        /// Rebuild the menu based on the current list of available cities.
        /// Only tabs for new cities will be created; existing ones are preserved ans reused.
        /// After reconstruction, the city switcher in each tab is updated.
        /// </summary>
        public void RebuildMenu()
        {
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
                    Destroyer.Destroy(oldMenus[oldIndex]);
                }
            }

            currentMenuCities = newCities;
            UpdateCitySwitcher().Forget();

            static async UniTask UpdateCitySwitcher()
            {
                await UniTask.WaitUntil(() => cityMenus.All(menu => menu != null));
                cityMenus.ForEach(menu => menu.UpdateCitySwitcher());
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
            if (SEEInput.ToggleConfigMenu())
            {
                if (blockOpening)
                {
                    ShowNotification.Warn("Menu opening is blocked.",
                        "A deletion process is in progress, and therefore the menu cannot be opened.");
                    return;
                }
                if (cityMenus.Length <= currentCity)
                {
                    currentCity = cityMenus.Length - 1;
                }
                if (!currentMenuCities.SequenceEqual(GetCities())
                    || needsRebuild)
                {
                    Debug.Log($"currentMenuCities.SequenceEqual(GetCieites()): {currentMenuCities.SequenceEqual(GetCities())}");
                    currentMenuCities.ForEach(c => Debug.Log($"{(c != null ? c.name : null)} is in currentMenuCities"));
                    GetCities().ForEach(c => Debug.Log($"{c.name} is in GetCities()"));
                    new RebuildNetAction().Execute();
                    RebuildMenu();
                }
                cityMenus[currentCity].ToggleMenu();
            }
        }

        /// <summary>
        /// Changes the currently selected table/city.
        /// </summary>
        /// <param name="i">index</param>
        private void SwitchCity(int i)
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
        /// Sets the notification that a rebuild is required.
        /// </summary>
        public void PerformRebuildOnNextOpening()
        {
            blockOpening = false;
            needsRebuild = true;
        }

        /// <summary>
        /// Sets the notification that the opening of the menu is blocked.
        /// </summary>
        public void BlockOpening()
        {
            blockOpening = true;
        }

        /// <summary>
        /// Performs the rebuild if necessary.
        /// </summary>
        /// <returns>True if a rebuild was performed, otherwise false.</returns>
        public bool PerformRebuildIfRequired()
        {
            if (needsRebuild)
            {
                needsRebuild = false;
                RebuildMenu();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Rebuilds the tab at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the tab to rebuild.</param>
        public async UniTask RebuildTabAsync(int index)
        {
            cityMenus[currentCity].ToggleMenu();
            string active = cityMenus[currentCity].ActiveEntry.Title;
            Destroyer.Destroy(cityMenus[index]);
            AddCity(index);
            await UniTask.Yield();
            cityMenus[currentCity].ToggleMenu();
            cityMenus[currentCity].SelectEntry(cityMenus[currentCity]
                .Entries.FirstOrDefault(entry => entry.Title.Equals(active)));
        }
    }
}
