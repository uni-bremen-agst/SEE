using NUnit.Framework;
using SEE.Game.Drawable.Configurations;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

namespace SEE.Game.Drawable.Line
{
    /// <summary>
    /// Tests creation and initialization of line-cap configurations.
    /// </summary>
    [TestFixture]
    public class TestGameLineCapConfiguration
    {
        /// <summary>
        /// Verifies that a new line cap without its own visual configuration
        /// inherits the complete visual style of its parent line.
        /// </summary>
        [Test]
        public void TestNewCapInheritsMainLineVisuals()
        {
            LineConf line = new()
            {
                ColorKind = ColorKind.TwoDashed,
                PrimaryColor = Color.red,
                SecondaryColor = Color.blue,
                Thickness = 0.75f,
                LineKind = LineKind.Dashed25,
                Tiling = 4.0f
            };

            LineCapConf cap = GameLineCapConfiguration.CreateLineCapConf(
                line,
                LineCapConf.CreateNone(),
                LineCap.Arrowhead);

            Assert.That(cap.CapKind, Is.EqualTo(LineCap.Arrowhead));
            Assert.That(cap.ColorKind, Is.EqualTo(ColorKind.TwoDashed));
            Assert.That(cap.PrimaryColor, Is.EqualTo(Color.red));
            Assert.That(cap.SecondaryColor, Is.EqualTo(Color.blue));
            Assert.That(cap.Thickness, Is.EqualTo(0.75f));
            Assert.That(cap.LineKind, Is.EqualTo(LineKind.Dashed25));
            Assert.That(cap.Tiling, Is.EqualTo(4.0f));
            Assert.That(cap.UseOwnVisuals, Is.False);
        }

        /// <summary>
        /// Verifies that an existing active cap keeps its visual configuration
        /// when the cap kind does not change.
        /// </summary>
        [Test]
        public void TestExistingCapPreservesOwnVisuals()
        {
            LineConf line = new()
            {
                ColorKind = ColorKind.Monochrome,
                PrimaryColor = Color.white,
                SecondaryColor = Color.clear,
                Thickness = 0.1f,
                LineKind = LineKind.Solid,
                Tiling = 1.0f
            };

            LineCapConf existing = new()
            {
                CapKind = LineCap.Arrowhead,
                ColorKind = ColorKind.Gradient,
                PrimaryColor = Color.red,
                SecondaryColor = Color.blue,
                Thickness = 0.5f,
                LineKind = LineKind.Dashed50,
                Tiling = 3.0f,
                FillOutStatus = true,
                FillOutColor = Color.green,
                UseOwnVisuals = true
            };

            LineCapConf cap = GameLineCapConfiguration.CreateLineCapConf(
                line,
                existing,
                LineCap.Arrowhead);

            Assert.That(cap.ColorKind, Is.EqualTo(ColorKind.Gradient));
            Assert.That(cap.PrimaryColor, Is.EqualTo(Color.red));
            Assert.That(cap.SecondaryColor, Is.EqualTo(Color.blue));
            Assert.That(cap.Thickness, Is.EqualTo(0.5f));
            Assert.That(cap.LineKind, Is.EqualTo(LineKind.Dashed50));
            Assert.That(cap.Tiling, Is.EqualTo(3.0f));
            Assert.That(cap.FillOutStatus, Is.True);
            Assert.That(cap.FillOutColor, Is.EqualTo(Color.green));
            Assert.That(cap.UseOwnVisuals, Is.True);
        }

        /// <summary>
        /// Verifies that composition caps enable their fill-out and use the
        /// primary color as the default fill-out color.
        /// </summary>
        [Test]
        public void TestCompositionCapAppliesFillOutDefaults()
        {
            LineConf line = new()
            {
                ColorKind = ColorKind.Monochrome,
                PrimaryColor = Color.red,
                SecondaryColor = Color.clear,
                Thickness = 0.25f,
                LineKind = LineKind.Solid,
                Tiling = 1.0f
            };

            LineCapConf cap = GameLineCapConfiguration.CreateLineCapConf(
                line,
                null,
                LineCap.Composition);

            Assert.That(cap.FillOutStatus, Is.True);
            Assert.That(cap.FillOutColor, Is.EqualTo(Color.red));
        }

        /// <summary>
        /// Verifies that selecting no cap clears cap-specific fill-out state.
        /// </summary>
        [Test]
        public void TestNoneCapClearsFillOutState()
        {
            LineConf line = new()
            {
                PrimaryColor = Color.red
            };

            LineCapConf existing = new()
            {
                CapKind = LineCap.Composition,
                FillOutStatus = true,
                FillOutColor = Color.green,
                UseOwnVisuals = true
            };

            LineCapConf cap = GameLineCapConfiguration.CreateLineCapConf(
                line,
                existing,
                LineCap.None);

            Assert.That(cap.CapKind, Is.EqualTo(LineCap.None));
            Assert.That(cap.FillOutStatus, Is.False);
            Assert.That(cap.FillOutColor, Is.EqualTo(Color.clear));
        }
    }
}
