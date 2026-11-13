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
        /// Creates the save menu and registers the required button handlers.
        /// </summary>
        /// <param name="saveButtonCall">
        /// The action that should be executed when the save button is pressed.
        /// </param>
        /// <param name="saveCurrentPageButtonCall">
        /// The action that should be executed when the save-current-page button is pressed.
        /// </param>
        /// <param name="saveAllButtonCall">
        /// The action that should be executed when the save-all button is pressed.
        /// </param>
        public void Enable(UnityAction saveButtonCall,
                           UnityAction saveCurrentPageButtonCall,
                           UnityAction saveAllButtonCall)
        {
            Instantiate(saveMenuPrefab);

            ButtonManager saveButton =
                GameFinder.FindAttachedOrLocalDescendant(gameObject, "Save")
                    .GetComponent<ButtonManager>();

            ButtonManager saveCurrentPageButton =
                GameFinder.FindAttachedOrLocalDescendant(gameObject, "SaveCurrentPage")
                    .GetComponent<ButtonManager>();

            ButtonManager saveAllButton =
                GameFinder.FindAttachedOrLocalDescendant(gameObject, "SaveAll")
                    .GetComponent<ButtonManager>();

            saveButton.onClick.AddListener(saveButtonCall);
            saveCurrentPageButton.onClick.AddListener(saveCurrentPageButtonCall);
            saveAllButton.onClick.AddListener(saveAllButtonCall);
        }
    }
}
