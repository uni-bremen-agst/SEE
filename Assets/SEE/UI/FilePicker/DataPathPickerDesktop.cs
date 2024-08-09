using System;
using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.GO;
using SEE.Utils;
using SEE.Utils.Paths;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;

namespace SEE.UI.FilePicker
{
    /// <summary>
    /// Allows a user to pick a file or folder for a <see cref="DataPath"/>.
    /// Implementation for the desktop environment.
    /// The dialog consists of a dropdown menu for the <see cref="DataPath.RootKind"/> and
    /// a button to open the <see cref="FileBrowser"/>.
    /// </summary>
    public partial class DataPathPicker
    {
        /// <summary>
        ///     The menu prefab.
        /// </summary>
        protected virtual string MenuPrefab => "Prefabs/UI/Input Group - File Picker";

        /// <summary>
        ///     The path to the dropdown.
        /// </summary>
        protected virtual string DropdownPath => "DropdownCombo/Dropdown";

        /// <summary>
        ///     The path to the custom input.
        /// </summary>
        protected virtual string CustomInputPath => "DropdownCombo/SelectableInput/Input";

        /// <summary>
        ///     The path to the picker button.
        /// </summary>
        protected virtual string PickerButtonPath => "DropdownCombo/SelectableInput/Button";

        /// <summary>
        ///     The path to the label.
        /// </summary>
        protected virtual string LabelPath => "Label";

        /// <summary>
        ///     The menu.
        /// </summary>
        protected GameObject Menu { get; private set; }

        /// <summary>
        ///     The dropdown.
        /// </summary>
        protected CustomDropdown Dropdown { get; private set; }

        /// <summary>
        ///     The custom input.
        /// </summary>
        protected TMP_InputField CustomInput { get; private set; }

        /// <summary>
        ///     The picker button.
        /// </summary>
        protected ButtonManagerBasic PickerButton { get; private set; }

        /// <summary>
        ///     The label.
        /// </summary>
        protected TextMeshProUGUI LabelText { get; private set; }


        /// <summary>
        ///     Destroys the component if the <see cref="Menu" /> is destroyed.
        /// </summary>
        protected override void Update()
        {
            if (Menu == null)
            {
                Destroyer.Destroy(this);
                return;
            }

            base.Update();
        }

        /// <summary>
        ///     Destroys the menu when this component is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (Menu != null)
            {
                Destroyer.Destroy(Menu);
            }
        }

