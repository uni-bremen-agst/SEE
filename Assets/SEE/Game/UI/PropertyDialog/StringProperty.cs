using SEE.GO;
using System;
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
        /// </summary>
        private TextMeshProUGUI textField;

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
        /// Sets <see cref="inputField"/> as an instantiation of prefab <see cref="StringInputFieldPrefab"/>.
        /// Sets the label and value of the field.
        /// </summary>
        protected override void StartDesktop()
        {
            inputField = Utils.PrefabInstantiator.InstantiatePrefab(StringInputFieldPrefab, instantiateInWorldSpace: false);
            if (parentOfInputField != null)
            {
                SetParent(parentOfInputField);
            }
            inputField.gameObject.name = Name;
            SetLabel(inputField);
            SetInitialInput(inputField, savedValue);
            textField = GetTextField(inputField);
            textField.text = savedValue;

            void SetInitialInput(GameObject inputField, string value)
            {
                if (!string.IsNullOrEmpty(value) && inputField.TryGetComponent(out TMP_InputField tmPro))
                {
                    tmPro.text = value;
                }
            }

            void SetLabel(GameObject inputField)
            {
                Transform placeHolder = inputField.transform.Find("Placeholder");
                if (placeHolder.gameObject.TryGetComponentOrLog(out TextMeshProUGUI text))
                {
                    text.text = Name;
                }
            }

            TextMeshProUGUI GetTextField(GameObject inputField)
            {
                Transform result = inputField.transform.Find("Text Area/Text");                
                if (result != null && result.gameObject.TryGetComponentOrLog(out TextMeshProUGUI text))
                {
                    return text;
                }
                else
                {
                    throw new Exception($"Prefab {StringInputFieldPrefab} does not have a TextMeshProUGUI component at child 'Text Area/Text'");
                }
            }
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
