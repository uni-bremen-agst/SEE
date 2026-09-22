using NUnit.Framework;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// Tests the visual appearance functionality for drawable lines.
    /// </summary>
    [TestFixture]
    public class TestGameLineAppearance
    {
        /// <summary>
        /// The temporary line object used by the tests.
        /// </summary>
        private GameObject line;

        /// <summary>
        /// The line renderer used by the tests.
        /// </summary>
        private LineRenderer renderer;

        /// <summary>
        /// Creates a temporary line renderer.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            line = new GameObject("GameLineAppearanceTest");
            renderer = line.AddComponent<LineRenderer>();
        }

        /// <summary>
        /// Destroys the temporary line object.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(line);
        }

        /// <summary>
        /// Verifies that all line kinds are returned.
        /// </summary>
        [Test]
        public void TestGetLineKindsReturnsAllKinds()
        {
            Assert.That(
                GameLineAppearance.GetLineKinds(),
                Is.EquivalentTo(new[]
                {
                    LineKind.Solid,
                    LineKind.Dashed,
                    LineKind.Dashed25,
                    LineKind.Dashed50,
                    LineKind.Dashed75,
                    LineKind.Dashed100
                }));
        }

        /// <summary>
        /// Verifies that two-colored dashed mode is not available for solid lines.
        /// </summary>
        [Test]
        public void TestGetColorKindsForSolidLine()
        {
            Assert.That(
                GameLineAppearance.GetColorKinds(false),
                Is.EquivalentTo(new[]
                {
                    ColorKind.Monochrome,
                    ColorKind.Gradient
                }));
        }

        /// <summary>
        /// Verifies that all color kinds are available for dashed lines.
        /// </summary>
        [Test]
        public void TestGetColorKindsForDashedLine()
        {
            Assert.That(
                GameLineAppearance.GetColorKinds(true),
                Is.EquivalentTo(new[]
                {
                    ColorKind.Monochrome,
                    ColorKind.Gradient,
                    ColorKind.TwoDashed
                }));
        }

        /// <summary>
        /// Verifies that solid lines use stretched texture mapping.
        /// </summary>
        [Test]
        public void TestSolidLineUsesStretchTextureMode()
        {
            GameLineAppearance.SetTextureMode(
                renderer,
                LineKind.Solid);

            Assert.That(
                renderer.textureMode,
                Is.EqualTo(LineTextureMode.Stretch));
        }

        /// <summary>
        /// Verifies that dashed line kinds use tiled texture mapping.
        /// </summary>
        /// <param name="lineKind">The dashed line kind to test.</param>
        [TestCase(LineKind.Dashed)]
        [TestCase(LineKind.Dashed25)]
        [TestCase(LineKind.Dashed50)]
        [TestCase(LineKind.Dashed75)]
        [TestCase(LineKind.Dashed100)]
        public void TestDashedLineKindsUseTileTextureMode(LineKind lineKind)
        {
            GameLineAppearance.SetTextureMode(
                renderer,
                lineKind);

            Assert.That(
                renderer.textureMode,
                Is.EqualTo(LineTextureMode.Tile));
        }

        /// <summary>
        /// Verifies the default texture scale of a custom dashed line.
        /// </summary>
        [Test]
        public void TestDashedLineUsesDefaultTilingForZero()
        {
            GameLineAppearance.SetRendererTextureScale(
                renderer,
                LineKind.Dashed,
                0.0f);

            Assert.That(
                renderer.textureScale.x,
                Is.EqualTo(0.05f).Within(0.0001f));
        }

        /// <summary>
        /// Verifies the predefined texture scales of dashed line kinds.
        /// </summary>
        /// <param name="lineKind">The line kind to test.</param>
        /// <param name="expectedScale">The expected texture scale.</param>
        [TestCase(LineKind.Dashed25, 5.0f / 3.0f)]
        [TestCase(LineKind.Dashed50, 10.0f / 3.0f)]
        [TestCase(LineKind.Dashed75, 5.0f)]
        [TestCase(LineKind.Dashed100, 20.0f / 3.0f)]
        public void TestPredefinedDashedTextureScale(
            LineKind lineKind,
            float expectedScale)
        {
            GameLineAppearance.SetRendererTextureScale(
                renderer,
                lineKind,
                1.0f);

            Assert.That(
                renderer.textureScale.x,
                Is.EqualTo(expectedScale).Within(0.0001f));
        }
    }
}
