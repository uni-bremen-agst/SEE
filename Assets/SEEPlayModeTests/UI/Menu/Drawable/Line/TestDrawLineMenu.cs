using NUnit.Framework;
using SEE.Game.Drawable;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using static SEE.Game.Drawable.GameDrawer;

namespace SEE.UI.Menu.Drawable.Line
{
    /// <summary>
    /// Play-mode integration tests for the drawing configuration of the
    /// <see cref="LineMenu"/>.
    /// The tests use the real line-menu prefab and verify that its controls
    /// update the drawing configuration as expected.
    /// </summary>
    /// <remarks>
    /// These tests are currently blocked by issue #983 because the play-mode
    /// test environment does not provide the SEE scene objects required by
    /// drawable menus.
    /// </remarks>
    [Ignore("Blocked by #983: required SEE UI scene objects are unavailable in play-mode tests.")]
    internal class TestDrawLineMenu : TestUI
    {
        /// <summary>
        /// The controls of the instantiated line menu.
        /// </summary>
        private LineMenuControls controls;

        /// <summary>
        /// Prepares the line menu for every test.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnitySetUp]
        public IEnumerator SetUpLineMenu()
        {
            ResetDrawingValues();

            LineMenu.Instance.Disable();

            yield return null;
        }

        /// <summary>
        /// Disables the line menu after every test.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTearDown]
        public IEnumerator TearDownLineMenu()
        {
            LineMenu.Instance.Disable();

            yield return null;
        }

        /// <summary>
        /// Verifies that reopening the drawing menu restores the primary color as the
        /// color-picker target instead of retaining a previously selected secondary
        /// color target.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestReopeningMenuResetsColorPickerTarget()
        {
            ValueHolder.CurrentPrimaryColor = Color.red;
            ValueHolder.CurrentSecondaryColor = Color.blue;

            yield return OpenDrawingMenu();

            controls.SecondaryColorButtonManager.clickEvent.Invoke();

            yield return null;

            Color secondaryColor = Color.green;
            controls.ColorPicker.onValueChanged.Invoke(secondaryColor);

            yield return null;

            Assert.That(
                ValueHolder.CurrentSecondaryColor,
                Is.EqualTo(secondaryColor));

            LineMenu.Instance.Disable();

            yield return null;

            yield return OpenDrawingMenu();

            Color primaryColor = Color.yellow;
            controls.ColorPicker.onValueChanged.Invoke(primaryColor);

            yield return null;

            Assert.That(
                ValueHolder.CurrentPrimaryColor,
                Is.EqualTo(primaryColor));

            Assert.That(
                ValueHolder.CurrentSecondaryColor,
                Is.EqualTo(secondaryColor));
        }

        /// <summary>
        /// Verifies that drawing mode displays only the controls applicable
        /// to drawing new lines.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestDrawingMenuUsesExpectedVisibility()
        {
            yield return OpenDrawingMenu();

            Assert.That(controls.LineKindSelectionObject.activeInHierarchy, Is.True);
            Assert.That(controls.ThicknessObject.activeInHierarchy, Is.True);

            Assert.That(controls.LayerObject.activeInHierarchy, Is.False);
            Assert.That(controls.LoopObject.activeInHierarchy, Is.False);
            Assert.That(controls.ReturnButtonObject.activeInHierarchy, Is.False);

            Assert.That(
                controls.TilingObject.activeInHierarchy,
                Is.False,
                "Tiling must be hidden for a solid line.");
        }

