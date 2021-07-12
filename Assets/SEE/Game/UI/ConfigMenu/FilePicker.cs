// Copyright 2021 Ruben Smidt
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.GO;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{

    /// <summary>
    /// Component that controls a file picker.
    /// It comes with a label to display next to the file dialog trigger.
    ///
    /// Unlike the other inputs, this component does not communicate via a controlled interface.
    /// It receives a DataPath instance and manipulates it directly because a lot of the necessary
    /// processing happens directly on the DataPath instance. It would be redundant to implement that
    /// same behavior as part of this component.
    ///
    /// To manipulate the RootKind of a DataPath, the prefab of this file picker comes with a select
    /// box change the root kind. The root kind should also be updated if you pick something via the
    /// file picker.
    ///
    /// The actual file picker dialog comes from an external library and uses a global interface to
    /// open the dialog.
    /// </summary>
    public class FilePicker : DynamicUIBehaviour
    {
        private CustomDropdown _dropdown;
        private TMP_InputField _customInput;
        private TextMeshProUGUI _labelText;
        private ButtonManagerBasic _pickerButton;

        /// <summary>
        /// The DataPath instance this file picker manipulates.
        /// </summary>
        public DataPath dataPathInstance;

        /// <summary>
        /// The picker mode (Files, Directories etc.)
        /// </summary>
        public FileBrowser.PickMode pickMode = FileBrowser.PickMode.Files;

        /// <summary>
        /// The label of this component.
        /// </summary>
        public string label;

        void Start()
        {
            MustGetComponentInChild("DropdownCombo/Dropdown", out _dropdown);
            MustGetComponentInChild("DropdownCombo/SelectableInput/Input", out _customInput);
            MustGetComponentInChild("DropdownCombo/SelectableInput/Button", out _pickerButton);
            MustGetComponentInChild("Label", out _labelText);

            _dropdown.dropdownItems.Clear();
            foreach (string kind in ConfigMenu.EnumToStr<DataPath.RootKind>())
            {
                _dropdown.CreateNewItemFast(kind, null);
            }
            _dropdown.selectedItemIndex =
                _dropdown.dropdownItems.FindIndex(
                    item => item.itemName == dataPathInstance.Root.ToString());
            _dropdown.SetupDropdown();
            _dropdown.dropdownEvent.AddListener(index =>
            {
                String selectedItem = _dropdown.dropdownItems[index].itemName;
                dataPathInstance.Root = ItemToRootKind(selectedItem);
                UpdateInput();
            });
            _dropdown.isListItem = true;
            _dropdown.listParent = FindObjectOfType<Canvas>().transform;

            _pickerButton.clickEvent.AddListener(() =>
            {
                FileBrowser.ShowLoadDialog(HandleFileBrowserSuccess,
                                           () => { },
                                           allowMultiSelection: false,
                                           pickMode: pickMode,
                                           title: "Pick a file/folder",
                                           initialPath: dataPathInstance.RootPath
                );

                // Find the newly opened file browser and optimize it for VR.
                GameObject fileBrowser = GameObject.FindWithTag("FileBrowser");
                fileBrowser.transform.Find("EventSystem").gameObject.SetActive(false);
                if (PlayerSettings.GetInputType() == PlayerInputType.VRPlayer)
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

            _customInput.onValueChanged.AddListener(path =>
            {
                if (dataPathInstance.Root == DataPath.RootKind.Absolute)
                {
                    dataPathInstance.AbsolutePath = path;
                }
                else
                {
                    dataPathInstance.RelativePath = path;
                }
                UpdateInput();
            });

            _labelText.text = label;
        }
        private void HandleFileBrowserSuccess(string[] paths)
        {
            if (paths.Length == 0)
            {
                Debug.LogError("Received no paths from file browser.");
            }
            // There should only be a single path since multiple selections are forbidden.
            dataPathInstance.Set(paths[0]);
            UpdateDropdownAndInput();
        }

        private void UpdateDropdownAndInput()
        {
            _dropdown.selectedItemIndex =
                _dropdown.dropdownItems.FindIndex(
                    item => item.itemName == dataPathInstance.Root.ToString());
            _dropdown.SetupDropdown();
            UpdateInput();
        }

        private void UpdateInput()
        {
            if (dataPathInstance.Root == DataPath.RootKind.Absolute)
            {
                _customInput.text = dataPathInstance.AbsolutePath;
            }
            else
            {
                _customInput.text = dataPathInstance.RelativePath;
            }
        }

        private DataPath.RootKind ItemToRootKind(string item)
        {
            return Enum.TryParse(item, out DataPath.RootKind rootKind) ? rootKind
                : DataPath.RootKind.Absolute;
        }
    }

    /// <summary>
    /// Instantiates a new file picker game object via prefab and sets the wrapper script.
    /// </summary>
    public class FilePickerBuilder : UiBuilder<FilePicker>
    {
        protected override string PrefabPath =>
            "Assets/Prefabs/UI/Input Group - File Picker.prefab";

        private FilePickerBuilder(Transform parent) : base(parent)
        {
        }

        public static FilePickerBuilder Init(Transform parent)
        {
            return new FilePickerBuilder(parent);
        }

        public FilePickerBuilder SetLabel(string label)
        {
            Instance.label = label;
            return this;
        }

        public FilePickerBuilder SetPathInstance(DataPath dataPath)
        {
            Instance.dataPathInstance = dataPath;
            return this;
        }

        public FilePickerBuilder SetPickMode(FileBrowser.PickMode pickMode)
        {
            Instance.pickMode = pickMode;
            return this;
        }
    }
}