        /// <summary>
        ///     Initializes the menu.
        /// </summary>
        protected override void StartDesktop()
        {
            // instantiates the menu
            Menu = PrefabInstantiator.InstantiatePrefab(MenuPrefab, transform, false);
            Menu.name = Label;

            Dropdown = Menu.transform.Find(DropdownPath).GetComponent<CustomDropdown>();
            CustomInput = Menu.transform.Find(CustomInputPath).GetComponent<TMP_InputField>();
            LabelText = Menu.transform.Find(LabelPath).GetComponent<TextMeshProUGUI>();
            PickerButton = Menu.transform.Find(PickerButtonPath).GetComponent<ButtonManagerBasic>();

            TMP_InputField inputField = Menu.transform.Find("DropdownCombo/SelectableInput/Input")
                                            .GetComponent<TMP_InputField>();

            LabelText.text = Label;

            // setup dropdown
            Dropdown.isListItem = true;
            Dropdown.listParent = Canvas.transform;
            Dropdown.dropdownItems.Clear();
            foreach (DataPath.RootKind kind in Enum.GetValues(typeof(DataPath.RootKind)))
            {
                Dropdown.CreateNewItemFast(kind.ToString(), null);
            }
            Dropdown.dropdownEvent.AddListener(index =>
            {
                string selectedItem = Dropdown.dropdownItems[index].itemName;
                Enum.TryParse(selectedItem, out DataPathInstance.Root);
                UpdateInput();
                OnChangedDropdown?.Invoke();
            });

            // opens a file picker with the picker button
            PickerButton.clickEvent.AddListener(() =>
            {
                FileBrowser.ShowLoadDialog(HandleFileBrowserSuccess,
                                           () => { },
                                           allowMultiSelection: false,
                                           pickMode: FileBrowser.PickMode.FilesAndFolders,
                                           title: "Pick a file/folder",
                                           initialPath: DataPathInstance.RootFileSystemPath);

                // Find the newly opened file browser and optimize it for VR.
                GameObject fileBrowser = GameObject.FindWithTag("FileBrowser");
                fileBrowser.transform.Find("EventSystem").gameObject.SetActive(false);
                if (SceneSettings.InputType == PlayerInputType.VRPlayer)
                {
                    Canvas parentCanvas = GetComponentInParent<Canvas>();
                    RectTransform fileBrowserRect = fileBrowser.GetComponent<RectTransform>();
                    Canvas fileBrowserCanvas = fileBrowser.GetComponent<Canvas>();

                    fileBrowserCanvas.worldCamera =
                        GameObject.FindWithTag("VRPointer").GetComponent<Camera>();
                    fileBrowserCanvas.renderMode = RenderMode.WorldSpace;

                    fileBrowserRect.transform.position = parentCanvas.transform.position;
                    fileBrowserRect.localScale = new Vector3(0.002f, 0.002f, 0.002f);
                }
            });

            CustomInput.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
            CustomInput.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);
            inputField.onEndEdit.AddListener(path =>
            {
                if (DataPathInstance.Root == DataPath.RootKind.Absolute)
                {
                    DataPathInstance.AbsolutePath = path;
                }
                else
                {
                    DataPathInstance.RelativePath = path;
                }
                UpdateInput();
                OnChangedPath?.Invoke();
            });
        }

        protected override void OnStartFinished()
        {
            base.OnStartFinished();
            UpdateDropdown();
            UpdateInput();
        }

        /// <summary>
        ///     See <see cref="StartDesktop" />.
        /// </summary>
        protected override void StartVR()
        {
            StartDesktop();
        }

        /// <summary>
        ///     See <see cref="StartDesktop" />.
        /// </summary>
        protected override void StartTouchGamepad()
        {
            StartDesktop();
        }

        /// <summary>
        ///     Handles the file browser success.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <exception cref="Exception">Received no paths from file browser</exception>
        protected void HandleFileBrowserSuccess(string[] paths)
        {
            if (paths.Length == 0)
            {
                throw new Exception("Received no paths from file browser.");
            }
            // There should only be a single path since multiple selections are forbidden.
            DataPathInstance.Path = paths[0];
            UpdateDropdown();
            OnChangedDropdown?.Invoke();
            UpdateInput();
            OnChangedPath?.Invoke();
        }

        /// <summary>
        ///     Updates the dropdown.
        /// </summary>
        private void UpdateDropdown()
        {
            Dropdown.selectedItemIndex =
                Dropdown.dropdownItems.FindIndex(item => item.itemName == DataPathInstance.Root.ToString());
            Dropdown.SetupDropdown();
        }

        /// <summary>
        ///     Updates the custom input text.
        /// </summary>
        private void UpdateInput()
        {
            CustomInput.text = DataPathInstance.Root == DataPath.RootKind.Absolute
                ? DataPathInstance.AbsolutePath
                : DataPathInstance.RelativePath;
        }

        public void SyncPath(string newValue, bool isAbsolute)
        {
            if (isAbsolute)
            {
                DataPathInstance.AbsolutePath = newValue;
            }
            else
            {
                DataPathInstance.RelativePath = newValue;
            }
            UpdateInput();
        }

        public void SyncDropdown(int newValue)
        {
            string selectedItem = Dropdown.dropdownItems[newValue].itemName;
            Enum.TryParse(selectedItem, out DataPathInstance.Root);
            UpdateDropdown();
            UpdateInput();
        }

        public void CloseDropdown()
        {
            if (!HasStarted)
            {
                return;
            }
            if (Dropdown.isOn)
            {
                Dropdown.Animate();
            }
        }

        public Action OnChangedDropdown;
        public Action OnChangedPath;
    }
}