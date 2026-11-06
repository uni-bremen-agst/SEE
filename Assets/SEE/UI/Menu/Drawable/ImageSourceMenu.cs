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
        /// Whether a source has been selected but not yet consumed.
        /// </summary>
        private bool gotSource;

        /// <summary>
        /// The selected image source.
        /// </summary>
        private Source chosenSource = Source.None;

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
        /// Enables the image source menu and registers the required button handlers.
        /// </summary>
        public override void Enable()
        {
            if (gameObject != null)
            {
                return;
            }

            gotSource = false;
            chosenSource = Source.None;

            Instantiate(imageSourceMenuPrefab);

            ButtonManagerBasic local =
                GameFinder.FindAttachedOrLocalDescendant(gameObject, "Local")
                    .GetComponent<ButtonManagerBasic>();

            local.clickEvent.AddListener(() =>
            {
                SelectSource(Source.Local);
            });

            ButtonManagerBasic web =
                GameFinder.FindAttachedOrLocalDescendant(gameObject, "Web")
                    .GetComponent<ButtonManagerBasic>();

            web.clickEvent.AddListener(() =>
            {
                SelectSource(Source.Web);
            });

            ButtonManagerBasic cancelButton =
                GameFinder.FindAttachedOrLocalDescendant(gameObject, "Cancel")
                    .GetComponent<ButtonManagerBasic>();

            cancelButton.clickEvent.AddListener(Destroy);
        }

        /// <summary>
        /// Stores the selected source until it is consumed by the image action
        /// and hides the source menu.
        /// </summary>
        /// <param name="source">The selected image source.</param>
        private void SelectSource(Source source)
        {
            chosenSource = source;
            gotSource = true;

            Disable();
        }

        /// <summary>
        /// Returns the selected image source if one is waiting to be consumed.
        /// </summary>
        /// <param name="source">
        /// The selected source, or <see cref="Source.None"/> if none is available.
        /// </param>
        /// <returns>Whether a source was available.</returns>
        public bool TryGetSource(out Source source)
        {
            if (!gotSource)
            {
                source = Source.None;
                return false;
            }

            source = chosenSource;

            gotSource = false;
            chosenSource = Source.None;

            Destroy();
            return true;
        }

        /// <summary>
        /// Destroys the menu and discards any pending source selection.
        /// </summary>
        public override void Destroy()
        {
            base.Destroy();

            gotSource = false;
            chosenSource = Source.None;
        }
    }
}