using Michsky.MUIP;
using SEE.Game.Drawable;
using SEE.UI.Drawable;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.Menu.Drawable.Text
{
    /// <summary>
    /// Holds the UI references used by the text menu.
    /// All referenced objects and components are resolved once when this class
    /// is created.
    /// </summary>
    internal sealed class TextMenuControls
    {
        /// <summary>
        /// The root object of the text menu.
        /// </summary>
        internal GameObject MenuObject { get; }

        /// <summary>
        /// The bold font-style button.
        /// </summary>
        internal Button BoldButton { get; }

        /// <summary>
        /// The button manager of the bold font-style button.
        /// </summary>
        internal ButtonManager BoldButtonManager { get; }

        /// <summary>
        /// The italic font-style button.
        /// </summary>
        internal Button ItalicButton { get; }

        /// <summary>
        /// The button manager of the italic font-style button.
        /// </summary>
        internal ButtonManager ItalicButtonManager { get; }

        /// <summary>
        /// The underline font-style button.
        /// </summary>
        internal Button UnderlineButton { get; }

        /// <summary>
        /// The button manager of the underline font-style button.
        /// </summary>
        internal ButtonManager UnderlineButtonManager { get; }

        /// <summary>
        /// The strikethrough font-style button.
        /// </summary>
        internal Button StrikethroughButton { get; }

        /// <summary>
        /// The button manager of the strikethrough font-style button.
        /// </summary>
        internal ButtonManager StrikethroughButtonManager { get; }

        /// <summary>
        /// The lower-case font-style button.
        /// </summary>
        internal Button LowerCaseButton { get; }

        /// <summary>
        /// The button manager of the lower-case font-style button.
        /// </summary>
        internal ButtonManager LowerCaseButtonManager { get; }

        /// <summary>
        /// The upper-case font-style button.
        /// </summary>
        internal Button UpperCaseButton { get; }

        /// <summary>
        /// The button manager of the upper-case font-style button.
        /// </summary>
        internal ButtonManager UpperCaseButtonManager { get; }

        /// <summary>
        /// The small-caps font-style button.
        /// </summary>
        internal Button SmallCapsButton { get; }

        /// <summary>
        /// The button manager of the small-caps font-style button.
        /// </summary>
        internal ButtonManager SmallCapsButtonManager { get; }

        /// <summary>
        /// The font-color button.
        /// </summary>
        internal Button FontColorButton { get; }

        /// <summary>
        /// The button manager of the font-color button.
        /// </summary>
        internal ButtonManager FontColorButtonManager { get; }

        /// <summary>
        /// The outline-color button.
        /// </summary>
        internal Button OutlineColorButton { get; }

        /// <summary>
        /// The button manager of the outline-color button.
        /// </summary>
        internal ButtonManager OutlineColorButtonManager { get; }

        /// <summary>
        /// The HSV color picker.
        /// </summary>
        internal HSVPicker.ColorPicker ColorPicker { get; }

        /// <summary>
        /// The object containing the outline switch.
        /// </summary>
        internal GameObject OutlineObject { get; }

        /// <summary>
        /// The outline switch.
        /// </summary>
        internal SwitchManager OutlineSwitch { get; }

        /// <summary>
        /// The object containing the outline-thickness slider.
        /// </summary>
        internal GameObject ThicknessObject { get; }

        /// <summary>
        /// The outline-thickness slider.
        /// </summary>
        internal FloatValueSliderController ThicknessSlider { get; }

        /// <summary>
        /// The font-size input control.
        /// </summary>
        internal InputFieldWithButtons FontSizeInput { get; }

        /// <summary>
        /// The object containing the edit-text button.
        /// </summary>
        internal GameObject EditTextObject { get; }

        /// <summary>
        /// The button manager of the edit-text button.
        /// </summary>
        internal ButtonManager EditTextButtonManager { get; }

        /// <summary>
        /// The object containing the order-in-layer control.
        /// </summary>
        internal GameObject OrderInLayerObject { get; }

        /// <summary>
        /// The order-in-layer controller.
        /// </summary>
        internal LayerSliderController OrderInLayerSlider { get; }

        /// <summary>
        /// The Unity slider contained in the order-in-layer control.
        /// </summary>
        internal Slider OrderInLayerUnitySlider { get; }

        /// <summary>
        /// The return-button object.
        /// </summary>
        internal GameObject ReturnButtonObject { get; }

        /// <summary>
        /// The button manager of the return button.
        /// </summary>
        internal ButtonManager ReturnButtonManager { get; }

        /// <summary>
        /// Resolves all UI references belonging to the text menu.
        /// </summary>
        /// <param name="menuObject">The root object of the text menu.</param>
        internal TextMenuControls(GameObject menuObject)
        {
            MenuObject = menuObject;

            GameObject bold =
                GameFinder.FindAttachedOrLocalDescendant(
                    menuObject,
                    "Bold");
            BoldButton = bold.GetComponent<Button>();
            BoldButtonManager = bold.GetComponent<ButtonManager>();

            GameObject italic =
                GameFinder.FindAttachedOrLocalDescendant(
                    menuObject,
                    "Italic");
            ItalicButton = italic.GetComponent<Button>();
            ItalicButtonManager = italic.GetComponent<ButtonManager>();

            GameObject underline =
                GameFinder.FindAttachedOrLocalDescendant(
                    menuObject,
                    "Underline");
            UnderlineButton = underline.GetComponent<Button>();
            UnderlineButtonManager =
                underline.GetComponent<ButtonManager>();

            GameObject strikethrough =
                GameFinder.FindAttachedOrLocalDescendant(
                    menuObject,
                    "Strikethrough");
            StrikethroughButton =
                strikethrough.GetComponent<Button>();
            StrikethroughButtonManager =
                strikethrough.GetComponent<ButtonManager>();

            GameObject lowerCase =
                GameFinder.FindAttachedOrLocalDescendant(
                    menuObject,
                    "LowerCase");
            LowerCaseButton = lowerCase.GetComponent<Button>();
            LowerCaseButtonManager =
                lowerCase.GetComponent<ButtonManager>();

            GameObject upperCase =
                GameFinder.FindAttachedOrLocalDescendant(
                    menuObject,
                    "UpperCase");
            UpperCaseButton = upperCase.GetComponent<Button>();
            UpperCaseButtonManager =
                upperCase.GetComponent<ButtonManager>();

            GameObject smallCaps =
                GameFinder.FindAttachedOrLocalDescendant(
                    menuObject,
                    "SmallCaps");
            SmallCapsButton = smallCaps.GetComponent<Button>();
            SmallCapsButtonManager =
                smallCaps.GetComponent<ButtonManager>();

            GameObject fontColor =
                GameFinder.FindAttachedOrLocalDescendant(
                    menuObject,
                    "FontColorBtn");
            FontColorButton = fontColor.GetComponent<Button>();
            FontColorButtonManager =
                fontColor.GetComponent<ButtonManager>();

            GameObject outlineColor =
                GameFinder.FindAttachedOrLocalDescendant(
                    menuObject,
                    "OutlineColorBtn");
            OutlineColorButton = outlineColor.GetComponent<Button>();
            OutlineColorButtonManager =
                outlineColor.GetComponent<ButtonManager>();

            ColorPicker =
                menuObject.GetComponentInChildren<HSVPicker.ColorPicker>();

            OutlineObject =
                GameFinder.FindAttachedOrLocalDescendant(
                    menuObject,
                    "Outline");
            OutlineSwitch =
                OutlineObject.GetComponentInChildren<SwitchManager>();

            ThicknessObject =
                GameFinder.FindAttachedOrLocalDescendant(
                    menuObject,
                    "Thickness");
            ThicknessSlider =
                ThicknessObject
                    .GetComponentInChildren<FloatValueSliderController>();

            GameObject fontSize =
                GameFinder.FindAttachedOrLocalDescendant(
                    menuObject,
                    "FontSize");
            FontSizeInput =
                fontSize.GetComponentInChildren<InputFieldWithButtons>();

            EditTextObject =
                GameFinder.FindAttachedOrLocalDescendant(
                    menuObject,
                    "EditText");
            EditTextButtonManager =
                EditTextObject
                    .GetComponentInChildren<ButtonManager>();

            OrderInLayerObject =
                GameFinder.FindAttachedOrLocalDescendant(
                    menuObject,
                    "Layer");
            OrderInLayerSlider =
                OrderInLayerObject
                    .GetComponentInChildren<LayerSliderController>();
            OrderInLayerUnitySlider =
                OrderInLayerObject.GetComponentInChildren<Slider>();

            ReturnButtonObject =
                menuObject.transform.Find("ReturnBtn").gameObject;
            ReturnButtonManager =
                ReturnButtonObject.GetComponent<ButtonManager>();
        }
    }
}
