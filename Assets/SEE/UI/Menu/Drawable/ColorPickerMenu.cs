using HSVPicker;
using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Provides the color picker menu for the <see cref="ColorPickerAction"/>.
    /// </summary>
    public class ColorPickerMenu : SingletonMenu
    {
        /// <summary>
        /// The location where the color picker menu prefab is placed.
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
        /// The switch selecting whether a picked color should become the primary
        /// or secondary color.
        /// </summary>
        private SwitchManager switchManager;

        /// <summary>
        /// The HSV color picker displaying the current primary color.
        /// </summary>
        private ColorPicker pickerForPrimaryColor;

        /// <summary>
        /// The HSV color picker displaying the current secondary color.
        /// </summary>
        private ColorPicker pickerForSecondaryColor;

        /// <summary>
        /// The color currently displayed by the primary color picker.
        /// </summary>
        internal Color PrimaryColor
        {
            get
            {
                return pickerForPrimaryColor != null ? pickerForPrimaryColor.CurrentColor : Color.clear;
            }
        }

        /// <summary>
        /// The color currently displayed by the secondary color picker.
        /// </summary>
        internal Color SecondaryColor
        {
            get
            {
                return pickerForSecondaryColor != null ? pickerForSecondaryColor.CurrentColor : Color.clear;
            }
        }

        /// <summary>
        /// Initializes the singleton instance.
        /// </summary>
        static ColorPickerMenu()
        {
            Instance = new ColorPickerMenu();
        }

        /// <summary>
        /// Enables the color picker menu.
        /// The menu is created when necessary and its displayed colors are synchronized
        /// with the current drawable color settings.
        /// </summary>
        public override void Enable()
        {
            if (gameObject == null)
            {
                Instantiate(colorPickerMenuPrefab);
                ResolveControls();
            }

            pickerForPrimaryColor.AssignColor(ValueHolder.CurrentPrimaryColor);
            pickerForSecondaryColor.AssignColor(ValueHolder.CurrentSecondaryColor);

            base.Enable();
        }

        /// <summary>
        /// Resolves the UI controls of the currently instantiated color picker menu.
        /// </summary>
        private void ResolveControls()
        {
            switchManager = GameFinder.FindAttachedOrLocalDescendant(gameObject, "Switch").GetComponent<SwitchManager>();
            pickerForPrimaryColor = GameFinder.FindAttachedOrLocalDescendant(gameObject, "Primary").GetComponent<ColorPicker>();
            pickerForSecondaryColor = GameFinder.FindAttachedOrLocalDescendant(gameObject, "Second").GetComponent<ColorPicker>();
        }

        /// <summary>
        /// Assigns a color to the primary HSV color picker if the menu currently exists.
        /// </summary>
        /// <param name="color">The color that should be assigned.</param>
        public void AssignPrimaryColor(Color color)
        {
            if (pickerForPrimaryColor != null)
            {
                pickerForPrimaryColor.AssignColor(color);
            }
        }

        /// <summary>
        /// Assigns a color to the secondary HSV color picker if the menu currently exists.
        /// </summary>
        /// <param name="color">The color that should be assigned.</param>
        public void AssignSecondaryColor(Color color)
        {
            if (pickerForSecondaryColor != null)
            {
                pickerForSecondaryColor.AssignColor(color);
            }
        }

        /// <summary>
        /// Returns whether picked colors should be assigned as secondary colors.
        /// </summary>
        /// <returns>True if secondary-color selection is active; otherwise false.</returns>
        public bool GetSwitchStatus()
        {
            return switchManager != null && switchManager.isOn;
        }

        /// <summary>
        /// Destroys the color picker menu and clears its cached UI references.
        /// </summary>
        public override void Destroy()
        {
            base.Destroy();

            switchManager = null;
            pickerForPrimaryColor = null;
            pickerForSecondaryColor = null;
        }
    }
}
