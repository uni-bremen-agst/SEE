using SEE.Game.Drawable;
using SEE.Game.Drawable.ValueHolders;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable.Text
{
    /// <summary>
    /// Configures the shared text-menu controls for writing new text.
    /// The selected values are stored in <see cref="ValueHolder"/> and are
    /// reused by subsequently created text objects.
    /// </summary>
    internal sealed class WriteTextMenu
    {
        /// <summary>
        /// The root object of the complete text menu.
        /// </summary>
        private readonly GameObject textMenu;

        /// <summary>
        /// The shared UI controls of the text menu.
        /// </summary>
        private readonly TextMenuControls controls;

        /// <summary>
        /// Enables and prepares the shared text menu.
        /// </summary>
        private readonly System.Action<UnityAction<Color>, Color, bool, bool> enableTextMenu;

        /// <summary>
        /// Assigns an action and initial color to the shared color picker.
        /// </summary>
        private readonly System.Action<UnityAction<Color>, Color> assignColorArea;

        /// <summary>
        /// Assigns an action and initial value to the outline-thickness control.
        /// </summary>
        private readonly System.Action<UnityAction<float>, float> assignOutlineThickness;

        /// <summary>
        /// Assigns an action and initial value to the font-size control.
        /// </summary>
        private readonly System.Action<UnityAction<float>, float> assignFontSize;

        /// <summary>
        /// Initializes the writing-specific part of the text menu.
        /// </summary>
        /// <param name="textMenu">The root object of the complete text menu.</param>
        /// <param name="controls">The shared text-menu controls.</param>
        /// <param name="enableTextMenu">Enables and prepares the shared text menu.</param>
        /// <param name="assignColorArea">Assigns the color-picker action and initial color.</param>
        /// <param name="assignOutlineThickness">
        /// Assigns the outline-thickness action and initial value.
        /// </param>
        /// <param name="assignFontSize">Assigns the font-size action and initial value.</param>
        internal WriteTextMenu(
            GameObject textMenu,
            TextMenuControls controls,
            System.Action<UnityAction<Color>, Color, bool, bool> enableTextMenu,
            System.Action<UnityAction<Color>, Color> assignColorArea,
            System.Action<UnityAction<float>, float> assignOutlineThickness,
            System.Action<UnityAction<float>, float> assignFontSize)
        {
            this.textMenu = textMenu;
            this.controls = controls;
            this.enableTextMenu = enableTextMenu;
            this.assignColorArea = assignColorArea;
            this.assignOutlineThickness = assignOutlineThickness;
            this.assignFontSize = assignFontSize;
        }

        /// <summary>
        /// Configures the shared text menu for writing new text.
        /// </summary>
        internal void Enable()
        {
            enableTextMenu(
                color => ValueHolder.CurrentPrimaryColor = color,
                ValueHolder.CurrentPrimaryColor,
                true,
                false);

            controls.ReturnButtonObject.SetActive(false);

            controls.FontColorButtonManager.clickEvent.AddListener(() =>
            {
                assignColorArea(
                    color => ValueHolder.CurrentPrimaryColor = color,
                    ValueHolder.CurrentPrimaryColor);

                MenuHelper.CalculateHeight(textMenu);
            });

            SetUpOutlineColorButton();

            assignOutlineThickness(
                thickness => ValueHolder.CurrentOutlineThickness = thickness,
                ValueHolder.CurrentOutlineThickness);

            controls.OutlineSwitch.isOn = false;
            controls.OutlineSwitch.UpdateUI();

            assignFontSize(
                size => ValueHolder.CurrentFontSize = size,
                ValueHolder.CurrentFontSize);

            MenuHelper.CalculateHeight(textMenu);
        }

        /// <summary>
        /// Configures the outline-color button for writing new text.
        /// </summary>
        private void SetUpOutlineColorButton()
        {
            controls.OutlineColorButtonManager.clickEvent.AddListener(() =>
            {
                if (ValueHolder.CurrentSecondaryColor == Color.clear)
                {
                    ValueHolder.CurrentSecondaryColor = Random.ColorHSV();
                }

                if (ValueHolder.CurrentSecondaryColor.a == 0)
                {
                    ValueHolder.CurrentSecondaryColor = new Color(
                        ValueHolder.CurrentSecondaryColor.r,
                        ValueHolder.CurrentSecondaryColor.g,
                        ValueHolder.CurrentSecondaryColor.b,
                        255);
                }

                assignColorArea(
                    color => ValueHolder.CurrentSecondaryColor = color,
                    ValueHolder.CurrentSecondaryColor);

                MenuHelper.CalculateHeight(textMenu);
            });
        }
    }
}
