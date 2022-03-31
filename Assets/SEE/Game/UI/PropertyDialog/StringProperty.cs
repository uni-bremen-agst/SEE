using System;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;

namespace SEE.Game.UI.PropertyDialog
{
    /// <summary>
    /// A string input field for a property dialog.
    /// </summary>
    public class StringProperty : Property<string>
    {
        /// <summary>
        /// The prefab for a string input field.
        /// </summary>
        private const string StringInputFieldPrefab = "Prefabs/UI/InputFields/StringInputField";

        /// <summary>
        /// The text field in which the value will be entered by the user.
        /// Note: The input field has a child Text Area/Text with a TextMeshProUGUI
        /// component holding the text, too. Yet, one should never use the latter, because
        /// the latter contains invisible characters. One must always use the attribute
        /// text of the TMP_InputField.
        /// </summary>
        private TMP_InputField textField;

        /// <summary>
        /// Instantiation of the prefab <see cref="StringInputFieldPrefab"/>.
        /// </summary>
        private GameObject inputField;

        /// <summary>
        /// The parent of <see cref="inputField"/>. Because <see cref="SetParent(GameObject)"/>
        /// may be called before <see cref="StartDesktop"/>, the parameter passed to
        /// <see cref="SetParent(GameObject)"/> will be buffered in this attribute.
        /// </summary>
        private GameObject parentOfInputField;

        /// <summary>
        /// The tooltip containing the <see cref="Description"/> of this <see cref="Property"/>, which will
        /// be displayed when hovering above it.
        /// </summary>
        private Tooltip.Tooltip tooltip;

        /// <summary>
        /// Sets <see cref="inputField"/> as an instantiation of prefab <see cref="StringInputFieldPrefab"/>.
        /// Sets the label and value of the field.
        /// </summary>
        protected override void StartDesktop()
        {
            inputField = PrefabInstantiator.InstantiatePrefab(StringInputFieldPrefab, instantiateInWorldSpace: false);
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

            #region Local Methods

            void SetupTooltip(GameObject field)
            {
                tooltip = gameObject.AddComponent<Tooltip.Tooltip>();
                if (field.TryGetComponentOrLog(out PointerHelper pointerHelper))
                {
                    // Register listeners on entry and exit events, respectively
                    pointerHelper.EnterEvent.AddListener(() => tooltip.Show(Description));
                    pointerHelper.ExitEvent.AddListener(tooltip.Hide);
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
                    tmPro.onValueChanged.AddListener(_ => tooltip.Hide());
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
                    throw new Exception($"Prefab {StringInputFieldPrefab} does not have a {typeof(TMP_InputField)}");
                }
            }

            #endregion
        }

        /// <summary>
        /// Refers to <see cref="StartDesktop"/>
        /// </summary>
        protected override void StartMobile()
        {
            StartDesktop();
        }

        /// <summary>
        /// Sets <paramref name="parent"/> as the parent of the <see cref="inputField"/>.
        /// </summary>
        /// <param name="parent">new parent of <see cref="inputField"/></param>
        public override void SetParent(GameObject parent)
        {
            if (HasStarted)
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
    }
}
