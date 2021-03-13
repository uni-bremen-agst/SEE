using System;
using System.Collections.Generic;
using System.Linq;
using Michsky.UI.ModernUIPack;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.Layout.EdgeLayouts;
using SEE.Layout.NodeLayouts;
using SEE.Net;
using UnityEngine;
using UnityEngine.Events;
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
        private static List<string> _numericAttributes =
            Enum.GetValues(typeof(NumericAttributeNames))
                .Cast<NumericAttributeNames>()
                .Select(x => x.Name())
                .ToList();

        private const string PagePrefabPath = "Assets/Prefabs/UI/Page.prefab";
        private const string TabButtonPrefabPath = "Assets/Prefabs/UI/TabButton.prefab";
        private const string ActionButtonPrefabPath = "Assets/Prefabs/UI/ActionButton.prefab";

        private const string ComboSelectPrefabPath =
            "Assets/Prefabs/UI/Input Group - Dropdown.prefab";
        private const string ColorPickerPrefabPath =
            "Assets/Prefabs/UI/Input Group - Color Picker.prefab";
        private const string SliderPrefabPath =
            "Assets/Prefabs/UI/Input Group - Slider.prefab";
        private const string SwitchPrefabPath =
            "Assets/Prefabs/UI/Input Group - Switch.prefab";
        private const string FilePickerPrefabPath =
            "Assets/Prefabs/UI/Input Group - File Picker.prefab";

        private GameObject _pagePrefab;
        private GameObject _actionButtonPrefab;
        private GameObject _tabButtonPrefab;
        private GameObject _comboSelectPrefab;
        private GameObject _colorPickerPrefab;
        private GameObject _sliderPrefab;
        private GameObject _switchPrefab;
        private GameObject _filePickerPrefab;

        private GameObject _tabOutlet;
        private GameObject _tabButtons;
        private GameObject _actions;

        private SEECity _city;
        private ColorPickerControl _colorPickerControl;
        private ButtonManager _cityLoadButton;

        private void Start()
        {
            GameObject.Find("Implementation")?.MustGetComponent(out _city);
            if (!_city)
            {
                Debug.LogError("Did not find a city instance.");
                return;
            }

            MustGetChild("Canvas/TabNavigation/TabOutlet", out _tabOutlet);
            MustGetChild("Canvas/TabNavigation/Sidebar/TabButtons", out _tabButtons);
            MustGetChild("Canvas/Actions", out _actions);

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


            LoadPrefabs();
            SetupActions();
            SetupPages();
        }

        private void LoadPrefabs()
        {
            _tabButtonPrefab = MustLoadPrefabAtPath(TabButtonPrefabPath);
            _pagePrefab = MustLoadPrefabAtPath(PagePrefabPath);
            _comboSelectPrefab = MustLoadPrefabAtPath(ComboSelectPrefabPath);
            _colorPickerPrefab = MustLoadPrefabAtPath(ColorPickerPrefabPath);
            _sliderPrefab = MustLoadPrefabAtPath(SliderPrefabPath);
            _actionButtonPrefab = MustLoadPrefabAtPath(ActionButtonPrefabPath);
            _switchPrefab = MustLoadPrefabAtPath(SwitchPrefabPath);
            _filePickerPrefab = MustLoadPrefabAtPath(FilePickerPrefabPath);
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
                gameObject.SetActive(false);
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
            GameObject widthMetricHost =
                Instantiate(_comboSelectPrefab, controls);
            ComboSelectBuilder.Init(widthMetricHost)
                .SetLabel("Width")
                .SetAllowedValues(_numericAttributes)
                .SetDefaultValue(_city.WidthMetric)
                .SetOnChangeHandler(s => _city.WidthMetric = s)
                .Build();

            // Height metric
            GameObject heightMetricHost =
                Instantiate(_comboSelectPrefab, controls);
            ComboSelectBuilder.Init(heightMetricHost)
                .SetLabel("Height")
                .SetAllowedValues(_numericAttributes)
                .SetDefaultValue(_city.HeightMetric)
                .SetOnChangeHandler(s => _city.HeightMetric = s)
                .Build();

            // Height metric
            GameObject depthMetricHost =
                Instantiate(_comboSelectPrefab, controls);
            ComboSelectBuilder.Init(depthMetricHost)
                .SetLabel("Depth")
                .SetAllowedValues(_numericAttributes)
                .SetDefaultValue(_city.DepthMetric)
                .SetOnChangeHandler(s => _city.DepthMetric = s)
                .Build();

            // Leaf style metric
            GameObject leafStyleMetricHost =
                Instantiate(_comboSelectPrefab, controls);
            ComboSelectBuilder.Init(leafStyleMetricHost)
                .SetLabel("Style")
                .SetAllowedValues(_numericAttributes)
                .SetDefaultValue(_city.LeafStyleMetric)
                .SetOnChangeHandler(s => _city.LeafStyleMetric = s)
                .Build();

            // Lower color
            GameObject lowerColorHost =
                Instantiate(_colorPickerPrefab, controls);
            ColorPickerBuilder.Init(lowerColorHost)
                .SetLabel("Lower color")
                .SetDefaultValue(_city.LeafNodeColorRange.lower)
                .SetOnChangeHandler(c => _city.LeafNodeColorRange.lower = c)
                .SetColorPickerControl(_colorPickerControl)
                .Build();

            // Upper color
            GameObject upperColorHost =
                Instantiate(_colorPickerPrefab, controls);
            ColorPickerBuilder.Init(upperColorHost)
                .SetLabel("Upper color")
                .SetDefaultValue(_city.LeafNodeColorRange.upper)
                .SetOnChangeHandler(c => _city.LeafNodeColorRange.upper = c)
                .SetColorPickerControl(_colorPickerControl)
                .Build();

            // Number of colors
            GameObject numberOfColorsHost =
                Instantiate(_sliderPrefab, controls);
            SliderBuilder.Init(numberOfColorsHost)
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
            GameObject heightMetricHost =
                Instantiate(_comboSelectPrefab, controls);
            ComboSelectBuilder.Init(heightMetricHost)
                .SetLabel("Height")
                .SetAllowedValues(_numericAttributes)
                .SetDefaultValue(_city.InnerNodeHeightMetric)
                .SetOnChangeHandler(s => _city.InnerNodeHeightMetric = s)
                .Build();

            // Leaf style metric
            GameObject leafStyleMetricHost =
                Instantiate(_comboSelectPrefab, controls);
            ComboSelectBuilder.Init(leafStyleMetricHost)
                .SetLabel("Style")
                .SetAllowedValues(_numericAttributes)
                .SetDefaultValue(_city.InnerNodeStyleMetric)
                .SetOnChangeHandler(s => _city.InnerNodeStyleMetric = s)
                .Build();

            // Lower color
            GameObject lowerColorHost =
                Instantiate(_colorPickerPrefab, controls);
            ColorPickerBuilder.Init(lowerColorHost)
                .SetLabel("Lower color")
                .SetDefaultValue(_city.InnerNodeColorRange.lower)
                .SetOnChangeHandler(c => _city.InnerNodeColorRange.lower = c)
                .SetColorPickerControl(_colorPickerControl)
                .Build();

            // Upper color
            GameObject upperColorHost =
                Instantiate(_colorPickerPrefab, controls);
            ColorPickerBuilder.Init(upperColorHost)
                .SetLabel("Upper color")
                .SetDefaultValue(_city.InnerNodeColorRange.upper)
                .SetOnChangeHandler(c => _city.InnerNodeColorRange.upper = c)
                .SetColorPickerControl(_colorPickerControl)
                .Build();

            // Number of colors
            GameObject numberOfColorsHost =
                Instantiate(_sliderPrefab, controls);
            SliderBuilder.Init(numberOfColorsHost)
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
            GameObject showLabelsHost =
                Instantiate(_switchPrefab, parent);
            SwitchBuilder.Init(showLabelsHost)
                .SetLabel("Show labels")
                .SetDefaultValue(labelSettings.Show)
                .SetOnChangeHandler(b => labelSettings.Show = b)
                .Build();

            // Label distance
            GameObject labelDistanceHost =
                Instantiate(_sliderPrefab, parent);
            SliderBuilder.Init(labelDistanceHost)
                .SetLabel("Label distance")
                .SetMode(SliderMode.Float)
                .SetDefaultValue(labelSettings.Distance)
                .SetOnChangeHandler(f => labelSettings.Distance = f)
                .SetRange((0, 2))
                .Build();

            // Label font size
            GameObject labelFontSizeHost =
                Instantiate(_sliderPrefab, parent);
            SliderBuilder.Init(labelFontSizeHost)
                .SetLabel("Label font size")
                .SetMode(SliderMode.Float)
                .SetDefaultValue(labelSettings.FontSize)
                .SetOnChangeHandler(f => labelSettings.FontSize = f)
                .SetRange((0, 2))
                .Build();

            // Label animation duration
            GameObject labelAnimationDurationHost =
                Instantiate(_sliderPrefab, parent);
            SliderBuilder.Init(labelAnimationDurationHost)
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
            GameObject leafNodesHost =
                Instantiate(_comboSelectPrefab, controls);
            ComboSelectBuilder.Init(leafNodesHost)
                .SetLabel("Leaf nodes")
                .SetAllowedValues(EnumToStr<LeafNodeKinds>())
                .SetDefaultValue(_city.LeafObjects.ToString())
                .SetOnChangeHandler(s => Enum.TryParse(s, out _city.LeafObjects))
                .SetComboSelectMode(ComboSelectMode.Restricted)
                .Build();

            // Node layout
            GameObject nodeLayoutHost =
                Instantiate(_comboSelectPrefab, controls);
            ComboSelectBuilder.Init(nodeLayoutHost)
                .SetLabel("Node layout")
                .SetAllowedValues(EnumToStr<NodeLayoutKind>())
                .SetDefaultValue(_city.NodeLayout.ToString())
                .SetOnChangeHandler(s => Enum.TryParse(s, out _city.NodeLayout))
                .SetComboSelectMode(ComboSelectMode.Restricted)
                .Build();

            // Inner nodes
            GameObject innerNodesHost =
                Instantiate(_comboSelectPrefab, controls);
            ComboSelectBuilder.Init(innerNodesHost)
                .SetLabel("Inner nodes")
                .SetAllowedValues(EnumToStr<InnerNodeKinds>())
                .SetDefaultValue(_city.InnerNodeObjects.ToString())
                .SetOnChangeHandler(s => Enum.TryParse(s, out _city.InnerNodeObjects))
                .SetComboSelectMode(ComboSelectMode.Restricted)
                .Build();

            // Layout file
            GameObject layoutFileHost = Instantiate(_filePickerPrefab, controls);
            FilePickerBuilder.Init(layoutFileHost)
                .SetLabel("Layout file")
                .SetPathInstance(_city.LayoutPath)
                .Build();

            // Z-score scaling
            GameObject zScoreScalingHost =
                Instantiate(_switchPrefab, controls);
            SwitchBuilder.Init(zScoreScalingHost)
                .SetLabel("Z-score scaling")
                .SetDefaultValue(_city.ZScoreScale)
                .SetOnChangeHandler(b => _city.ZScoreScale = b)
                .Build();

            // Show erosions
            GameObject showErosionsHost =
                Instantiate(_switchPrefab, controls);
            SwitchBuilder.Init(showErosionsHost)
                .SetLabel("Show erosions")
                .SetDefaultValue(_city.ShowErosions)
                .SetOnChangeHandler(b => _city.ShowErosions = b)
                .Build();

            // Max erosion width
            GameObject maxErosionWidth =
                Instantiate(_sliderPrefab, controls);
            SliderBuilder.Init(maxErosionWidth)
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
            GameObject edgeLayoutHost =
                Instantiate(_comboSelectPrefab, controls);
            ComboSelectBuilder.Init(edgeLayoutHost)
                .SetLabel("Edge layout")
                .SetAllowedValues(EnumToStr<EdgeLayoutKind>())
                .SetDefaultValue(_city.EdgeLayout.ToString())
                .SetOnChangeHandler(s => Enum.TryParse(s, out _city.EdgeLayout))
                .SetComboSelectMode(ComboSelectMode.Restricted)
                .Build();

            // Edge width
            GameObject edgeWidthHost =
                Instantiate(_sliderPrefab, controls);
            SliderBuilder.Init(edgeWidthHost)
                .SetLabel("Edge width")
                .SetMode(SliderMode.Float)
                .SetDefaultValue(_city.EdgeWidth)
                .SetOnChangeHandler(f => _city.EdgeWidth = f)
                .SetRange((0, 0.5f))
                .Build();

            // Edges above block
            GameObject edgesAboveBlockHost =
                Instantiate(_switchPrefab, controls);
            SwitchBuilder.Init(edgesAboveBlockHost)
                .SetLabel("Edges above block")
                .SetDefaultValue(_city.EdgesAboveBlocks)
                .SetOnChangeHandler(b => _city.EdgesAboveBlocks = b)
                .Build();

            // Bundling tension
            GameObject bundlingTensionHost =
                Instantiate(_sliderPrefab, controls);
            SliderBuilder.Init(bundlingTensionHost)
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
            CreateAndInsertPage("Miscellaneous");
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
    }
}
