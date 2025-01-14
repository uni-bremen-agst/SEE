using System;
using System.Xml;
using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.PropertyDialog
{
    /// <summary>
    /// A string input field for a property dialog.
    /// </summary>
    public class StringProperty : Property<string>
    {
        /// <summary>
        /// The prefab for a string input field.
        /// </summary>
        private const string stringInputFieldPrefab = "Prefabs/UI/InputFields/StringInputField";

        /// <summary>
        /// The text field in which the value will be entered by the user.
        /// Note: The input field has a child Text Area/Text with a TextMeshProUGUI
        /// component holding the text, too. Yet, one should never use the latter, because
        /// the latter contains invisible characters. One must always use the attribute
        /// text of the TMP_InputField.
        /// </summary>
        private TMP_InputField textField;

        /// <summary>
        /// Instantiation of the prefab <see cref="stringInputFieldPrefab"/>.
        /// </summary>
        private GameObject inputField;

        /// <summary>
        /// The parent of <see cref="inputField"/>. Because <see cref="SetParent(GameObject)"/>
        /// may be called before <see cref="StartDesktop"/>, the parameter passed to
        /// <see cref="SetParent(GameObject)"/> will be buffered in this attribute.
        /// </summary>
        private GameObject parentOfInputField;

        /// <summary>
        /// Indicator of whether the validation error message should be displayed.
        /// </summary>
        private bool showValidationFailedMessage = false;

        /// <summary>
        /// Sets <see cref="inputField"/> as an instantiation of prefab <see cref="stringInputFieldPrefab"/>.
        /// Sets the label and value of the field.
        /// </summary>
        protected override void StartDesktop()
        {
            inputField = PrefabInstantiator.InstantiatePrefab(stringInputFieldPrefab, instantiateInWorldSpace: false);
            if (parentOfInputField != null)
            {
                SetParent(parentOfInputField);
            }
            inputField.gameObject.name = Name;
            SetLabel(inputField);
            SetInitialInput(inputField, savedValue);
            textField = GetInputField(inputField);
            textField.text = savedValue;
            SetupTooltip(inputField);
            SetupDisableValidationMessageOnValueChange(inputField, textField);

            #region Local Methods
            void SetupTooltip(GameObject field)
            {
                if (field.TryGetComponentOrLog(out PointerHelper pointerHelper))
                {
                    // Register listeners on entry and exit events, respectively
                    pointerHelper.EnterEvent.AddListener(_ => Tooltip.ActivateWith(Description));
                    pointerHelper.ExitEvent.AddListener(_ => Tooltip.Deactivate());
                    // FIXME scrolling doesn't work while hovering above the field, because
                    // the Modern UI Pack uses an Event Trigger (see Utils/PointerHelper for an explanation.)
                    // It is unclear how to resolve this without either abstaining from using the Modern UI Pack
                    // in this instance or without modifying the Modern UI Pack, which would complicate
                    // updates greatly. Perhaps the author of the Modern UI Pack (or Unity developers?) should
                    // be contacted about this.
                }
            }

            void SetInitialInput(GameObject field, string value)
            {
                if (!string.IsNullOrEmpty(value) && field.TryGetComponent(out TMP_InputField tmPro))
                {
                    tmPro.text = value;
                    // Hide tooltip when any text is entered so as not to obscure the text
                    tmPro.onValueChanged.AddListener(_ => Tooltip.Deactivate());
                }
            }

            void SetLabel(GameObject field)
            {
                Transform placeHolder = field.transform.Find("Placeholder");
                placeHolder.gameObject.TryGetComponentOrLog(out TextMeshProUGUI nameTextMeshPro);
                {
                    nameTextMeshPro.text = Name;
                }
            }

            static TMP_InputField GetInputField(GameObject field)
            {
                if (field.TryGetComponent(out TMP_InputField inputField))
                {
                    return inputField;
                }
                else
                {
                    throw new Exception($"Prefab {stringInputFieldPrefab} does not have a {typeof(TMP_InputField)}");
                }
            }

            void SetupDisableValidationMessageOnValueChange(GameObject inputField, TMP_InputField textField)
            {
                textField.onValueChanged.AddListener(value =>
                {
                    if (showValidationFailedMessage)
                    {
                        GameFinder.FindChild(inputField, "Validation Area").SetActive(false);
                        ChangeColorOfValidation(Color.white);
                        showValidationFailedMessage = false;
                    }
                });
            }

            #endregion
        }

        protected override void StartVR()
        {
            StartDesktop();
        }

        /// <summary>
        /// Sets <paramref name="parent"/> as the parent of the <see cref="inputField"/>.
        /// </summary>
        /// <param name="parent">new parent of <see cref="inputField"/></param>
        public override void SetParent(GameObject parent)
        {
            if (inputField != null)
            {
                inputField.transform.SetParent(parent.transform);
            }
            else
            {
                /// save for later assignment in <see cref="StartDesktop"/>
                parentOfInputField = parent;
            }
        }

        /// <summary>
        /// The buffered value of the <see cref="textField"/>. Because <see cref="Value"/>
        /// may be set before <see cref="StartDesktop"/> is called, the parameter passed to
        /// <see cref="Value"/> will be buffered in this attribute if <see cref="StartDesktop"/>
        /// has not been called and, hence, <see cref="textField"/> does not exist yet.
        /// </summary>
        private string savedValue;
        /// <summary>
        /// Value of the input field.
        /// </summary>
        public override string Value
        {
            get => textField == null ? savedValue : textField.text;
            set
            {
                // Because the Value could be set before StartDesktop() was called,
                // the textField might not yet exist. In that case, we are buffering
                // the given value in savedValue.
                savedValue = value;
                if (textField != null)
                {
                    textField.text = value;
                }
            }
        }

        /// <summary>
        /// Displays the text field for the validaiton failed message with the given <paramref name="errorMessage"/>.
        /// </summary>
        /// <param name="errorMessage">The error message which should be displayed.</param>
        public void ValidateFailed(string errorMessage)
        {
            GameObject validationArea = GameFinder.FindChild(inputField, "Validation Area");
            validationArea.SetActive(true);
            validationArea.GetComponentInChildren<TextMeshProUGUI>().text = errorMessage;
            showValidationFailedMessage = true;
            ChangeColorOfValidation(Color.red);
        }

        /// <summary>
        /// Changes the colors of the objects based on the validation status.
        /// Adjusts the caret, the input field text, the font color of the entered text,
        /// the placeholder and the background images (background and filled).
        /// </summary>
        /// <param name="color">The color of the validation status.</param>
        private void ChangeColorOfValidation(Color color)
        {
            textField.caretColor = color;
            // Change the color of the TMP_Inputfield.
            textField.colors = new ColorBlock
            {
                normalColor = color,
                highlightedColor = color,
                pressedColor = color,
                disabledColor = color,
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };
            // Change the color of the input text and of the placeholder text.
            TextMeshProUGUI textTMP = GameFinder.FindChild(inputField, "Text Area").GetComponentInChildren<TextMeshProUGUI>();
            textTMP.faceColor = color;
            GameFinder.FindChild(inputField, "Placeholder").GetComponent<TextMeshProUGUI>().color = color;

            // Change the color of the input field background (the underlaying line).
            UIManagerInputField managerInputField = inputField.GetComponentInChildren<UIManagerInputField>();
            managerInputField.UIManagerAsset.inputFieldColor = color;
        }
    }
}
