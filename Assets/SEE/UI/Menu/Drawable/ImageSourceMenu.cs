using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// This class provides a menu, with which the player can select 
    /// from which source an image should be load.
    /// </summary>
    public static class ImageSourceMenu
    {
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private const string imageSourceMenuPrefab = "Prefabs/UI/Drawable/ImageSource";

        /// <summary>
        /// The instance for the image source menu.
        /// </summary>
        private static GameObject instance;

        /// <summary>
        /// Whether this class has a source in store that wasn't yet fetched.
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
        /// Enables the image source menu and register the needed Handler to the button's.
        /// </summary>
        public static void Enable()
        {
            if (instance == null)
            {
                instance = PrefabInstantiator.InstantiatePrefab(imageSourceMenuPrefab,
                    GameObject.Find("UI Canvas").transform, false);

                /// Initialize the button for load the image from local disk.
                ButtonManagerBasic local = GameFinder.FindChild(instance, "Local")
                    .GetComponent<ButtonManagerBasic>();
                local.clickEvent.AddListener(() =>
                {
                    gotSource = true;
                    chosenSource = Source.Local;
                    Disable();
                });

                /// Initialize the button for load the image from web.
                ButtonManagerBasic web = GameFinder.FindChild(instance, "Web")
                    .GetComponent<ButtonManagerBasic>();
                web.clickEvent.AddListener(() =>
                {
                    gotSource = true;
                    chosenSource = Source.Web;
                    Disable();
                });

                /// Initialize the button for canceling the menu.
                ButtonManagerBasic cancelBtn = GameFinder.FindChild(instance, "Cancel")
                    .GetComponent<ButtonManagerBasic>();
                cancelBtn.clickEvent.AddListener(() =>
                {
                    Disable();
                });
            }
        }

        /// <summary>
        /// Destroy's the menu.
        /// </summary>
        public static void Disable()
        {
            if (instance != null)
            {
                Destroyer.Destroy(instance);
            }
        }

        /// <summary>
        /// Gets the state, if the menu is opened.
        /// </summary>
        /// <returns>The respective status of whether the menu is open.</returns>
        public static bool IsOpen()
        {
            return instance != null;
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
                Disable();
                return true;
            }

            source = Source.None;
            return false;
        }
    }
}