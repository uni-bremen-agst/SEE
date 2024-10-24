using System.Linq;
using SEE.Controls;
using SEE.Game;
using SEE.Game.City;
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
        /// Instantiates the tab menu for each city.
        /// </summary>
        private void Start()
        {
            int cityCount = GameObject.FindGameObjectsWithTag(Tags.CodeCity).Length;
            cityMenus = new RuntimeTabMenu[cityCount];
            for (int i = 0; i < cityCount; i++)
            {
                cityMenus[i] = gameObject.AddComponent<RuntimeTabMenu>();
                cityMenus[i].Title = "City Configuration";
                cityMenus[i].HideAfterSelection = false;
                cityMenus[i].CityIndex = i;
                cityMenus[i].OnSwitchCity += SwitchCity;
            }
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
        /// <returns>table list</returns>
        public static AbstractSEECity[] GetCities()
        {
            return GameObject.FindGameObjectsWithTag(Tags.CodeCity).Select(go => go.GetComponent<AbstractSEECity>())
                             .Where(component => component != null)
                             .OrderBy(go => go.name).ToArray();
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
    }
}