        /// <summary>
        /// Verifies that changing the line kind updates the drawing state,
        /// displays the tiling control for dashed lines, and prevents the
        /// unsupported combination of a solid line with two-dashed colors.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestLineKindSelectionUpdatesDrawingConfiguration()
        {
            yield return OpenDrawingMenu();

            int dashedIndex = GetLineKinds().IndexOf(LineKind.Dashed);
            int solidIndex = GetLineKinds().IndexOf(LineKind.Solid);
            int twoDashedIndex = GetColorKinds(true).IndexOf(ColorKind.TwoDashed);

            Assert.That(dashedIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(solidIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(twoDashedIndex, Is.GreaterThanOrEqualTo(0));

            controls.LineKindSelector.selectorEvent.Invoke(dashedIndex);

            yield return null;

            Assert.That(ValueHolder.CurrentLineKind, Is.EqualTo(LineKind.Dashed));
            Assert.That(controls.TilingObject.activeInHierarchy, Is.True);

            controls.ColorKindSelector.selectorEvent.Invoke(twoDashedIndex);

            yield return null;

            Assert.That(ValueHolder.CurrentColorKind, Is.EqualTo(ColorKind.TwoDashed));

            controls.LineKindSelector.selectorEvent.Invoke(solidIndex);

            yield return null;

            Assert.That(ValueHolder.CurrentLineKind, Is.EqualTo(LineKind.Solid));
            Assert.That(ValueHolder.CurrentColorKind, Is.EqualTo(ColorKind.Monochrome));
            Assert.That(controls.TilingObject.activeInHierarchy, Is.False);
        }

        /// <summary>
        /// Verifies that selecting a non-monochrome color kind updates the
        /// drawing configuration and initializes a missing secondary color.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestColorKindSelectionUpdatesDrawingConfiguration()
        {
            ValueHolder.CurrentPrimaryColor = Color.red;
            ValueHolder.CurrentSecondaryColor = Color.clear;

            yield return OpenDrawingMenu();

            int gradientIndex = GetColorKinds(true).IndexOf(ColorKind.Gradient);

            Assert.That(gradientIndex, Is.GreaterThanOrEqualTo(0));

            controls.ColorKindSelector.selectorEvent.Invoke(gradientIndex);

            yield return null;

            Assert.That(ValueHolder.CurrentColorKind, Is.EqualTo(ColorKind.Gradient));
            Assert.That(ValueHolder.CurrentSecondaryColor, Is.EqualTo(Color.red));
            Assert.That(controls.ColorAreaSelectorObject.activeInHierarchy, Is.True);
        }

        /// <summary>
        /// Verifies that the primary and secondary color buttons select the
        /// correct target for subsequent color-picker changes.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestColorButtonsRouteColorPickerChanges()
        {
            ValueHolder.CurrentPrimaryColor = Color.red;
            ValueHolder.CurrentSecondaryColor = Color.blue;

            yield return OpenDrawingMenu();

            Color firstPrimaryColor = Color.green;

            controls.ColorPicker.onValueChanged.Invoke(firstPrimaryColor);

            yield return null;

            Assert.That(ValueHolder.CurrentPrimaryColor, Is.EqualTo(firstPrimaryColor));
            Assert.That(ValueHolder.CurrentSecondaryColor, Is.EqualTo(Color.blue));

            controls.SecondaryColorButtonManager.clickEvent.Invoke();

            yield return null;

            Assert.That(
                controls.PrimaryColorButtonManager.buttonVar.interactable,
                Is.True);
            Assert.That(
                controls.SecondaryColorButtonManager.buttonVar.interactable,
                Is.False);

            Color newSecondaryColor = Color.yellow;

            controls.ColorPicker.onValueChanged.Invoke(newSecondaryColor);

            yield return null;

            Assert.That(ValueHolder.CurrentPrimaryColor, Is.EqualTo(firstPrimaryColor));
            Assert.That(ValueHolder.CurrentSecondaryColor, Is.EqualTo(newSecondaryColor));

            controls.PrimaryColorButtonManager.clickEvent.Invoke();

            yield return null;

            Assert.That(
                controls.PrimaryColorButtonManager.buttonVar.interactable,
                Is.False);
            Assert.That(
                controls.SecondaryColorButtonManager.buttonVar.interactable,
                Is.True);

            Color secondPrimaryColor = Color.cyan;

            controls.ColorPicker.onValueChanged.Invoke(secondPrimaryColor);

            yield return null;

            Assert.That(ValueHolder.CurrentPrimaryColor, Is.EqualTo(secondPrimaryColor));
            Assert.That(ValueHolder.CurrentSecondaryColor, Is.EqualTo(newSecondaryColor));
        }

        /// <summary>
        /// Verifies that selecting the fill-out area routes color changes to
        /// the tertiary color and that the fill-out switch controls the
        /// drawing fill-out state.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestFillOutControlsUpdateDrawingConfiguration()
        {
            ValueHolder.CurrentPrimaryColor = Color.red;
            ValueHolder.CurrentTertiaryColor = Color.clear;
            ValueHolder.CurrentFillOutStatus = false;

            yield return OpenDrawingMenu();

            controls.FillOutButtonManager.clickEvent.Invoke();

            yield return null;

            Assert.That(controls.FillOutObject.activeInHierarchy, Is.True);
            Assert.That(controls.ColorKindSelectionObject.activeInHierarchy, Is.False);
            Assert.That(ValueHolder.CurrentTertiaryColor, Is.EqualTo(Color.red));

            Color fillOutColor = Color.green;

            controls.ColorPicker.onValueChanged.Invoke(fillOutColor);

            yield return null;

            Assert.That(ValueHolder.CurrentTertiaryColor, Is.EqualTo(fillOutColor));

            controls.FillOutManager.isOn = true;
            controls.FillOutManager.OnEvents.Invoke();

            yield return null;

            Assert.That(ValueHolder.CurrentFillOutStatus, Is.True);
            Assert.That(
                LineMenu.GetFillOutColorForDrawing(),
                Is.EqualTo(fillOutColor));

            controls.FillOutManager.isOn = false;
            controls.FillOutManager.OffEvents.Invoke();

            yield return null;

            Assert.That(ValueHolder.CurrentFillOutStatus, Is.False);
            Assert.That(LineMenu.GetFillOutColorForDrawing(), Is.Null);
        }

        /// <summary>
        /// Verifies that the thickness and tiling controls update the current
        /// values used for newly drawn lines.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestSlidersUpdateDrawingConfiguration()
        {
            yield return OpenDrawingMenu();

            float thickness = 0.125f;
            float tiling = 4.5f;

            controls.ThicknessSlider.OnValueChanged.Invoke(thickness);
            controls.TilingSlider.onValueChanged.Invoke(tiling);

            yield return null;

            Assert.That(ValueHolder.CurrentThickness, Is.EqualTo(thickness));
            Assert.That(ValueHolder.CurrentTiling, Is.EqualTo(tiling));
        }

        /// <summary>
        /// Verifies that reopening the drawing menu restores its selectors,
        /// switches, and visibility from the current drawing values.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestReopeningMenuRestoresDrawingConfiguration()
        {
            ValueHolder.CurrentLineKind = LineKind.Dashed;
            ValueHolder.CurrentColorKind = ColorKind.Gradient;
            ValueHolder.CurrentPrimaryColor = Color.red;
            ValueHolder.CurrentSecondaryColor = Color.blue;
            ValueHolder.CurrentTertiaryColor = Color.green;
            ValueHolder.CurrentThickness = 0.2f;
            ValueHolder.CurrentTiling = 3.5f;
            ValueHolder.CurrentFillOutStatus = true;

            yield return OpenDrawingMenu();

            int dashedIndex = GetLineKinds().IndexOf(LineKind.Dashed);
            int gradientIndex = GetColorKinds(true).IndexOf(ColorKind.Gradient);

            Assert.That(controls.LineKindSelector.index, Is.EqualTo(dashedIndex));
            Assert.That(controls.ColorKindSelector.index, Is.EqualTo(gradientIndex));
            Assert.That(controls.TilingObject.activeInHierarchy, Is.True);
            Assert.That(controls.FillOutManager.isOn, Is.True);

            LineMenu.Instance.Disable();

            yield return null;

            Assert.That(LineMenu.Instance.IsOpen(), Is.False);
            Assert.That(LineMenu.Instance.IsInDrawingMode(), Is.False);

            yield return OpenDrawingMenu();

            Assert.That(controls.LineKindSelector.index, Is.EqualTo(dashedIndex));
            Assert.That(controls.ColorKindSelector.index, Is.EqualTo(gradientIndex));
            Assert.That(controls.TilingObject.activeInHierarchy, Is.True);
            Assert.That(controls.FillOutManager.isOn, Is.True);

            Assert.That(ValueHolder.CurrentLineKind, Is.EqualTo(LineKind.Dashed));
            Assert.That(ValueHolder.CurrentColorKind, Is.EqualTo(ColorKind.Gradient));
            Assert.That(ValueHolder.CurrentPrimaryColor, Is.EqualTo(Color.red));
            Assert.That(ValueHolder.CurrentSecondaryColor, Is.EqualTo(Color.blue));
            Assert.That(ValueHolder.CurrentTertiaryColor, Is.EqualTo(Color.green));
            Assert.That(ValueHolder.CurrentThickness, Is.EqualTo(0.2f));
            Assert.That(ValueHolder.CurrentTiling, Is.EqualTo(3.5f));
            Assert.That(ValueHolder.CurrentFillOutStatus, Is.True);
        }

        /// <summary>
        /// Opens the real line menu for drawing and resolves its controls.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        private IEnumerator OpenDrawingMenu()
        {
            LineMenu.Instance.EnableForDrawing();

            yield return null;

            controls = new LineMenuControls(LineMenu.Instance.GameObject);

            Assert.That(LineMenu.Instance.GameObject, Is.Not.Null);
        }

        /// <summary>
        /// Resets all drawing values relevant to the line menu to a deterministic
        /// configuration.
        /// </summary>
        private static void ResetDrawingValues()
        {
            ValueHolder.CurrentPrimaryColor = Color.red;
            ValueHolder.CurrentSecondaryColor = Color.clear;
            ValueHolder.CurrentTertiaryColor = Color.clear;

            ValueHolder.CurrentThickness = ValueHolder.StandardLineThickness;
            ValueHolder.CurrentTiling = ValueHolder.StandardLineTiling;

            ValueHolder.CurrentLineKind = LineKind.Solid;
            ValueHolder.CurrentColorKind = ColorKind.Monochrome;
            ValueHolder.CurrentFillOutStatus = false;
        }
    }
}
