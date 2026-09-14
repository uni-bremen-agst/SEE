using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Drawable;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;
using static SEE.Game.Drawable.ActionHelpers.ShapePointsCalculator;
using static SEE.Game.Drawable.ActionHelpers.UMLShapePointsCalculator;

namespace SEE.UI.Menu.Drawable.Shapes
{
    /// <summary>
    /// Configures the visible controls and their default values for the
    /// currently selected shape and UML shape.
    /// </summary>
    internal sealed class ShapeMenuLayout
    {
        /// <summary>
        /// The controls of the shape menu.
        /// </summary>
        private readonly ShapeMenuControls controls;

        /// <summary>
        /// The current shape-menu state.
        /// </summary>
        private readonly ShapeMenuState state;

        /// <summary>
        /// Creates a layout manager for the given shape-menu controls and state.
        /// </summary>
        /// <param name="controls">The controls to configure.</param>
        /// <param name="state">The state determining the current layout.</param>
        internal ShapeMenuLayout(
            ShapeMenuControls controls,
            ShapeMenuState state)
        {
            this.controls = controls;
            this.state = state;
        }

        /// <summary>
        /// Updates the shape-menu layout for the currently selected shape.
        /// </summary>
        /// <exception cref="NotImplementedException">
        /// Thrown if the selected shape has not yet been integrated into
        /// the menu configuration.
        /// </exception>
        internal void UpdateForSelection()
        {
            ResetAllValues();
            DisableAllValues();

            switch (state.SelectedShape)
            {
                case Shape.Line:
                    controls.FinishObject.SetActive(true);
                    ActivateAndConfigureValue(
                        controls.BoolValueObject,
                        "Loop");
                    SetLineStartActive(true);
                    SetLineEndActive(true);
                    MoveBoolValueToLinePosition();
                    break;

                case Shape.Square:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "a");
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.Rectangle:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "a");
                    ActivateAndConfigureValue(
                        controls.Value2Object,
                        "b");
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.Rhombus:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "f");
                    ActivateAndConfigureValue(
                        controls.Value2Object,
                        "e");
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.Kite:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "f1");
                    ActivateAndConfigureValue(
                        controls.Value2Object,
                        "f2");
                    ActivateAndConfigureValue(
                        controls.Value3Object,
                        "e");
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.Triangle:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "c");
                    ActivateAndConfigureValue(
                        controls.Value2Object,
                        "h");
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.Circle:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "Radius");
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.HalfCircle:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "Radius");
                    SetOrientationActive(true);
                    break;

                case Shape.Ellipse:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "X-Scale");
                    ActivateAndConfigureValue(
                        controls.Value2Object,
                        "Y-Scale");
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.Parallelogram:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "a");
                    ActivateAndConfigureValue(
                        controls.Value2Object,
                        "h");
                    ActivateAndConfigureValue(
                        controls.OffsetObject,
                        "Shift");
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.Trapezoid:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "a");
                    ActivateAndConfigureValue(
                        controls.Value2Object,
                        "c");
                    ActivateAndConfigureValue(
                        controls.Value3Object,
                        "h");
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.Polygon:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "Length");
                    controls.VerticesObject.SetActive(true);
                    controls.InfoObject.SetActive(true);
                    break;

                case Shape.Arc:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "Radius");
                    ActivateAndConfigureValue(
                        controls.Angle1Object,
                        "Start Angle");
                    ActivateAndConfigureValue(
                        controls.Angle2Object,
                        "End Angle",
                        360);
                    ActivateAndConfigureValue(
                        controls.VerticesObject,
                        "Verticies",
                        PointsCalculator.DefaultVertices);
                    break;

                case Shape.UML:
                    ConfigureUMLLayout();
                    break;

                default:
                    throw new NotImplementedException(
                        $"The selected shape {state.SelectedShape} has not been integrated yet.");
            }

            MenuHelper.CalculateHeight(controls.MenuObject);
        }

        /// <summary>
        /// Configures the visible controls for the currently selected UML shape.
        /// </summary>
        /// <exception cref="NotImplementedException">
        /// Thrown if the selected UML shape has not yet been integrated into
        /// the menu configuration.
        /// </exception>
        private void ConfigureUMLLayout()
        {
            if (state.SelectedShape != Shape.UML)
            {
                return;
            }

            controls.UMLShapeSelectorObject.SetActive(true);

            switch (state.SelectedUMLShape)
            {
                case UMLShape.Actor:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "Length",
                        10);
                    break;

                case UMLShape.Note:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "a",
                        30);
                    ActivateAndConfigureValue(
                        controls.Value2Object,
                        "b",
                        20);
                    break;

                case UMLShape.Package:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "a",
                        30);
                    ActivateAndConfigureValue(
                        controls.Value2Object,
                        "b",
                        20);
                    ActivateAndConfigureValue(
                        controls.Value3Object,
                        "Title-Width",
                        15);
                    ActivateAndConfigureValue(
                        controls.Value4Object,
                        "Title-Height");
                    break;

                case UMLShape.ProvideInterf:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "Radius",
                        10);
                    ActivateAndConfigureOrientation(
                        Orientation.Left);
                    break;

                case UMLShape.ReceiveInterf:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "Radius",
                        10);
                    ActivateAndConfigureOrientation(
                        Orientation.Right);
                    break;

                case UMLShape.SendActivity:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "a",
                        20);
                    ActivateAndConfigureValue(
                        controls.Value2Object,
                        "b",
                        10);
                    ActivateAndConfigureOrientation(
                        Orientation.Right);
                    break;

                case UMLShape.ReceiveActivity:
                    ActivateAndConfigureValue(
                        controls.Value1Object,
                        "a",
                        20);
                    ActivateAndConfigureValue(
                        controls.Value2Object,
                        "b",
                        10);
                    ActivateAndConfigureOrientation(
                        Orientation.Left);
                    break;

                default:
                    throw new NotImplementedException(
                        $"The selected UML shape {state.SelectedUMLShape} has not been integrated yet.");
            }
        }

        /// <summary>
        /// Resets all shape controls and their configuration to their defaults.
        /// </summary>
        private void ResetAllValues()
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

            SetLineCaps(
                LineCapConf.CreateNone(),
                LineCapConf.CreateNone());

            state.Orientation = Orientation.Up;
        }

        /// <summary>
        /// Disables all shape-specific controls.
        /// </summary>
        private void DisableAllValues()
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
        /// Restores the boolean option to its original menu position.
        /// </summary>
        private void RestoreBoolValuePosition()
        {
            controls.BoolValueObject.transform.SetSiblingIndex(
                controls.BoolValueDefaultSiblingIndex);
        }

        /// <summary>
        /// Moves the boolean option directly before the finish button.
        /// </summary>
        private void MoveBoolValueToLinePosition()
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
        /// Activates a value control and optionally configures its label
        /// and default value.
        /// </summary>
        /// <param name="valueObject">The value control to activate.</param>
        /// <param name="identifier">
        /// The optional label displayed for the control.
        /// </param>
        /// <param name="defaultValue">
        /// The optional default slider value.
        /// </param>
        private static void ActivateAndConfigureValue(
            GameObject valueObject,
            string identifier = null,
            int? defaultValue = null)
        {
            if (valueObject == null)
            {
                return;
            }

            valueObject.SetActive(true);

            if (!string.IsNullOrWhiteSpace(identifier))
            {
                TMP_Text tmpText =
                    valueObject
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
                    valueObject.GetComponentInChildren<SliderManager>();

                if (sliderManager != null)
                {
                    sliderManager.mainSlider.value =
                        defaultValue.Value;
                }
            }
        }

        /// <summary>
        /// Activates the orientation selector and selects the given default.
        /// </summary>
        /// <param name="defaultOrientation">
        /// The default orientation.
        /// </param>
        private void ActivateAndConfigureOrientation(
            Orientation defaultOrientation)
        {
            SetOrientationActive(true);

            state.Orientation = defaultOrientation;

            int index =
                GetOrientations().IndexOf(defaultOrientation);

            controls.OrientationSelector.index = index;
            controls.OrientationSelector.defaultIndex = index;
            controls.OrientationSelector.UpdateUI();
        }

        /// <summary>
        /// Sets whether the start line-cap controls are visible.
        /// </summary>
        /// <param name="isActive">
        /// True to show them; otherwise, false.
        /// </param>
        private void SetLineStartActive(bool isActive)
        {
            SetUIElementActive(
                controls.LineStartObject,
                controls.LineStartTextObject,
                isActive);
        }

        /// <summary>
        /// Sets whether the end line-cap controls are visible.
        /// </summary>
        /// <param name="isActive">
        /// True to show them; otherwise, false.
        /// </param>
        private void SetLineEndActive(bool isActive)
        {
            SetUIElementActive(
                controls.LineEndObject,
                controls.LineEndTextObject,
                isActive);
        }

        /// <summary>
        /// Sets whether the orientation controls are visible.
        /// </summary>
        /// <param name="isActive">
        /// True to show them; otherwise, false.
        /// </param>
        private void SetOrientationActive(bool isActive)
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

        /// <summary>
        /// Sets the selected start and end line-cap configurations.
        /// </summary>
        /// <param name="startCapConf">
        /// The start line-cap configuration.
        /// </param>
        /// <param name="endCapConf">
        /// The end line-cap configuration.
        /// </param>
        internal void SetLineCaps(
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
        internal void SetLineStartCap(LineCapConf capConf)
        {
            state.SetLineStartCap(capConf);

            SetSelectorIndex(
                controls.LineStartSelector,
                GetAllLineCaps().IndexOf(state.GetLineStartCap()));
        }

        /// <summary>
        /// Sets the selected end line-cap configuration.
        /// </summary>
        /// <param name="capConf">
        /// The end line-cap configuration.
        /// </param>
        internal void SetLineEndCap(LineCapConf capConf)
        {
            state.SetLineEndCap(capConf);

            SetSelectorIndex(
                controls.LineEndSelector,
                GetAllLineCaps().IndexOf(state.GetLineEndCap()));
        }

        /// <summary>
        /// Resets a selector to its first entry.
        /// </summary>
        /// <param name="selector">The selector to reset.</param>
        private static void ResetSelector(HorizontalSelector selector)
        {
            selector.index = 0;
            selector.defaultIndex = 0;
            selector.UpdateUI();
        }

        /// <summary>
        /// Updates the given selector to the requested index.
        /// </summary>
        /// <param name="selector">The selector to update.</param>
        /// <param name="index">The index to select.</param>
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
    }
}
