using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// The save menu for drawable type objects
    /// </summary>
    public class SaveMenu : SingletonMenu
    {
        /// <summary>
        /// The location where the save menu prefeb is placed.
        /// </summary>
        private const string saveMenuPrefab = "Prefabs/UI/Drawable/SaveMenu";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private SaveMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static SaveMenu Instance { get; private set; }

        static SaveMenu()
        {
            Instance = new SaveMenu();
        }

        /// <summary>
        /// The instance for the save single or more drawable button.
        /// </summary>
        private static ButtonManagerBasic saveButton;

        /// <summary>
        /// The instance for the save all drawables button.
        /// </summary>
        private static ButtonManagerBasic saveAllButton;

        /// <summary>
        /// Creates the save menu.
        /// </summary>
        /// <param name="saveButtonCall">The action that should be executed when the save button is pressed.</param>
        /// <param name="saveAllButtonCall">The action that should be executed when the save all button is pressed.</param>
        public static void Enable(UnityAction saveButtonCall, UnityAction saveAllButtonCall)
        {
            /// Instantiate the menu.
            Instance.Instantiate(saveMenuPrefab);
            saveButton = GameFinder.FindChild(Instance.gameObject, "Save").GetComponent<ButtonManagerBasic>();
            saveAllButton = GameFinder.FindChild(Instance.gameObject, "SaveAll").GetComponent<ButtonManagerBasic>();

            /// Adds a handler for the <paramref name="saveButtonCall"/>.
            saveButton.clickEvent.AddListener(saveButtonCall);
            /// Adds a handler for the <paramref name="saveAllButtonCall"/>.
            saveAllButton.clickEvent.AddListener(saveAllButtonCall);
        }
    }
}
