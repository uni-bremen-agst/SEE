using System;
using System.Collections.Generic;
using System.Linq;
using Michsky.UI.ModernUIPack;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.Net;
using UnityEngine;
using UnityEngine.Events;

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

        private GameObject _pagePrefab;
        private GameObject _actionButtonPrefab;
        private GameObject _tabButtonPrefab;
        private GameObject _comboSelectPrefab;
        private GameObject _colorPickerPrefab;
        private GameObject _sliderPrefab;

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

            // Label distance
            GameObject labelDistanceHost =
                Instantiate(_sliderPrefab, controls);
            SliderBuilder.Init(labelDistanceHost)
                .SetLabel("Label distance")
                .SetMode(SliderMode.Float)
                .SetDefaultValue(_city.LeafLabelSettings.Distance)
                .SetOnChangeHandler(f => _city.LeafLabelSettings.Distance = f)
                .SetRange((0, 2))
                .Build();

            // Label distance
            GameObject labelFontSizeHost =
                Instantiate(_sliderPrefab, controls);
            SliderBuilder.Init(labelFontSizeHost)
                .SetLabel("Label font size")
                .SetMode(SliderMode.Float)
                .SetDefaultValue(_city.LeafLabelSettings.FontSize)
                .SetOnChangeHandler(f => _city.LeafLabelSettings.FontSize = f)
                .SetRange((0, 2))
                .Build();

            // Label distance
            GameObject labelAnimationDurationHost =
                Instantiate(_sliderPrefab, controls);
            SliderBuilder.Init(labelAnimationDurationHost)
                .SetLabel("Label anim. duration")
                .SetMode(SliderMode.Float)
                .SetDefaultValue(_city.LeafLabelSettings.AnimationDuration)
                .SetOnChangeHandler(f => _city.LeafLabelSettings.AnimationDuration = f)
                .SetRange((0, 2))
                .Build();
        }

        private void SetupInnerNodesPage()
        {
            CreateAndInsertTabButton("Inner nodes");
            CreateAndInsertPage("Attributes of inner nodes");
        }

        private void SetupNodesLayoutPage()
        {
            CreateAndInsertTabButton("Nodes layout");
            CreateAndInsertPage("Nodes and node layout");
        }

        private void SetupEdgesLayoutPage()
        {
            CreateAndInsertTabButton("Edges layout");
            CreateAndInsertPage("Edges and edge layout");
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
    }
}
