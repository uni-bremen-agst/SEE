using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// The load menu for <see cref="DrawableType"/> objects
    /// </summary>
    public static class LoadMenu
    {
        /// <summary>
        /// The location where the load menu prefeb is placed.
        /// </summary>
        private const string loadMenuPrefab = "Prefabs/UI/Drawable/LoadMenu";

        /// <summary>
        /// The instance for the load menu.
        /// </summary>
        private static GameObject instance;

        /// <summary>
        /// The instance for the regular load drawable button.
        /// </summary>
        private static ButtonManagerBasic loadButton;

        /// <summary>
        /// The instance for the specific load drawables button.
        /// </summary>
        private static ButtonManagerBasic loadSpecificButton;

        /// <summary>
        /// Creates the load menu.
        /// </summary>
        /// <param name="loadButtonCall">The action that should be executed when the load button is pressed.</param>
        /// <param name="loadSpecificButtonCall">The action that should be executed when the load specific button is pressed.</param>
        public static void Enable(UnityAction loadButtonCall, UnityAction loadSpecificButtonCall)
        {
            /// Instantiate the menu.
            instance = PrefabInstantiator.InstantiatePrefab(loadMenuPrefab,
                 GameObject.Find("UI Canvas").transform, false);
            loadButton = GameFinder.FindChild(instance, "Load").GetComponent<ButtonManagerBasic>();
            loadSpecificButton = GameFinder.FindChild(instance, "LoadSpecific").GetComponent<ButtonManagerBasic>();

            /// Adds a handler for the <paramref name="loadButtonCall"/>.
            loadButton.clickEvent.AddListener(loadButtonCall);

            /// Adds a handler for the <param name="loadSpecificButtonCall"/>.
            loadSpecificButton.clickEvent.AddListener(loadSpecificButtonCall);
        }

        /// <summary>
        /// Destroys the menu.
        /// </summary>
        public static void Disable()
        {
            Destroyer.Destroy(instance);
        }
    }
}