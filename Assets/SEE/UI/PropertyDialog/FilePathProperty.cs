using System;
using System.Linq;
using SEE.GO;
using SEE.Utils;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.PropertyDialog
{
    /// <summary>
    /// A string input field with a file browser button for a property dialog.
    /// </summary>
    public class FilePathProperty : Property<string>
    {
        /// <summary>
        /// The pick mode.
        /// </summary>
        public FileBrowser.PickMode PickMode = FileBrowser.PickMode.Files;

        /// <summary>
        /// Allows multi selection.
        /// </summary>
        public bool AllowMultiSelection = false;

        /// <summary>
        /// The filters.
        /// The first entry is used as the default filter.
        /// </summary>
        public FileBrowser.Filter[] Filters = { };

        /// <summary>
        /// Used if <see cref="Value"/> is a relative path.
        /// </summary>
        public string FallbackDirectory = "";

        /// <summary>
        /// The prefab for a string input field.
        /// </summary>
        private const string filePathInputFieldPrefab = "Prefabs/UI/InputFields/FilePathInputField";

        /// <summary>
        /// The text field in which the value will be entered by the user.
        /// Note: The input field has a child Text Area/Text with a TextMeshProUGUI
        /// component holding the text, too. Yet, one should never use the latter, because
        /// the latter contains invisible characters. One must always use the attribute
        /// text of the TMP_InputField.
        /// </summary>
        private TMP_InputField textField;

        /// <summary>
        /// Instantiation of the prefab <see cref="filePathInputFieldPrefab"/>.
        /// </summary>
        private GameObject inputField;

        /// <summary>
        /// The parent of <see cref="inputField"/>. Because <see cref="SetParent(GameObject)"/>
        /// may be called before <see cref="StartDesktop"/>, the parameter passed to
        /// <see cref="SetParent(GameObject)"/> will be buffered in this attribute.
        /// </summary>
        private GameObject parentOfInputField;

        /// <summary>
        /// Sets <see cref="inputField"/> as an instantiation of prefab <see cref="filePathInputFieldPrefab"/>.
        /// Sets the label and value of the field.
        /// </summary>
        protected override void StartDesktop()
        {
            inputField = PrefabInstantiator.InstantiatePrefab(filePathInputFieldPrefab, instantiateInWorldSpace: false);
            if (parentOfInputField != null)
            {
                SetParent(parentOfInputField);
            }
            inputField.name = Name;
            SetLabel(inputField);
            SetInitialInput(inputField, savedValue);
            textField = GetInputField(inputField);
            textField.text = savedValue;
            SetupInputField();
            SetupFileBrowserButton();
            SetupTooltip(inputField);

            #region Local Methods

            void SetupTooltip(GameObject field)
            {
                if (field.TryGetComponentOrLog(out PointerHelper pointerHelper))
                {
                    // Register listeners on entry and exit events, respectively
                    pointerHelper.EnterEvent.AddListener(_ => Tooltip.ActivateWith(Description));
                    pointerHelper.ExitEvent.AddListener(_ => Tooltip.Deactivate());
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
                    throw new Exception($"Prefab {filePathInputFieldPrefab} does not have a {typeof(TMP_InputField)}");
                }
            }
            void SetupInputField()
            {
                textField.onSelect.AddListener(_ =>
                {
                    textField.textComponent.overflowMode = TextOverflowModes.ScrollRect;
                });
                textField.onEndEdit.AddListener(_ =>
                {
                    ((RectTransform)textField.textComponent.transform).localPosition = Vector2.zero;
                    textField.textComponent.overflowMode = TextOverflowModes.Ellipsis;
                });
            }
            void SetupFileBrowserButton()
            {
                Button button = inputField.transform.Find("Button").gameObject.MustGetComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    FileBrowser.SetFilters(true, Filters);
                    if (Filters.Length > 0)
                    {
                        FileBrowser.SetDefaultFilter(Filters[0].extensions.First());
                    }
                    FileBrowser.ShowLoadDialog(
                        paths =>
                        {
                            Value = paths[0];
                            FileBrowser.SetFilters(true, new string[] { });
                        },
                        () =>
                        {
                            FileBrowser.SetFilters(true, new string[] { });
                        },
                        PickMode,
                        AllowMultiSelection,
                        FileBrowserHelpers.GetDirectoryName(Value) != "" ? FileBrowserHelpers.GetDirectoryName(Value) : FallbackDirectory,
                        FileBrowserHelpers.GetFilename(Value),
                        title: $"{Name} - {Description}"
                    );
                });

            }
            #endregion
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
    }
}
