using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable.Line;
using SEE.UI.Menu.Drawable.Shapes;
using SEE.UI.Notification;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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

        #region Value Holders

        /// <summary>
        /// Contains the currently selected shape type.
        /// </summary>
        private static Shape selectedShape;

        /// <summary>
        /// Contains the currently selected UML shape type.
        /// This value is only relevant when <see cref="selectedShape"/>
        /// is <see cref="Shape.UML"/>.
        /// </summary>
        private static UMLShape selectedUMLShape;

        /// <summary>
        /// Contains the currently selected first shape value.
        /// </summary>
        private static float value1;

        /// <summary>
        /// Contains the currently selected second shape value.
        /// </summary>
        private static float value2;

        /// <summary>
        /// Contains the currently selected third shape value.
        /// </summary>
        private static float value3;

        /// <summary>
        /// Contains the currently selected fourth shape value.
        /// </summary>
        private static float value4;

        /// <summary>
        /// Contains the currently selected first angle.
        /// </summary>
        private static float angle1;

        /// <summary>
        /// Contains the currently selected second angle.
        /// </summary>
        private static float angle2;

        /// <summary>
        /// Contains the currently selected shape offset.
        /// </summary>
        private static float offset;

        /// <summary>
        /// Contains the currently selected number of vertices.
        /// </summary>
        private static int vertices;

        /// <summary>
        /// Contains the currently selected shape orientation.
        /// </summary>
        public static Orientation orientation;

        /// <summary>
        /// The currently selected start line-cap configuration.
        /// </summary>
        private static LineCapConf lineStartCapConf;

        /// <summary>
        /// The currently selected end line-cap configuration.
        /// </summary>
        private static LineCapConf lineEndCapConf;

        /// <summary>
        /// Whether the shape information image is visible.
        /// </summary>
        private static bool infoVisibility;

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
            return selectedShape;
        }

        /// <summary>
        /// Gets the currently selected UML shape type.
        /// </summary>
        /// <returns>The selected UML shape type.</returns>
        public static UMLShape GetSelectedUMLShape()
        {
            return selectedUMLShape;
        }

        /// <summary>
        /// Gets the first shape value.
        /// </summary>
        /// <returns>The first shape value.</returns>
        public static float GetValue1()
        {
            return value1;
        }

        /// <summary>
        /// Gets the second shape value.
        /// </summary>
        /// <returns>The second shape value.</returns>
        public static float GetValue2()
        {
            return value2;
        }

        /// <summary>
        /// Gets the third shape value.
        /// </summary>
        /// <returns>The third shape value.</returns>
        public static float GetValue3()
        {
            return value3;
        }

        /// <summary>
        /// Gets the fourth shape value.
        /// </summary>
        /// <returns>The fourth shape value.</returns>
        public static float GetValue4()
        {
            return value4;
        }

        /// <summary>
        /// Gets the first angle.
        /// </summary>
        /// <returns>The first angle.</returns>
        public static float GetAngle1()
        {
            return angle1;
        }

        /// <summary>
        /// Gets the second angle.
        /// </summary>
        /// <returns>The second angle.</returns>
        public static float GetAngle2()
        {
            return angle2;
        }

        /// <summary>
        /// Gets the shape offset.
        /// </summary>
        /// <returns>The shape offset.</returns>
        public static float GetOffset()
        {
            return offset;
        }

        /// <summary>
        /// Gets the number of polygon or arc vertices.
        /// </summary>
        /// <returns>The number of vertices.</returns>
        public static int GetVertices()
        {
            return vertices;
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
            return lineStartCapConf != null
                ? lineStartCapConf.Clone()
                : LineCapConf.CreateNone();
        }

        /// <summary>
        /// Gets a copy of the currently selected end line-cap configuration.
        /// </summary>
        /// <returns>The selected end line-cap configuration.</returns>
        public static LineCapConf GetLineEndCapConf()
        {
            return lineEndCapConf != null
                ? lineEndCapConf.Clone()
                : LineCapConf.CreateNone();
        }

        /// <summary>
        /// Gets the currently selected start line-cap kind.
        /// </summary>
        /// <returns>The selected start line-cap kind.</returns>
        public static LineCap GetLineStartCap()
        {
            return lineStartCapConf?.CapKind ?? LineCap.None;
        }

        /// <summary>
        /// Gets the currently selected end line-cap kind.
        /// </summary>
        /// <returns>The selected end line-cap kind.</returns>
        public static LineCap GetLineEndCap()
        {
            return lineEndCapConf?.CapKind ?? LineCap.None;
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
                selected => orientation = selected);

            InitializeSelector(
                controls.LineStartSelector,
                GetAllLineCaps(),
                selected =>
                {
                    LineCapConf conf = GetLineStartCapConf();
                    conf.CapKind = selected;
                    SetLineStartCap(conf);
                });

            InitializeSelector(
                controls.LineEndSelector,
                GetAllLineCaps(),
                selected =>
                {
                    LineCapConf conf = GetLineEndCapConf();
                    conf.CapKind = selected;
                    SetLineEndCap(conf);
                });

            InitializeFloatSlider(
                controls.Value1Slider,
                value => value1 = value);

            InitializeFloatSlider(
                controls.Value2Slider,
                value => value2 = value);

            InitializeFloatSlider(
                controls.Value3Slider,
                value => value3 = value);

            InitializeFloatSlider(
                controls.Value4Slider,
                value => value4 = value);

            InitializeFloatSlider(
                controls.Angle1Slider,
                value => angle1 = value);

            InitializeFloatSlider(
                controls.Angle2Slider,
                value => angle2 = value);

            InitializeFloatSlider(
                controls.OffsetSlider,
                value => offset = value);

            InitializeIntSlider(
                controls.VerticesSlider,
                value => vertices = value);

            infoVisibility = false;
            controls.InfoButtonManager.clickEvent.AddListener(ToggleInfo);

            controls.PartUndoObject
                .AddComponent<UIHoverTooltip>()
                .SetMessage("Part Undo");

            controls.PartUndoObject.SetActive(false);

            controls.DraggerInfoButtonManager.clickEvent.AddListener(() =>
            {
                if (selectedShape == Shape.Line)
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

            switch (selectedShape)
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
        /// Sets the selected shape type and refreshes the menu.
        /// </summary>
        /// <param name="shape">The selected shape type.</param>
        private static void SetSelectedShape(Shape shape)
        {
            selectedShape = shape;
            ChangeMenu();
        }

        /// <summary>
        /// Sets the selected UML shape type and refreshes the menu.
        /// </summary>
        /// <param name="umlShape">The selected UML shape type.</param>
        private static void SetSelectedUMLShape(UMLShape umlShape)
        {
            selectedUMLShape = umlShape;
            ChangeMenu();
        }

        /// <summary>
        /// Resets all shape values and selectors to their defaults.
        /// </summary>
        private static void AllValuesReset()
        {
            RestoreBoolValuePosition();

            controls.UMLShapeSelectorObject.SetActive(true);
            controls.Value1Object.SetActive(true);
            controls.Value2Object.SetActive(true);
            controls.Value3Object.SetActive(true);
            controls.Value4Object.SetActive(true);
            controls.Angle1Object.SetActive(true);
            controls.Angle2Object.SetActive(true);
            controls.OffsetObject.SetActive(true);
            controls.VerticesObject.SetActive(true);
            controls.BoolValueObject.SetActive(true);
            SetOrientationActive(true);
            SetLineStartActive(true);
            SetLineEndActive(true);
            controls.FinishObject.SetActive(true);

            controls.Value1Slider.ResetToMin();
            controls.Value2Slider.ResetToMin();
            controls.Value3Slider.ResetToMin();
            controls.Value4Slider.ResetToMin();
            controls.Angle1Slider.ResetToMin();
            controls.Angle2Slider.ResetToMin();
            controls.OffsetSlider.ResetToMin();
            controls.VerticesSlider.ResetToMin();

            controls.BoolValueManager.isOn = false;

            ResetSelector(controls.OrientationSelector);
            ResetSelector(controls.LineStartSelector);
            ResetSelector(controls.LineEndSelector);

            infoVisibility = false;

            SetLineCaps(
                LineCapConf.CreateNone(),
                LineCapConf.CreateNone());

            orientation = Orientation.Up;

            static void ResetSelector(HorizontalSelector selector)
            {
                selector.index = 0;
                selector.defaultIndex = 0;
                selector.UpdateUI();
            }
        }

        /// <summary>
        /// Restores the boolean option to its original menu position.
        /// </summary>
        private static void RestoreBoolValuePosition()
        {
            controls.BoolValueObject.transform.SetSiblingIndex(
                controls.BoolValueDefaultSiblingIndex);
        }

        /// <summary>
        /// Moves the boolean option to the line-specific position
        /// directly before the finish button.
        /// </summary>
        private static void MoveBoolValueToLinePosition()
        {
            int boolIndex =
                controls.BoolValueObject.transform.GetSiblingIndex();

            int finishIndex =
                controls.FinishObject.transform.GetSiblingIndex();

            if (boolIndex < finishIndex)
            {
                finishIndex--;
            }

            controls.BoolValueObject.transform.SetSiblingIndex(
                finishIndex);
        }

        /// <summary>
        /// Disables all shape-specific value controls.
        /// </summary>
        private static void AllValuesDisable()
        {
            controls.UMLShapeSelectorObject.SetActive(false);
            controls.Value1Object.SetActive(false);
            controls.Value2Object.SetActive(false);
            controls.Value3Object.SetActive(false);
            controls.Value4Object.SetActive(false);
            controls.Angle1Object.SetActive(false);
            controls.Angle2Object.SetActive(false);
            controls.OffsetObject.SetActive(false);
            controls.VerticesObject.SetActive(false);
            controls.BoolValueObject.SetActive(false);

            SetOrientationActive(false);
            SetLineStartActive(false);
            SetLineEndActive(false);

            controls.InfoObject.SetActive(false);
            controls.ImageObject.SetActive(false);
            controls.FinishObject.SetActive(false);
        }

        /// <summary>
        /// Sets whether the start line-cap selector and its label are visible.
        /// </summary>
        /// <param name="isActive">
        /// True to show the controls; otherwise, false.
        /// </param>
        private static void SetLineStartActive(bool isActive)
        {
            SetUIElementActive(
                controls.LineStartObject,
                controls.LineStartTextObject,
                isActive);
        }

        /// <summary>
        /// Sets whether the end line-cap selector and its label are visible.
        /// </summary>
        /// <param name="isActive">
        /// True to show the controls; otherwise, false.
        /// </param>
        private static void SetLineEndActive(bool isActive)
        {
            SetUIElementActive(
                controls.LineEndObject,
                controls.LineEndTextObject,
                isActive);
        }

        /// <summary>
        /// Sets whether the orientation selector and its label are visible.
        /// </summary>
        /// <param name="isActive">
        /// True to show the controls; otherwise, false.
        /// </param>
        private static void SetOrientationActive(bool isActive)
        {
            SetUIElementActive(
                controls.OrientationObject,
                controls.OrientationTextObject,
                isActive);
        }

        /// <summary>
        /// Sets the active state of a UI object and its associated label.
        /// </summary>
        /// <param name="uiObject">The UI object.</param>
        /// <param name="labelObject">The associated label.</param>
        /// <param name="isActive">
        /// True to enable both objects; otherwise, false.
        /// </param>
        private static void SetUIElementActive(
            GameObject uiObject,
            GameObject labelObject,
            bool isActive)
        {
            uiObject.SetActive(isActive);
            labelObject.SetActive(isActive);
        }

        #endregion

        #region Shape Layout

        /// <summary>
        /// Updates the visible controls for the selected shape.
        /// </summary>
        /// <exception cref="NotImplementedException">
        /// Thrown if the selected shape has not been integrated into
        /// the menu configuration.
        /// </exception>
        private static void ChangeMenu()
        {
            AllValuesReset();
            AllValuesDisable();

            switch (selectedShape)
            {
                case Shape.Line:
                    controls.FinishObject.SetActive(true);
                    ActivateAndConfigurateValue(
                        controls.BoolValueObject,
                        "Loop");
                    SetLineStartActive(true);
                    SetLineEndActive(true);
                    MoveBoolValueToLinePosition();
                    break;

                case Shape.Square:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "a");
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.Rectangle:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "a");
                    ActivateAndConfigurateValue(
                        controls.Value2Object,
                        "b");
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.Rhombus:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "f");
                    ActivateAndConfigurateValue(
                        controls.Value2Object,
                        "e");
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.Kite:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "f1");
                    ActivateAndConfigurateValue(
                        controls.Value2Object,
                        "f2");
                    ActivateAndConfigurateValue(
                        controls.Value3Object,
                        "e");
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.Triangle:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "c");
                    ActivateAndConfigurateValue(
                        controls.Value2Object,
                        "h");
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.Circle:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "Radius");
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.HalfCircle:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "Radius");
                    SetOrientationActive(true);
                    break;

                case Shape.Ellipse:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "X-Scale");
                    ActivateAndConfigurateValue(
                        controls.Value2Object,
                        "Y-Scale");
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.Parallelogram:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "a");
                    ActivateAndConfigurateValue(
                        controls.Value2Object,
                        "h");
                    ActivateAndConfigurateValue(
                        controls.OffsetObject,
                        "Shift");
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.Trapezoid:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "a");
                    ActivateAndConfigurateValue(
                        controls.Value2Object,
                        "c");
                    ActivateAndConfigurateValue(
                        controls.Value3Object,
                        "h");
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.Polygon:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "Length");
                    controls.VerticesObject.SetActive(true);
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.Arc:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "Radius");
                    ActivateAndConfigurateValue(
                        controls.Angle1Object,
                        "Start Angle");
                    ActivateAndConfigurateValue(
                        controls.Angle2Object,
                        "End Angle",
                        360);
                    ActivateAndConfigurateValue(
                        controls.VerticesObject,
                        "Verticies",
                        PointsCalculator.DefaultVertices);
                    break;

                case Shape.UML:
                    ChangeUMLMenu();
                    break;

                default:
                    throw new NotImplementedException(
                        $"The selected shape {selectedShape} has not been integrated yet.");
            }

            MenuHelper.CalculateHeight(
                controls.MenuObject);
        }

        /// <summary>
        /// Updates the visible controls for the selected UML shape.
        /// </summary>
        /// <exception cref="NotImplementedException">
        /// Thrown if the selected UML shape has not been integrated into
        /// the menu configuration.
        /// </exception>
        private static void ChangeUMLMenu()
        {
            if (selectedShape != Shape.UML)
            {
                return;
            }

            controls.UMLShapeSelectorObject.SetActive(true);

            switch (selectedUMLShape)
            {
                case UMLShape.Actor:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "Length",
                        10);
                    break;

                case UMLShape.Note:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "a",
                        30);
                    ActivateAndConfigurateValue(
                        controls.Value2Object,
                        "b",
                        20);
                    break;

                case UMLShape.Package:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "a",
                        30);
                    ActivateAndConfigurateValue(
                        controls.Value2Object,
                        "b",
                        20);
                    ActivateAndConfigurateValue(
                        controls.Value3Object,
                        "Title-Width",
                        15);
                    ActivateAndConfigurateValue(
                        controls.Value4Object,
                        "Title-Height");
                    break;

                case UMLShape.ProvideInterf:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "Radius",
                        10);
                    ActivateAndConfigurateOrientation(
                        Orientation.Left);
                    break;

                case UMLShape.ReceiveInterf:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "Radius",
                        10);
                    ActivateAndConfigurateOrientation(
                        Orientation.Right);
                    break;

                case UMLShape.SendActivity:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "a",
                        20);
                    ActivateAndConfigurateValue(
                        controls.Value2Object,
                        "b",
                        10);
                    ActivateAndConfigurateOrientation(
                        Orientation.Right);
                    break;

                case UMLShape.ReceiveActivity:
                    ActivateAndConfigurateValue(
                        controls.Value1Object,
                        "a",
                        20);
                    ActivateAndConfigurateValue(
                        controls.Value2Object,
                        "b",
                        10);
                    ActivateAndConfigurateOrientation(
                        Orientation.Left);
                    break;

                default:
                    throw new NotImplementedException(
                        $"The selected UML shape {selectedUMLShape} has not been integrated yet.");
            }
        }

        /// <summary>
        /// Activates a value control and optionally updates its label
        /// and default slider value.
        /// </summary>
        /// <param name="valueObj">The value control to activate.</param>
        /// <param name="identifier">
        /// The optional label displayed for the control.
        /// </param>
        /// <param name="defaultValue">
        /// The optional default slider value.
        /// </param>
        private static void ActivateAndConfigurateValue(
            GameObject valueObj,
            string identifier = null,
            int? defaultValue = null)
        {
            if (valueObj == null)
            {
                return;
            }

            valueObj.SetActive(true);

            if (!string.IsNullOrWhiteSpace(identifier))
            {
                TMP_Text tmpText =
                    valueObj
                        .GetComponentsInChildren<TMP_Text>()
                        .FirstOrDefault();

                if (tmpText != null)
                {
                    tmpText.text = identifier;
                }
            }

            if (defaultValue.HasValue)
            {
                SliderManager sliderManager =
                    valueObj.GetComponentInChildren<SliderManager>();

                if (sliderManager != null)
                {
                    sliderManager.mainSlider.value =
                        defaultValue.Value;
                }
            }
        }

        /// <summary>
        /// Activates the orientation selector and assigns its default orientation.
        /// </summary>
        /// <param name="defaultOrientation">
        /// The orientation to select.
        /// </param>
        private static void ActivateAndConfigurateOrientation(
            Orientation defaultOrientation)
        {
            SetOrientationActive(true);

            orientation = defaultOrientation;

            int index =
                GetOrientations().IndexOf(defaultOrientation);

            controls.OrientationSelector.index = index;
            controls.OrientationSelector.defaultIndex = index;
            controls.OrientationSelector.UpdateUI();
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
            SetLineStartCap(startCapConf);
            SetLineEndCap(endCapConf);
        }

        /// <summary>
        /// Sets the selected start line-cap configuration.
        /// </summary>
        /// <param name="capConf">
        /// The start line-cap configuration.
        /// </param>
        private static void SetLineStartCap(
            LineCapConf capConf)
        {
            lineStartCapConf = capConf != null
                ? capConf.Clone()
                : LineCapConf.CreateNone();

            SetSelectorIndex(
                controls.LineStartSelector,
                GetAllLineCaps().IndexOf(
                    lineStartCapConf.CapKind));
        }

        /// <summary>
        /// Sets the selected end line-cap configuration.
        /// </summary>
        /// <param name="capConf">
        /// The end line-cap configuration.
        /// </param>
        private static void SetLineEndCap(
            LineCapConf capConf)
        {
            lineEndCapConf = capConf != null
                ? capConf.Clone()
                : LineCapConf.CreateNone();

            SetSelectorIndex(
                controls.LineEndSelector,
                GetAllLineCaps().IndexOf(
                    lineEndCapConf.CapKind));
        }

        /// <summary>
        /// Updates the given selector to the requested index.
        /// </summary>
        /// <param name="selector">
        /// The selector to update.
        /// </param>
        /// <param name="index">
        /// The index to select.
        /// </param>
        private static void SetSelectorIndex(
            HorizontalSelector selector,
            int index)
        {
            if (selector == null || index < 0)
            {
                return;
            }

            selector.index = index;
            selector.defaultIndex = index;
            selector.UpdateUI();
        }

        #endregion
    }
}
