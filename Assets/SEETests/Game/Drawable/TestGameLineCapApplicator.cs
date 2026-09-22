using NUnit.Framework;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// Tests the integration of line-cap application, rendering, and line geometry.
    /// </summary>
    [TestFixture]
    public class TestGameLineCapApplicator
    {
        /// <summary>
        /// The drawable surface used by the tests.
        /// </summary>
        private GameObject surface;

        /// <summary>
        /// The line used by the tests.
        /// </summary>
        private GameObject line;

        /// <summary>
        /// The original unshortened line positions.
        /// </summary>
        private Vector3[] originalPositions;

        /// <summary>
        /// Creates a drawable surface and a line with original anchors.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            surface = new GameObject("LineCapApplicatorTestSurface")
            {
                tag = Tags.Drawable
            };

            surface.AddComponent<DrawableHolder>();

            originalPositions = new[]
            {
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(2.0f, 0.0f, 0.0f),
                new Vector3(4.0f, 0.0f, 0.0f)
            };

            line = GameLineDrawer.DrawLine(
                surface,
                "LineCapApplicatorTestLine",
                originalPositions,
                ColorKind.Monochrome,
                Color.red,
                Color.clear,
                0.1f,
                false,
                LineKind.Solid,
                1.0f,
                increaseCurrentOrder: false);

            GameLineGeometry.UpdateOriginalAnchors(
                line,
                originalPositions);
        }

        /// <summary>
        /// Destroys the complete temporary drawable hierarchy.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (surface != null)
            {
                Object.DestroyImmediate(
                    surface.transform.root.gameObject);
            }
        }

        /// <summary>
        /// Verifies that applying a cap creates the cap object and shortens
        /// the visible line without losing its original anchors. Removing
        /// the cap afterwards restores the original line geometry.
        /// </summary>
        [Test]
        public void TestApplyAndRemoveLineCapPreservesOriginalGeometry()
        {
            LineConf lineConf = LineConf.GetLine(line);

            LineCapConf startCap =
                GameLineCapConfiguration.CreateLineCapConf(
                    lineConf,
                    LineCapConf.CreateNone(),
                    LineCap.Arrowhead);

            LineCapConf endCap = LineCapConf.CreateNone();

            GameLineCapApplicator.ApplyLineCaps(
                line,
                startCap,
                endCap);

            LineCapValueHolder holder =
                line.GetComponent<LineCapValueHolder>();

            Assert.That(holder.StartCap, Is.EqualTo(LineCap.Arrowhead));
            Assert.That(holder.EndCap, Is.EqualTo(LineCap.None));

            Assert.That(
                GameLineCapRenderer.GetLineCapObjects(line, true),
                Has.Count.EqualTo(1));

            Assert.That(
                GameLineCapRenderer.GetLineCapObjects(line, false),
                Is.Empty);

            LineConf appliedLine = LineConf.GetLine(line);

            Assert.That(
                appliedLine.RendererPositions[0],
                Is.Not.EqualTo(originalPositions[0]));

            Assert.That(
                appliedLine.OriginalStartAnchor,
                Is.EqualTo(originalPositions[0]));

            Assert.That(
                appliedLine.OriginalEndAnchor,
                Is.EqualTo(originalPositions[originalPositions.Length - 1]));

            GameLineCapApplicator.ApplyLineCaps(
                line,
                LineCapConf.CreateNone(),
                LineCapConf.CreateNone());

            Assert.That(
                GameLineCapRenderer.GetLineCapObjects(line, true),
                Is.Empty);

            LineConf restoredLine = LineConf.GetLine(line);

            Assert.That(
                restoredLine.RendererPositions[0],
                Is.EqualTo(originalPositions[0]));

            Assert.That(
                restoredLine.RendererPositions[
                    restoredLine.RendererPositions.Length - 1],
                Is.EqualTo(originalPositions[
                    originalPositions.Length - 1]));
        }
    }
}
