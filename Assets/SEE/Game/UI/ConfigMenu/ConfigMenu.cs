using System;
using System.Collections.Generic;
using System.Linq;
using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.Layout.EdgeLayouts;
using SEE.Layout.NodeLayouts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using static SEE.Game.AbstractSEECity;

namespace SEE.Game.UI.ConfigMenu
{
    enum TabButtonState
    {
        InitialActive,
        Inactive,
    }

    public class ConfigMenu : DynamicUIBehaviour
    {
        private static readonly List<string> _numericAttributes =
            Enum.GetValues(typeof(NumericAttributeNames))
                .Cast<NumericAttributeNames>()
                .Select(x => x.Name())
                .ToList();
        private static readonly List<EditableInstance> EditableInstances =
            new List<EditableInstance>
            {
                EditableInstance.Architecture,
                EditableInstance.Implementation
            };

        private const string PagePrefabPath = "Assets/Prefabs/UI/Page.prefab";
        private const string TabButtonPrefabPath = "Assets/Prefabs/UI/TabButton.prefab";
        private const string ActionButtonPrefabPath = "Assets/Prefabs/UI/ActionButton.prefab";
        private const string PointerPrefabPath = "Assets/Prefabs/UI/Pointer.prefab";

        private GameObject _pagePrefab;
        private GameObject _actionButtonPrefab;
        private GameObject _tabButtonPrefab;

        private GameObject _tabOutlet;
        private GameObject _tabButtons;
        private GameObject _actions;

        private SEECity _city;
        private ColorPickerControl _colorPickerControl;
        private ButtonManager _cityLoadButton;
        private Canvas _canvas;
        private HorizontalSelector _editingInstanceSelector;

        public UnityEvent<EditableInstance> OnInstanceChangeRequest =
            new UnityEvent<EditableInstance>();
        public EditableInstance CurrentlyEditing = EditableInstance.Implementation;

        private void Start()
        {
            SetupCity(CurrentlyEditing);
            MustGetChild("Canvas/TabNavigation/TabOutlet", out _tabOutlet);
            MustGetChild("Canvas/TabNavigation/Sidebar/TabButtons", out _tabButtons);
            MustGetChild("Canvas/Actions", out _actions);

            MustGetComponentInChild("Canvas", out _canvas);
            MustGetComponentInChild("Canvas/Picker 2.0", out _colorPickerControl);
            _colorPickerControl.gameObject.SetActive(false);

            // Reset (hide) the color picker on page changes.
            _tabButtons.MustGetComponent(out TabGroup tabGroupController);
            tabGroupController.SubscribeToUpdates(_colorPickerControl.Reset);

            MustGetComponentInChild("Canvas/TabNavigation/Sidebar/CityLoadButton",
                                    out _cityLoadButton);
            _cityLoadButton.clickEvent.AddListener(() =>
            {
                _city.LoadData();
                _actions.SetActive(true);
                _cityLoadButton.gameObject.SetActive(false);
            });


            SetupInstanceSwitch();
            SetupEnvironment();
            LoadPrefabs();
            SetupActions();
            SetupPages();
        }

        private void SetupCity(EditableInstance instanceToEdit)
        {
            GameObject.Find(instanceToEdit.GameObjectName)?.MustGetComponent(out _city);
            if (!_city)
            {
                Debug.LogError("Did not find a city instance.");
            }
        }

        private void SetupInstanceSwitch()
        {
            MustGetComponentInChild("Canvas/TabNavigation/Sidebar/CitySwitch",
                                    out _editingInstanceSelector);
            _editingInstanceSelector.itemList.Clear();
            EditableInstances.ForEach(instance =>
                                          _editingInstanceSelector.CreateNewItem(
                                              instance.DisplayValue));
            _editingInstanceSelector.defaultIndex = EditableInstances.IndexOf(CurrentlyEditing);
            _editingInstanceSelector.SetupSelector();
            _editingInstanceSelector.selectorEvent.AddListener(index =>
            {
                string displayValue = _editingInstanceSelector.itemList[index].itemTitle;
                EditableInstance newInstance =
                    EditableInstances.Find(instance => instance.DisplayValue == displayValue);
                OnInstanceChangeRequest.Invoke(newInstance);
            });
        }

