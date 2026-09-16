using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable;
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
        /// Assigns the active font styles and the action reacting to style changes.
        /// </summary>
        private readonly System.Action<UnityAction<FontStyles>, FontStyles> assignFontStyles;

        /// <summary>
        /// Initializes the editing-specific part of the text menu.
        /// </summary>
        /// <param name="textMenu">The root object of the complete text menu.</param>
        /// <param name="controls">The shared text-menu controls.</param>
        /// <param name="enableTextMenu">Enables and prepares the shared text menu.</param>
        /// <param name="assignColorArea">Assigns the color-picker action and initial color.</param>
        /// <param name="assignOutlineThickness">
        /// Assigns the outline-thickness action and initial value.
        /// </param>
        /// <param name="assignFontSize">Assigns the font-size action and initial value.</param>
        /// <param name="assignFontStyles">
        /// Assigns the active font styles and style-change action.
        /// </param>
        internal EditTextMenu(
            GameObject textMenu,
            TextMenuControls controls,
            System.Action<UnityAction<Color>, Color, bool, bool> enableTextMenu,
            System.Action<UnityAction<Color>, Color> assignColorArea,
            System.Action<UnityAction<float>, float> assignOutlineThickness,
            System.Action<UnityAction<float>, float> assignFontSize,
            System.Action<UnityAction<FontStyles>, FontStyles> assignFontStyles)
        {
            this.textMenu = textMenu;
            this.controls = controls;
            this.enableTextMenu = enableTextMenu;
            this.assignColorArea = assignColorArea;
            this.assignOutlineThickness = assignOutlineThickness;
            this.assignFontSize = assignFontSize;
            this.assignFontStyles = assignFontStyles;
        }

        /// <summary>
        /// Configures the text menu for editing an existing text object.
        /// </summary>
        /// <param name="selectedText">The selected text object.</param>
        /// <param name="newValueHolder">
        /// The configuration containing the editable text values.
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
                    GameEdit.ChangeFontColor(selectedText, color);
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

            assignOutlineThickness(
                thickness =>
                {
                    GameEdit.ChangeOutlineThickness(selectedText, thickness);
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

            assignFontSize(
                size =>
                {
                    GameEdit.ChangeFontSize(selectedText, size);
                    textHolder.FontSize = size;

                    new EditTextNetAction(
                        surface.name,
                        surfaceParentName,
                        TextConf.GetText(selectedText)).Execute();
                },
                textHolder.FontSize);

            assignFontStyles(
                style =>
                {
                    GameEdit.ChangeFontStyles(selectedText, style);
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
        /// Configures the optional return button.
        /// </summary>
        /// <param name="returnCall">
        /// The callback returning to the parent menu.
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

            controls.OrderInLayerUnitySlider.interactable = false;
        }

        /// <summary>
        /// Configures font-color editing.
        /// </summary>
        /// <param name="selectedText">The selected text object.</param>
        /// <param name="textHolder">The configuration containing the edited values.</param>
        /// <param name="surface">The drawable surface containing the text.</param>
        /// <param name="surfaceParentName">The drawable surface parent's name.</param>
        private void SetUpFontColorButton(
            GameObject selectedText,
            TextConf textHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.FontColorButtonManager.clickEvent.AddListener(() =>
            {
                assignColorArea(
                    color =>
                    {
                        GameEdit.ChangeFontColor(selectedText, color);
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
        /// Configures outline-color editing.
        /// </summary>
        /// <param name="selectedText">The selected text object.</param>
        /// <param name="textHolder">The configuration containing the edited values.</param>
        /// <param name="surface">The drawable surface containing the text.</param>
        /// <param name="surfaceParentName">The drawable surface parent's name.</param>
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

                assignColorArea(
                    color =>
                    {
                        GameEdit.ChangeOutlineColor(selectedText, color);
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
        /// Configures the outline enabled state.
        /// </summary>
        /// <param name="selectedText">The selected text object.</param>
        /// <param name="textHolder">The configuration containing the edited values.</param>
        /// <param name="surface">The drawable surface containing the text.</param>
        /// <param name="surfaceParentName">The drawable surface parent's name.</param>
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
                    GameEdit.ChangeOutlineColor(
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
        /// Configures the button for editing the text content.
        /// </summary>
        /// <param name="selectedText">The selected text object.</param>
        /// <param name="textHolder">The configuration containing the edited values.</param>
        /// <param name="surface">The drawable surface containing the text.</param>
        /// <param name="surfaceParentName">The drawable surface parent's name.</param>
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

                        GameEdit.ChangeText(selectedText, textOut);
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
        /// Configures editing of the text object's order in layer.
        /// </summary>
        /// <param name="selectedText">The selected text object.</param>
        /// <param name="textHolder">The configuration containing the edited values.</param>
        /// <param name="surface">The drawable surface containing the text.</param>
        /// <param name="surfaceParentName">The drawable surface parent's name.</param>
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
                GameEdit.ChangeLayer(selectedText, order);
                textHolder.OrderInLayer = order;

                new EditTextNetAction(
                    surface.name,
                    surfaceParentName,
                    TextConf.GetText(selectedText)).Execute();
            });
        }
    }
}
