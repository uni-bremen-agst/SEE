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
    public class ColorPickerMenu : SingletonMenu
    {
        /// <summary>
        /// The location where the color picker menu prefeb is placed.
        /// </summary>
        private const string colorPickerMenuPrefab = "Prefabs/UI/Drawable/ColorPickerMenu";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private ColorPickerMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static ColorPickerMenu Instance { get; private set; }

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
            Instance = new ColorPickerMenu();

            Instance.Instantiate(colorPickerMenuPrefab);

            switchManager = GameFinder.FindChild(Instance.gameObject, "Switch").GetComponent<SwitchManager>();

            /// Gets and assign the color picker for the primary color.
            pickerForPrimaryColor = GameFinder.FindChild(Instance.gameObject, "Primary").GetComponent<ColorPicker>();
            pickerForPrimaryColor.AssignColor(ValueHolder.CurrentPrimaryColor);

            /// Gets and assign the color picker for the secondary color.
            pickerForSecondColor = GameFinder.FindChild(Instance.gameObject, "Second").GetComponent<ColorPicker>();
            pickerForSecondColor.AssignColor(ValueHolder.CurrentSecondaryColor);

            /// Hides the menu.
            Instance.Enable();
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
