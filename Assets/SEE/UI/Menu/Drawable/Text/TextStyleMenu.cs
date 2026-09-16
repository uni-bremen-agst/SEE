using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SEE.UI.Menu.Drawable.Text
{
    /// <summary>
    /// Manages the style-related state and shared style controls of the text menu.
    /// The same component is used for writing new text and editing existing text.
    /// </summary>
    internal sealed class TextStyleMenu
    {
        /// <summary>
        /// The label for the bold font style state.
        /// </summary>
        private const string Bold = "Bold";

        /// <summary>
        /// The label for the italic font style state.
        /// </summary>
        private const string Italic = "Italic";

        /// <summary>
        /// The label for the underline font style state.
        /// </summary>
        private const string Underline = "Underline";

        /// <summary>
        /// The label for the strikethrough font style state.
        /// </summary>
        private const string Strikethrough = "Strikethrough";

        /// <summary>
        /// The label for the lower-case font style state.
        /// </summary>
        private const string LowerCase = "LowerCase";

        /// <summary>
        /// The label for the upper-case font style state.
        /// </summary>
        private const string UpperCase = "UpperCase";

        /// <summary>
        /// The label for the small-caps font style state.
        /// </summary>
        private const string SmallCaps = "SmallCaps";

        /// <summary>
        /// The root object of the complete text menu.
        /// </summary>
        private readonly GameObject textMenu;

        /// <summary>
        /// The shared UI controls of the text menu.
        /// </summary>
        private readonly TextMenuControls controls;

        /// <summary>
        /// The action invoked when the selected font style changes.
        /// </summary>
        private UnityAction<FontStyles> fontStyleAction;

        /// <summary>
        /// The action currently registered at the color picker.
        /// </summary>
        private UnityAction<Color> pickerAction;

        /// <summary>
        /// Holds the activation state of the supported font styles.
        /// </summary>
        private readonly Dictionary<string, bool> styles = new()
        {
            { Bold, false },
            { Italic, false },
            { Underline, false },
            { Strikethrough, false },
            { LowerCase, false },
            { UpperCase, false },
            { SmallCaps, false }
        };

        /// <summary>
        /// The colors used for unselected font-style buttons.
        /// </summary>
        private ColorBlock notSelectedBlock;

        /// <summary>
        /// The colors used for selected font-style buttons.
        /// </summary>
        private ColorBlock selectedBlock;

        /// <summary>
        /// Initializes the shared text-style behavior.
        /// </summary>
        /// <param name="textMenu">The root object of the complete text menu.</param>
        /// <param name="controls">The shared text-menu controls.</param>
        internal TextStyleMenu(
            GameObject textMenu,
            TextMenuControls controls)
        {
            this.textMenu = textMenu;
            this.controls = controls;

            Initialize();
        }

        /// <summary>
        /// Initializes persistent style state and style-related UI handlers.
        /// </summary>
        private void Initialize()
        {
            controls.BoldButton
                .gameObject
                .AddComponent<UIHoverTooltip>()
                .SetMessage("Bold");

            controls.ItalicButton
                .gameObject
                .AddComponent<UIHoverTooltip>()
                .SetMessage("Italic");

            controls.UnderlineButton
                .gameObject
                .AddComponent<UIHoverTooltip>()
                .SetMessage("Underline");

            controls.StrikethroughButton
                .gameObject
                .AddComponent<UIHoverTooltip>()
                .SetMessage("Strikethrough");

            controls.LowerCaseButton
                .gameObject
                .AddComponent<UIHoverTooltip>()
                .SetMessage("Lower Case");

            controls.UpperCaseButton
                .gameObject
                .AddComponent<UIHoverTooltip>()
                .SetMessage("Upper Case");

            controls.SmallCapsButton
                .gameObject
                .AddComponent<UIHoverTooltip>()
                .SetMessage("Small Caps");

            InitializeFontStyleButtons();

            notSelectedBlock = controls.BoldButton.colors;
            selectedBlock = notSelectedBlock;

            selectedBlock.normalColor =
                selectedBlock.selectedColor =
                selectedBlock.disabledColor =
                selectedBlock.highlightedColor =
                selectedBlock.pressedColor =
                Color.gray;

            controls.FontColorButton.interactable = false;

            controls.FontColorButtonManager
                .clickEvent
                .AddListener(ToggleColorButtons);

            controls.OutlineColorButtonManager
                .clickEvent
                .AddListener(ToggleColorButtons);

            controls.OutlineObject.SetActive(false);
            controls.ThicknessObject.SetActive(false);
        }

        /// <summary>
        /// Registers the persistent handlers of the font-style buttons.
        /// </summary>
        private void InitializeFontStyleButtons()
        {
            controls.BoldButtonManager.clickEvent.AddListener(
                () => Press(Bold));

            controls.ItalicButtonManager.clickEvent.AddListener(
                () => Press(Italic));

            controls.UnderlineButtonManager.clickEvent.AddListener(
                () => Press(Underline));

            controls.StrikethroughButtonManager.clickEvent.AddListener(
                () => Press(Strikethrough));

            controls.LowerCaseButtonManager.clickEvent.AddListener(
                () => Press(LowerCase));

            controls.UpperCaseButtonManager.clickEvent.AddListener(
                () => Press(UpperCase));

            controls.SmallCapsButtonManager.clickEvent.AddListener(
                () => Press(SmallCaps));
        }

        /// <summary>
        /// Resets style state and removes mode-specific style handlers.
        /// Persistent handlers are restored afterwards.
        /// </summary>
        internal void Reset()
        {
            ResetStyles();

            controls.FontColorButtonManager.clickEvent.RemoveAllListeners();
            controls.FontColorButtonManager.clickEvent.AddListener(
                ToggleColorButtons);

            controls.OutlineColorButtonManager.clickEvent.RemoveAllListeners();
            controls.OutlineColorButtonManager.clickEvent.AddListener(
                ToggleColorButtons);

            controls.ThicknessSlider.onValueChanged.RemoveAllListeners();

            controls.OutlineSwitch.OffEvents.RemoveAllListeners();
            controls.OutlineSwitch.OnEvents.RemoveAllListeners();

            controls.FontSizeInput.OnValueChanged.RemoveAllListeners();
        }

        /// <summary>
        /// Ensures that the font-color button is the selected color control.
        /// </summary>
        internal void EnsureFontColorSelected()
        {
            if (controls.FontColorButton.interactable)
            {
                ToggleColorButtons();
            }
        }

        /// <summary>
        /// Switches between font-color and outline-color editing.
        /// </summary>
        private void ToggleColorButtons()
        {
            controls.FontColorButton.interactable =
                !controls.FontColorButton.IsInteractable();

            controls.OutlineColorButton.interactable =
                !controls.OutlineColorButton.IsInteractable();

            bool outlineColorSelected =
                !controls.OutlineColorButton.interactable;

            controls.ThicknessObject.SetActive(
                outlineColorSelected);

            controls.OutlineObject.SetActive(
                outlineColorSelected);

            MenuHelper.CalculateHeight(textMenu, true);
        }

        /// <summary>
        /// Assigns an action and a color to the HSV color picker.
        /// </summary>
        /// <param name="colorAction">The color action to be assigned.</param>
        /// <param name="color">The color to be assigned.</param>
        internal void AssignColorArea(
            UnityAction<Color> colorAction,
            Color color)
        {
            if (pickerAction != null)
            {
                controls.ColorPicker.onValueChanged.RemoveListener(
                    pickerAction);
            }

            pickerAction = colorAction;

            controls.ColorPicker.AssignColor(color);
            controls.ColorPicker.onValueChanged.AddListener(colorAction);
        }

        /// <summary>
        /// Assigns an action and a thickness to the outline-thickness slider.
        /// </summary>
        /// <param name="thicknessAction">
        /// The action invoked when the thickness changes.
        /// </param>
        /// <param name="thickness">The initial outline thickness.</param>
        internal void AssignOutlineThickness(
            UnityAction<float> thicknessAction,
            float thickness)
        {
            controls.ThicknessSlider.onValueChanged.RemoveAllListeners();
            controls.ThicknessSlider.AssignValue(thickness);
            controls.ThicknessSlider.onValueChanged.AddListener(
                thicknessAction);
        }

        /// <summary>
        /// Assigns an action and a font size to the font-size input.
        /// </summary>
        /// <param name="fontSizeAction">
        /// The action invoked when the font size changes.
        /// </param>
        /// <param name="fontSize">The initial font size.</param>
        internal void AssignFontSize(
            UnityAction<float> fontSizeAction,
            float fontSize)
        {
            controls.FontSizeInput.OnValueChanged.RemoveAllListeners();
            controls.FontSizeInput.AssignValue(fontSize);
            controls.FontSizeInput.OnValueChanged.AddListener(
                fontSizeAction);
        }

        /// <summary>
        /// Assigns the active font styles and the action invoked on changes.
        /// </summary>
        /// <param name="action">
        /// The action invoked when the selected font styles change.
        /// </param>
        /// <param name="fontStyles">The font styles to be assigned.</param>
        internal void AssignFontStyles(
            UnityAction<FontStyles> action,
            FontStyles fontStyles)
        {
            fontStyleAction = action;
            AssignStyles(fontStyles);
        }

        /// <summary>
        /// Updates the state of the selected font-style button.
        /// </summary>
        /// <param name="pressedStyle">The selected font-style label.</param>
        private void Press(string pressedStyle)
        {
            if (styles.TryGetValue(pressedStyle, out bool value))
            {
                styles[pressedStyle] = !value;

                if (styles[pressedStyle])
                {
                    GetPressedButton(pressedStyle).colors = selectedBlock;
                    MutuallyExclusiveStyles(pressedStyle);
                }
                else
                {
                    GetPressedButton(pressedStyle).colors = notSelectedBlock;
                }

                fontStyleAction?.Invoke(GetFontStyle());
            }
        }

        /// <summary>
        /// Ensures that mutually exclusive capitalization styles do not overlap.
        /// </summary>
        /// <param name="selectedStyle">The selected font-style label.</param>
        private void MutuallyExclusiveStyles(string selectedStyle)
        {
            switch (selectedStyle)
            {
                case LowerCase:
                    styles[UpperCase] = false;
                    controls.UpperCaseButton.colors = notSelectedBlock;

                    styles[SmallCaps] = false;
                    controls.SmallCapsButton.colors = notSelectedBlock;
                    break;

                case UpperCase:
                    styles[LowerCase] = false;
                    controls.LowerCaseButton.colors = notSelectedBlock;

                    styles[SmallCaps] = false;
                    controls.SmallCapsButton.colors = notSelectedBlock;
                    break;

                case SmallCaps:
                    styles[LowerCase] = false;
                    controls.LowerCaseButton.colors = notSelectedBlock;

                    styles[UpperCase] = false;
                    controls.UpperCaseButton.colors = notSelectedBlock;
                    break;
            }
        }

        /// <summary>
        /// Returns the button corresponding to the given font-style label.
        /// </summary>
        /// <param name="pressedStyle">The font-style label.</param>
        /// <returns>The corresponding button.</returns>
        private Button GetPressedButton(string pressedStyle)
        {
            return pressedStyle switch
            {
                Bold => controls.BoldButton,
                Italic => controls.ItalicButton,
                Underline => controls.UnderlineButton,
                Strikethrough => controls.StrikethroughButton,
                LowerCase => controls.LowerCaseButton,
                UpperCase => controls.UpperCaseButton,
                SmallCaps => controls.SmallCapsButton,
                _ => null,
            };
        }

        /// <summary>
        /// Resets all selected font styles.
        /// </summary>
        private void ResetStyles()
        {
            foreach (string key in styles.Keys.ToList())
            {
                styles[key] = false;
            }

            controls.BoldButton.colors = notSelectedBlock;
            controls.ItalicButton.colors = notSelectedBlock;
            controls.UnderlineButton.colors = notSelectedBlock;
            controls.StrikethroughButton.colors = notSelectedBlock;
            controls.LowerCaseButton.colors = notSelectedBlock;
            controls.UpperCaseButton.colors = notSelectedBlock;
            controls.SmallCapsButton.colors = notSelectedBlock;

            fontStyleAction = null;
        }

        /// <summary>
        /// Assigns the given font styles to the internal style state.
        /// </summary>
        /// <param name="style">The font styles to be assigned.</param>
        private void AssignStyles(FontStyles style)
        {
            styles[Bold] = (style & FontStyles.Bold) != 0;
            styles[Italic] = (style & FontStyles.Italic) != 0;
            styles[Underline] = (style & FontStyles.Underline) != 0;
            styles[Strikethrough] =
                (style & FontStyles.Strikethrough) != 0;
            styles[LowerCase] = (style & FontStyles.LowerCase) != 0;
            styles[UpperCase] = (style & FontStyles.UpperCase) != 0;
            styles[SmallCaps] = (style & FontStyles.SmallCaps) != 0;

            foreach (string key in styles.Keys.ToList())
            {
                if (styles[key])
                {
                    GetPressedButton(key).colors = selectedBlock;
                    MutuallyExclusiveStyles(key);
                }
            }
        }

        /// <summary>
        /// Returns the font style represented by the given style label.
        /// </summary>
        /// <param name="key">The font-style label.</param>
        /// <returns>The corresponding font style.</returns>
        private static FontStyles GetFontStyleOfKey(string key)
        {
            return key switch
            {
                Bold => FontStyles.Bold,
                Italic => FontStyles.Italic,
                Underline => FontStyles.Underline,
                Strikethrough => FontStyles.Strikethrough,
                LowerCase => FontStyles.LowerCase,
                UpperCase => FontStyles.UpperCase,
                SmallCaps => FontStyles.SmallCaps,
                _ => FontStyles.Normal
            };
        }

        /// <summary>
        /// Returns all currently selected font styles.
        /// </summary>
        /// <returns>The combined selected font styles.</returns>
        internal FontStyles GetFontStyle()
        {
            FontStyles style = FontStyles.Normal;

            foreach (string key in styles.Keys)
            {
                if (styles[key])
                {
                    style |= GetFontStyleOfKey(key);
                }
            }

            return style;
        }

        /// <summary>
        /// Returns whether outlining is currently enabled.
        /// </summary>
        /// <returns>True if outlining is enabled.</returns>
        internal bool IsOutlineEnabled()
        {
            return controls.OutlineSwitch.isOn;
        }
    }
}
