using HSVPicker;
using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// This singleton class provides the color picker menu for the <see cref="ColorPickerAction"/>.
    /// </summary>
    public class ColorPickerMenu : Menu
    {
        /// <summary>
        /// The location where the color picker menu prefeb is placed.
        /// </summary>
        private const string colorPickerMenuPrefab = "Prefabs/UI/Drawable/ColorPickerMenu";

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        private readonly static ColorPickerMenu instance;

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private ColorPickerMenu() { }

        /// <summary>
        /// The selection for which color the picker should pick the color.
        /// Primary or secondary color.
        /// </summary>
        private static readonly SwitchManager switchManager;

        /// <summary>
        /// The HSV color picker to show the selected primary color.
        /// </summary>
        private static readonly ColorPicker pickerForPrimaryColor;

        /// <summary>
        /// The HSV color picker to show the selected second color.
        /// </summary>
        private static readonly ColorPicker pickerForSecondColor;

        /// <summary>
        /// The init constructor that create the instance for the color picker menu.
        /// It hides the text menu by default.
        /// </summary>
        static ColorPickerMenu()
        {
            instance = new ColorPickerMenu();

            instance.Instantiate(colorPickerMenuPrefab);

            switchManager = GameFinder.FindChild(instance.menu, "Switch").GetComponent<SwitchManager>();

            /// Gets and assign the color picker for the primary color.
            pickerForPrimaryColor = GameFinder.FindChild(instance.menu, "Primary").GetComponent<ColorPicker>();
            pickerForPrimaryColor.AssignColor(ValueHolder.CurrentPrimaryColor);

            /// Gets and assign the color picker for the secondary color.
            pickerForSecondColor = GameFinder.FindChild(instance.menu, "Second").GetComponent<ColorPicker>();
            pickerForSecondColor.AssignColor(ValueHolder.CurrentSecondaryColor);

            /// Hides the menu.
            instance.menu.SetActive(false);
        }

        /// <summary>
        /// Enables the color picker menu.
        /// </summary>
        public static void EnableMenu()
        {
            instance.menu.SetActive(true);
        }

        /// <summary>
        /// Hides the color picker menu.
        /// </summary>
        public static void DisableMenu()
        {
            instance.menu.SetActive(false);
        }

        /// <summary>
        /// Destroys the color picker menu. It cannot be re-enabled afterward.
        /// </summary>
        public static void DestroyMenu()
        {
            instance.Destroy();
        }

        /// <summary>
        /// Assigns a color to the primary HSV color picker.
        /// </summary>
        /// <param name="color">The color that should be assigned.</param>
        public static void AssignPrimaryColor(Color color)
        {
            pickerForPrimaryColor.AssignColor(color);
        }

        /// <summary>
        /// Assigns a color to the secondary HSV color picker.
        /// </summary>
        /// <param name="color">The color that should be assigned.</param>
        public static void AssignSecondaryColor(Color color)
        {
            pickerForSecondColor.AssignColor(color);
        }

        /// <summary>
        /// Returns the state of the switch.
        /// </summary>
        /// <returns>The switch status.</returns>
        public static bool GetSwitchStatus()
        {
            return switchManager.isOn;
        }
    }
}
