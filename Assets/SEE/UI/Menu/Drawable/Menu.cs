using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// The base class for all menus dealing with a <see cref="Drawable"/>.
    /// </summary>
    public abstract class Menu
    {
        /// <summary>
        /// The instance of the menu.
        /// </summary>
        protected GameObject menu;

        /// <summary>
        /// Instantiates the menu by loading the prefab from the given <paramref name="prefabPath"/>
        /// </summary>
        /// <param name="prefabPath">path to the prefab to instantiate</param>
        protected void Instantiate(string prefabPath)
        {
            menu = InstantiatePrefab(prefabPath);
        }

        /// <summary>
        /// Returns an instance of the prefab at the given <paramref name="prefabPath"/>.
        /// </summary>
        /// <param name="prefabPath">path to the prefab to instantiate</param>
        /// <returns>instantiated prefab</returns>
        protected static GameObject InstantiatePrefab(string prefabPath)
        {
            return PrefabInstantiator.InstantiatePrefab(prefabPath,
                                                        UICanvas.Canvas.transform, false);
        }

        /// <summary>
        /// Returns true if the menu is open.
        /// </summary>
        /// <returns>Whether the menu is open.</returns>
        public bool IsOpen()
        {
            return menu != null;
        }

        /// <summary>
        /// Enables the menu.
        /// </summary>
        protected void Enable()
        {
            menu.SetActive(true);
        }

        /// <summary>
        /// Disables the menu.
        /// </summary>
        protected void Disable()
        {
            menu.SetActive(false);
        }

        /// <summary>
        /// Destroys the menu. Unlike <see cref="Disable"/>, the menu cannot
        /// be re-enabled afterward.
        /// </summary>
        public void Destroy()
        {
            if (menu != null)
            {
                Destroyer.Destroy(menu);
            }
        }
    }
}