        private void SetupEnvironment()
        {
            if (PlayerSettings.GetInputType() == PlayerInputType.VRPlayer)
            {
                Transform attachmentPoint = GameObject
                    .Find("VRPlayer/SteamVRObjects/RightHand/ObjectAttachmentPoint").transform;
                GameObject pointer =
                    Instantiate(MustLoadPrefabAtPath(PointerPrefabPath), attachmentPoint);
                Camera pointerCamera = pointer.GetComponent<Camera>();

                GameObject vrEventSystem =
                    GameObject.FindWithTag("VREventSystem");
                vrEventSystem.GetComponent<StandaloneInputModule>().enabled = false;
                vrEventSystem.AddComponent<VRInputModule>();
                VRInputModule vrInputModule = vrEventSystem.GetComponent<VRInputModule>();
                vrInputModule.PointerCamera = pointerCamera;
                pointer.GetComponent<Pointer>().InputModule = vrInputModule;

                MustGetComponentInChild("Canvas", out RectTransform rectTransform);
                _canvas.renderMode = RenderMode.WorldSpace;
                _canvas.worldCamera = pointerCamera;
                rectTransform.anchoredPosition3D = Vector3.zero;
                rectTransform.localScale = Vector3.one;

                _colorPickerControl.gameObject.transform.Rotate(0f, 45f, 0f);

                gameObject.transform.position = new Vector3(-0.36f, 1.692f, -0.634f);
            }
        }

        private void LoadPrefabs()
        {
            _tabButtonPrefab = MustLoadPrefabAtPath(TabButtonPrefabPath);
            _pagePrefab = MustLoadPrefabAtPath(PagePrefabPath);
            _actionButtonPrefab = MustLoadPrefabAtPath(ActionButtonPrefabPath);
        }

        private void SetupActions()
        {
            _actions.SetActive(false);
            CreateActionButton("Delete Graph", () =>
            {
                _city.Reset();
                _cityLoadButton.gameObject.SetActive(true);
            });
            CreateActionButton("Save Graph", _city.Save);
            CreateActionButton("Draw", () =>
            {
                _city.DrawGraph();
                Toggle();
            });
            CreateActionButton("Re-Draw", _city.ReDrawGraph);
            CreateActionButton("Save layout", _city.SaveLayout);
            CreateActionButton("Add References", _city.SetNodeEdgeRefs);
        }
        private void CreateActionButton(string buttonText, UnityAction onClick)
        {
            GameObject deleteGraphButtonGo =
                Instantiate(_actionButtonPrefab, _actions.transform, false);
            deleteGraphButtonGo.MustGetComponent(out ButtonManagerBasic deleteGraphButton);
            deleteGraphButton.buttonText = buttonText;
            deleteGraphButton.clickEvent.AddListener(onClick);
        }


        private void SetupPages()
        {
            SetupLeafNodesPage();
            SetupInnerNodesPage();
            SetupNodesLayoutPage();
            SetupEdgesLayoutPage();
            SetupMiscellaneousPage();
        }

