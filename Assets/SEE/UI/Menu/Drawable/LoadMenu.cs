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
        /// Creates the load menu and registers the required button handlers.
        /// </summary>
        /// <param name="loadButtonCall">
        /// The action that should be executed when the load button is pressed.
        /// </param>
        /// <param name="loadSpecificButtonCall">
        /// The action that should be executed when the load-specific button is pressed.
        /// </param>
        /// <param name="loadSpecificCurrentPageButtonCall">
        /// The action that should be executed when loading onto the current page is pressed.
        /// </param>
        public void Enable(UnityAction loadButtonCall,
                           UnityAction loadSpecificButtonCall,
                           UnityAction loadSpecificCurrentPageButtonCall)
        {
            Instantiate(loadMenuPrefab);

            ButtonManagerBasic loadButton =
                GameFinder.FindAttachedOrLocalDescendant(gameObject, "Load")
                    .GetComponent<ButtonManagerBasic>();

            ButtonManagerBasic loadSpecificButton =
                GameFinder.FindAttachedOrLocalDescendant(gameObject, "LoadSpecific")
                    .GetComponent<ButtonManagerBasic>();

            ButtonManagerBasic loadSpecificCurrentPageButton =
                GameFinder.FindAttachedOrLocalDescendant(gameObject, "LoadSpecificCurrentPage")
                    .GetComponent<ButtonManagerBasic>();

            loadButton.clickEvent.AddListener(loadButtonCall);
            loadSpecificButton.clickEvent.AddListener(loadSpecificButtonCall);
            loadSpecificCurrentPageButton.clickEvent.AddListener(
                loadSpecificCurrentPageButtonCall);
        }
    }
}
