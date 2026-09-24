using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Editing;
using SEE.Game.Drawable.Text;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.UI.Notification;
using SEE.UI.PropertyDialog.Drawable;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable.Text
{
    /// <summary>
    /// Configures the shared text-menu controls for editing an existing text object.
    /// Changes are applied directly to the selected text object and synchronized
    /// through the corresponding network actions.
    /// </summary>
    internal sealed class EditTextMenu
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
        /// Manages the shared text-style behavior used by writing and editing.
        /// </summary>
        private readonly TextStyleMenu textStyleMenu;

        /// <summary>
        /// Initializes the editing-specific part of the text menu.
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
        internal EditTextMenu(
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
        /// Configures the text menu for editing an existing text object.
        /// The current configuration is assigned to the shared controls and
        /// subsequent user changes are applied to the selected text object,
        /// stored in its configuration, and synchronized through network actions.
        /// </summary>
        /// <param name="selectedText">
        /// The selected text object to be edited.
        /// </param>
        /// <param name="newValueHolder">
        /// The configuration containing the editable text values.
        /// If it is not a <see cref="TextConf"/>, no editing controls are configured.
        /// </param>
        /// <param name="returnCall">
        /// An optional callback returning to the parent menu.
        /// </param>
        internal void Enable(
            GameObject selectedText,
            DrawableType newValueHolder,
            UnityAction returnCall = null)
        {
            if (newValueHolder is not TextConf textHolder)
            {
                return;
            }

            GameObject surface = GameFinder.GetDrawableSurface(selectedText);
            string surfaceParentName =
                GameFinder.GetDrawableSurfaceParentName(surface);

            enableTextMenu(
                color =>
                {
                    GameTextEdit.ChangeFontColor(selectedText, color);
                    textHolder.FontColor = color;

                    new EditTextNetAction(
                        surface.name,
                        surfaceParentName,
                        TextConf.GetText(selectedText)).Execute();
                },
                textHolder.FontColor,
                true,
                true);

            SetUpReturnButton(returnCall);

            SetUpFontColorButton(
                selectedText,
                textHolder,
                surface,
                surfaceParentName);

            SetUpOutlineColorButton(
                selectedText,
                textHolder,
                surface,
                surfaceParentName);

            textStyleMenu.AssignOutlineThickness(
                thickness =>
                {
                    GameTextEdit.ChangeOutlineThickness(selectedText, thickness);
                    textHolder.OutlineThickness = thickness;

                    new EditTextNetAction(
                        surface.name,
                        surfaceParentName,
                        TextConf.GetText(selectedText)).Execute();
                },
                textHolder.OutlineThickness);

            SetUpOutlineStatus(
                selectedText,
                textHolder,
                surface,
                surfaceParentName);

            controls.OutlineSwitch.isOn = textHolder.IsOutlined;
            controls.OutlineSwitch.UpdateUI();

            textStyleMenu.AssignFontSize(
                size =>
                {
                    GameTextEdit.ChangeFontSize(selectedText, size);
                    textHolder.FontSize = size;

                    new EditTextNetAction(
                        surface.name,
                        surfaceParentName,
                        TextConf.GetText(selectedText)).Execute();
                },
                textHolder.FontSize);

            textStyleMenu.AssignFontStyles(
                style =>
                {
                    GameTextEdit.ChangeFontStyles(selectedText, style);
                    textHolder.FontStyles = style;

                    new EditTextNetAction(
                        surface.name,
                        surfaceParentName,
                        TextConf.GetText(selectedText)).Execute();
                },
                textHolder.FontStyles);

            SetUpEditTextButton(
                selectedText,
                textHolder,
                surface,
                surfaceParentName);

            controls.OrderInLayerSlider.AssignMaxOrder(
                surface.GetComponent<DrawableHolder>().OrderInLayer);

            SetUpOrderInLayer(
                selectedText,
                textHolder,
                surface,
                surfaceParentName);

            MenuHelper.CalculateHeight(textMenu);
        }

        /// <summary>
        /// Configures the optional return button used to leave the text-editing
        /// menu and return to its parent menu.
        /// </summary>
        /// <param name="returnCall">
        /// The callback invoked when the return button is pressed.
        /// If it is null, the return button is not configured.
        /// </param>
        private void SetUpReturnButton(UnityAction returnCall)
        {
            if (returnCall == null)
            {
                return;
            }

            controls.ReturnButtonObject.SetActive(true);
            controls.ReturnButtonManager.clickEvent.RemoveAllListeners();
            controls.ReturnButtonManager.clickEvent.AddListener(returnCall);

            /// When text editing is opened from a parent menu, the text must keep
            /// the order in layer controlled by its parent drawable object.
            controls.OrderInLayerUnitySlider.interactable = false;
        }

        /// <summary>
        /// Configures the font-color button for editing the selected text object.
        /// When the color picker value changes, the new font color is applied to
        /// the text object, stored in its configuration, and synchronized.
        /// </summary>
        /// <param name="selectedText">
        /// The selected text object to be edited.
        /// </param>
        /// <param name="textHolder">
        /// The configuration containing the edited text values.
        /// </param>
        /// <param name="surface">
        /// The drawable surface containing the selected text object.
        /// </param>
        /// <param name="surfaceParentName">
        /// The name of the drawable surface's parent.
        /// </param>
        private void SetUpFontColorButton(
            GameObject selectedText,
            TextConf textHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.FontColorButtonManager.clickEvent.AddListener(() =>
            {
                textStyleMenu.AssignColorArea(
                    color =>
                    {
                        GameTextEdit.ChangeFontColor(selectedText, color);
                        textHolder.FontColor = color;

                        new EditTextNetAction(
                            surface.name,
                            surfaceParentName,
                            TextConf.GetText(selectedText)).Execute();
                    },
                    textHolder.FontColor);

                MenuHelper.CalculateHeight(textMenu);
            });
        }

        /// <summary>
        /// Configures the outline-color button for editing the selected text object.
        /// A visible outline color is ensured before the color picker is configured.
        /// Subsequent color changes are applied to the text object, stored in its
        /// configuration, and synchronized.
        /// </summary>
        /// <param name="selectedText">
        /// The selected text object to be edited.
        /// </param>
        /// <param name="textHolder">
        /// The configuration containing the edited text values.
        /// </param>
        /// <param name="surface">
        /// The drawable surface containing the selected text object.
        /// </param>
        /// <param name="surfaceParentName">
        /// The name of the drawable surface's parent.
        /// </param>
        private void SetUpOutlineColorButton(
            GameObject selectedText,
            TextConf textHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.OutlineColorButtonManager.clickEvent.AddListener(() =>
            {
                if (textHolder.OutlineColor == Color.clear)
                {
                    textHolder.OutlineColor = Random.ColorHSV();
                }

                if (textHolder.OutlineColor.a == 0)
                {
                    textHolder.OutlineColor = new Color(
                        textHolder.OutlineColor.r,
                        textHolder.OutlineColor.g,
                        textHolder.OutlineColor.b,
                        255);
                }

                textStyleMenu.AssignColorArea(
                    color =>
                    {
                        GameTextEdit.ChangeOutlineColor(selectedText, color);
                        textHolder.OutlineColor = color;

                        new EditTextNetAction(
                            surface.name,
                            surfaceParentName,
                            TextConf.GetText(selectedText)).Execute();
                    },
                    textHolder.OutlineColor);

                MenuHelper.CalculateHeight(textMenu);
            });
        }

        /// <summary>
        /// Configures the outline switch for the selected text object.
        /// Changes to the outline state are applied to the object, stored in its
        /// configuration, and synchronized.
        /// </summary>
        /// <param name="selectedText">
        /// The selected text object to be edited.
        /// </param>
        /// <param name="textHolder">
        /// The configuration containing the edited text values.
        /// </param>
        /// <param name="surface">
        /// The drawable surface containing the selected text object.
        /// </param>
        /// <param name="surfaceParentName">
        /// The name of the drawable surface's parent.
        /// </param>
        private void SetUpOutlineStatus(
            GameObject selectedText,
            TextConf textHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.OutlineSwitch.OffEvents.AddListener(() =>
            {
                GameTexter.ChangeOutlineStatus(selectedText, false);
                textHolder.IsOutlined = false;

                new EditTextNetAction(
                    surface.name,
                    surfaceParentName,
                    TextConf.GetText(selectedText)).Execute();
            });

            controls.OutlineSwitch.OnEvents.AddListener(() =>
            {
                GameTexter.ChangeOutlineStatus(selectedText, true);
                textHolder.IsOutlined = true;

                TextMeshPro tmp = selectedText.GetComponent<TextMeshPro>();

                if (textHolder.OutlineColor != tmp.outlineColor
                    && tmp.outlineColor == Color.clear)
                {
                    GameTextEdit.ChangeOutlineColor(
                        selectedText,
                        textHolder.OutlineColor);
                }

                new EditTextNetAction(
                    surface.name,
                    surfaceParentName,
                    TextConf.GetText(selectedText)).Execute();
            });
        }

        /// <summary>
        /// Configures the button used to edit the textual content of the selected
        /// text object. The dialog is initialized with the current text and valid
        /// changes are applied to the object, stored, and synchronized.
        /// </summary>
        /// <param name="selectedText">
        /// The selected text object to be edited.
        /// </param>
        /// <param name="textHolder">
        /// The configuration containing the edited text values.
        /// </param>
        /// <param name="surface">
        /// The drawable surface containing the selected text object.
        /// </param>
        /// <param name="surfaceParentName">
        /// The name of the drawable surface's parent.
        /// </param>
        private void SetUpEditTextButton(
            GameObject selectedText,
            TextConf textHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.EditTextButtonManager.clickEvent.RemoveAllListeners();

            controls.EditTextButtonManager.clickEvent.AddListener(() =>
            {
                WriteEditTextDialog writeTextDialog = new();
                writeTextDialog.SetStringInit(textHolder.Text);

                UnityAction<string> stringAction = textOut =>
                {
                    if (textOut != null && textOut != "")
                    {
                        TextMeshPro tmp =
                            selectedText.GetComponent<TextMeshPro>();

                        tmp.rectTransform.sizeDelta =
                            GameTexter.CalculateWidthAndHeight(
                                textOut,
                                tmp.font,
                                textHolder.FontSize,
                                textHolder.FontStyles);

                        GameTextEdit.ChangeText(selectedText, textOut);
                        textHolder.Text = textOut;

                        new EditTextNetAction(
                            surface.name,
                            surfaceParentName,
                            TextConf.GetText(selectedText)).Execute();
                    }
                    else
                    {
                        ShowNotification.Warn(
                            "Empty text",
                            "The text to write is empty. Please add one.");
                    }
                };

                writeTextDialog.Open(stringAction);
            });
        }

        /// <summary>
        /// Configures the order-in-layer control for the selected text object.
        /// Changes are applied to the text object, stored in its configuration,
        /// and synchronized.
        /// </summary>
        /// <param name="selectedText">
        /// The selected text object to be edited.
        /// </param>
        /// <param name="textHolder">
        /// The configuration containing the edited text values.
        /// </param>
        /// <param name="surface">
        /// The drawable surface containing the selected text object.
        /// </param>
        /// <param name="surfaceParentName">
        /// The name of the drawable surface's parent.
        /// </param>
        private void SetUpOrderInLayer(
            GameObject selectedText,
            TextConf textHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.OrderInLayerSlider.OnValueChanged.RemoveAllListeners();
            controls.OrderInLayerSlider.AssignValue(textHolder.OrderInLayer);

            controls.OrderInLayerSlider.OnValueChanged.AddListener(order =>
            {
                GameLayerChanger.SetOrderInLayer(selectedText, order);
                textHolder.OrderInLayer = order;

                new EditTextNetAction(
                    surface.name,
                    surfaceParentName,
                    TextConf.GetText(selectedText)).Execute();
            });
        }
    }
}
