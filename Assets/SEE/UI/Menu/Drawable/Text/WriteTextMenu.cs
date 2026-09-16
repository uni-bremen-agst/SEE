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
        /// Manages the shared text-style behavior.
        /// </summary>
        private readonly TextStyleMenu textStyleMenu;

        /// <summary>
        /// Initializes the writing-specific part of the text menu.
        /// </summary>
        /// <param name="textMenu">
        /// The root object of the complete text menu.
        /// </param>
        /// <param name="controls">
        /// The shared UI controls of the text menu.
        /// </param>
        /// <param name="enableTextMenu">
        /// Enables and prepares the shared text menu for the requested mode.
        /// </param>
        /// <param name="textStyleMenu">
        /// Manages the shared text-style behavior used by writing and editing.
        /// </param>
        internal WriteTextMenu(
            GameObject textMenu,
            TextMenuControls controls,
            System.Action<UnityAction<Color>, Color, bool, bool> enableTextMenu,
            TextStyleMenu textStyleMenu)
        {
            this.textMenu = textMenu;
            this.controls = controls;
            this.enableTextMenu = enableTextMenu;
            this.textStyleMenu = textStyleMenu;
        }

        /// <summary>
        /// Configures the shared text menu for writing new text.
        /// The selected style values are stored in <see cref="ValueHolder"/>
        /// and are reused by subsequently created text objects.
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
                textStyleMenu.AssignColorArea(
                    color => ValueHolder.CurrentPrimaryColor = color,
                    ValueHolder.CurrentPrimaryColor);

                MenuHelper.CalculateHeight(textMenu);
            });

            SetUpOutlineColorButton();

            textStyleMenu.AssignOutlineThickness(
                thickness => ValueHolder.CurrentOutlineThickness = thickness,
                ValueHolder.CurrentOutlineThickness);

            controls.OutlineSwitch.isOn = false;
            controls.OutlineSwitch.UpdateUI();

            textStyleMenu.AssignFontSize(
                size => ValueHolder.CurrentFontSize = size,
                ValueHolder.CurrentFontSize);

            MenuHelper.CalculateHeight(textMenu);
        }

        /// <summary>
        /// Configures the outline-color button for writing new text.
        /// A valid secondary color is ensured before the shared color picker
        /// is configured to update <see cref="ValueHolder.CurrentSecondaryColor"/>.
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

                textStyleMenu.AssignColorArea(
                    color => ValueHolder.CurrentSecondaryColor = color,
                    ValueHolder.CurrentSecondaryColor);

                MenuHelper.CalculateHeight(textMenu);
            });
        }
    }
}
