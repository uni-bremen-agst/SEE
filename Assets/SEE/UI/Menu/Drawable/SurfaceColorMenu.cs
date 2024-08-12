using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// This class provides a menu with which the player can select
    /// from which source an image should be loaded.
    /// </summary>
    public class SurfaceColorMenu : SingletonMenu
    {
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private const string surfaceColorMenuPrefab = "Prefabs/UI/Drawable/SurfaceColorMenu";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private SurfaceColorMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static SurfaceColorMenu Instance { get; private set; }

        static SurfaceColorMenu()
        {
            Instance = new SurfaceColorMenu();
        }

        /// <summary>
        /// Enables the surface color menu and registers the needed handlers to the buttons.
        /// </summary>
        /// <param name="surface">The surface whose color should be changed.</param>
        /// <param name="colorAction">An action which changes the color palette icon.</param>
        public void Enable(GameObject surface, UnityAction<Color> colorAction)
        {
            if (Instance.gameObject == null)
            {
                Instantiate(surfaceColorMenuPrefab);

                string name = !string.IsNullOrEmpty(GameFinder.GetDrawableSurfaceParentName(surface)) ?
                    GameFinder.GetDrawableSurfaceParentName(surface) : surface.name;
                GameFinder.FindChild(Instance.gameObject, "Text").GetComponent<TextMeshProUGUI>().text = "Change Color:\n" + name;

                HSVPicker.ColorPicker picker = Instance.gameObject.GetComponentInChildren<HSVPicker.ColorPicker>();
                picker.AssignColor(DrawableConfigManager.GetDrawableConfig(surface).Color);
                picker.onValueChanged.AddListener(color =>
                {
                    GameDrawableManager.ChangeColor(surface, color);
                    new DrawableChangeColorNetAction(DrawableConfigManager.GetDrawableConfig(surface)).Execute();
                    colorAction.Invoke(color);
                });

                /// Initialize the button for canceling the menu.
                ButtonManagerBasic cancelBtn = GameFinder.FindChild(Instance.gameObject, "Cancel")
                            .GetComponent<ButtonManagerBasic>();
                cancelBtn.clickEvent.AddListener(() =>
                {
                    Disable();
                });
            }
        }
    }
}
