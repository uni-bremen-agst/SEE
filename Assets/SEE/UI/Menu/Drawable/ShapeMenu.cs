using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable.Line;
using SEE.UI.Menu.Drawable.Shapes;
using SEE.UI.Notification;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;
using static SEE.Game.Drawable.ActionHelpers.ShapePointsCalculator;
using static SEE.Game.Drawable.ActionHelpers.UMLShapePointsCalculator;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Provides the menu for configuring drawable shapes.
    /// The menu stores the selected shape settings and coordinates the
    /// shape-specific controls with the line configuration menu.
    /// </summary>
    public static class ShapeMenu
    {
        #region Prefabs

        /// <summary>
        /// The prefab for the switch that opens either the shape menu
        /// or the line configuration menu.
        /// </summary>
        private const string drawableSwitchPrefab =
            "Prefabs/UI/Drawable/ShapeSwitch";

        /// <summary>
        /// The prefab containing the shape type selection,
        /// shape-specific values and information controls.
        /// </summary>
        private const string drawableShapePrefab =
            "Prefabs/UI/Drawable/ShapeMenu";

        #endregion

        #region Controls

        /// <summary>
        /// Holds all UI references used by the shape menu.
        /// </summary>
        private static readonly ShapeMenuControls controls;

        #endregion

        #region State

        /// <summary>
        /// Holds the current shape configuration.
        /// </summary>
        private static readonly ShapeMenuState state = new();

        /// <summary>
        /// Whether the shape information image is visible.
        /// </summary>
        private static bool infoVisibility;

        /// <summary>
        /// Gets or sets the currently selected shape orientation.
        /// </summary>
        public static Orientation orientation
        {
            get => state.Orientation;
            set => state.Orientation = value;
        }

        #endregion

        #region Layout

        /// <summary>
        /// Configures the shape-specific menu layout.
        /// </summary>
        private static readonly ShapeMenuLayout layout;

        #endregion

        /// <summary>
        /// Initializes the shape menu, resolves its controls and
        /// registers the required UI handlers.
        /// </summary>
        static ShapeMenu()
        {
            GameObject switchObject =
                PrefabInstantiator.InstantiatePrefab(
                    drawableSwitchPrefab,
                    UICanvas.Canvas.transform,
                    false);

            GameObject menuObject =
                PrefabInstantiator.InstantiatePrefab(
                    drawableShapePrefab,
                    UICanvas.Canvas.transform,
                    false);

            controls = new ShapeMenuControls(
                switchObject,
                menuObject);

            layout = new ShapeMenuLayout(
                controls,
                state);

            InitSwitchMenu();
            InitShapeMenu();
            InitConfigMenu();
        }

        #region Getters and Setters

        /// <summary>
        /// Gets the currently selected shape type.
        /// </summary>
        /// <returns>The selected shape type.</returns>
        public static Shape GetSelectedShape()
        {
            return state.SelectedShape;
        }

        /// <summary>
        /// Gets the currently selected UML shape type.
        /// </summary>
        /// <returns>The selected UML shape type.</returns>
        public static UMLShape GetSelectedUMLShape()
        {
            return state.SelectedUMLShape;
        }

        /// <summary>
        /// Gets the first shape value.
        /// </summary>
        /// <returns>The first shape value.</returns>
        public static float GetValue1()
        {
            return state.Value1;
        }

        /// <summary>
        /// Gets the second shape value.
        /// </summary>
        /// <returns>The second shape value.</returns>
        public static float GetValue2()
        {
            return state.Value2;
        }

        /// <summary>
        /// Gets the third shape value.
        /// </summary>
        /// <returns>The third shape value.</returns>
        public static float GetValue3()
        {
            return state.Value3;
        }

        /// <summary>
        /// Gets the fourth shape value.
        /// </summary>
        /// <returns>The fourth shape value.</returns>
        public static float GetValue4()
        {
            return state.Value4;
        }

        /// <summary>
        /// Gets the first angle.
        /// </summary>
        /// <returns>The first angle.</returns>
        public static float GetAngle1()
        {
            return state.Angle1;
        }

        /// <summary>
        /// Gets the second angle.
        /// </summary>
        /// <returns>The second angle.</returns>
        public static float GetAngle2()
        {
            return state.Angle2;
        }

        /// <summary>
        /// Gets the shape offset.
        /// </summary>
        /// <returns>The shape offset.</returns>
        public static float GetOffset()
        {
            return state.Offset;
        }

        /// <summary>
        /// Gets the number of polygon or arc vertices.
        /// </summary>
        /// <returns>The number of vertices.</returns>
        public static int GetVertices()
        {
            return state.Vertices;
        }

        /// <summary>
        /// Gets the current boolean shape option.
        /// </summary>
        /// <returns>
        /// True if the boolean option is enabled; otherwise, false.
        /// </returns>
        public static bool GetBoolValue()
        {
            return controls.BoolValueManager.isOn;
        }

        /// <summary>
        /// Sets the boolean shape option and refreshes its UI.
        /// </summary>
        /// <param name="value">The new boolean value.</param>
        public static void SetBoolValue(bool value)
        {
            controls.BoolValueManager.isOn = value;
            controls.BoolValueManager.UpdateUI();
        }

        /// <summary>
        /// Gets the currently selected shape orientation.
        /// </summary>
        /// <returns>The selected orientation.</returns>
        public static Orientation GetOrientation()
        {
            return orientation;
        }

        /// <summary>
        /// Gets a copy of the currently selected start line-cap configuration.
        /// </summary>
        /// <returns>The selected start line-cap configuration.</returns>
        public static LineCapConf GetLineStartCapConf()
        {
            return state.GetLineStartCapConf();
        }

        /// <summary>
        /// Gets a copy of the currently selected end line-cap configuration.
        /// </summary>
        /// <returns>The selected end line-cap configuration.</returns>
        public static LineCapConf GetLineEndCapConf()
        {
            return state.GetLineEndCapConf();
        }

        /// <summary>
        /// Gets the currently selected start line-cap kind.
        /// </summary>
        /// <returns>The selected start line-cap kind.</returns>
        public static LineCap GetLineStartCap()
        {
            return state.GetLineStartCap();
        }

        /// <summary>
        /// Gets the currently selected end line-cap kind.
        /// </summary>
        /// <returns>The selected end line-cap kind.</returns>
        public static LineCap GetLineEndCap()
        {
            return state.GetLineEndCap();
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Enables the shape switch and the menu that is currently selected.
        /// </summary>
        public static void Enable()
        {
            controls.SwitchObject.SetActive(true);

            if (!controls.ShapeButton.interactable)
            {
                LineMenu.Instance.Disable();
                controls.MenuObject.SetActive(true);
                BindShapeMenu();
            }
            else
            {
                controls.MenuObject.SetActive(false);
                LineMenu.Instance.EnableForDrawing();
                BindLineMenu();
            }
        }

        /// <summary>
        /// Disables the shape menu, the line menu and their switch.
        /// </summary>
        public static void Disable()
        {
            DisablePartUndo();
            controls.MenuObject.SetActive(false);
            LineMenu.Instance.Disable();
            controls.SwitchObject.SetActive(false);
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the switch between the shape menu and line configuration menu.
        /// By default, the shape menu is selected.
        /// </summary>
        private static void InitSwitchMenu()
        {
            controls.ShapeButtonManager.clickEvent.AddListener(ShapeOnClick);
            controls.ConfigButtonManager.clickEvent.AddListener(ConfigOnClick);

            controls.ShapeButton.interactable = false;
            controls.ShapeButtonManager.enabled = false;
        }

        /// <summary>
        /// Initializes the shape menu and registers the handlers for all controls.
        /// </summary>
        private static void InitShapeMenu()
        {
            InitializeSelector(
                controls.ShapeSelector,
                GetShapes(),
                selected => SetSelectedShape(selected));

            InitializeSelector(
                controls.UMLShapeSelector,
                GetUMLShapes(),
                selected => SetSelectedUMLShape(selected));

            InitializeSelector(
                controls.OrientationSelector,
                GetOrientations(),
                selected => state.Orientation = selected);

            InitializeSelector(
                controls.LineStartSelector,
                GetAllLineCaps(),
                selected =>
                {
                    LineCapConf conf = GetLineStartCapConf();
                    conf.CapKind = selected;
                    layout.SetLineStartCap(conf);
                });

            InitializeSelector(
                controls.LineEndSelector,
                GetAllLineCaps(),
                selected =>
                {
                    LineCapConf conf = GetLineEndCapConf();
                    conf.CapKind = selected;
                    layout.SetLineEndCap(conf);
                });

            InitializeFloatSlider(
                controls.Value1Slider,
                value => state.Value1 = value);

            InitializeFloatSlider(
                controls.Value2Slider,
                value => state.Value2 = value);

            InitializeFloatSlider(
                controls.Value3Slider,
                value => state.Value3 = value);

            InitializeFloatSlider(
                controls.Value4Slider,
                value => state.Value4 = value);

            InitializeFloatSlider(
                controls.Angle1Slider,
                value => state.Angle1 = value);

            InitializeFloatSlider(
                controls.Angle2Slider,
                value => state.Angle2 = value);

            InitializeFloatSlider(
                controls.OffsetSlider,
                value => state.Offset = value);

            InitializeIntSlider(
                controls.VerticesSlider,
                value => state.Vertices = value);

            infoVisibility = false;

            controls.InfoButtonManager.clickEvent.AddListener(ToggleInfo);

            controls.PartUndoObject
                .AddComponent<UIHoverTooltip>()
                .SetMessage("Part Undo");

            controls.PartUndoObject.SetActive(false);

            controls.DraggerInfoButtonManager.clickEvent.AddListener(() =>
            {
                if (state.SelectedShape == Shape.Line)
                {
                    ShowNotification.Info(
                        "Control instructions",
                        "Left mouse button = Adds a point to the line.\n"
                        + "Middle mouse button / mouse wheel click = Ends drawing the line without adding an additional point.\n"
                        + "Left Ctrl key + left mouse button = Ends drawing and adds a final point.");
                }
                else
                {
                    ShowNotification.Info(
                        "Control instructions",
                        "Middle mouse button / mouse wheel click = Fixes a point for the shape preview.\n"
                        + "Left Ctrl key + middle mouse button = Releases the fixed point.");
                }
            });

            SetSelectedShape(Shape.Line);
        }

        /// <summary>
        /// Initializes the line configuration menu used by the shape menu.
        /// </summary>
        private static void InitConfigMenu()
        {
            LineMenu.Instance.EnableForDrawing();
            LineMenu.Instance.Enable();
        }

        /// <summary>
        /// Initializes a horizontal selector with the given values
        /// and registers the selection callback.
        /// </summary>
        /// <typeparam name="T">The selectable value type.</typeparam>
        /// <param name="selector">The selector to initialize.</param>
        /// <param name="values">The values displayed in the selector.</param>
        /// <param name="onSelected">
        /// The callback invoked when the user selects a value.
        /// </param>
        private static void InitializeSelector<T>(
            HorizontalSelector selector,
            List<T> values,
            Action<T> onSelected)
        {
            foreach (T value in values)
            {
                selector.CreateNewItem(value.ToString());
            }

            selector.selectorEvent.AddListener(index =>
            {
                onSelected(values[index]);
            });

            selector.defaultIndex = 0;
        }

        /// <summary>
        /// Registers a value changed callback for the given float slider.
        /// </summary>
        /// <param name="slider">The slider to initialize.</param>
        /// <param name="onValueChanged">
        /// The callback invoked when the value changes.
        /// </param>
        private static void InitializeFloatSlider(
            FloatValueSliderController slider,
            Action<float> onValueChanged)
        {
            slider.onValueChanged.AddListener(value =>
            {
                onValueChanged(value);
            });
        }

        /// <summary>
        /// Initializes the given integer slider and registers its value changed callback.
        /// </summary>
        /// <param name="slider">The slider to initialize.</param>
        /// <param name="onValueChanged">
        /// The callback invoked when the value changes.
        /// </param>
        private static void InitializeIntSlider(
            IntValueSliderController slider,
            Action<int> onValueChanged)
        {
            onValueChanged(slider.GetValue());

            slider.OnValueChanged.AddListener(value =>
            {
                onValueChanged(value);
            });
        }

        #endregion

        #region Information and Action Buttons

        /// <summary>
        /// Toggles the visibility of the shape information image.
        /// </summary>
        private static void ToggleInfo()
        {
            infoVisibility = !infoVisibility;
            controls.ImageObject.SetActive(infoVisibility);

            if (infoVisibility)
            {
                LoadImage();
            }

            MenuHelper.CalculateHeight(controls.MenuObject);
        }

        /// <summary>
        /// Enables the partial undo button and assigns an action to it.
        /// </summary>
        /// <param name="action">The action assigned to the button.</param>
        public static void ActivatePartUndo(UnityAction action)
        {
            controls.PartUndoObject.SetActive(true);
            controls.PartUndoButtonManager.clickEvent.RemoveAllListeners();
            controls.PartUndoButtonManager.clickEvent.AddListener(action);
        }

        /// <summary>
        /// Disables the partial undo button.
        /// </summary>
        public static void DisablePartUndo()
        {
            controls.PartUndoObject.SetActive(false);
        }

        /// <summary>
        /// Loads the information image belonging to the selected shape.
        /// </summary>
        private static void LoadImage()
        {
            string path = "";

            switch (state.SelectedShape)
            {
                case Shape.Square:
                    path = "Textures/Drawable/Square";
                    break;

                case Shape.Rectangle:
                    path = "Textures/Drawable/Rectangle";
                    break;

                case Shape.Rhombus:
                    path = "Textures/Drawable/Rhombus";
                    break;

                case Shape.Kite:
                    path = "Textures/Drawable/Kite";
                    break;

                case Shape.Triangle:
                    path = "Textures/Drawable/Triangle";
                    break;

                case Shape.Circle:
                    path = "Textures/Drawable/Circle";
                    break;

                case Shape.Ellipse:
                    path = "Textures/Drawable/Ellipse";
                    break;

                case Shape.Parallelogram:
                    path = "Textures/Drawable/Parallelogram";
                    break;

                case Shape.Trapezoid:
                    path = "Textures/Drawable/Trapezoid";
                    break;

                case Shape.Polygon:
                    path = "Textures/Drawable/Polygon";
                    break;
            }

            controls.InfoImage.sprite =
                Resources.Load<Sprite>(path);
        }

        #endregion

        #region Shape State

        /// <summary>
        /// Sets the selected shape type and refreshes the menu layout.
        /// </summary>
        /// <param name="shape">The selected shape type.</param>
        private static void SetSelectedShape(Shape shape)
        {
            state.SelectedShape = shape;
            UpdateLayout();
        }

        /// <summary>
        /// Sets the selected UML shape type and refreshes the menu layout.
        /// </summary>
        /// <param name="umlShape">The selected UML shape type.</param>
        private static void SetSelectedUMLShape(UMLShape umlShape)
        {
            state.SelectedUMLShape = umlShape;
            UpdateLayout();
        }

        /// <summary>
        /// Updates the shape-specific layout.
        /// </summary>
        private static void UpdateLayout()
        {
            infoVisibility = false;
            layout.UpdateForSelection();
        }

        #endregion

        #region Menu Switching

        /// <summary>
        /// Opens the line configuration menu in the appropriate drawing
        /// or editing mode.
        /// </summary>
        public static void OpenLineMenuInCorrectMode()
        {
            ConfigOnClick();
        }

        /// <summary>
        /// Opens the line configuration menu and closes the shape menu.
        /// </summary>
        private static void ConfigOnClick()
        {
            controls.ConfigButton.interactable = false;
            controls.ConfigButtonManager.enabled = false;

            controls.ShapeButtonManager.enabled = true;
            controls.ShapeButton.interactable = true;

            if (DrawShapesAction.currentShape == null)
            {
                LineMenu.Instance.EnableForDrawing();
            }
            else
            {
                LineMenu.Instance.EnableForEditing(
                    DrawShapesAction.currentShape,
                    LineConf.Get(DrawShapesAction.currentShape));
            }

            MenuHelper.CalculateHeight(
                LineMenu.Instance.GameObject);

            BindLineMenu();

            controls.MenuObject.SetActive(false);
        }

        /// <summary>
        /// Binds the line configuration menu to the shape switch.
        /// </summary>
        private static void BindLineMenu()
        {
            LineMenu.Instance.GameObject.transform.SetParent(
                controls.SwitchContent);

            GameObject dragger =
                GameFinder.FindAttachedOrLocalDescendant(
                    LineMenu.Instance.GameObject,
                    "Dragger");

            dragger
                .GetComponent<WindowDragger>()
                .enabled = false;
        }

        /// <summary>
        /// Binds the shape menu to the shape switch.
        /// </summary>
        private static void BindShapeMenu()
        {
            controls.MenuObject.transform.SetParent(
                controls.SwitchContent);

            controls.MenuDragger.enabled = false;
        }

        /// <summary>
        /// Opens the shape menu and closes the line configuration menu.
        /// </summary>
        private static void ShapeOnClick()
        {
            controls.ShapeButton.interactable = false;
            controls.ShapeButtonManager.enabled = false;

            controls.ConfigButton.interactable = true;
            controls.ConfigButtonManager.enabled = true;

            LineMenu.Instance.Disable();

            BindShapeMenu();

            controls.MenuObject.SetActive(true);
        }

        #endregion

        #region External Button Configuration

        /// <summary>
        /// Assigns an action to the finish button.
        /// </summary>
        /// <param name="action">
        /// The action assigned to the finish button.
        /// </param>
        public static void AssignFinishButton(UnityAction action)
        {
            controls.FinishButtonManager
                .clickEvent
                .RemoveAllListeners();

            controls.FinishButtonManager
                .clickEvent
                .AddListener(action);
        }

        #endregion

        #region Line Caps

        /// <summary>
        /// Sets the selected start and end line-cap configurations.
        /// </summary>
        /// <param name="startCapConf">
        /// The start line-cap configuration.
        /// </param>
        /// <param name="endCapConf">
        /// The end line-cap configuration.
        /// </param>
        public static void SetLineCaps(
            LineCapConf startCapConf,
            LineCapConf endCapConf)
        {
            layout.SetLineCaps(
                startCapConf,
                endCapConf);
        }

        #endregion
    }
}
