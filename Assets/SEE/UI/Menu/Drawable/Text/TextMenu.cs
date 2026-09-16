using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable.Text;
using SEE.UI.Notification;
using SEE.UI.PropertyDialog.Drawable;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TextConf = SEE.Game.Drawable.Configurations.TextConf;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// This class holds the instance for the text menu.
    /// </summary>
    public class TextMenu : SingletonMenu
    {
        #region Variables
        /// <summary>
        /// The location where the text menu prefeb is placed.
        /// </summary>
        private const string textMenuPrefab = "Prefabs/UI/Drawable/TextMenu";

        #region Label for font styles
        /// <summary>
        /// The label for the bold font style state
        /// </summary>
        private const string Bold = "Bold";

        /// <summary>
        /// The label for the italic font style state
        /// </summary>
        private const string Italic = "Italic";

        /// <summary>
        /// The label for the underline font style state
        /// </summary>
        private const string Underline = "Underline";

        /// <summary>
        /// The label for the strikethrough font style state
        /// </summary>
        private const string Strikethrough = "Strikethrough";

        /// <summary>
        /// The label for the lower case font style state
        /// </summary>
        private const string LowerCase = "LowerCase";

        /// <summary>
        /// The label for the upper case font style state
        /// </summary>
        private const string UpperCase = "UpperCase";

        /// <summary>
        /// The label for the small caps font style state
        /// </summary>
        private const string SmallCaps = "SmallCaps";
        #endregion
        /// <summary>
        /// Holds all UI references used by this text-menu instance.
        /// </summary>
        private readonly TextMenuControls controls;

        /// <summary>
        /// Manages writing-specific text-menu behavior.
        /// </summary>
        private readonly WriteTextMenu writeTextMenu;

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
        #endregion

        /// <summary>
        /// Creates and initializes the text menu.
        /// </summary>
        private TextMenu()
        {
            Instantiate(textMenuPrefab);

            controls = new TextMenuControls(gameObject);

            Initialize();

            writeTextMenu = new WriteTextMenu(
                gameObject,
                controls,
                EnableTextMenu,
                AssignColorArea,
                AssignOutlineThickness,
                AssignFontSize);
        }

        /// <summary>
        /// The only text-menu instance.
        /// </summary>
        public static TextMenu Instance { get; private set; }

        /// <summary>
        /// Creates the singleton text-menu instance.
        /// </summary>
        static TextMenu()
        {
            Instance = new TextMenu();
            Instance.Enable();
        }

        /// <summary>
        /// Returns true if the menu is already opened.
        /// </summary>
        /// <returns>True if the menu is alreay opened. Otherwise false.</returns>
        public override bool IsOpen()
        {
            return gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Initializes persistent text-menu UI state and handlers.
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
                .AddListener(MutuallyExclusiveColorButtons);

            controls.OutlineColorButtonManager
                .clickEvent
                .AddListener(MutuallyExclusiveColorButtons);

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
        /// Hides the text menu and restores keyboard shortcuts.
        /// </summary>
        public override void Disable()
        {
            base.Disable();
            controls.ReturnButtonObject.SetActive(false);
            SEEInput.KeyboardShortcutsEnabled = true;
        }

        /// <summary>
        /// Resets the text menu to its initial state.
        /// </summary>
        private void Reset()
        {
            ResetStyles();

            controls.FontColorButtonManager.clickEvent.RemoveAllListeners();
            controls.FontColorButtonManager.clickEvent.AddListener(
                MutuallyExclusiveColorButtons);

            controls.OutlineColorButtonManager.clickEvent.RemoveAllListeners();
            controls.OutlineColorButtonManager.clickEvent.AddListener(
                MutuallyExclusiveColorButtons);

            controls.ThicknessSlider.onValueChanged.RemoveAllListeners();

            controls.OutlineSwitch.OffEvents.RemoveAllListeners();
            controls.OutlineSwitch.OnEvents.RemoveAllListeners();

            controls.FontSizeInput.OnValueChanged.RemoveAllListeners();

            controls.OrderInLayerSlider.OnValueChanged.RemoveAllListeners();
        }

        /// <summary>
        /// Enables the text menu in its default writing configuration.
        /// </summary>
        public override void Enable()
        {
            Enable(
                reset: true,
                showEditMode: false);
        }

        /// <summary>
        /// Enables the text menu using the requested configuration.
        /// </summary>
        /// <param name="reset">
        /// Whether the current menu handlers and state should be reset.
        /// </param>
        /// <param name="showEditMode">
        /// Whether editing-specific controls should be shown.
        /// </param>
        public void Enable(bool reset, bool showEditMode = false)
        {
            if (reset)
            {
                Reset();
            }

            ConfigureModeControls(showEditMode);

            MenuHelper.CalculateHeight(gameObject);

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Configures controls that differ between writing and editing.
        /// </summary>
        /// <param name="showEditMode">
        /// Whether editing-specific controls should be visible.
        /// </param>
        private void ConfigureModeControls(bool showEditMode)
        {
            if (showEditMode)
            {
                controls.OrderInLayerObject.SetActive(true);
                controls.OrderInLayerUnitySlider.interactable = true;
                controls.EditTextObject.SetActive(true);
            }
            else
            {
                controls.OrderInLayerObject.SetActive(false);
                controls.EditTextObject.SetActive(false);
            }
        }

        /// <summary>
        /// Reveals the text menu
        /// </summary>
        /// <param name="colorAction">The inital action for the HSV color picker.</param>
        /// <param name="color">The inital color for the HSV color picker.</param>
        /// <param name="reset">Specifies whether the menu should be reset to its initial state.</param>
        /// <param name="showEditMode">Specifies whether the menu should be opened for edit mode.
        /// Otherwise it will be opened for the WriteTextAction.</param>
        private void EnableTextMenu(
            UnityAction<Color> colorAction,
            Color color,
            bool reset = true,
            bool showEditMode = false)
        {
            if (reset)
            {
                Reset();
            }

            ConfigureModeControls(showEditMode);

            gameObject.SetActive(true);

            if (controls.FontColorButton.interactable)
            {
                MutuallyExclusiveColorButtons();
            }

            AssignColorArea(
                colorAction,
                color);

            MenuHelper.CalculateHeight(gameObject);
        }

        /// <summary>
        /// Configures the text menu for writing new text.
        /// </summary>
        public void EnableForWriting()
        {
            writeTextMenu.Enable();
        }

        /// <summary>
        /// Provides the text menu for editing, adding the necessary handlers to the respective components.
        /// </summary>
        /// <param name="selectedText">The selected text object for editing.</param>
        /// <param name="newValueHolder">The <see cref="TextConf"/> value holder. If differnt
        /// from this type, nothing happens.</param>
        /// <param name="returnCall">The return call action to return to the parent menu.</param>
        public void EnableForEditing(
            GameObject selectedText,
            DrawableType newValueHolder,
            UnityAction returnCall = null)
        {
            if (newValueHolder is TextConf textHolder)
            {
                GameObject surface = GameFinder.GetDrawableSurface(selectedText);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

                /// Enables the text menu in edit mode.
                EnableTextMenu(color =>
                {
                    GameEdit.ChangeFontColor(selectedText, color);
                    textHolder.FontColor = color;
                    new EditTextNetAction(surface.name, surfaceParentName, TextConf.GetText(selectedText)).Execute();
                }, textHolder.FontColor, true, true);

                /// Adds the handler for the return button, if <paramref name="returnCall"/> not null.
                AddReturnCall(returnCall);

                /// Adds the handler for the font color button.
                AddFontColorButtonForEdit(selectedText, textHolder, surface, surfaceParentName);

                /// Adds the handler for the outline color button.
                AddOutlineColorButtonForEdit(selectedText, textHolder, surface, surfaceParentName);

                /// Adds the handler for the outline thickness slider.
                /// Changes are saved in the configuration.
                AssignOutlineThickness(thickness =>
                {
                    GameEdit.ChangeOutlineThickness(selectedText, thickness);
                    textHolder.OutlineThickness = thickness;
                    new EditTextNetAction(surface.name, surfaceParentName, TextConf.GetText(selectedText)).Execute();
                }, textHolder.OutlineThickness);

                /// Adds the handler for the outline color status.
                /// Changes are saved in the configuration.
                AssignOutlineStatus(selectedText, textHolder, surface, surfaceParentName);

                /// Assigns the current status to the switch and updates the UI.
                controls.OutlineSwitch.isOn = textHolder.IsOutlined;
                controls.OutlineSwitch.UpdateUI();

                /// Adds the handler for the font size component.
                /// Changes are saved in the configuration.
                AssignFontSize(size =>
                {
                    GameEdit.ChangeFontSize(selectedText, size);
                    textHolder.FontSize = size;
                    new EditTextNetAction(surface.name, surfaceParentName, TextConf.GetText(selectedText)).Execute();
                }, textHolder.FontSize);

                /// Adds the handler to the font style buttons.
                /// Changes are saved in the configuration.
                AssignFontStyles(style =>
                {
                    GameEdit.ChangeFontStyles(selectedText, style);
                    textHolder.FontStyles = style;
                    new EditTextNetAction(surface.name, surfaceParentName, TextConf.GetText(selectedText)).Execute();
                }, textHolder.FontStyles);

                /// Adds the handler to the edit text button.
                /// Changes are saved in the configuration.
                AssignEditTextButton(() =>
                {
                    WriteEditTextDialog writeTextDialog = new();
                    writeTextDialog.SetStringInit(textHolder.Text);
                    UnityAction<string> stringAction = textOut =>
                    {
                        if (textOut != null && textOut != "")
                        {
                            /// The size of the new text is calculated, and the object is adjusted accordingly.
                            TextMeshPro tmp = selectedText.GetComponent<TextMeshPro>();
                            tmp.rectTransform.sizeDelta = GameTexter.CalculateWidthAndHeight(textOut, tmp.font,
                                textHolder.FontSize, textHolder.FontStyles);
                            GameEdit.ChangeText(selectedText, textOut);
                            textHolder.Text = textOut;
                            new EditTextNetAction(surface.name, surfaceParentName, TextConf.GetText(selectedText)).Execute();
                        }
                        else
                        {
                            ShowNotification.Warn("Empty text", "The text to write is empty. Please add one.");
                        }
                    };

                    writeTextDialog.Open(stringAction);
                });
                controls.OrderInLayerSlider.AssignMaxOrder(surface.GetComponent<DrawableHolder>().OrderInLayer);
                /// Adds the handler to the order in layer slider.
                /// Changes are saved in the configuration.
                AssignOrderInLayer(order =>
                {
                    GameEdit.ChangeLayer(selectedText, order);
                    textHolder.OrderInLayer = order;
                    new EditTextNetAction(surface.name, surfaceParentName, TextConf.GetText(selectedText)).Execute();
                }, textHolder.OrderInLayer);

                /// Re-calculate the menus height.
                MenuHelper.CalculateHeight(gameObject);
            }
        }

        /// <summary>
        /// Registers the action used to return to the parent menu.
        /// </summary>
        /// <param name="returnCall">
        /// The callback used to return to the parent menu.
        /// </param>
        private void AddReturnCall(UnityAction returnCall)
        {
            if (returnCall == null)
            {
                return;
            }

            controls.ReturnButtonObject.SetActive(true);

            controls.ReturnButtonManager.clickEvent.RemoveAllListeners();
            controls.ReturnButtonManager.clickEvent.AddListener(returnCall);

            controls.OrderInLayerUnitySlider.interactable = false;
        }

        /// <summary>
        /// Adds the handler for the font color button.
        /// After the button is pressed, the <see cref="HSVPicker.ColorPicker"/> changes the font color of the text.
        /// Changes are saved in the configuration.
        /// </summary>
        /// <param name="selectedText">The text to be edited.</param>
        /// <param name="textHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the text is displayed.</param>
        /// <param name="surfaceParentName">The id of the drawable surface parent.</param>
        private void AddFontColorButtonForEdit(GameObject selectedText, TextConf textHolder,
            GameObject surface, string surfaceParentName)
        {
            controls.FontColorButtonManager.clickEvent.AddListener(() =>
            {
                AssignColorArea(color =>
                {
                    GameEdit.ChangeFontColor(selectedText, color);
                    textHolder.FontColor = color;
                    new EditTextNetAction(surface.name, surfaceParentName, TextConf.GetText(selectedText)).Execute();
                }, textHolder.FontColor);
                MenuHelper.CalculateHeight(gameObject);
            });
        }

        /// <summary>
        /// Adds the handler for the outline color button.
        /// After the button is pressed, the <see cref="HSVPicker.ColorPicker"/> changes the outline color of the text.
        /// Changes are saved in the configuration.
        /// </summary>
        /// <param name="selectedText">The text to be edited.</param>
        /// <param name="textHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the text is displayed.</param>
        /// <param name="surfaceParentName">The id of the drawable surface parent.</param>
        private void AddOutlineColorButtonForEdit(GameObject selectedText, TextConf textHolder,
            GameObject surface, string surfaceParentName)
        {
            controls.OutlineColorButtonManager.clickEvent.AddListener(() =>
            {
                /// If the <see cref="GameDrawer.LineKind"/> was <see cref="GameDrawer.LineKind.Solid"/> before,
                /// the secondary color is clear.
                /// Therefore, a random color is added first,
                /// and if the color's alpha is 0, it is set to 255 to ensure the color is not transparent.
                if (textHolder.OutlineColor == Color.clear)
                {
                    textHolder.OutlineColor = Random.ColorHSV();
                }

                if (textHolder.OutlineColor.a == 0)
                {
                    textHolder.OutlineColor = new Color(textHolder.OutlineColor.r, textHolder.OutlineColor.g,
                        textHolder.OutlineColor.b, 255);
                }

                AssignColorArea(color =>
                {
                    GameEdit.ChangeOutlineColor(selectedText, color);
                    textHolder.OutlineColor = color;
                    new EditTextNetAction(surface.name, surfaceParentName, TextConf.GetText(selectedText)).Execute();
                }, textHolder.OutlineColor);
                MenuHelper.CalculateHeight(gameObject);
            });
        }

        /// <summary>
        /// This method will be used as an action for the handler of the color buttons (font/outline).
        /// This allows only one color to be active at a time.
        /// </summary>
        private void MutuallyExclusiveColorButtons()
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

            MenuHelper.CalculateHeight(gameObject, true);
        }

        /// <summary>
        /// Assigns an action and a color to the HSV Color Picker.
        /// </summary>
        /// <param name="colorAction">The color action that should be assigned.</param>
        /// <param name="color">The color that should be assigned.</param>
        public void AssignColorArea(UnityAction<Color> colorAction, Color color)
        {
            if (pickerAction != null)
            {
                controls.ColorPicker.onValueChanged.RemoveListener(pickerAction);
            }

            pickerAction = colorAction;

            controls.ColorPicker.AssignColor(color);
            controls.ColorPicker.onValueChanged.AddListener(colorAction);
        }

        /// <summary>
        /// Assigns an action and a thickness to the outline thickness slider.
        /// </summary>
        /// <param name="thicknessAction">The float action that should be assigned.</param>
        /// <param name="thickness">The thickness that should be assigned.</param>
        public void AssignOutlineThickness(UnityAction<float> thicknessAction, float thickness)
        {
            controls.ThicknessSlider.onValueChanged.RemoveAllListeners();
            controls.ThicknessSlider.AssignValue(thickness);
            controls.ThicknessSlider.onValueChanged.AddListener(thicknessAction);
        }

        /// <summary>
        /// Assigns the action to edit the outline color status.
        /// </summary>
        /// <param name="selectedText">The chosen text to be edited.</param>
        /// <param name="textHolder">The configuration which holds the new value.</param>
        /// <param name="surface">The drawable surface on which the text is displayed.</param>
        /// <param name="surfaceParentName">The id of the drawable surface parent.</param>
        public void AssignOutlineStatus(GameObject selectedText, TextConf textHolder,
            GameObject surface, string surfaceParentName)
        {
            controls.OutlineSwitch.OffEvents.AddListener(() =>
            {
                GameTexter.ChangeOutlineStatus(selectedText, false);
                textHolder.IsOutlined = false;
                new EditTextNetAction(surface.name, surfaceParentName, TextConf.GetText(selectedText)).Execute();
            });

            controls.OutlineSwitch.OnEvents.AddListener(() =>
            {
                GameTexter.ChangeOutlineStatus(selectedText, true);
                textHolder.IsOutlined = true;
                /// Changes the outline color if the outline was clear.
                TextMeshPro tmp = selectedText.GetComponent<TextMeshPro>();
                if (textHolder.OutlineColor != tmp.outlineColor
                    && tmp.outlineColor == Color.clear)
                {
                    GameEdit.ChangeOutlineColor(selectedText, textHolder.OutlineColor);
                }
                new EditTextNetAction(surface.name, surfaceParentName, TextConf.GetText(selectedText)).Execute();
            });
        }

        /// <summary>
        /// Assigns an action and a font size to the font size input field.
        /// </summary>
        /// <param name="fontSizeAction">The float action that should be assigned.</param>
        /// <param name="fontSize">The font size that should be assigned.</param>
        public void AssignFontSize(UnityAction<float> fontSizeAction, float fontSize)
        {
            controls.FontSizeInput.OnValueChanged.RemoveAllListeners();
            controls.FontSizeInput.AssignValue(fontSize);
            controls.FontSizeInput.OnValueChanged.AddListener(fontSizeAction);
        }

        /// <summary>
        /// Assigns an action and an order to the order in layer slider.
        /// </summary>
        /// <param name="orderInLayerAction">The action that should be assigned.</param>
        /// <param name="order">The order that should be assigned.</param>
        public void AssignOrderInLayer(UnityAction<int> orderInLayerAction, int order)
        {
            controls.OrderInLayerSlider.OnValueChanged.RemoveAllListeners();
            controls.OrderInLayerSlider.AssignValue(order);
            controls.OrderInLayerSlider.OnValueChanged.AddListener(orderInLayerAction);
        }

        /// <summary>
        /// Assigns an action to the edit text button.
        /// </summary>
        /// <param name="action">The action that should be assigned.</param>
        public void AssignEditTextButton(UnityAction action)
        {
            controls.EditTextButtonManager.clickEvent.RemoveAllListeners();
            controls.EditTextButtonManager.clickEvent.AddListener(action);
        }

        /// <summary>
        /// Assigns an action and font styles to the font style buttons.
        /// </summary>
        /// <param name="action">The font styles action that should be assigned.</param>
        /// <param name="styles">The styles that should be assigned.</param>
        public void AssignFontStyles(UnityAction<FontStyles> action, FontStyles styles)
        {
            fontStyleAction = action;
            AssignStyles(styles);
        }

        /// <summary>
        /// This method will be used as inital handler action for the font style buttons.
        /// It enters the status of the selected font style into the dictionary and
        /// ensures that mutually exclusive font styles remain exclusive.
        /// </summary>
        /// <param name="pressedStyle">.</param>
        public void Press(string pressedStyle)
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
        /// Ensures that the three mutually exclusive font styles do not overlap.
        /// </summary>
        /// <param name="selectedStyle">The chosen font style.</param>
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
        /// Returns the corresponding button for a given string with a style name.
        /// </summary>
        /// <param name="pressedStyle">The given style name.</param>
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
        /// Sets the font style stats in dictionary <see cref="styles"/> to false
        /// and changes the color block to not selected.
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
        /// Assigns the respective font styles their value and
        /// changes their button color when they are selected.
        /// </summary>
        /// <param name="style">Style to be assigned.</param>
        private void AssignStyles(FontStyles style)
        {
            styles[Bold] = (style & FontStyles.Bold) != 0;
            styles[Italic] = (style & FontStyles.Italic) != 0;
            styles[Underline] = (style & FontStyles.Underline) != 0;
            styles[Strikethrough] = (style & FontStyles.Strikethrough) != 0;
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
        /// Returns the corresponding font style of a given keyword.
        /// </summary>
        /// <param name="key">The font style keyword.</param>
        /// <returns>The corresponding font style.</returns>
        private FontStyles GetFontStyleOfKey(string key)
        {
            FontStyles style = FontStyles.Normal;
            switch (key)
            {
                case Bold:
                    style = FontStyles.Bold;
                    break;
                case Italic:
                    style = FontStyles.Italic;
                    break;
                case Underline:
                    style = FontStyles.Underline;
                    break;
                case Strikethrough:
                    style = FontStyles.Strikethrough;
                    break;
                case LowerCase:
                    style = FontStyles.LowerCase;
                    break;
                case UpperCase:
                    style = FontStyles.UpperCase;
                    break;
                case SmallCaps:
                    style = FontStyles.SmallCaps;
                    break;
            }
            return style;
        }

        /// <summary>
        /// Creates a font style which contains all the selected font styles.
        /// </summary>
        /// <returns>A font style with the chosen font styles.</returns>
        public FontStyles GetFontStyle()
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
        /// True if the outline is enabled, otherwise false.
        /// </summary>
        /// <returns>The status of outline.</returns>
        public bool IsOutlineEnabled()
        {
            return controls.OutlineSwitch.isOn;
        }
    }
}
