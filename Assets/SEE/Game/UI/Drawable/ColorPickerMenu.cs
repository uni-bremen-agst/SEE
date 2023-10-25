using Assets.SEE.Game.Drawable;
using HSVPicker;
using Michsky.UI.ModernUIPack;
using SEE.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using SEE.Controls.Actions.Drawable;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// This class provides the color picker menu for the <see cref="ColorPickerAction"/>
    /// </summary>
    public static class ColorPickerMenu
    {
        /// <summary>
        /// The location where the color picker menu prefeb is placed.
        /// </summary>
        private const string colorPickerMenuPrefab = "Prefabs/UI/Drawable/ColorPickerMenu";

        /// <summary>
        /// The instance for the colorPickerMenu
        /// </summary>
        private static GameObject instance;

        /// <summary>
        /// The selection for which color the picker should pick the color.
        /// Primary or secondary color.
        /// </summary>
        private static SwitchManager switchManager;

        /// <summary>
        /// The HSV color picker to show the selected primary color.
        /// </summary>
        private static ColorPicker pickerForPrimaryColor;

        /// <summary>
        /// The HSV color picker to show the selected second color.
        /// </summary>
        private static ColorPicker pickerForSecondColor;

        /// <summary>
        /// The init constructor that create the instance for the color picker menu.
        /// It hides the text menu by default.
        /// </summary>
        static ColorPickerMenu (){
            instance = PrefabInstantiator.InstantiatePrefab(colorPickerMenuPrefab,
                    GameObject.Find("UI Canvas").transform, false);
            switchManager = GameDrawableFinder.FindChild(instance, "Switch").GetComponent<SwitchManager>();
            pickerForPrimaryColor = GameDrawableFinder.FindChild(instance, "Primary").GetComponent<HSVPicker.ColorPicker>();
            pickerForPrimaryColor.AssignColor(ValueHolder.currentPrimaryColor);
            pickerForSecondColor = GameDrawableFinder.FindChild(instance, "Second").GetComponent<HSVPicker.ColorPicker>();
            pickerForSecondColor.AssignColor(ValueHolder.currentSecondaryColor);
            instance.SetActive(false);
        }

        /// <summary>
        /// Method to enable the color picker menu.
        /// </summary>
        public static void Enable()
        {
            instance.SetActive(true);
        }

        /// <summary>
        /// Hides the color picker menu.
        /// </summary>
        public static void Disable()
        {
            instance.SetActive(false);
        }

        /// <summary>
        /// Assigns a color to the primary hsv color picker.
        /// </summary>
        /// <param name="color">The color that should be assigned.</param>
        public static void AssignPrimaryColor(Color color)
        {
            pickerForPrimaryColor.AssignColor(color);
        }

        /// <summary>
        /// Assigns a color to the secondary hsv color picker.
        /// </summary>
        /// <param name="color">The color that should be assigned.</param>
        public static void AssignSecondaryColor(Color color)
        {
            pickerForSecondColor.AssignColor(color);
        }

        /// <summary>
        /// Get the state of the switch.
        /// </summary>
        /// <returns>The bool of the switch status.</returns>
        public static bool GetSwitchStatus()
        {
            return switchManager.isOn;
        }
    }
}