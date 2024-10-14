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
        protected GameObject gameObject;

        /// <summary>
        /// Instantiates the menu by loading the prefab from the given <paramref name="prefabPath"/>
        /// </summary>
        /// <param name="prefabPath">path to the prefab to instantiate</param>
        protected void Instantiate(string prefabPath)
        {
            gameObject = InstantiatePrefab(prefabPath);
        }

        /// <summary>
        /// Returns an instance of the prefab at the given <paramref name="prefabPath"/>.
        /// It will be a child of the <see cref="UICanvas.Canvas"/> in local space.
        /// </summary>
        /// <param name="prefabPath">path to the prefab to instantiate</param>
        /// <returns>instantiated prefab</returns>
        public static GameObject InstantiatePrefab(string prefabPath)
        {
            return PrefabInstantiator.InstantiatePrefab(prefabPath,
                                                        UICanvas.Canvas.transform, false);
        }

        /// <summary>
        /// Returns true if the menu is open.
        /// </summary>
        /// <returns>Whether the menu is open.</returns>
        public virtual bool IsOpen()
        {
            return gameObject != null;
        }

        /// <summary>
        /// Enables the menu.
        /// </summary>
        public virtual void Enable()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Disables the menu.
        /// </summary>
        public virtual void Disable()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Destroys the menu. Unlike <see cref="Disable"/>, the menu cannot
        /// be re-enabled afterward.
        /// </summary>
        public virtual void Destroy()
        {
            if (gameObject != null)
            {
                Destroyer.Destroy(gameObject);
            }
        }
    }
}
