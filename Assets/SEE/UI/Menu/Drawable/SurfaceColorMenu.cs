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
    /// This class provides a menu, with which the player can select 
    /// from which source an image should be load.
    /// </summary>
    public static class SurfaceColorMenu
    {
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private const string surfaceColorMenuPrefab = "Prefabs/UI/Drawable/SurfaceColorMenu";

        /// <summary>
        /// The instance for the surface color menu.
        /// </summary>
        private static GameObject instance;

        /// <summary>
        /// Enables the surface color menu and register the needed Handler to the button's.
        /// </summary>
        /// <param name="surface">The surface which color should be changed.</param>
        /// <param name="colorAction">An action which changes the color palette icon.</param>
        public static void Enable(GameObject surface, UnityAction<Color> colorAction)
        {
            if (instance == null)
            {
                instance = PrefabInstantiator.InstantiatePrefab(surfaceColorMenuPrefab,
                    GameObject.Find("UI Canvas").transform, false);

                string name = !string.IsNullOrEmpty(GameFinder.GetDrawableSurfaceParentName(surface)) ? 
                    GameFinder.GetDrawableSurfaceParentName(surface) : surface.name;
                GameFinder.FindChild(instance, "Text").GetComponent<TextMeshProUGUI>().text = "Change Color:\n" + name;

                HSVPicker.ColorPicker picker = instance.GetComponentInChildren<HSVPicker.ColorPicker>();
                picker.AssignColor(DrawableConfigManager.GetDrawableConfig(surface).Color);
                picker.onValueChanged.AddListener(color => 
                { 
                    GameDrawableManager.ChangeColor(surface, color);
                    new DrawableChangeColorNetAction(DrawableConfigManager.GetDrawableConfig(surface)).Execute();
                    colorAction.Invoke(color);
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
    }
}