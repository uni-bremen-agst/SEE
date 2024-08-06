using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// This class provides a menu, with which the player can select
    /// from which source an image should be loaded.
    /// </summary>
    public class ImageSourceMenu : SingletonMenu
    {
        /// <summary>
        /// The location where the menu prefab is placed.
        /// </summary>
        private const string imageSourceMenuPrefab = "Prefabs/UI/Drawable/ImageSource";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private ImageSourceMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static ImageSourceMenu Instance { get; private set; }

        static ImageSourceMenu()
        {
            Instance = new ImageSourceMenu();
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
            if (Instance.gameObject == null)
            {
                Instance.Instantiate(imageSourceMenuPrefab);

                /// Initialize the button for loading the image from local disk.
                ButtonManagerBasic local = GameFinder.FindChild(Instance.gameObject, "Local")
                    .GetComponent<ButtonManagerBasic>();
                local.clickEvent.AddListener(() =>
                {
                    gotSource = true;
                    chosenSource = Source.Local;
                    Instance.Destroy();
                });

                /// Initialize the button for loading the image from the web.
                ButtonManagerBasic web = GameFinder.FindChild(Instance.gameObject, "Web")
                    .GetComponent<ButtonManagerBasic>();
                web.clickEvent.AddListener(() =>
                {
                    gotSource = true;
                    chosenSource = Source.Web;
                    Instance.Destroy();
                });

                /// Initialize the button for canceling the menu.
                ButtonManagerBasic cancelBtn = GameFinder.FindChild(Instance.gameObject, "Cancel")
                    .GetComponent<ButtonManagerBasic>();
                cancelBtn.clickEvent.AddListener(() =>
                {
                    Instance.Destroy();
                });
            }
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
                Instance.Destroy();
                return true;
            }

            source = Source.None;
            return false;
        }
    }
}