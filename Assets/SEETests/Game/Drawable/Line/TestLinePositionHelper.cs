
using NUnit.Framework;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Editing;
using SEE.Game.Drawable.Line;
using SEE.Game.Drawable.ValueHolders;
using System;
using System.Collections.Generic;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// Tests the translation of rendered line positions into
    /// original logical line indices.
    /// </summary>
    [TestFixture]
    public class TestLinePositionHelper
    {
        /// <summary>
        /// The original global drawing order.
        /// </summary>
        private int originalMaxOrderInLayer;

        /// <summary>
        /// The temporary drawable surface.
        /// </summary>
        private GameObject surface;

        /// <summary>
        /// The line used by the tests.
        /// </summary>
        private GameObject line;

        /// <summary>
        /// Creates a line with three distinct points.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            originalMaxOrderInLayer = ValueHolder.MaxOrderInLayer;

            surface = new GameObject("LinePositionHelperTestSurface")
            {
                tag = Tags.Drawable
            };

            surface.AddComponent<DrawableHolder>();

            Vector3[] positions =
            {
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(3.0f, 0.0f, 0.0f),
                new Vector3(1.0f, 3.0f, 0.0f)
            };

            line = GameLineDrawer.DrawLine(
                surface,
                "LinePositionHelperTestLine",
                positions,
                ColorKind.Monochrome,
                Color.red,
                Color.clear,
                0.1f,
                false,
                LineKind.Solid,
                1.0f,
                increaseCurrentOrder: false);

            GameLineGeometry.UpdateOriginalAnchors(line, positions);
        }

        /// <summary>
        /// Removes the temporary objects and restores drawing order.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            ValueHolder.MaxOrderInLayer = originalMaxOrderInLayer;

            if (surface != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    surface.transform.root.gameObject);
            }
        }

        /// <summary>
        /// Selecting the end cap of a capped loop must select the first
        /// logical point. Moving it must preserve the other vertices.
        /// </summary>
        [Test]
        public void TestCappedLoopEndCapMovesFirstLogicalVertex()
        {
            ConfigureLoop(false, true);

            LineRenderer renderer = line.GetComponent<LineRenderer>();

            Vector3[] original =
                LinePositionHelper.GetSafeOriginalPositions(line);

            Assert.That(
                renderer.positionCount,
                Is.EqualTo(original.Length + 1));

            List<int> indices = LinePositionHelper.ToLogicalIndices(
                line,
                new[] { renderer.positionCount - 1 });

            Assert.That(indices, Is.EqualTo(new[] { 0 }));

            Vector3 moved = new Vector3(-1.0f, 1.0f, 0.0f);

            GameLineGeometry.MovePoint(line, indices, moved);

            Vector3[] result =
                LinePositionHelper.GetSafeOriginalPositions(line);

            Assert.That(result[0], Is.EqualTo(moved));
            Assert.That(result[1], Is.EqualTo(original[1]));
            Assert.That(result[2], Is.EqualTo(original[2]));
        }

        /// <summary>
        /// Both caps of a closed line refer to the first logical point.
        /// </summary>
        [Test]
        public void TestBothLoopCapsMapToFirstLogicalVertex()
        {
            ConfigureLoop(true, true);

            LineRenderer renderer = line.GetComponent<LineRenderer>();

            Assert.That(
                LinePositionHelper.ToLogicalIndices(line, new[] { 0 }),
                Is.EqualTo(new[] { 0 }));

            Assert.That(
                LinePositionHelper.ToLogicalIndices(
                    line,
                    new[] { renderer.positionCount - 1 }),
                Is.EqualTo(new[] { 0 }));
        }

        /// <summary>
        /// The synthetic closing position found by nearest-point
        /// selection must resolve to a valid logical point.
        /// </summary>
        [Test]
        public void TestNearestSyntheticClosingPointMapsToLogicalStart()
        {
            ConfigureLoop(false, true);

            LineRenderer renderer = line.GetComponent<LineRenderer>();

            Vector3 hitPoint = line.transform.TransformPoint(
                renderer.GetPosition(renderer.positionCount - 1));

            NearestPoints.GetNearestPoints(
                line,
                hitPoint,
                out List<Vector3> _,
                out List<int> matchedIndices);

            Assert.That(
                matchedIndices,
                Does.Contain(renderer.positionCount - 1));

            Assert.That(
                LinePositionHelper.ToLogicalIndices(line, matchedIndices),
                Is.EqualTo(new[] { 0 }));
        }

        /// <summary>
        /// Duplicate indices must be removed and the remaining indices
        /// sorted for subsequent split and erase operations.
        /// </summary>
        [Test]
        public void TestSyntheticClosingPointProducesSortedDistinctIndices()
        {
            ConfigureLoop(false, true);

            int lastRendererIndex =
                line.GetComponent<LineRenderer>().positionCount - 1;

            Assert.That(
                LinePositionHelper.ToLogicalIndices(
                    line,
                    new[] { 2, lastRendererIndex, 0, 2 }),
                Is.EqualTo(new[] { 0, 2 }));
        }

        /// <summary>
        /// An uncapped loop must retain the index of its final point.
        /// </summary>
        [Test]
        public void TestUncappedLoopKeepsFinalVertexIndex()
        {
            GameLineEdit.ChangeLoop(line, true);

            LineRenderer renderer = line.GetComponent<LineRenderer>();

            Assert.That(renderer.positionCount, Is.EqualTo(3));

            Assert.That(
                LinePositionHelper.ToLogicalIndices(
                    line,
                    new[] { renderer.positionCount - 1 }),
                Is.EqualTo(new[] { 2 }));
        }

        /// <summary>
        /// An open line with a cap must retain its final logical index.
        /// </summary>
        [Test]
        public void TestOpenCappedLineKeepsFinalVertexIndex()
        {
            LineCapConf endCap =
                GameLineCapConfiguration.CreateLineCapConf(
                    LineConf.GetLine(line),
                    LineCapConf.CreateNone(),
                    LineCap.Arrowhead);

            GameLineCapApplicator.ApplyLineCaps(
                line,
                LineCapConf.CreateNone(),
                endCap);

            LineRenderer renderer = line.GetComponent<LineRenderer>();

            Assert.That(renderer.positionCount, Is.EqualTo(3));

            List<int> indices = LinePositionHelper.ToLogicalIndices(
                line,
                new[] { renderer.positionCount - 1 });

            Assert.That(indices, Is.EqualTo(new[] { 2 }));
        }

        /// <summary>
        /// An invalid renderer index must be rejected rather than
        /// selecting an unrelated original point.
        /// </summary>
        [Test]
        public void TestInvalidRendererIndexIsRejected()
        {
            ConfigureLoop(false, true);

            int invalidIndex =
                line.GetComponent<LineRenderer>().positionCount;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                LinePositionHelper.ToLogicalIndices(
                    line,
                    new[] { invalidIndex }));
        }

        /// <summary>
        /// Enables looping and applies the requested line caps through
        /// the production line-cap application pipeline.
        /// </summary>
        /// <param name="startCap">
        /// Whether an arrowhead is applied to the start.
        /// </param>
        /// <param name="endCap">
        /// Whether an arrowhead is applied to the end.
        /// </param>
        private void ConfigureLoop(bool startCap, bool endCap)
        {
            GameLineEdit.ChangeLoop(line, true);

            LineConf lineConf = LineConf.GetLine(line);

            LineCapConf startConf = startCap
                ? GameLineCapConfiguration.CreateLineCapConf(
                    lineConf,
                    LineCapConf.CreateNone(),
                    LineCap.Arrowhead)
                : LineCapConf.CreateNone();

            LineCapConf endConf = endCap
                ? GameLineCapConfiguration.CreateLineCapConf(
                    lineConf,
                    LineCapConf.CreateNone(),
                    LineCap.Arrowhead)
                : LineCapConf.CreateNone();

            GameLineCapApplicator.ApplyLineCaps(
                line,
                startConf,
                endConf);
        }
    }
}
