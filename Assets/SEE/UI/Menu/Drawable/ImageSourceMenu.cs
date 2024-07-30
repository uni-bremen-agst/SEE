using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// This class provides a menu, with which the player can select
    /// from which source an image should be loaded.
    /// </summary>
    public class ImageSourceMenu : Menu
    {
        /// <summary>
        /// The location where the menu prefab is placed.
        /// </summary>
        private const string imageSourceMenuPrefab = "Prefabs/UI/Drawable/ImageSource";

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        private readonly static ImageSourceMenu instance;

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private ImageSourceMenu() { }

        static ImageSourceMenu()
        {
            instance = new ImageSourceMenu();
        }

        /// <summary>
        /// Whether this class has a source in store that hasn't been fetched yet.
        /// </summary>
        private static bool gotSource;

        /// <summary>
        /// If <see cref="gotSource"/> is true, this contains the source which the player selected.
        /// </summary>
        private static Source chosenSource;

        /// <summary>
        /// The different sources
        /// </summary>
        public enum Source
        {
            None,
            Local,
            Web
        }

        /// <summary>
        /// Enables the image source menu and registers the needed handler to the buttons.
        /// </summary>
        public static void EnableMenu()
        {
            if (instance.menu == null)
            {
                instance.Instantiate(imageSourceMenuPrefab);

                /// Initialize the button for loading the image from local disk.
                ButtonManagerBasic local = GameFinder.FindChild(instance.menu, "Local")
                    .GetComponent<ButtonManagerBasic>();
                local.clickEvent.AddListener(() =>
                {
                    gotSource = true;
                    chosenSource = Source.Local;
                    DisableMenu();
                });

                /// Initialize the button for loading the image from the web.
                ButtonManagerBasic web = GameFinder.FindChild(instance.menu, "Web")
                    .GetComponent<ButtonManagerBasic>();
                web.clickEvent.AddListener(() =>
                {
                    gotSource = true;
                    chosenSource = Source.Web;
                    DisableMenu();
                });

                /// Initialize the button for canceling the menu.
                ButtonManagerBasic cancelBtn = GameFinder.FindChild(instance.menu, "Cancel")
                    .GetComponent<ButtonManagerBasic>();
                cancelBtn.clickEvent.AddListener(() =>
                {
                    DisableMenu();
                });
            }
        }

        /// <summary>
        /// Destroys the menu.
        /// </summary>
        public static void DisableMenu()
        {
            instance.Destroy();
        }

        /// <summary>
        /// Gets the state, if the menu is opened.
        /// </summary>
        /// <returns>The respective status of whether the menu is open.</returns>
        public static bool IsMenuOpen()
        {
            return instance.IsOpen();
        }

        /// <summary>
        /// If <see cref="gotSource"/> is true, the <paramref name="source"/> will be the chosen source by the
        /// player. Otherwise it will be some dummy value.
        /// </summary>
        /// <param name="source">The source the player confirmed, if that doesn't exist, some dummy value</param>
        /// <returns><see cref="gotSource"/></returns>
        public static bool TryGetSource(out Source source)
        {
            if (gotSource)
            {
                source = chosenSource;
                gotSource = false;
                DisableMenu();
                return true;
            }

            source = Source.None;
            return false;
        }
    }
}