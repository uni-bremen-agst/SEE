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

using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.GO;
using SEE.Utils;
using SimpleFileBrowser;
using System;
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
    /// box to change the root kind. The root kind should also be updated if you pick something via the
    /// file picker.
    ///
    /// The actual file picker dialog comes from an external library and uses a global interface to
    /// open the dialog.
    /// </summary>
    public class FilePicker : DynamicUIBehaviour
    {
        private CustomDropdown dropdown;
        private TMP_InputField customInput;
        private TextMeshProUGUI labelText;
        private ButtonManagerBasic pickerButton;

        /// <summary>
        /// The DataPath instance this file picker manipulates.
        /// </summary>
        public DataPath DataPathInstance;

        /// <summary>
        /// The picker mode (Files, Directories etc.)
        /// </summary>
        public FileBrowser.PickMode PickMode = FileBrowser.PickMode.Files;

        /// <summary>
        /// The label of this component.
        /// </summary>
        public string Label;
        private void Start()
        {
            MustGetComponentInChild("DropdownCombo/Dropdown", out dropdown);
            MustGetComponentInChild("DropdownCombo/SelectableInput/Input", out customInput);
            MustGetComponentInChild("DropdownCombo/SelectableInput/Button", out pickerButton);
            MustGetComponentInChild("Label", out labelText);

            dropdown.dropdownItems.Clear();
            foreach (string kind in ConfigMenu.EnumToStr<DataPath.RootKind>())
            {
                dropdown.CreateNewItemFast(kind, null);
            }
            dropdown.selectedItemIndex =
                dropdown.dropdownItems.FindIndex(
                    item => item.itemName == DataPathInstance.Root.ToString());
            dropdown.SetupDropdown();
            dropdown.dropdownEvent.AddListener(index =>
            {
                String selectedItem = dropdown.dropdownItems[index].itemName;
                DataPathInstance.Root = ItemToRootKind(selectedItem);
                UpdateInput();
            });
            dropdown.isListItem = true;
            dropdown.listParent = FindCanvas(gameObject);

            pickerButton.clickEvent.AddListener(() =>
            {
                FileBrowser.ShowLoadDialog(HandleFileBrowserSuccess,
                                           () => { },
                                           allowMultiSelection: false,
                                           pickMode: PickMode,
                                           title: "Pick a file/folder",
                                           initialPath: DataPathInstance.RootPath
                );

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

            customInput.onValueChanged.AddListener(path =>
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
            });

            labelText.text = Label;
        }

        private void HandleFileBrowserSuccess(string[] paths)
        {
            if (paths.Length == 0)
            {
               throw new Exception("Received no paths from file browser.");
            }
            // There should only be a single path since multiple selections are forbidden.
            DataPathInstance.Set(paths[0]);
            UpdateDropdownAndInput();
        }

        private void UpdateDropdownAndInput()
        {
            dropdown.selectedItemIndex =
                dropdown.dropdownItems.FindIndex(
                    item => item.itemName == DataPathInstance.Root.ToString());
            dropdown.SetupDropdown();
            UpdateInput();
        }

        private void UpdateInput()
        {
            if (DataPathInstance.Root == DataPath.RootKind.Absolute)
            {
                customInput.text = DataPathInstance.AbsolutePath;
            }
            else
            {
                customInput.text = DataPathInstance.RelativePath;
            }
        }

        private DataPath.RootKind ItemToRootKind(string item)
        {
            return Enum.TryParse(item, out DataPath.RootKind rootKind) ?
                     rootKind
                   : DataPath.RootKind.Absolute;
        }
    }

    /// <summary>
    /// Instantiates a new file picker game object via prefab and sets the wrapper script.
    /// </summary>
    public class FilePickerBuilder : UIBuilder<FilePicker>
    {
        protected override string PrefabPath => "Prefabs/UI/Input Group - File Picker";
        private FilePickerBuilder(Transform parent) : base(parent)
        {
        }
        public static FilePickerBuilder Init(Transform parent)
        {
            return new FilePickerBuilder(parent);
        }
        public FilePickerBuilder SetLabel(string label)
        {
            Instance.Label = label;
            return this;
        }
        public FilePickerBuilder SetPathInstance(DataPath dataPath)
        {
            Instance.DataPathInstance = dataPath;
            return this;
        }
        public FilePickerBuilder SetPickMode(FileBrowser.PickMode pickMode)
        {
            Instance.PickMode = pickMode;
            return this;
        }
    }
}
