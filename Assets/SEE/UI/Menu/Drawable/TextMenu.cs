using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Notification;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.UI.PropertyDialog.Drawable;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TextConf = SEE.Game.Drawable.Configurations.TextConf;
using SEE.Game.Drawable.ValueHolders;

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

        /// <summary>
        /// The action for the Font Style Buttons that should also be carried out.
        /// </summary>
        private static UnityAction<FontStyles> fontStyleAction;

        /// <summary>
        /// The action for the HSV Color Picker that should also be carried out.
        /// </summary>
        private static UnityAction<Color> pickerAction;

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
        /// The dictionary that holds the font style states with their values whether they should be active or not.
        /// </summary>
        private static readonly Dictionary<string, bool> styles = new() { { Bold, false },
            {Italic, false },
            {Underline, false },
            {Strikethrough, false},
            {LowerCase, false},
            {UpperCase, false},
            {SmallCaps, false} };

        /// <summary>
        /// The button for the bold font style.
        /// </summary>
        private static Button boldBtn;

        /// <summary>
        /// The button manager for the bold button.
        /// </summary>
        private static ButtonManagerBasic boldBMB;

        /// <summary>
        /// The button for the italic font style.
        /// </summary>
        private static Button italicBtn;

        /// <summary>
        /// The button manager for the italic button.
        /// </summary>
        private static ButtonManagerBasic italicBMB;

        /// <summary>
        /// The button for the underline font style.
        /// </summary>
        private static Button underlineBtn;

        /// <summary>
        /// The button manager for the underline button.
        /// </summary>
        private static ButtonManagerBasic underlineBMB;

        /// <summary>
        /// The button for the strikethrough font style.
        /// </summary>
        private static Button strikethroughBtn;

        /// <summary>
        /// The button manager for the strikethrough button.
        /// </summary>
        private static ButtonManagerBasic strikethroughBMB;

        /// <summary>
        /// The button for the lower case font style.
        /// </summary>
        private static Button lowerCaseBtn;

        /// <summary>
        /// The button manager for the lower case button.
        /// </summary>
        private static ButtonManagerBasic lowerCaseBMB;

        /// <summary>
        /// The button for the upper case font style.
        /// </summary>
        private static Button upperCaseBtn;

        /// <summary>
        /// The button manager for the upper case button.
        /// </summary>
        private static ButtonManagerBasic upperCaseBMB;

        /// <summary>
        /// The button for the small caps font style.
        /// </summary>
        private static Button smallCapsBtn;

        /// <summary>
        /// The button manager for the small caps button.
        /// </summary>
        private static ButtonManagerBasic smallCapsBMB;

        /// <summary>
        /// The color block for the colors if a font style button is not selected.
        /// </summary>
        private static ColorBlock notSelectedBlock;

        /// <summary>
        /// The color block for the colors if a font style button is selected.
        /// </summary>
        private static ColorBlock selectedBlock = new();

        /// <summary>
        /// The game object of the layer with the edit text button.
        /// </summary>
        private static GameObject editText;

        /// <summary>
        /// The button manager for the edit text button.
        /// </summary>
        private static ButtonManagerBasic editTextBMB;

        /// <summary>
        /// The font color button.
        /// </summary>
        private static Button fontColorBtn;

        /// <summary>
        /// The button manager for the font color button.
        /// </summary>
        private static ButtonManagerBasic fontColorBMB;

        /// <summary>
        /// The outline color button.
        /// </summary>
        private static Button outlineColorBtn;

        /// <summary>
        /// The button manager for the outline color button.
        /// </summary>
        private static ButtonManagerBasic outlineColorBMB;

        /// <summary>
        /// The HSV color picker.
        /// </summary>
        private static HSVPicker.ColorPicker picker;

        /// <summary>
        /// The thickness slider controller for the outline thickness.
        /// </summary>
        private static FloatValueSliderController thicknessSlider;

        /// <summary>
        /// The game object of the outline thickness layer.
        /// </summary>
        private static GameObject thicknessLayer;

        /// <summary>
        /// The switch to enable or disable the outline.
        /// </summary>
        private static SwitchManager outlineSwitch;

        /// <summary>
        /// The game object of the outline switch layer.
        /// </summary>
        private static GameObject outlineSwitchLayer;

        /// <summary>
        /// The game object of the order in layer layer.
        /// </summary>
        private static GameObject orderInLayer;

        /// <summary>
        /// The slider controller for the order in layer.
        /// </summary>
        private static LayerSliderController orderInLayerSlider;

        /// <summary>
        /// The input field with their up and down button for the font size.
        /// </summary>
        private static InputFieldWithButtons fontSizeInput;
        #endregion

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private TextMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static TextMenu Instance { get; private set; }

        /// <summary>
        /// The init constructor that create the instance for the text menu.
        /// It hides the text menu by default.
        /// </summary>
        static TextMenu()
        {
            Instance = new TextMenu();
            Instance.Instantiate(textMenuPrefab);
            InitBtn();
            Enable();
        }

        /// <summary>
        /// Returns true if the menu is already opened.
        /// </summary>
        /// <returns>True if the menu is alreay opened. Otherwise false.</returns>
        public override bool IsOpen()
        {
            return Instance.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Assigns the corresponding objects of the TextMenu instance to the buttons, sliders and other GameObjects.
        /// It also adds the initial handlers to the components.
        /// </summary>
        private static void InitBtn()
        {
            /// Initialize the objects for the font style buttons
            GameObject bold = GameFinder.FindChild(Instance.gameObject, "Bold");
            boldBtn = bold.GetComponent<Button>();
            boldBMB = bold.GetComponent<ButtonManagerBasic>();
            bold.AddComponent<UIHoverTooltip>().SetMessage("Bold");

            GameObject italic = GameFinder.FindChild(Instance.gameObject, "Italic");
            italicBtn = italic.GetComponent<Button>();
            italicBMB = italic.GetComponent<ButtonManagerBasic>();
            italic.AddComponent<UIHoverTooltip>().SetMessage("Italic");

            GameObject underline = GameFinder.FindChild(Instance.gameObject, "Underline");
            underlineBtn = underline.GetComponent<Button>();
            underlineBMB = underline.GetComponent<ButtonManagerBasic>();
            underline.AddComponent<UIHoverTooltip>().SetMessage("Underline");

            GameObject strikethrough = GameFinder.FindChild(Instance.gameObject, "Strikethrough");
            strikethroughBtn = strikethrough.GetComponent<Button>();
            strikethroughBMB = strikethrough.GetComponent<ButtonManagerBasic>();
            strikethrough.AddComponent<UIHoverTooltip>().SetMessage("Strikethrough");

            GameObject lowerCase = GameFinder.FindChild(Instance.gameObject, "LowerCase");
            lowerCaseBtn = lowerCase.GetComponent<Button>();
            lowerCaseBMB = lowerCase.GetComponent<ButtonManagerBasic>();
            lowerCase.AddComponent<UIHoverTooltip>().SetMessage("Lower Case");

            GameObject upperCase = GameFinder.FindChild(Instance.gameObject, "UpperCase");
            upperCaseBtn = upperCase.GetComponent<Button>();
            upperCaseBMB = upperCase.GetComponent<ButtonManagerBasic>();
            upperCase.AddComponent<UIHoverTooltip>().SetMessage("Upper Case");

            GameObject smallCaps = GameFinder.FindChild(Instance.gameObject, "SmallCaps");
            smallCapsBtn = smallCaps.GetComponent<Button>();
            smallCapsBMB = smallCaps.GetComponent<ButtonManagerBasic>();
            smallCaps.AddComponent<UIHoverTooltip>().SetMessage("Small Caps");

            /// Initialize the handler for the buttons
            InitFontStyleButtons();

            /// Initialize button colors for not selected and selected.
            notSelectedBlock = boldBtn.colors;
            selectedBlock = notSelectedBlock;
            selectedBlock.normalColor = selectedBlock.selectedColor = selectedBlock.disabledColor =
                selectedBlock.highlightedColor = selectedBlock.pressedColor = Color.gray;

            /// Initialize the font color button and adds an exclusion mechanism with the outline color button.
            fontColorBtn = GameFinder.FindChild(Instance.gameObject, "FontColorBtn").GetComponent<Button>();
            fontColorBMB = GameFinder.FindChild(Instance.gameObject, "FontColorBtn").GetComponent<ButtonManagerBasic>();
            fontColorBtn.interactable = false;
            fontColorBMB.clickEvent.AddListener(MutuallyExclusiveColorButtons);

            /// Initialize the outline color button and adds an exclusion mechanism with the font color button.
            outlineColorBtn = GameFinder.FindChild(Instance.gameObject, "OutlineColorBtn").GetComponent<Button>();
            outlineColorBMB = GameFinder.FindChild(Instance.gameObject, "OutlineColorBtn").GetComponent<ButtonManagerBasic>();
            outlineColorBMB.clickEvent.AddListener(MutuallyExclusiveColorButtons);

            /// Initialize the outline switch and their layer.
            outlineSwitchLayer = GameFinder.FindChild(Instance.gameObject, "Outline");
            outlineSwitch = outlineSwitchLayer.GetComponentInChildren<SwitchManager>();
            outlineSwitchLayer.SetActive(false);

            /// Initialize the font outline thickness slider.
            thicknessLayer = GameFinder.FindChild(Instance.gameObject, "Thickness");
            thicknessSlider = thicknessLayer.GetComponentInChildren<FloatValueSliderController>();
            thicknessLayer.SetActive(false);

            /// Initialize the remaining GUI elements.
            picker = Instance.gameObject.GetComponentInChildren<HSVPicker.ColorPicker>();
            fontSizeInput = GameFinder.FindChild(Instance.gameObject, "FontSize").GetComponentInChildren<InputFieldWithButtons>();
            editText = GameFinder.FindChild(Instance.gameObject, "EditText");
            editTextBMB = editText.GetComponentInChildren<ButtonManagerBasic>();
            orderInLayer = GameFinder.FindChild(Instance.gameObject, "Layer");
            orderInLayerSlider = orderInLayer.GetComponentInChildren<LayerSliderController>();
        }

        /// <summary>
        /// Adds the inital handlers to the font style buttons.
        /// </summary>
        private static void InitFontStyleButtons()
        {
            boldBMB.clickEvent.AddListener(() => Press(Bold));
            italicBMB.clickEvent.AddListener(() => Press(Italic));
            underlineBMB.clickEvent.AddListener(() => Press(Underline));
            strikethroughBMB.clickEvent.AddListener(() => Press(Strikethrough));
            lowerCaseBMB.clickEvent.AddListener(() => Press(LowerCase));
            upperCaseBMB.clickEvent.AddListener(() => Press(UpperCase));
            smallCapsBMB.clickEvent.AddListener(() => Press(SmallCaps));
        }

        /// <summary>
        /// To hide the text menu.
        /// It enables the keyboard shortcuts.
        /// </summary>
        public override void Disable()
        {
            base.Disable();
            Instance.gameObject.transform.Find("ReturnBtn").gameObject.SetActive(false);
            SEEInput.KeyboardShortcutsEnabled = true;
        }

        /// <summary>
        /// Resets the text menu to its initial state.
        /// </summary>
        private static void Reset()
        {
            /// Resets the font styles.
            ResetStyles();

            fontColorBMB.clickEvent.RemoveAllListeners();
            fontColorBMB.clickEvent.AddListener(MutuallyExclusiveColorButtons);
            outlineColorBMB.clickEvent.RemoveAllListeners();
            outlineColorBMB.clickEvent.AddListener(MutuallyExclusiveColorButtons);
            thicknessSlider.onValueChanged.RemoveAllListeners();
            outlineSwitch.OffEvents.RemoveAllListeners();
            outlineSwitch.OnEvents.RemoveAllListeners();
            fontSizeInput.OnValueChanged.RemoveAllListeners();
            orderInLayerSlider.OnValueChanged.RemoveAllListeners();
        }

        /// <summary>
        /// Reveals the text menu.
        /// </summary>
        /// <param name="reset">Specifies whether the menu should be reset to its initial state.</param>
        /// <param name="showEditMode">Specifies whether the menu should be opened for edit mode.
        /// Otherwise it will be opened for the WriteTextAction.</param>
        public static void Enable(bool reset = true, bool showEditMode = false)
        {
            /// Resets the handlers, if desired (depending on <paramref name="reset").
            if (reset)
            {
                Reset();
            }

            /// Determines whether the text menu should be launched for the edit mode or not.
            if (showEditMode)
            {
                /// If yes, the order in layer and the edit text is available.
                orderInLayer.SetActive(true);
                GameFinder.FindChild(Instance.gameObject, "Layer").GetComponentInChildren<Slider>().interactable = true;
                editText.SetActive(true);
            }
            else
            {
                /// If not order in layer and edit text are disabled.
                orderInLayer.SetActive(false);
                editText.SetActive(false);
            }
            /// Re-calculate the text menu height.
            MenuHelper.CalculateHeight(Instance.gameObject);

            /// Makes the menu active.
            Instance.gameObject.SetActive(true);
        }

        /// <summary>
        /// Reveals the text menu
        /// </summary>
        /// <param name="colorAction">The inital action for the HSV color picker.</param>
        /// <param name="color">The inital color for the HSV color picker.</param>
        /// <param name="reset">Specifies whether the menu should be reset to its initial state.</param>
        /// <param name="showEditMode">Specifies whether the menu should be opened for edit mode.
        /// Otherwise it will be opened for the WriteTextAction.</param>
        private static void EnableTextMenu(UnityAction<Color> colorAction, Color color, bool reset = true,
            bool showEditMode = false)
        {
            /// Resets the handlers, if desired (depending on <paramref name="reset").
            if (reset)
            {
                Reset();
            }

            /// Determines whether the text menu should be launched for the edit mode or not.
            if (showEditMode)
            {
                /// If yes, the order in layer and the edit text is available.
                orderInLayer.SetActive(true);
                GameFinder.FindChild(Instance.gameObject, "Layer").GetComponentInChildren<Slider>().interactable = true;
                editText.SetActive(true);
            }
            else
            {
                /// If not order in layer and edit text are disabled.
                orderInLayer.SetActive(false);
                editText.SetActive(false);
            }
            /// Makes the menu active.
            Instance.gameObject.SetActive(true);

            /// Toggles the interactable of the mutually buttons.
            if (fontColorBtn.interactable)
            {
                MutuallyExclusiveColorButtons();
            }

            /// Adds the color action to the <see cref="HSVPicker.ColorPicker"/>.
            AssignColorArea(colorAction, color);

            /// Re-calculate the menu height.
            MenuHelper.CalculateHeight(Instance.gameObject);
        }

        /// <summary>
        /// Provides the text menu for writing action. It adds the needed handlers to the respective components.
        /// </summary>
        public static void EnableForWriting()
        {
            /// Enables the text menu in writing mode.
            EnableTextMenu(color => ValueHolder.CurrentPrimaryColor = color, ValueHolder.CurrentPrimaryColor, true);

            /// Disables the return button.
            Instance.gameObject.transform.Find("ReturnBtn").gameObject.SetActive(false);

            /// Adds the handler for the font color button.
            /// It saves the changes in the global value for the primary color <see cref="ValueHolder.CurrentPrimaryColor"/>.
            fontColorBMB.clickEvent.AddListener(() =>
            {
                AssignColorArea(color => ValueHolder.CurrentPrimaryColor = color, ValueHolder.CurrentPrimaryColor);
                MenuHelper.CalculateHeight(Instance.gameObject);
            });

            /// Adds the handler for the outline color button.
            AssignOutlineThicknessForWriting();

            /// Adds the handler for the outline thickness slider.
            /// It saves the changes in the global value for the outline thickness <see cref="ValueHolder.CurrentOutlineThickness"/>.
            AssignOutlineThickness(thickness => ValueHolder.CurrentOutlineThickness = thickness,
                ValueHolder.CurrentOutlineThickness);

            /// Disables the outline color.
            outlineSwitch.isOn = false;
            outlineSwitch.UpdateUI();

            /// Adds the handler for the font size component.
            AssignFontSize(size => ValueHolder.CurrentFontSize = size, ValueHolder.CurrentFontSize);

            /// Re-calculate the menu height.
            MenuHelper.CalculateHeight(Instance.gameObject);
        }

        /// <summary>
        /// Adds the handler for the outline color button.
        /// It saves the changes in the global value for the secondary color
        /// <see cref="ValueHolder.CurrentSecondaryColor"/>.
        ///
        /// Checks the current secondary color before assigning it.
        /// If it is clear (completely transparent), a new random color is chosen.
        /// Subsequently, the alpha value is checked, which also indicates transparency.
        /// If it is set to 0, the color would also be fully transparent.
        /// In this case, the alpha would be set to full visibility.
        /// </summary>
        private static void AssignOutlineThicknessForWriting()
        {
            outlineColorBMB.clickEvent.AddListener(() =>
            {
                /// If the <see cref="GameDrawer.LineKind"/> was <see cref="GameDrawer.LineKind.Solid"/> before,
                /// the secondary color is clear.
                /// Therefore, a random color is added first,
                /// and if the color's alpha is 0, it is set to 255 to ensure the color is not transparent.
                if (ValueHolder.CurrentSecondaryColor == Color.clear)
                {
                    ValueHolder.CurrentSecondaryColor = Random.ColorHSV();
                }

                if (ValueHolder.CurrentSecondaryColor.a == 0)
                {
                    ValueHolder.CurrentSecondaryColor = new Color(ValueHolder.CurrentSecondaryColor.r,
                        ValueHolder.CurrentSecondaryColor.g, ValueHolder.CurrentSecondaryColor.b, 255);
                }
                AssignColorArea(color => ValueHolder.CurrentSecondaryColor = color, ValueHolder.CurrentSecondaryColor);
                MenuHelper.CalculateHeight(Instance.gameObject);
            });
        }

        /// <summary>
        /// Provides the text menu for editing, adding the necessary handlers to the respective components.
        /// </summary>
        /// <param name="selectedText">The selected text object for editing.</param>
        /// <param name="newValueHolder">The <see cref="TextConf"/> value holder. If differnt
        /// from this type, nothing happens.</param>
        /// <param name="returnCall">The return call action to return to the parent menu.</param>
        public static void EnableForEditing(GameObject selectedText, DrawableType newValueHolder,
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
                outlineSwitch.isOn = textHolder.IsOutlined;
                outlineSwitch.UpdateUI();

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
                orderInLayerSlider.AssignMaxOrder(surface.GetComponent<DrawableHolder>().OrderInLayer);
                /// Adds the handler to the order in layer slider.
                /// Changes are saved in the configuration.
                AssignOrderInLayer(order =>
                {
                    GameEdit.ChangeLayer(selectedText, order);
                    textHolder.OrderInLayer = order;
                    new EditTextNetAction(surface.name, surfaceParentName, TextConf.GetText(selectedText)).Execute();
                }, textHolder.OrderInLayer);

                /// Re-calculate the menus height.
                MenuHelper.CalculateHeight(Instance.gameObject);
            }
        }

        /// <summary>
        /// Adds the return call to the return button.
        /// It deactivates the slider for the order in layer because this
        /// variation is only used by the edit mind map node,
        /// and the layer for the mind map text must not be altered.
        /// </summary>
        /// <param name="returnCall">The return call action to return to the parent menu.</param>
        private static void AddReturnCall(UnityAction returnCall)
        {
            if (returnCall != null)
            {
                GameObject returnButton = Instance.gameObject.transform.Find("ReturnBtn").gameObject;
                returnButton.SetActive(true);
                ButtonManagerBasic returnBtn = returnButton.GetComponent<ButtonManagerBasic>();
                returnBtn.clickEvent.RemoveAllListeners();
                returnBtn.clickEvent.AddListener(returnCall);
                GameFinder.FindChild(Instance.gameObject, "Layer").GetComponentInChildren<Slider>().interactable = false;
            }
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
        private static void AddFontColorButtonForEdit(GameObject selectedText, TextConf textHolder,
            GameObject surface, string surfaceParentName)
        {
            fontColorBMB.clickEvent.AddListener(() =>
            {
                AssignColorArea(color =>
                {
                    GameEdit.ChangeFontColor(selectedText, color);
                    textHolder.FontColor = color;
                    new EditTextNetAction(surface.name, surfaceParentName, TextConf.GetText(selectedText)).Execute();
                }, textHolder.FontColor);
                MenuHelper.CalculateHeight(Instance.gameObject);
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
        private static void AddOutlineColorButtonForEdit(GameObject selectedText, TextConf textHolder,
            GameObject surface, string surfaceParentName)
        {
            outlineColorBMB.clickEvent.AddListener(() =>
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
                MenuHelper.CalculateHeight(Instance.gameObject);
            });
        }

        /// <summary>
        /// This method will be used as an action for the handler of the color buttons (font/outline).
        /// This allows only one color to be active at a time.
        /// </summary>
        private static void MutuallyExclusiveColorButtons()
        {
            fontColorBtn.interactable = !fontColorBtn.IsInteractable();
            outlineColorBtn.interactable = !outlineColorBtn.IsInteractable();
            if (!outlineColorBtn.interactable)
            {
                thicknessLayer.SetActive(true);
                outlineSwitchLayer.SetActive(true);
            }
            else
            {
                thicknessLayer.SetActive(false);
                outlineSwitchLayer.SetActive(false);
            }
            /// Re-calculate the text menu height.
            MenuHelper.CalculateHeight(Instance.gameObject, true);
        }

        /// <summary>
        /// Assigns an action and a color to the HSV Color Picker.
        /// </summary>
        /// <param name="colorAction">The color action that should be assigned.</param>
        /// <param name="color">The color that should be assigned.</param>
        public static void AssignColorArea(UnityAction<Color> colorAction, Color color)
        {
            if (pickerAction != null)
            {
                picker.onValueChanged.RemoveListener(pickerAction);
            }
            pickerAction = colorAction;
            picker.AssignColor(color);
            picker.onValueChanged.AddListener(colorAction);
        }

        /// <summary>
        /// Assigns an action and a thickness to the outline thickness slider.
        /// </summary>
        /// <param name="thicknessAction">The float action that should be assigned.</param>
        /// <param name="thickness">The thickness that should be assigned.</param>
        public static void AssignOutlineThickness(UnityAction<float> thicknessAction, float thickness)
        {
            thicknessSlider.onValueChanged.RemoveAllListeners();
            thicknessSlider.AssignValue(thickness);
            thicknessSlider.onValueChanged.AddListener(thicknessAction);
        }

        /// <summary>
        /// Assigns the action to edit the outline color status.
        /// </summary>
        /// <param name="selectedText">The chosen text to be edited.</param>
        /// <param name="textHolder">The configuration which holds the new value.</param>
        /// <param name="surface">The drawable surface on which the text is displayed.</param>
        /// <param name="surfaceParentName">The id of the drawable surface parent.</param>
        public static void AssignOutlineStatus(GameObject selectedText, TextConf textHolder,
            GameObject surface, string surfaceParentName)
        {
            outlineSwitch.OffEvents.AddListener(() =>
            {
                GameTexter.ChangeOutlineStatus(selectedText, false);
                textHolder.IsOutlined = false;
                new EditTextNetAction(surface.name, surfaceParentName, TextConf.GetText(selectedText)).Execute();
            });

            outlineSwitch.OnEvents.AddListener(() =>
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
        public static void AssignFontSize(UnityAction<float> fontSizeAction, float fontSize)
        {
            fontSizeInput.OnValueChanged.RemoveAllListeners();
            fontSizeInput.AssignValue(fontSize);
            fontSizeInput.OnValueChanged.AddListener(fontSizeAction);
        }

        /// <summary>
        /// Assigns an action and an order to the order in layer slider.
        /// </summary>
        /// <param name="orderInLayerAction">The action that should be assigned.</param>
        /// <param name="order">The order that should be assigned.</param>
        public static void AssignOrderInLayer(UnityAction<int> orderInLayerAction, int order)
        {
            orderInLayerSlider.OnValueChanged.RemoveAllListeners();
            orderInLayerSlider.AssignValue(order);
            orderInLayerSlider.OnValueChanged.AddListener(orderInLayerAction);
        }

        /// <summary>
        /// Assigns an action to the edit text button.
        /// </summary>
        /// <param name="action">The action that should be assigned.</param>
        public static void AssignEditTextButton(UnityAction action)
        {
            editTextBMB.clickEvent.RemoveAllListeners();
            editTextBMB.clickEvent.AddListener(action);
        }

        /// <summary>
        /// Assigns an action and font styles to the font style buttons.
        /// </summary>
        /// <param name="action">The font styles action that should be assigned.</param>
        /// <param name="styles">The styles that should be assigned.</param>
        public static void AssignFontStyles(UnityAction<FontStyles> action, FontStyles styles)
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
        public static void Press(string pressedStyle)
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
        private static void MutuallyExclusiveStyles(string selectedStyle)
        {
            switch (selectedStyle)
            {
                case LowerCase:
                    styles[UpperCase] = false;
                    upperCaseBtn.colors = notSelectedBlock;
                    styles[SmallCaps] = false;
                    smallCapsBtn.colors = notSelectedBlock;
                    break;
                case UpperCase:
                    styles[LowerCase] = false;
                    lowerCaseBtn.colors = notSelectedBlock;
                    styles[SmallCaps] = false;
                    smallCapsBtn.colors = notSelectedBlock;
                    break;
                case SmallCaps:
                    styles[LowerCase] = false;
                    lowerCaseBtn.colors = notSelectedBlock;
                    styles[UpperCase] = false;
                    upperCaseBtn.colors = notSelectedBlock;
                    break;
            }
        }

        /// <summary>
        /// Returns the corresponding button for a given string with a style name.
        /// </summary>
        /// <param name="pressedStyle">The given style name.</param>
        /// <returns>The corresponding button.</returns>
        private static Button GetPressedButton(string pressedStyle)
        {
            Button btn = null;
            switch (pressedStyle)
            {
                case Bold:
                    btn = boldBtn;
                    break;
                case Italic:
                    btn = italicBtn;
                    break;
                case Underline:
                    btn = underlineBtn;
                    break;
                case Strikethrough:
                    btn = strikethroughBtn;
                    break;
                case LowerCase:
                    btn = lowerCaseBtn;
                    break;
                case UpperCase:
                    btn = upperCaseBtn;
                    break;
                case SmallCaps:
                    btn = smallCapsBtn;
                    break;
            }
            return btn;
        }

        /// <summary>
        /// Sets the font style stats in dictionary <see cref="styles"/> to false
        /// and changes the color block to not selected.
        /// </summary>
        private static void ResetStyles()
        {
            foreach (string key in styles.Keys.ToList())
            {
                styles[key] = false;
            }
            boldBtn.colors = notSelectedBlock;
            italicBtn.colors = notSelectedBlock;
            underlineBtn.colors = notSelectedBlock;
            strikethroughBtn.colors = notSelectedBlock;
            lowerCaseBtn.colors = notSelectedBlock;
            upperCaseBtn.colors = notSelectedBlock;
            smallCapsBtn.colors = notSelectedBlock;

            if (fontStyleAction != null)
            {
                fontStyleAction = null;
            }
        }

        /// <summary>
        /// Assigns the respective font styles their value and
        /// changes their button color when they are selected.
        /// </summary>
        /// <param name="style">Style to be assigned.</param>
        private static void AssignStyles(FontStyles style)
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
        private static FontStyles GetFontStyleOfKey(string key)
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
        public static FontStyles GetFontStyle()
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
        public static bool IsOutlineEnabled()
        {
            return outlineSwitch.isOn;
        }
    }
}
