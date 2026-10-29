using NUnit.Framework;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Editing;
using SEE.Game.Drawable.ValueHolders;
using System.Collections.Generic;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

namespace SEE.Game.Drawable.Line
{
    /// <summary>
    /// Tests the integration of line-cap application, rendering, and line geometry.
    /// </summary>
    [TestFixture]
    public class TestGameLineCapApplicator
    {
        /// <summary>
        /// The global drawing order restored after configuration reconstruction.
        /// </summary>
        private int originalMaxOrderInLayer;

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
            originalMaxOrderInLayer = ValueHolder.MaxOrderInLayer;
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
            ValueHolder.MaxOrderInLayer = originalMaxOrderInLayer;
            if (surface != null)
            {
                Object.DestroyImmediate(
                    surface.transform.root.gameObject);
            }
        }

        /// <summary>
        /// Moving an interior logical vertex must survive cap rebuilding, repeated
        /// edits, and configuration extraction without moving the end-cap tip.
        /// </summary>
        /// <param name="editCount">The number of consecutive edits.</param>
        [TestCase(1)]
        [TestCase(3)]
        public void TestMovePointPreservesCappedLoopInteriorGeometry(int editCount)
        {
            PrepareGeometry(new[] { Vector3.zero, new Vector3(3, 0, 0), new Vector3(1, 3, 0) });
            GameLineEdit.ChangeLoop(line, true);
            ApplyEndArrowhead();

            for (int edit = 0; edit < editCount; edit++)
            {
                Vector3 target = new Vector3(4 + edit, 1 + edit, 0);
                GameLineGeometry.MovePoint(line, new List<int> { 1 }, target);
                Assert.That(GameLineGeometry.GetOriginalLinePositions(line)[1], Is.EqualTo(target),
                    $"Logical interior vertex after edit {edit + 1}.");
                Assert.That(line.GetComponent<LineRenderer>().GetPosition(1), Is.EqualTo(target),
                    $"Visible interior vertex after edit {edit + 1}.");
                LineConf extracted = LineConf.GetLine(line);
                Assert.That(extracted.RendererPositions[1], Is.EqualTo(target));
                Assert.That(extracted.RendererPositions, Has.Length.EqualTo(3));
                Assert.That(extracted.OriginalStartAnchor, Is.EqualTo(originalPositions[0]));
                Assert.That(extracted.OriginalEndAnchor, Is.EqualTo(originalPositions[2]));
                Assert.That(extracted.Loop, Is.True);
                AssertEndArrowhead(line, originalPositions[0], originalPositions[0] - originalPositions[2]);
            }
        }

        /// <summary>
        /// Exercises the inverse and replay geometry operations used by Move Point
        /// Undo/Redo without invoking its input, history, or networking infrastructure.
        /// </summary>
        [Test]
        public void TestCappedLoopMoveRestoreAndReplayPreservesGeometry()
        {
            PrepareGeometry(new[] { Vector3.zero, new Vector3(3, 0, 0), new Vector3(1, 3, 0) });
            GameLineEdit.ChangeLoop(line, true);
            ApplyEndArrowhead();
            Vector3 moved = new Vector3(4, 2, 0);
            Vector3[] targets = { moved, originalPositions[1], moved };

            foreach (Vector3 target in targets)
            {
                GameLineGeometry.MovePoint(line, new List<int> { 1 }, target);
                Assert.That(GameLineGeometry.GetOriginalLinePositions(line)[1], Is.EqualTo(target));
                Assert.That(line.GetComponent<LineRenderer>().GetPosition(1), Is.EqualTo(target));
            }
        }

        /// <summary>
        /// Extracting and reconstructing an edited capped loop must retain its
        /// edited logical geometry and shortened renderer endpoint.
        /// </summary>
        [Test]
        public void TestEditedCappedLoopConfigurationReconstructsGeometry()
        {
            PrepareGeometry(new[] { Vector3.zero, new Vector3(3, 0, 0), new Vector3(1, 3, 0) });
            GameLineEdit.ChangeLoop(line, true);
            ApplyEndArrowhead();
            Vector3 moved = new Vector3(4, 2, 0);
            GameLineGeometry.MovePoint(line, new List<int> { 1 }, moved);
            LineConf saved = LineConf.GetLine(line);
            saved.ID = "Line-ReconstructedCappedLoop";
            GameObject restored = GameLineDrawer.ReDrawLine(surface, saved);

            Assert.That(saved.RendererPositions[1], Is.EqualTo(moved));
            Assert.That(GameLineGeometry.GetOriginalLinePositions(restored)[1], Is.EqualTo(moved));
            Assert.That(restored.GetComponent<LineRenderer>().GetPosition(1), Is.EqualTo(moved));
            Assert.That(LineConf.GetLine(restored).Loop, Is.True);
            AssertEndArrowhead(restored, originalPositions[0], originalPositions[0] - originalPositions[2]);
        }
        /// <summary>
        /// Supplies unshortened geometry before any caps have been applied.
        /// </summary>
        /// <param name="positions">The logical positions for the test.</param>
        private void PrepareGeometry(Vector3[] positions)
        {
            originalPositions = positions;
            GameLineDrawer.Drawing(line, positions);
            GameLineGeometry.UpdateOriginalAnchors(line, positions);
        }

