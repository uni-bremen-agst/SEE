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

            ButtonManagerBasic saveButton =
                GameFinder.FindAttachedOrLocalDescendant(gameObject, "Save")
                    .GetComponent<ButtonManagerBasic>();

            ButtonManagerBasic saveCurrentPageButton =
                GameFinder.FindAttachedOrLocalDescendant(gameObject, "SaveCurrentPage")
                    .GetComponent<ButtonManagerBasic>();

            ButtonManagerBasic saveAllButton =
                GameFinder.FindAttachedOrLocalDescendant(gameObject, "SaveAll")
                    .GetComponent<ButtonManagerBasic>();

            saveButton.clickEvent.AddListener(saveButtonCall);
            saveCurrentPageButton.clickEvent.AddListener(saveCurrentPageButtonCall);
            saveAllButton.clickEvent.AddListener(saveAllButtonCall);
        }
    }
}