        private void SetupLeafNodesPage()
        {
            CreateAndInsertTabButton("Leaf nodes", TabButtonState.InitialActive);
            GameObject page = CreateAndInsertPage("Attributes of leaf nodes");
            Transform controls = page.transform.Find("ControlsViewport/ControlsContent");

            // Width metric
            ComboSelectBuilder.Init(controls.transform)
                .SetLabel("Width")
                .SetAllowedValues(_numericAttributes)
                .SetDefaultValue(_city.WidthMetric)
                .SetOnChangeHandler(s => _city.WidthMetric = s)
                .Build();

            // Height metric
            ComboSelectBuilder.Init(controls.transform)
                .SetLabel("Height")
                .SetAllowedValues(_numericAttributes)
                .SetDefaultValue(_city.HeightMetric)
                .SetOnChangeHandler(s => _city.HeightMetric = s)
                .Build();

            // Height metric
            ComboSelectBuilder.Init(controls.transform)
                .SetLabel("Depth")
                .SetAllowedValues(_numericAttributes)
                .SetDefaultValue(_city.DepthMetric)
                .SetOnChangeHandler(s => _city.DepthMetric = s)
                .Build();

            // Leaf style metric
            ComboSelectBuilder.Init(controls.transform)
                .SetLabel("Style")
                .SetAllowedValues(_numericAttributes)
                .SetDefaultValue(_city.LeafStyleMetric)
                .SetOnChangeHandler(s => _city.LeafStyleMetric = s)
                .Build();

            // Lower color
            ColorPickerBuilder.Init(controls.transform)
                .SetLabel("Lower color")
                .SetDefaultValue(_city.LeafNodeColorRange.lower)
                .SetOnChangeHandler(c => _city.LeafNodeColorRange.lower = c)
                .SetColorPickerControl(_colorPickerControl)
                .Build();

            // Upper color
            ColorPickerBuilder.Init(controls.transform)
                .SetLabel("Upper color")
                .SetDefaultValue(_city.LeafNodeColorRange.upper)
                .SetOnChangeHandler(c => _city.LeafNodeColorRange.upper = c)
                .SetColorPickerControl(_colorPickerControl)
                .Build();

            // Number of colors
            SliderBuilder.Init(controls.transform)
                .SetLabel("# Colors")
                .SetMode(SliderMode.Integer)
                .SetDefaultValue(_city.LeafNodeColorRange.NumberOfColors)
                .SetOnChangeHandler(f => _city.LeafNodeColorRange.NumberOfColors =
                                        (uint)Math.Round(f))
                .SetRange((0, 15))
                .Build();

            CreateLabelSettingsInputs(controls, _city.LeafLabelSettings);
        }

        private void SetupInnerNodesPage()
        {
            CreateAndInsertTabButton("Inner nodes");
            GameObject page = CreateAndInsertPage("Attributes of inner nodes");
            Transform controls = page.transform.Find("ControlsViewport/ControlsContent");

            // Height metric
            ComboSelectBuilder.Init(controls.transform)
                .SetLabel("Height")
                .SetAllowedValues(_numericAttributes)
                .SetDefaultValue(_city.InnerNodeHeightMetric)
                .SetOnChangeHandler(s => _city.InnerNodeHeightMetric = s)
                .Build();

            // Leaf style metric
            ComboSelectBuilder.Init(controls.transform)
                .SetLabel("Style")
                .SetAllowedValues(_numericAttributes)
                .SetDefaultValue(_city.InnerNodeStyleMetric)
                .SetOnChangeHandler(s => _city.InnerNodeStyleMetric = s)
                .Build();

            // Lower color
            ColorPickerBuilder.Init(controls.transform)
                .SetLabel("Lower color")
                .SetDefaultValue(_city.InnerNodeColorRange.lower)
                .SetOnChangeHandler(c => _city.InnerNodeColorRange.lower = c)
                .SetColorPickerControl(_colorPickerControl)
                .Build();

            // Upper color
            ColorPickerBuilder.Init(controls.transform)
                .SetLabel("Upper color")
                .SetDefaultValue(_city.InnerNodeColorRange.upper)
                .SetOnChangeHandler(c => _city.InnerNodeColorRange.upper = c)
                .SetColorPickerControl(_colorPickerControl)
                .Build();

            // Number of colors
            SliderBuilder.Init(controls.transform)
                .SetLabel("# Colors")
                .SetMode(SliderMode.Integer)
                .SetDefaultValue(_city.InnerNodeColorRange.NumberOfColors)
                .SetOnChangeHandler(f => _city.InnerNodeColorRange.NumberOfColors =
                                        (uint)Math.Round(f))
                .SetRange((0, 15))
                .Build();

            CreateLabelSettingsInputs(controls, _city.InnerNodeLabelSettings);
        }