        /// <summary>
        /// Applies an end arrowhead through the production configuration and cap pipeline.
        /// </summary>
        private void ApplyEndArrowhead()
        {
            LineCapConf endCap = GameLineCapConfiguration.CreateLineCapConf(
                LineConf.GetLine(line), LineCapConf.CreateNone(), LineCap.Arrowhead);
            GameLineCapApplicator.ApplyLineCaps(line, LineCapConf.CreateNone(), endCap);
        }

        /// <summary>
        /// Checks cap kind, tip anchor, incoming direction, and renderer shortening
        /// without requiring logical and renderer endpoints to coincide.
        /// </summary>
        /// <param name="shape">The capped line.</param>
        /// <param name="anchor">The intended local tip anchor.</param>
        /// <param name="incoming">The intended incoming edge direction.</param>
        private static void AssertEndArrowhead(GameObject shape, Vector3 anchor, Vector3 incoming)
        {
            Assert.That(LineConf.GetLine(shape).LineCapEnd.CapKind, Is.EqualTo(LineCap.Arrowhead));
            List<GameObject> caps = GameLineCapRenderer.GetLineCapObjects(shape, false);
            if (caps.Count != 1)
            {
                Assert.That(caps, Has.Count.EqualTo(1), "The end cap must remain instantiated.");
                return;
            }

            Transform cap = caps[0].transform;
            Assert.That(Vector3.Distance(cap.localPosition, anchor), Is.LessThan(0.0001f));
            Assert.That(Vector3.Distance(cap.TransformPoint(Vector3.zero), shape.transform.TransformPoint(anchor)),
                Is.LessThan(0.0001f), "The arrowhead tip must stay at the logical anchor.");
            Assert.That(Vector3.Angle(cap.localRotation * Vector3.right, incoming), Is.LessThan(0.01f));

            LineRenderer renderer = shape.GetComponent<LineRenderer>();
            Vector3 shortened = renderer.GetPosition(renderer.positionCount - 1);
            Vector3 retreat = anchor - shortened;
            Assert.That(Vector3.Dot(retreat, incoming.normalized), Is.GreaterThan(0));
            Assert.That(retreat.magnitude, Is.LessThan(incoming.magnitude));
            Assert.That(Vector3.Cross(retreat, incoming.normalized).magnitude, Is.LessThan(0.0001f));

            LineConf logical = LineConf.GetLine(shape);
            logical.RendererPositions = GameLineGeometry.GetOriginalLinePositions(shape);
            List<LineCapShape> capShapes = GetShapes(logical.LineCapEnd, logical, LineCapPosition.End, true);
            Assert.That(Vector3.Distance(shape.transform.TransformPoint(shortened),
                cap.TransformPoint(capShapes[0].ConnectionPoint)), Is.LessThan(0.0001f),
                "The shortened line must meet the cap's connection point.");
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


        /// <summary>
        /// Verifies that generated start and end caps lie on the
        /// same depth plane as their parent line.
        /// </summary>
        [Test]
        public void TestLineCapsShareParentLineDepth()
        {
            LineConf lineConf = LineConf.GetLine(line);

            LineCapConf startCap =
                GameLineCapConfiguration.CreateLineCapConf(
                    lineConf,
                    LineCapConf.CreateNone(),
                    LineCap.Arrowhead);

            LineCapConf endCap =
                GameLineCapConfiguration.CreateLineCapConf(
                    lineConf,
                    LineCapConf.CreateNone(),
                    LineCap.Arrow);

            GameLineCapApplicator.ApplyLineCaps(
                line,
                startCap,
                endCap);

            Assert.That(
                GameLineCapRenderer.GetLineCapObjects(line, true),
                Has.Count.EqualTo(1));

            Assert.That(
                GameLineCapRenderer.GetLineCapObjects(line, false),
                Has.Count.EqualTo(1));

            Transform startTransform =
                GameLineCapRenderer.GetLineCapObjects(line, true)[0]
                    .transform;

            Transform endTransform =
                GameLineCapRenderer.GetLineCapObjects(line, false)[0]
                    .transform;

            Assert.That(
                Mathf.Abs(line.transform.localPosition.z),
                Is.GreaterThan(0.000001f),
                "The test requires a nonzero parent depth.");

            Assert.That(
                startTransform.localPosition.z,
                Is.EqualTo(originalPositions[0].z).Within(0.000001f),
                "The start cap must not repeat its parent's depth.");

            Assert.That(
                endTransform.localPosition.z,
                Is.EqualTo(
                    originalPositions[originalPositions.Length - 1].z)
                    .Within(0.000001f),
                "The end cap must not repeat its parent's depth.");
        }
    }
}
