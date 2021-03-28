using System;
using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.GO;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Game.UI.ConfigMenu
{


    public class FilePicker : DynamicUIBehaviour
    {

        private CustomDropdown _dropdown;
        private TMP_InputField _customInput;
        private TextMeshProUGUI _labelText;
        private ButtonManagerBasic _pickerButton;

        public DataPath dataPathInstance;
        public FileBrowser.PickMode pickMode = FileBrowser.PickMode.Files;
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

    public class FilePickerBuilder : BaseUiBuilder<FilePicker>
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
