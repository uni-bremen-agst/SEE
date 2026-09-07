using NUnit.Framework;
using SEE.Game.Drawable.Configurations;
using System.Reflection;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;
using static SEE.Game.Drawable.GameDrawer;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Tests line-configuration equality as used by <see cref="EditAction"/>.
    /// </summary>
    [TestFixture]
    internal class TestEditActionLineEquality
    {
        /// <summary>
        /// Verifies that independently cloned line-cap configurations with equal
        /// values do not turn an unchanged edit into a history action.
        /// </summary>
        [Test]
        public void TestClonedLineCapsAreConsideredEqual()
        {
            LineConf oldLine = CreateLine();
            LineConf newLine = CreateLine();

            newLine.LineCapStart = oldLine.LineCapStart.Clone();
            newLine.LineCapEnd = oldLine.LineCapEnd.Clone();

            Assert.That(CheckEquals(oldLine, newLine), Is.True);
        }

        /// <summary>
        /// Verifies that changing a start-cap visual property is detected.
        /// </summary>
        [Test]
        public void TestStartCapVisualChangeIsDetected()
        {
            LineConf oldLine = CreateLine();
            LineConf newLine = CreateLine();

            newLine.LineCapStart.PrimaryColor = Color.yellow;

            Assert.That(CheckEquals(oldLine, newLine), Is.False);
        }

        /// <summary>
        /// Verifies that changing the end-cap kind is detected.
        /// </summary>
        [Test]
        public void TestEndCapKindChangeIsDetected()
        {
            LineConf oldLine = CreateLine();
            LineConf newLine = CreateLine();

            newLine.LineCapEnd.CapKind = LineCap.Composition;

            Assert.That(CheckEquals(oldLine, newLine), Is.False);
        }

        /// <summary>
        /// Verifies that two null line-cap configurations are considered equal.
        /// </summary>
        [Test]
        public void TestNullLineCapsAreConsideredEqual()
        {
            LineConf oldLine = CreateLine();
            LineConf newLine = CreateLine();

            oldLine.LineCapStart = null;
            newLine.LineCapStart = null;

            oldLine.LineCapEnd = null;
            newLine.LineCapEnd = null;

            Assert.That(CheckEquals(oldLine, newLine), Is.True);
        }

        /// <summary>
        /// Verifies that a missing cap on only one configuration is detected
        /// as a difference.
        /// </summary>
        [Test]
        public void TestSingleNullLineCapIsDetected()
        {
            LineConf oldLine = CreateLine();
            LineConf newLine = CreateLine();

            newLine.LineCapStart = null;

            Assert.That(CheckEquals(oldLine, newLine), Is.False);
        }

        /// <summary>
        /// Calls the equality implementation used by <see cref="EditAction"/>.
        /// </summary>
        /// <param name="oldLine">The original line configuration.</param>
        /// <param name="newLine">The edited line configuration.</param>
        /// <returns>Whether the configurations are considered equal.</returns>
        private static bool CheckEquals(
            LineConf oldLine,
            LineConf newLine)
        {
            EditAction action = new();

            MethodInfo method = typeof(EditAction).GetMethod(
                "CheckEquals",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);

            return (bool)method.Invoke(
                action,
                new object[]
                {
                    oldLine,
                    newLine
                });
        }

        /// <summary>
        /// Creates a deterministic line configuration containing independent
        /// start-cap and end-cap configurations.
        /// </summary>
        /// <returns>The created line configuration.</returns>
        private static LineConf CreateLine()
        {
            return new LineConf
            {
                PrimaryColor = Color.red,
                SecondaryColor = Color.blue,
                OrderInLayer = 2,
                Thickness = 0.2f,
                Loop = false,
                LineKind = LineKind.Dashed,
                ColorKind = ColorKind.Gradient,
                Tiling = 3.0f,
                FillOutStatus = true,
                FillOutColor = Color.green,

                LineCapStart = CreateCap(
                    LineCap.Arrow,
                    Color.red,
                    Color.blue,
                    0.25f),

                LineCapEnd = CreateCap(
                    LineCap.Aggregation,
                    Color.cyan,
                    Color.magenta,
                    0.4f)
            };
        }

        /// <summary>
        /// Creates a line-cap configuration.
        /// </summary>
        /// <param name="capKind">The cap kind.</param>
        /// <param name="primaryColor">The primary color.</param>
        /// <param name="secondaryColor">The secondary color.</param>
        /// <param name="thickness">The cap thickness.</param>
        /// <returns>The created line-cap configuration.</returns>
        private static LineCapConf CreateCap(
            LineCap capKind,
            Color primaryColor,
            Color secondaryColor,
            float thickness)
        {
            return new LineCapConf
            {
                CapKind = capKind,
                ColorKind = ColorKind.Gradient,
                PrimaryColor = primaryColor,
                SecondaryColor = secondaryColor,
                Thickness = thickness,
                LineKind = LineKind.Dashed,
                Tiling = 2.0f,
                FillOutStatus = true,
                FillOutColor = Color.green,
                UseOwnVisuals = true
            };
        }
    }
}
