using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// The load menu for <see cref="DrawableType"/> objects.
    /// </summary>
    public class LoadMenu : SingletonMenu
    {
        /// <summary>
        /// The location where the load menu prefeb is placed.
        /// </summary>
        private const string loadMenuPrefab = "Prefabs/UI/Drawable/LoadMenu";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private LoadMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static LoadMenu Instance { get; private set; }

        static LoadMenu()
        {
            Instance = new LoadMenu();
        }

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
            Instance = new LoadMenu();
            Instance.Instantiate(loadMenuPrefab);
            loadButton = GameFinder.FindChild(Instance.gameObject, "Load").GetComponent<ButtonManagerBasic>();
            loadSpecificButton = GameFinder.FindChild(Instance.gameObject, "LoadSpecific").GetComponent<ButtonManagerBasic>();

            /// Adds a handler for the <paramref name="loadButtonCall"/>.
            loadButton.clickEvent.AddListener(loadButtonCall);

            /// Adds a handler for the <param name="loadSpecificButtonCall"/>.
            loadSpecificButton.clickEvent.AddListener(loadSpecificButtonCall);
        }
    }
}
