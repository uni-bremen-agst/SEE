using Michsky.MUIP;
using SEE.Game.Drawable;
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
        private static ButtonManager saveButton;

        /// <summary>
        /// The instance for the save current page button.
        /// </summary>
        private static ButtonManager saveCurrentPageButton;

        /// <summary>
        /// The instance for the save all drawables button.
        /// </summary>
        private static ButtonManager saveAllButton;

        /// <summary>
        /// Creates the save menu.
        /// </summary>
        /// <param name="saveButtonCall">The action that should be executed when the save button is pressed.</param>
        /// <param name="saveCurrentPageButtonCall">The action that should be executed when the save current page button is pressed.</param>
        /// <param name="saveAllButtonCall">The action that should be executed when the save all button is pressed.</param>
        public static void Enable(UnityAction saveButtonCall,
                                  UnityAction saveCurrentPageButtonCall,
                                  UnityAction saveAllButtonCall)
        {
            /// Instantiate the menu.
            Instance.Instantiate(saveMenuPrefab);
            saveButton = GameFinder.FindAttachedOrLocalDescendant(Instance.gameObject, "Save").GetComponent<ButtonManager>();
            saveCurrentPageButton = GameFinder.FindAttachedOrLocalDescendant(Instance.gameObject, "SaveCurrentPage")
                .GetComponent<ButtonManager>();
            saveAllButton = GameFinder.FindAttachedOrLocalDescendant(Instance.gameObject, "SaveAll").GetComponent<ButtonManager>();

            /// Adds a handler for the <paramref name="saveButtonCall"/>.
            saveButton.onClick.AddListener(saveButtonCall);
            /// Adds a handler for the <paramref name="saveCurrentPageButtonCall"/>.
            saveCurrentPageButton.onClick.AddListener(saveCurrentPageButtonCall);
            /// Adds a handler for the <paramref name="saveAllButtonCall"/>.
            saveAllButton.onClick.AddListener(saveAllButtonCall);
        }
    }
}
