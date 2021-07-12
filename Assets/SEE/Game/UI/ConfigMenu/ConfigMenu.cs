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
using System.Collections.Generic;
using System.Linq;
using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SEE.Game.UI.ConfigMenu
{
    enum TabButtonState
    {
        InitialActive,
        Inactive,
    }

    /// <summary>
    /// The primary wrapper script for the config menu prefab. The config menu allows for runtime
    /// configuration of a SEECity instance. It's agnostic about what instance can be manipulated and
    /// offers an easy way to extend the list of instances that can be accessed.
    ///
    /// This script instantiates almost all of its used game objects.
    /// </summary>
    public class ConfigMenu : DynamicUIBehaviour
    {
        /// <summary>
        /// A list of numeric attributes digestible by the ComboSelect component.
        /// </summary>
        private static readonly List<string> NumericAttributes =
            Enum.GetValues(typeof(NumericAttributeNames))
                .Cast<NumericAttributeNames>()
                .Select(x => x.Name())
                .ToList();

        /// <summary>
        /// The list of SEECity instances this menu can manipulate.
        /// </summary>
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

        /// <summary>
        /// The event handler that gets called when an user interaction changes the currently edited
        /// SEECity instance.
        /// </summary>
        public UnityEvent<EditableInstance> OnInstanceChangeRequest =
            new UnityEvent<EditableInstance>();

        /// <summary>
        /// The currently edited SEECity instance.
        /// </summary>
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
                // Attach the pointer to the appropriate hand.
                Transform attachmentPoint = GameObject
                    .Find("VRPlayer/SteamVRObjects/RightHand/ObjectAttachmentPoint").transform;
                GameObject pointer =
                    Instantiate(MustLoadPrefabAtPath(PointerPrefabPath), attachmentPoint);
                Camera pointerCamera = pointer.GetComponent<Camera>();

                // Replace the default input system with our VR input system.
                GameObject vrEventSystem =
                    GameObject.FindWithTag("VREventSystem");
                vrEventSystem.GetComponent<StandaloneInputModule>().enabled = false;
                vrEventSystem.AddComponent<VRInputModule>();
                VRInputModule vrInputModule = vrEventSystem.GetComponent<VRInputModule>();
                vrInputModule.PointerCamera = pointerCamera;
                pointer.GetComponent<Pointer>().InputModule = vrInputModule;

                // Set the canvas to world space and adjust its positition.
                MustGetComponentInChild("Canvas", out RectTransform rectTransform);
                _canvas.renderMode = RenderMode.WorldSpace;
                _canvas.worldCamera = pointerCamera;
                rectTransform.anchoredPosition3D = Vector3.zero;
                rectTransform.localScale = Vector3.one;

                // Maker the color picker slightly rotated towards the user.
                _colorPickerControl.gameObject.transform.Rotate(0f, 45f, 0f);

                // Place the menu as a whole in front of the 'table'.
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

            foreach (LeafNodeAttributes leafNodeAttributes in _city.leafNodeAttributesPerKind)
            {
                // FIXME: the domain must be appended to these labels

                // Shape type for leaf nodes
                ComboSelectBuilder.Init(controls.transform)
                    .SetLabel("Shape")
                    .SetAllowedValues(EnumToStr<LeafNodeKinds>())
                    .SetDefaultValue(leafNodeAttributes.kind.ToString())
                    .SetOnChangeHandler(s => Enum.TryParse(s, out leafNodeAttributes.kind))
                    .SetComboSelectMode(ComboSelectMode.Restricted)
                    .Build();

                // Width metric
                ComboSelectBuilder.Init(controls.transform)
                    .SetLabel("Width")
                    .SetAllowedValues(NumericAttributes)
                    .SetDefaultValue(leafNodeAttributes.widthMetric)
                    .SetOnChangeHandler(s => leafNodeAttributes.widthMetric = s)
                    .Build();

                // Height metric
                ComboSelectBuilder.Init(controls.transform)
                    .SetLabel("Height")
                    .SetAllowedValues(NumericAttributes)
                    .SetDefaultValue(leafNodeAttributes.heightMetric)
                    .SetOnChangeHandler(s => leafNodeAttributes.heightMetric = s)
                    .Build();

                // Height metric
                ComboSelectBuilder.Init(controls.transform)
                    .SetLabel("Depth")
                    .SetAllowedValues(NumericAttributes)
                    .SetDefaultValue(leafNodeAttributes.depthMetric)
                    .SetOnChangeHandler(s => leafNodeAttributes.depthMetric = s)
                    .Build();

                // Leaf style metric
                ComboSelectBuilder.Init(controls.transform)
                    .SetLabel("Style")
                    .SetAllowedValues(NumericAttributes)
                    .SetDefaultValue(leafNodeAttributes.styleMetric)
                    .SetOnChangeHandler(s => leafNodeAttributes.styleMetric = s)
                    .Build();

                // Lower color
                ColorPickerBuilder.Init(controls.transform)
                    .SetLabel("Lower color")
                    .SetDefaultValue(leafNodeAttributes.colorRange.lower)
                    .SetOnChangeHandler(c => leafNodeAttributes.colorRange.lower = c)
                    .SetColorPickerControl(_colorPickerControl)
                    .Build();

                // Upper color
                ColorPickerBuilder.Init(controls.transform)
                    .SetLabel("Upper color")
                    .SetDefaultValue(leafNodeAttributes.colorRange.upper)
                    .SetOnChangeHandler(c => leafNodeAttributes.colorRange.upper = c)
                    .SetColorPickerControl(_colorPickerControl)
                    .Build();

                // Number of colors
                SliderBuilder.Init(controls.transform)
                    .SetLabel("# Colors")
                    .SetMode(SliderMode.Integer)
                    .SetDefaultValue(leafNodeAttributes.colorRange.NumberOfColors)
                    .SetOnChangeHandler(f => leafNodeAttributes.colorRange.NumberOfColors =
                                            (uint)Math.Round(f))
                    .SetRange((0, 15))
                    .Build();

                CreateLabelSettingsInputs(controls, leafNodeAttributes.labelSettings);
            }
        }

        private void SetupInnerNodesPage()
        {
            CreateAndInsertTabButton("Inner nodes");
            GameObject page = CreateAndInsertPage("Attributes of inner nodes");
            Transform controls = page.transform.Find("ControlsViewport/ControlsContent");

            foreach (InnerNodeAttributes innerNodeAttributes in _city.innerNodeAttributesPerKind)
            {
                // FIXME: the domain must be appended to these labels

                // Shape type for inner nodes
                ComboSelectBuilder.Init(controls.transform)
                    .SetLabel("Shape")
                    .SetAllowedValues(EnumToStr<InnerNodeKinds>())
                    .SetDefaultValue(innerNodeAttributes.kind.ToString())
                    .SetOnChangeHandler(s => Enum.TryParse(s, out innerNodeAttributes.kind))
                    .SetComboSelectMode(ComboSelectMode.Restricted)
                    .Build();

                // Height metric
                ComboSelectBuilder.Init(controls.transform)
                    .SetLabel("Height")
                    .SetAllowedValues(NumericAttributes)
                    .SetDefaultValue(innerNodeAttributes.heightMetric)
                    .SetOnChangeHandler(s => innerNodeAttributes.heightMetric = s)
                    .Build();

                // Leaf style metric
                ComboSelectBuilder.Init(controls.transform)
                    .SetLabel("Style")
                    .SetAllowedValues(NumericAttributes)
                    .SetDefaultValue(innerNodeAttributes.styleMetric)
                    .SetOnChangeHandler(s => innerNodeAttributes.styleMetric = s)
                    .Build();

                // Lower color
                ColorPickerBuilder.Init(controls.transform)
                    .SetLabel("Lower color")
                    .SetDefaultValue(innerNodeAttributes.colorRange.lower)
                    .SetOnChangeHandler(c => innerNodeAttributes.colorRange.lower = c)
                    .SetColorPickerControl(_colorPickerControl)
                    .Build();

                // Upper color
                ColorPickerBuilder.Init(controls.transform)
                    .SetLabel("Upper color")
                    .SetDefaultValue(innerNodeAttributes.colorRange.upper)
                    .SetOnChangeHandler(c => innerNodeAttributes.colorRange.upper = c)
                    .SetColorPickerControl(_colorPickerControl)
                    .Build();

                // Number of colors
                SliderBuilder.Init(controls.transform)
                    .SetLabel("# Colors")
                    .SetMode(SliderMode.Integer)
                    .SetDefaultValue(innerNodeAttributes.colorRange.NumberOfColors)
                    .SetOnChangeHandler(f => innerNodeAttributes.colorRange.NumberOfColors =
                                            (uint)Math.Round(f))
                    .SetRange((0, 15))
                    .Build();

                CreateLabelSettingsInputs(controls, innerNodeAttributes.labelSettings);
            }
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

            // Node layout
            ComboSelectBuilder.Init(controls.transform)
                .SetLabel("Node layout")
                .SetAllowedValues(EnumToStr<NodeLayoutKind>())
                .SetDefaultValue(_city.nodeLayoutSettings.kind.ToString())
                .SetOnChangeHandler(s => Enum.TryParse(s, out _city.nodeLayoutSettings.kind))
                .SetComboSelectMode(ComboSelectMode.Restricted)
                .Build();

            // Layout file
            FilePickerBuilder.Init(controls.transform)
                .SetLabel("Layout file")
                .SetPathInstance(_city.globalCityAttributes.layoutPath)
                .Build();

            // Z-score scaling
            SwitchBuilder.Init(controls.transform)
                .SetLabel("Z-score scaling")
                .SetDefaultValue(_city.nodeLayoutSettings.zScoreScale)
                .SetOnChangeHandler(b => _city.nodeLayoutSettings.zScoreScale = b)
                .Build();

            // Show erosions
            SwitchBuilder.Init(controls.transform)
                .SetLabel("Show erosions")
                .SetDefaultValue(_city.nodeLayoutSettings.showErosions)
                .SetOnChangeHandler(b => _city.nodeLayoutSettings.showErosions = b)
                .Build();

            // Max erosion width
            SliderBuilder.Init(controls.transform)
                .SetLabel("Max erosion width")
                .SetMode(SliderMode.Integer)
                .SetDefaultValue(_city.nodeLayoutSettings.maxErosionWidth)
                .SetOnChangeHandler(f => _city.nodeLayoutSettings.maxErosionWidth = f)
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
                .SetDefaultValue(_city.edgeLayoutSettings.kind.ToString())
                .SetOnChangeHandler(s => Enum.TryParse(s, out _city.edgeLayoutSettings.kind))
                .SetComboSelectMode(ComboSelectMode.Restricted)
                .Build();

            // Edge width
            SliderBuilder.Init(controls.transform)
                .SetLabel("Edge width")
                .SetMode(SliderMode.Float)
                .SetDefaultValue(_city.edgeLayoutSettings.edgeWidth)
                .SetOnChangeHandler(f => _city.edgeLayoutSettings.edgeWidth = f)
                .SetRange((0, 0.5f))
                .Build();

            // Edges above block
            SwitchBuilder.Init(controls.transform)
                .SetLabel("Edges above block")
                .SetDefaultValue(_city.edgeLayoutSettings.edgesAboveBlocks)
                .SetOnChangeHandler(b => _city.edgeLayoutSettings.edgesAboveBlocks = b)
                .Build();

            // Bundling tension
            SliderBuilder.Init(controls.transform)
                .SetLabel("Bundling tension")
                .SetMode(SliderMode.Float)
                .SetDefaultValue(_city.edgeLayoutSettings.tension)
                .SetOnChangeHandler(f => _city.edgeLayoutSettings.tension = f)
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
                .SetDefaultValue(_city.globalCityAttributes.lodCulling)
                .SetOnChangeHandler(f => _city.globalCityAttributes.lodCulling = f);

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

        /// <summary>
        /// Converts an enum to a list of strings.
        /// </summary>
        /// <typeparam name="EnumType">The enum to map.</typeparam>
        /// <returns>a list of string representations of the enum.</returns>
        public static List<string> EnumToStr<EnumType>() where EnumType : Enum
        {
            return Enum.GetValues(typeof(EnumType))
                .Cast<EnumType>()
                .Select(v => v.ToString())
                .ToList();
        }

        /// <summary>
        /// Toggles the visibility of the menu.
        /// </summary>
        public void Toggle()
        {
            _canvas.gameObject.SetActive(!_canvas.gameObject.activeSelf);
        }
    }
}