        private void CreateLabelSettingsInputs(Transform parent, LabelSettings labelSettings)
        {
            // Show labels
            SwitchBuilder.Init(parent)
                .SetLabel("Show labels")
                .SetDefaultValue(labelSettings.Show)
                .SetOnChangeHandler(b => labelSettings.Show = b)
                .Build();

            // Label distance
            SliderBuilder.Init(parent)
                .SetLabel("Label distance")
                .SetMode(SliderMode.Float)
                .SetDefaultValue(labelSettings.Distance)
                .SetOnChangeHandler(f => labelSettings.Distance = f)
                .SetRange((0, 2))
                .Build();

            // Label font size
            SliderBuilder.Init(parent)
                .SetLabel("Label font size")
                .SetMode(SliderMode.Float)
                .SetDefaultValue(labelSettings.FontSize)
                .SetOnChangeHandler(f => labelSettings.FontSize = f)
                .SetRange((0, 2))
                .Build();

            // Label animation duration
            SliderBuilder.Init(parent)
                .SetLabel("Label anim. duration")
                .SetMode(SliderMode.Float)
                .SetDefaultValue(labelSettings.AnimationDuration)
                .SetOnChangeHandler(f => labelSettings.AnimationDuration = f)
                .SetRange((0, 2))
                .Build();
        }

        private void SetupNodesLayoutPage()
        {
            CreateAndInsertTabButton("Nodes layout");
            GameObject page = CreateAndInsertPage("Nodes and node layout");
            Transform controls = page.transform.Find("ControlsViewport/ControlsContent");

            // Leaf nodes
            ComboSelectBuilder.Init(controls.transform)
                .SetLabel("Leaf nodes")
                .SetAllowedValues(EnumToStr<LeafNodeKinds>())
                .SetDefaultValue(_city.LeafObjects.ToString())
                .SetOnChangeHandler(s => Enum.TryParse(s, out _city.LeafObjects))
                .SetComboSelectMode(ComboSelectMode.Restricted)
                .Build();

            // Node layout
            ComboSelectBuilder.Init(controls.transform)
                .SetLabel("Node layout")
                .SetAllowedValues(EnumToStr<NodeLayoutKind>())
                .SetDefaultValue(_city.NodeLayout.ToString())
                .SetOnChangeHandler(s => Enum.TryParse(s, out _city.NodeLayout))
                .SetComboSelectMode(ComboSelectMode.Restricted)
                .Build();

            // Inner nodes
            ComboSelectBuilder.Init(controls.transform)
                .SetLabel("Inner nodes")
                .SetAllowedValues(EnumToStr<InnerNodeKinds>())
                .SetDefaultValue(_city.InnerNodeObjects.ToString())
                .SetOnChangeHandler(s => Enum.TryParse(s, out _city.InnerNodeObjects))
                .SetComboSelectMode(ComboSelectMode.Restricted)
                .Build();

            // Layout file
            FilePickerBuilder.Init(controls.transform)
                .SetLabel("Layout file")
                .SetPathInstance(_city.LayoutPath)
                .Build();

            // Z-score scaling
            SwitchBuilder.Init(controls.transform)
                .SetLabel("Z-score scaling")
                .SetDefaultValue(_city.ZScoreScale)
                .SetOnChangeHandler(b => _city.ZScoreScale = b)
                .Build();

            // Show erosions
            SwitchBuilder.Init(controls.transform)
                .SetLabel("Show erosions")
                .SetDefaultValue(_city.ShowErosions)
                .SetOnChangeHandler(b => _city.ShowErosions = b)
                .Build();

            // Max erosion width
            SliderBuilder.Init(controls.transform)
                .SetLabel("Max erosion width")
                .SetMode(SliderMode.Integer)
                .SetDefaultValue(_city.MaxErosionWidth)
                .SetOnChangeHandler(f => _city.MaxErosionWidth = f)
                .SetRange((1, 10))
                .Build();
        }

