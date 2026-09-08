using NUnit.Framework;
using SEE.Game.Drawable.Configurations;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;
using static SEE.Game.Drawable.GameDrawer;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Tests the temporary state used while editing line-cap configurations.
    /// </summary>
    [TestFixture]
    public class TestLineCapEditState
    {
        /// <summary>
        /// Verifies that the visual properties of the remembered start cap are
        /// restored when another start-cap configuration is initialized.
        /// </summary>
        [Test]
        public void TestInitializeCapConfUsesRememberedStartCap()
        {
            LineCapConf startCap = CreateCap(
                LineCap.Arrow,
                ColorKind.Gradient,
                Color.red,
                Color.blue,
                0.25f,
                LineKind.Dashed,
                3.0f,
                true,
                Color.green);

            LineConf line = CreateLine();
            line.LineCapStart = startCap;
            line.LineCapEnd = LineCapConf.CreateNone();

            LineCapEditState state = new();
            state.Initialize(line);

            LineCapConf target = CreateCap(
                LineCap.Aggregation,
                ColorKind.Monochrome,
                Color.black,
                Color.black,
                1.0f,
                LineKind.Solid,
                1.0f,
                false,
                Color.clear);

            state.InitializeCapConf(line, target, true);

            AssertVisualPropertiesEqual(startCap, target);
            Assert.That(target.CapKind, Is.EqualTo(LineCap.Aggregation));
        }

        /// <summary>
        /// Verifies that remembered line-cap values are independent of later
        /// modifications to the original configuration.
        /// </summary>
        [Test]
        public void TestInitializeStoresIndependentCapConfiguration()
        {
            LineCapConf startCap = CreateCap(
                LineCap.Arrow,
                ColorKind.Gradient,
                Color.red,
                Color.blue,
                0.25f,
                LineKind.Dashed,
                3.0f,
                true,
                Color.green);

            LineConf line = CreateLine();
            line.LineCapStart = startCap;
            line.LineCapEnd = LineCapConf.CreateNone();

            LineCapEditState state = new();
            state.Initialize(line);

            startCap.PrimaryColor = Color.yellow;
            startCap.SecondaryColor = Color.magenta;
            startCap.Thickness = 2.0f;
            startCap.Tiling = 8.0f;
            startCap.FillOutStatus = false;
            startCap.FillOutColor = Color.clear;

            LineCapConf target = LineCapConf.CreateNone();
            target.CapKind = LineCap.Arrowhead;

            state.InitializeCapConf(line, target, true);

            Assert.That(target.PrimaryColor, Is.EqualTo(Color.red));
            Assert.That(target.SecondaryColor, Is.EqualTo(Color.blue));
            Assert.That(target.Thickness, Is.EqualTo(0.25f));
            Assert.That(target.Tiling, Is.EqualTo(3.0f));
            Assert.That(target.FillOutStatus, Is.True);
            Assert.That(target.FillOutColor, Is.EqualTo(Color.green));
        }

        /// <summary>
        /// Verifies that the main-line visual properties are used when no
        /// non-none configuration has been remembered for the selected cap.
        /// </summary>
        [Test]
        public void TestInitializeCapConfUsesMainLineWithoutRememberedCap()
        {
            LineConf line = CreateLine();
            line.LineCapStart = LineCapConf.CreateNone();
            line.LineCapEnd = LineCapConf.CreateNone();

            LineCapEditState state = new();
            state.Initialize(line);

            LineCapConf target = CreateCap(
                LineCap.Arrowhead,
                ColorKind.Monochrome,
                Color.white,
                Color.white,
                5.0f,
                LineKind.Dashed,
                9.0f,
                false,
                Color.clear);

            state.InitializeCapConf(line, target, true);

            AssertVisualPropertiesEqual(line, target);
            Assert.That(target.CapKind, Is.EqualTo(LineCap.Arrowhead));
        }

        /// <summary>
        /// Verifies that remembered start-cap and end-cap configurations are
        /// stored and restored independently.
        /// </summary>
        [Test]
        public void TestStartAndEndCapStatesAreIndependent()
        {
            LineConf line = CreateLine();

            line.LineCapStart = CreateCap(
                LineCap.Arrow,
                ColorKind.Monochrome,
                Color.red,
                Color.clear,
                0.2f,
                LineKind.Solid,
                1.0f,
                false,
                Color.clear);

            line.LineCapEnd = CreateCap(
                LineCap.Aggregation,
                ColorKind.Gradient,
                Color.blue,
                Color.green,
                0.8f,
                LineKind.Dashed,
                4.0f,
                true,
                Color.yellow);

            LineCapEditState state = new();
            state.Initialize(line);

            LineCapConf startTarget = LineCapConf.CreateNone();
            startTarget.CapKind = LineCap.Arrowhead;

            LineCapConf endTarget = LineCapConf.CreateNone();
            endTarget.CapKind = LineCap.Composition;

            state.InitializeCapConf(line, startTarget, true);
            state.InitializeCapConf(line, endTarget, false);

            AssertVisualPropertiesEqual(line.LineCapStart, startTarget);
            AssertVisualPropertiesEqual(line.LineCapEnd, endTarget);
        }

        /// <summary>
        /// Verifies that remembering a none cap does not overwrite the last
        /// remembered non-none configuration.
        /// </summary>
        [Test]
        public void TestRememberPreviousCapConfIgnoresNone()
        {
            LineConf line = CreateLine();

            line.LineCapStart = CreateCap(
                LineCap.Arrow,
                ColorKind.Gradient,
                Color.red,
                Color.blue,
                0.4f,
                LineKind.Dashed,
                2.0f,
                true,
                Color.green);

            line.LineCapEnd = LineCapConf.CreateNone();

            LineCapEditState state = new();
            state.Initialize(line);

            state.RememberPreviousCapConf(LineCapConf.CreateNone(), true);

            LineCapConf target = LineCapConf.CreateNone();
            target.CapKind = LineCap.Aggregation;

            state.InitializeCapConf(line, target, true);

            AssertVisualPropertiesEqual(line.LineCapStart, target);
        }

        /// <summary>
        /// Verifies that the remembered fill-out configuration is restored
        /// when the user did not explicitly modify it.
        /// </summary>
        [Test]
        public void TestRestoreRememberedFillOutWhenUnchanged()
        {
            LineConf line = CreateLine();

            line.LineCapStart = CreateCap(
                LineCap.Arrow,
                ColorKind.Monochrome,
                Color.red,
                Color.clear,
                0.3f,
                LineKind.Solid,
                1.0f,
                true,
                Color.green);

            line.LineCapEnd = LineCapConf.CreateNone();

            LineCapEditState state = new();
            state.Initialize(line);

            LineCapConf currentCap = CreateCap(
                LineCap.Arrowhead,
                ColorKind.Monochrome,
                Color.red,
                Color.clear,
                0.3f,
                LineKind.Solid,
                1.0f,
                false,
                Color.clear);

            bool restored =
                state.RestoreRememberedFillOutIfNotChangedByUser(currentCap, true);

            Assert.That(restored, Is.True);
            Assert.That(currentCap.FillOutStatus, Is.True);
            Assert.That(currentCap.FillOutColor, Is.EqualTo(Color.green));
        }

        /// <summary>
        /// Verifies that the remembered fill-out configuration is not restored
        /// after the user explicitly changed the current fill-out configuration.
        /// </summary>
        [Test]
        public void TestDoesNotRestoreFillOutChangedByUser()
        {
            LineConf line = CreateLine();

            line.LineCapStart = CreateCap(
                LineCap.Arrow,
                ColorKind.Monochrome,
                Color.red,
                Color.clear,
                0.3f,
                LineKind.Solid,
                1.0f,
                false,
                Color.clear);

            line.LineCapEnd = LineCapConf.CreateNone();

            LineCapEditState state = new();
            state.Initialize(line);

            LineCapConf currentCap = CreateCap(
                LineCap.Arrowhead,
                ColorKind.Monochrome,
                Color.red,
                Color.clear,
                0.3f,
                LineKind.Solid,
                1.0f,
                true,
                Color.yellow);

            state.UpdateFillOutChangedByUser(currentCap, true);

            bool restored =
                state.RestoreRememberedFillOutIfNotChangedByUser(currentCap, true);

            Assert.That(restored, Is.False);
            Assert.That(currentCap.FillOutStatus, Is.True);
            Assert.That(currentCap.FillOutColor, Is.EqualTo(Color.yellow));
        }

        /// <summary>
        /// Verifies that a line cap with its own fill-out default keeps that
        /// default instead of receiving the remembered fill-out configuration.
        /// </summary>
        [Test]
        public void TestCapWithOwnFillOutDefaultIsNotOverwritten()
        {
            LineConf line = CreateLine();

            line.LineCapStart = CreateCap(
                LineCap.Arrow,
                ColorKind.Monochrome,
                Color.red,
                Color.clear,
                0.3f,
                LineKind.Solid,
                1.0f,
                false,
                Color.clear);

            line.LineCapEnd = LineCapConf.CreateNone();

            LineCapEditState state = new();
            state.Initialize(line);

            LineCapConf composition = CreateCap(
                LineCap.Composition,
                ColorKind.Monochrome,
                Color.blue,
                Color.clear,
                0.3f,
                LineKind.Solid,
                1.0f,
                true,
                Color.blue);

            bool restored =
                state.RestoreRememberedFillOutIfNotChangedByUser(composition, true);

            Assert.That(restored, Is.False);
            Assert.That(composition.FillOutStatus, Is.True);
            Assert.That(composition.FillOutColor, Is.EqualTo(Color.blue));
        }

        /// <summary>
        /// Verifies that initializing a new edit operation resets information
        /// about fill-out changes made during the previous edit operation.
        /// </summary>
        [Test]
        public void TestInitializeResetsFillOutChangedState()
        {
            LineConf line = CreateLine();

            line.LineCapStart = CreateCap(
                LineCap.Arrow,
                ColorKind.Monochrome,
                Color.red,
                Color.clear,
                0.3f,
                LineKind.Solid,
                1.0f,
                true,
                Color.green);

            line.LineCapEnd = LineCapConf.CreateNone();

            LineCapEditState state = new();
            state.Initialize(line);

            LineCapConf changedCap = CreateCap(
                LineCap.Arrowhead,
                ColorKind.Monochrome,
                Color.red,
                Color.clear,
                0.3f,
                LineKind.Solid,
                1.0f,
                false,
                Color.yellow);

            state.UpdateFillOutChangedByUser(changedCap, true);

            state.Initialize(line);

            LineCapConf currentCap = CreateCap(
                LineCap.Arrowhead,
                ColorKind.Monochrome,
                Color.red,
                Color.clear,
                0.3f,
                LineKind.Solid,
                1.0f,
                false,
                Color.clear);

            bool restored =
                state.RestoreRememberedFillOutIfNotChangedByUser(currentCap, true);

            Assert.That(restored, Is.True);
            Assert.That(currentCap.FillOutStatus, Is.True);
            Assert.That(currentCap.FillOutColor, Is.EqualTo(Color.green));
        }

        /// <summary>
        /// Verifies that the fill-out default of a cap with its own default is not
        /// inherited by a cap that does not define its own fill-out default.
        /// The fill-out color is retained so that it can be reused when switching
        /// back to the original cap kind.
        /// </summary>
        [Test]
        public void TestOwnFillOutDefaultIsNotInheritedByOtherCap()
        {
            LineConf line = CreateLine();

            line.LineCapStart = CreateCap(
                LineCap.Composition,
                ColorKind.Monochrome,
                Color.blue,
                Color.clear,
                0.3f,
                LineKind.Solid,
                1.0f,
                true,
                Color.red);

            line.LineCapEnd = LineCapConf.CreateNone();

            LineCapEditState state = new();
            state.Initialize(line);

            LineCapConf aggregation = CreateCap(
                LineCap.Aggregation,
                ColorKind.Monochrome,
                Color.blue,
                Color.clear,
                0.3f,
                LineKind.Solid,
                1.0f,
                true,
                Color.red);

            bool restored =
                state.RestoreRememberedFillOutIfNotChangedByUser(aggregation, true);

            Assert.That(restored, Is.True);
            Assert.That(aggregation.FillOutStatus, Is.False);
            Assert.That(aggregation.FillOutColor, Is.EqualTo(Color.red));
        }

        /// <summary>
        /// Verifies that an explicitly changed fill-out color of a cap with its own
        /// fill-out default is restored when returning to that cap kind.
        /// </summary>
        [Test]
        public void TestOwnFillOutDefaultStateIsRestoredWhenReturningToCap()
        {
            LineConf line = CreateLine();

            LineCapConf composition = CreateCap(
                LineCap.Composition,
                ColorKind.Monochrome,
                Color.blue,
                Color.clear,
                0.3f,
                LineKind.Solid,
                1.0f,
                true,
                Color.blue);

            line.LineCapStart = composition;
            line.LineCapEnd = LineCapConf.CreateNone();

            LineCapEditState state = new();
            state.Initialize(line);

            composition.FillOutColor = Color.red;
            state.UpdateFillOutChangedByUser(composition, true);
            state.RememberPreviousCapConf(composition, true);

            LineCapConf aggregation = CreateCap(
                LineCap.Aggregation,
                ColorKind.Monochrome,
                Color.blue,
                Color.clear,
                0.3f,
                LineKind.Solid,
                1.0f,
                true,
                Color.red);

            bool aggregationAdjusted =
                state.RestoreRememberedFillOutIfNotChangedByUser(aggregation, true);

            Assert.That(aggregationAdjusted, Is.True);
            Assert.That(aggregation.FillOutStatus, Is.False);

            state.RememberPreviousCapConf(aggregation, true);

            LineCapConf returnedComposition = CreateCap(
                LineCap.Composition,
                ColorKind.Monochrome,
                Color.blue,
                Color.clear,
                0.3f,
                LineKind.Solid,
                1.0f,
                true,
                Color.blue);

            bool compositionRestored =
                state.RestoreRememberedFillOutIfNotChangedByUser(returnedComposition, true);

            Assert.That(compositionRestored, Is.True);
            Assert.That(returnedComposition.FillOutStatus, Is.True);
            Assert.That(returnedComposition.FillOutColor, Is.EqualTo(Color.red));
        }

        /// <summary>
        /// Verifies that explicitly disabling fill-out for a cap with its own
        /// fill-out default remains effective after switching away and back.
        /// </summary>
        [Test]
        public void TestDisabledOwnFillOutDefaultIsRestoredWhenReturningToCap()
        {
            LineConf line = CreateLine();

            LineCapConf composition = CreateCap(
                LineCap.Composition,
                ColorKind.Monochrome,
                Color.blue,
                Color.clear,
                0.3f,
                LineKind.Solid,
                1.0f,
                true,
                Color.red);

            line.LineCapStart = composition;
            line.LineCapEnd = LineCapConf.CreateNone();

            LineCapEditState state = new();
            state.Initialize(line);

            composition.FillOutStatus = false;
            state.UpdateFillOutChangedByUser(composition, true);
            state.RememberPreviousCapConf(composition, true);

            LineCapConf aggregation = CreateCap(
                LineCap.Aggregation,
                ColorKind.Monochrome,
                Color.blue,
                Color.clear,
                0.3f,
                LineKind.Solid,
                1.0f,
                false,
                Color.red);

            state.RestoreRememberedFillOutIfNotChangedByUser(aggregation, true);
            state.RememberPreviousCapConf(aggregation, true);

            LineCapConf returnedComposition = CreateCap(
                LineCap.Composition,
                ColorKind.Monochrome,
                Color.blue,
                Color.clear,
                0.3f,
                LineKind.Solid,
                1.0f,
                true,
                Color.blue);

            bool compositionRestored =
                state.RestoreRememberedFillOutIfNotChangedByUser(returnedComposition, true);

            Assert.That(compositionRestored, Is.True);
            Assert.That(returnedComposition.FillOutStatus, Is.False);
            Assert.That(returnedComposition.FillOutColor, Is.EqualTo(Color.red));
        }

        /// <summary>
        /// Creates a line configuration with distinctive visual properties.
        /// </summary>
        /// <returns>The created line configuration.</returns>
        private static LineConf CreateLine()
        {
            return new LineConf
            {
                ColorKind = ColorKind.Gradient,
                PrimaryColor = Color.cyan,
                SecondaryColor = Color.magenta,
                Thickness = 0.75f,
                LineKind = LineKind.Dashed,
                Tiling = 5.0f,
                FillOutStatus = true,
                FillOutColor = Color.yellow
            };
        }

        /// <summary>
        /// Creates a line-cap configuration with the specified properties.
        /// </summary>
        /// <param name="capKind">The kind of the line cap.</param>
        /// <param name="colorKind">The color kind of the line cap.</param>
        /// <param name="primaryColor">The primary color of the line cap.</param>
        /// <param name="secondaryColor">The secondary color of the line cap.</param>
        /// <param name="thickness">The thickness of the line cap.</param>
        /// <param name="lineKind">The line kind of the line cap.</param>
        /// <param name="tiling">The tiling of the line cap.</param>
        /// <param name="fillOutStatus">Whether the line cap is filled out.</param>
        /// <param name="fillOutColor">The fill-out color of the line cap.</param>
        /// <returns>The created line-cap configuration.</returns>
        private static LineCapConf CreateCap(
            LineCap capKind,
            ColorKind colorKind,
            Color primaryColor,
            Color secondaryColor,
            float thickness,
            LineKind lineKind,
            float tiling,
            bool fillOutStatus,
            Color fillOutColor)
        {
            return new LineCapConf
            {
                CapKind = capKind,
                ColorKind = colorKind,
                PrimaryColor = primaryColor,
                SecondaryColor = secondaryColor,
                Thickness = thickness,
                LineKind = lineKind,
                Tiling = tiling,
                FillOutStatus = fillOutStatus,
                FillOutColor = fillOutColor,
                UseOwnVisuals = true
            };
        }

        /// <summary>
        /// Verifies that two line visual configurations have equal visual properties.
        /// </summary>
        /// <param name="expected">The expected visual configuration.</param>
        /// <param name="actual">The actual visual configuration.</param>
        private static void AssertVisualPropertiesEqual(
            ILineVisualConf expected, ILineVisualConf actual)
        {
            Assert.That(actual.ColorKind, Is.EqualTo(expected.ColorKind));
            Assert.That(actual.PrimaryColor, Is.EqualTo(expected.PrimaryColor));
            Assert.That(actual.SecondaryColor, Is.EqualTo(expected.SecondaryColor));
            Assert.That(actual.Thickness, Is.EqualTo(expected.Thickness));
            Assert.That(actual.LineKind, Is.EqualTo(expected.LineKind));
            Assert.That(actual.Tiling, Is.EqualTo(expected.Tiling));
            Assert.That(actual.FillOutStatus, Is.EqualTo(expected.FillOutStatus));
            Assert.That(actual.FillOutColor, Is.EqualTo(expected.FillOutColor));
        }
    }
}