        private void SetupEdgesLayoutPage()
        {
            CreateAndInsertTabButton("Edges layout");
            GameObject page = CreateAndInsertPage("Edges and edge layout");
            Transform controls = page.transform.Find("ControlsViewport/ControlsContent");

            // Edge layout
            ComboSelectBuilder.Init(controls.transform)
                .SetLabel("Edge layout")
                .SetAllowedValues(EnumToStr<EdgeLayoutKind>())
                .SetDefaultValue(_city.EdgeLayout.ToString())
                .SetOnChangeHandler(s => Enum.TryParse(s, out _city.EdgeLayout))
                .SetComboSelectMode(ComboSelectMode.Restricted)
                .Build();

            // Edge width
            SliderBuilder.Init(controls.transform)
                .SetLabel("Edge width")
                .SetMode(SliderMode.Float)
                .SetDefaultValue(_city.EdgeWidth)
                .SetOnChangeHandler(f => _city.EdgeWidth = f)
                .SetRange((0, 0.5f))
                .Build();

            // Edges above block
            SwitchBuilder.Init(controls.transform)
                .SetLabel("Edges above block")
                .SetDefaultValue(_city.EdgesAboveBlocks)
                .SetOnChangeHandler(b => _city.EdgesAboveBlocks = b)
                .Build();

            // Bundling tension
            SliderBuilder.Init(controls.transform)
                .SetLabel("Bundling tension")
                .SetMode(SliderMode.Float)
                .SetDefaultValue(_city.Tension)
                .SetOnChangeHandler(f => _city.Tension = f)
                .SetRange((0, 1))
                .Build();

            // TODO: rdp
        }

        private void SetupMiscellaneousPage()
        {
            CreateAndInsertTabButton("Miscellaneous");
            GameObject page = CreateAndInsertPage("Miscellaneous");
            Transform controls = page.transform.Find("ControlsViewport/ControlsContent");

            // Settings file
            FilePickerBuilder.Init(controls.transform)
                .SetLabel("Settings file")
                .SetPathInstance(_city.CityPath)
                .Build();

            // LOD culling
            SliderBuilder.Init(controls.transform)
                .SetLabel("LOD culling")
                .SetMode(SliderMode.Float)
                .SetRange((0f, 1f))
                .SetDefaultValue(_city.LODCulling)
                .SetOnChangeHandler(f => _city.LODCulling = f);

            // GXL file
            FilePickerBuilder.Init(controls.transform)
                .SetLabel("GXL file")
                .SetPathInstance(_city.GXLPath)
                .Build();

            // Metric file
            FilePickerBuilder.Init(controls.transform)
                .SetLabel("Metric file")
                .SetPathInstance(_city.CSVPath)
                .Build();
        }

        private GameObject CreateAndInsertPage(string headline)
        {
            GameObject page = Instantiate(_pagePrefab, _tabOutlet.transform, false);
            page.MustGetComponent(out PageController pageController);
            pageController.headlineText = headline;
            return page;
        }

        private void CreateAndInsertTabButton(string label,
                                              TabButtonState initialState = TabButtonState.Inactive)
        {
            GameObject tabButton = Instantiate(_tabButtonPrefab, _tabButtons.transform, false);
            tabButton.name = $"{label}Button";
            tabButton.MustGetComponent(out TabButton button);
            button.buttonText = label;
            if (initialState == TabButtonState.InitialActive)
            {
                button.isDefaultActive = true;
            }
        }

        public static List<string> EnumToStr<EnumType>() where EnumType : Enum
        {
            return Enum.GetValues(typeof(EnumType))
                .Cast<EnumType>()
                .Select(v => v.ToString())
                .ToList();
        }

        public void Toggle()
        {
            _canvas.gameObject.SetActive(!_canvas.gameObject.activeSelf);
        }
    }
}
