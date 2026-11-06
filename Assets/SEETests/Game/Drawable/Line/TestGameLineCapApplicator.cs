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
        /// Verifies that enabling a loop with a short closing edge
        /// preserves the end cap's size and orientation. The visible
        /// line must connect to the cap without reversing direction,
        /// while the original logical geometry remains unchanged.
        /// </summary>
        /// <param name="distance">
        /// The distance between the final logical point and the
        /// loop's starting point.
        /// </param>
        [TestCase(0.00005f)]
        [TestCase(0.02f)]
        [TestCase(0.1f)]
        public void TestNearClosedLoopPreservesEndCapAfterMovePoint(
            float distance)
        {
            Vector3[] positions =
            {
                Vector3.zero,
                new Vector3(3.0f, 0.0f, 0.0f),
                new Vector3(3.0f, 3.0f, 0.0f),
                new Vector3(1.0f, 2.0f, 0.0f)
            };

            PrepareGeometry(positions);
            ApplyEndArrowhead();

            Vector3 nearStart =
                new Vector3(distance, 0.0f, 0.0f);

            GameLineGeometry.MovePoint(
                line,
                new List<int> { 3 },
                nearStart);

            Assert.That(LineConf.GetLine(line).Loop, Is.False);

            AssertEndArrowhead(
                line,
                nearStart,
                nearStart - positions[2]);

            List<GameObject> capsBefore =
                GameLineCapRenderer.GetLineCapObjects(line, false);

            Assert.That(capsBefore, Has.Count.EqualTo(1));

            LineRenderer capRendererBefore =
                capsBefore[0].GetComponent<LineRenderer>();

            float previousCapSize = Vector3.Distance(
                capRendererBefore.GetPosition(0),
                capRendererBefore.GetPosition(1));

            GameLineEdit.ChangeLoop(line, true);

            LineConf configuration = LineConf.GetLine(line);

            Assert.That(configuration.Loop, Is.True);

            Assert.That(
                GameLineGeometry.GetOriginalLinePositions(line)[3],
                Is.EqualTo(nearStart));

            Assert.That(
                configuration.LineCapEnd.CapKind,
                Is.EqualTo(LineCap.Arrowhead));

            List<GameObject> capsAfter =
                GameLineCapRenderer.GetLineCapObjects(line, false);

            Assert.That(
                capsAfter,
                Has.Count.EqualTo(1),
                "The end cap must survive loop activation.");

            Transform cap = capsAfter[0].transform;

            Assert.That(
                Vector3.Distance(
                    cap.localPosition,
                    positions[0]),
                Is.LessThan(0.0001f));

            Assert.That(
                Vector3.Angle(
                    cap.localRotation * Vector3.right,
                    positions[0] - positions[2]),
                Is.LessThan(0.01f));

            LineRenderer capRendererAfter =
                capsAfter[0].GetComponent<LineRenderer>();

            float currentCapSize = Vector3.Distance(
                capRendererAfter.GetPosition(0),
                capRendererAfter.GetPosition(1));

            Assert.That(
                currentCapSize,
                Is.EqualTo(previousCapSize).Within(0.001f),
                "The end cap must retain its previous size.");

            LineRenderer renderer =
                line.GetComponent<LineRenderer>();

            Assert.That(
                renderer.positionCount,
                Is.EqualTo(positions.Length + 1));

            Vector3 beforeLast =
                renderer.GetPosition(renderer.positionCount - 3);

            Vector3 lastLogical =
                renderer.GetPosition(renderer.positionCount - 2);

            Vector3 connection =
                renderer.GetPosition(renderer.positionCount - 1);

            Vector3 closingDirection =
                connection - beforeLast;

            Assert.That(
                Vector3.Dot(
                    lastLogical - beforeLast,
                    closingDirection),
                Is.GreaterThan(0.0f));

            Assert.That(
                Vector3.Dot(
                    connection - lastLogical,
                    closingDirection),
                Is.GreaterThan(0.0f));

            Assert.That(
                Vector3.Cross(
                    lastLogical - beforeLast,
                    closingDirection).magnitude,
                Is.LessThan(0.0001f));

            LineConf capCalculation = LineConf.GetLine(line);

            Vector3[] calculationPositions =
                GameLineGeometry.GetOriginalLinePositions(line);

            calculationPositions[calculationPositions.Length - 1] =
                calculationPositions[calculationPositions.Length - 2];

            capCalculation.RendererPositions = calculationPositions;

            List<LineCapShape> shapes =
                GetShapes(
                    capCalculation.LineCapEnd,
                    capCalculation,
                    LineCapPosition.End,
                    true);

            Assert.That(
                Vector3.Distance(
                    line.transform.TransformPoint(connection),
                    cap.TransformPoint(shapes[0].ConnectionPoint)),
                Is.LessThan(0.0001f));

            GameLineEdit.ChangeLoop(line, false);

            Assert.That(LineConf.GetLine(line).Loop, Is.False);

            Assert.That(
                GameLineGeometry.GetOriginalLinePositions(line)[3],
                Is.EqualTo(nearStart));

            AssertEndArrowhead(
                line,
                nearStart,
                nearStart - positions[2]);
        }

        /// <summary>
        /// Verifies that a short terminal segment on an open line
        /// does not cause earlier renderer vertices to be consumed
        /// or change the direction of its end cap.
        /// Reapplying the open loop state must preserve the geometry.
        /// </summary>
        [Test]
        public void TestShortOpenTerminalSegmentPreservesPreviousVertices()
        {
            Vector3[] positions =
            {
                Vector3.zero,
                new Vector3(4.0f, 0.0f, 0.0f),
                new Vector3(4.0f, 3.0f, 0.0f),
                new Vector3(4.02f, 3.005f, 0.0f)
            };

            PrepareGeometry(positions);
            ApplyEndArrowhead();

            LineRenderer renderer =
                line.GetComponent<LineRenderer>();

            Assert.That(renderer.loop, Is.False);

            Assert.That(
                renderer.positionCount,
                Is.EqualTo(positions.Length));

            Assert.That(
                renderer.GetPosition(1),
                Is.EqualTo(positions[1]));

            Assert.That(
                renderer.GetPosition(2),
                Is.EqualTo(positions[2]),
                "The previous corner must not be consumed by the end cap.");

            AssertEndArrowhead(
                line,
                positions[3],
                positions[3] - positions[2]);

            GameLineEdit.ChangeLoop(line, false);

            Assert.That(LineConf.GetLine(line).Loop, Is.False);

            Assert.That(
                renderer.GetPosition(1),
                Is.EqualTo(positions[1]));

            Assert.That(
                renderer.GetPosition(2),
                Is.EqualTo(positions[2]));

            Assert.That(
                GameLineGeometry.GetOriginalLinePositions(line),
                Is.EqualTo(positions));

            AssertEndArrowhead(
                line,
                positions[3],
                positions[3] - positions[2]);
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

        /// <summary>
        /// Verifies that an open segment retaining a loop's end cap
        /// uses the preceding incoming edge when its final edge is
        /// too short, without changing its original logical vertices.
        /// </summary>
        /// <param name="capKind">The end-cap kind to verify.</param>
        [TestCase(LineCap.Arrowhead)]
        [TestCase(LineCap.Arrow)]
        [TestCase(LineCap.Composition)]
        [TestCase(LineCap.Aggregation)]
        [TestCase(LineCap.Provided)]
        [TestCase(LineCap.RequiredProvided)]
        public void TestOpenedLoopPreservesEndCapAndLogicalGeometry(
            LineCap capKind)
        {
            Vector3[] positions =
            {
        Vector3.zero,
        new Vector3(3.0f, 0.0f, 0.0f),
        new Vector3(3.0f, 3.0f, 0.0f),
        new Vector3(0.02f, 0.0f, 0.0f),
        Vector3.zero
    };

            PrepareGeometry(positions);

            LineAnchorValueHolder anchorHolder =
                line.GetComponent<LineAnchorValueHolder>();

            anchorHolder.UsePreviousSegmentForEndCap = true;

            LineConf currentLine = LineConf.GetLine(line);

            LineCapConf endCap =
                GameLineCapConfiguration.CreateLineCapConf(
                    currentLine,
                    LineCapConf.CreateNone(),
                    capKind);

            GameLineCapApplicator.ApplyLineCaps(
                line,
                LineCapConf.CreateNone(),
                endCap,
                useCapConfVisuals: true);

            LineConf result = LineConf.GetLine(line);

            Assert.That(result.Loop, Is.False);
            Assert.That(result.UsePreviousSegmentForEndCap, Is.True);
            Assert.That(result.RendererPositions, Is.EqualTo(positions));

            Assert.That(
                GameLineGeometry.GetOriginalLinePositions(line),
                Is.EqualTo(positions));

            Assert.That(
                result.LineCapEnd.CapKind,
                Is.EqualTo(capKind));

            LineRenderer renderer =
                line.GetComponent<LineRenderer>();

            Assert.That(renderer.loop, Is.False);

            Assert.That(
                renderer.positionCount,
                Is.EqualTo(positions.Length));

            Assert.That(
                renderer.GetPosition(3),
                Is.Not.EqualTo(positions[3]),
                "Only the visible short-edge vertex should be adjusted.");

            Assert.That(
                anchorHolder.OriginalRendererPositions,
                Is.EqualTo(positions));

            List<GameObject> caps =
                GameLineCapRenderer.GetLineCapObjects(line, false);

            Assert.That(caps, Is.Not.Empty);

            Vector3 expectedDirection =
                positions[4] - positions[2];

            Vector3 actualDirection =
                caps[0].transform.localRotation * Vector3.right;

            Assert.That(
                Vector3.Angle(actualDirection, expectedDirection),
                Is.LessThan(0.01f),
                "The end cap must use the preceding incoming segment.");

            Vector3[] capCalculationPositions =
                new Vector3[positions.Length];

            System.Array.Copy(
                positions,
                capCalculationPositions,
                positions.Length);

            capCalculationPositions[3] = positions[2];

            result.RendererPositions = capCalculationPositions;

            List<LineCapShape> capShapes =
                GetShapes(
                    result.LineCapEnd,
                    result,
                    LineCapPosition.End,
                    true);

            Vector3 connection =
                renderer.GetPosition(renderer.positionCount - 1);

            Assert.That(
                Vector3.Distance(
                    line.transform.TransformPoint(connection),
                    caps[0].transform.TransformPoint(
                        capShapes[0].ConnectionPoint)),
                Is.LessThan(0.0001f),
                "The shortened line must meet the end cap.");
        }

        /// <summary>
        /// Verifies that moving the short-edge point away from an
        /// opened loop and restoring it preserves the logical geometry
        /// and reapplies the end-cap fallback when required.
        /// </summary>
        [Test]
        public void TestOpenedLoopEndCapSurvivesPointMoveAndRestore()
        {
            Vector3[] positions =
            {
        Vector3.zero,
        new Vector3(3.0f, 0.0f, 0.0f),
        new Vector3(3.0f, 3.0f, 0.0f),
        new Vector3(0.02f, 0.0f, 0.0f),
        Vector3.zero
    };

            PrepareGeometry(positions);

            LineAnchorValueHolder anchorHolder =
                line.GetComponent<LineAnchorValueHolder>();

            anchorHolder.UsePreviousSegmentForEndCap = true;

            ApplyEndArrowhead();

            Assert.That(
                LineConf.GetLine(line).RendererPositions,
                Is.EqualTo(positions));

            Vector3 movedPoint =
                new Vector3(2.0f, 1.0f, 0.0f);

            GameLineGeometry.MovePoint(
                line,
                new List<int> { 3 },
                movedPoint);

            LineConf moved = LineConf.GetLine(line);

            Assert.That(moved.Loop, Is.False);
            Assert.That(moved.UsePreviousSegmentForEndCap, Is.True);

            Assert.That(
                moved.RendererPositions[3],
                Is.EqualTo(movedPoint));

            Assert.That(
                line.GetComponent<LineRenderer>().GetPosition(3),
                Is.EqualTo(movedPoint));

            GameLineGeometry.MovePoint(
                line,
                new List<int> { 3 },
                positions[3]);

            LineConf restored = LineConf.GetLine(line);

            Assert.That(restored.Loop, Is.False);
            Assert.That(restored.UsePreviousSegmentForEndCap, Is.True);

            Assert.That(
                restored.RendererPositions,
                Is.EqualTo(positions));

            Assert.That(
                GameLineGeometry.GetOriginalLinePositions(line),
                Is.EqualTo(positions));

            LineRenderer renderer =
                line.GetComponent<LineRenderer>();

            Assert.That(
                renderer.GetPosition(3),
                Is.Not.EqualTo(positions[3]));

            List<GameObject> caps =
                GameLineCapRenderer.GetLineCapObjects(line, false);

            Assert.That(caps, Has.Count.EqualTo(1));

            Assert.That(
                Vector3.Angle(
                    caps[0].transform.localRotation * Vector3.right,
                    positions[4] - positions[2]),
                Is.LessThan(0.01f));
        }

        /// <summary>
        /// Verifies that the preserved end-cap context survives Unity
        /// JSON serialization and reconstruction through ReDrawLine.
        /// The complete logical point sequence must remain unchanged.
        /// </summary>
        [Test]
        public void TestOpenedLoopEndCapSurvivesSerializationAndRedraw()
        {
            Vector3[] positions =
            {
        Vector3.zero,
        new Vector3(3.0f, 0.0f, 0.0f),
        new Vector3(3.0f, 3.0f, 0.0f),
        new Vector3(0.02f, 0.0f, 0.0f),
        Vector3.zero
    };

            PrepareGeometry(positions);

            LineAnchorValueHolder anchorHolder =
                line.GetComponent<LineAnchorValueHolder>();

            anchorHolder.UsePreviousSegmentForEndCap = true;

            ApplyEndArrowhead();

            LineConf saved = LineConf.GetLine(line);

            Assert.That(saved.UsePreviousSegmentForEndCap, Is.True);
            Assert.That(saved.RendererPositions, Is.EqualTo(positions));

            string serialized = JsonUtility.ToJson(saved);

            LineConf deserialized =
                JsonUtility.FromJson<LineConf>(serialized);

            Assert.That(
                deserialized.UsePreviousSegmentForEndCap,
                Is.True);

            Assert.That(
                deserialized.RendererPositions,
                Is.EqualTo(positions));

            deserialized.ID = "OpenedLoopEndCapReconstructed";

            GameObject reconstructed =
                GameLineDrawer.ReDrawLine(surface, deserialized);

            LineConf result = LineConf.GetLine(reconstructed);

            Assert.That(result.Loop, Is.False);
            Assert.That(result.UsePreviousSegmentForEndCap, Is.True);

            Assert.That(
                result.RendererPositions,
                Is.EqualTo(positions));

            Assert.That(
                GameLineGeometry.GetOriginalLinePositions(reconstructed),
                Is.EqualTo(positions));

            LineRenderer renderer =
                reconstructed.GetComponent<LineRenderer>();

            Assert.That(renderer.loop, Is.False);

            Assert.That(
                renderer.positionCount,
                Is.EqualTo(positions.Length));

            Assert.That(
                renderer.GetPosition(3),
                Is.Not.EqualTo(positions[3]));

            List<GameObject> caps =
                GameLineCapRenderer.GetLineCapObjects(reconstructed, false);

            Assert.That(caps, Has.Count.EqualTo(1));

            Assert.That(
                Vector3.Angle(
                    caps[0].transform.localRotation * Vector3.right,
                    positions[4] - positions[2]),
                Is.LessThan(0.01f));
        }

        /// <summary>
        /// Verifies that an open segment created by splitting a loop
        /// at an interior point can retain the original end cap,
        /// even when its start and end positions differ.
        /// </summary>
        [Test]
        public void TestInteriorLoopSplitRetainsEndCapGeometry()
        {
            Vector3[] positions =
            {
        new Vector3(3.0f, 3.0f, 0.0f),
        new Vector3(0.02f, 0.0f, 0.0f),
        Vector3.zero
    };

            PrepareGeometry(positions);

            LineAnchorValueHolder anchorHolder =
                line.GetComponent<LineAnchorValueHolder>();

            anchorHolder.UsePreviousSegmentForEndCap = true;

            ApplyEndArrowhead();

            LineConf result = LineConf.GetLine(line);

            Assert.That(result.Loop, Is.False);
            Assert.That(result.UsePreviousSegmentForEndCap, Is.True);

            Assert.That(
                result.RendererPositions,
                Is.EqualTo(positions));

            Assert.That(
                GameLineGeometry.GetOriginalLinePositions(line),
                Is.EqualTo(positions));

            List<GameObject> caps =
                GameLineCapRenderer.GetLineCapObjects(line, false);

            Assert.That(caps, Has.Count.EqualTo(1));

            Assert.That(
                Vector3.Angle(
                    caps[0].transform.localRotation * Vector3.right,
                    positions[2] - positions[0]),
                Is.LessThan(0.01f));

            LineRenderer renderer =
                line.GetComponent<LineRenderer>();

            Assert.That(renderer.loop, Is.False);

            Assert.That(
                renderer.GetPosition(1),
                Is.Not.EqualTo(positions[1]));

            Assert.That(
                renderer.positionCount,
                Is.EqualTo(positions.Length));
        }

        /// <summary>
        /// Verifies that restoring an explicitly closed line with an
        /// invalid active loop removes the redundant loop while
        /// preserving the original logical positions and end cap.
        /// The normalized configuration must survive another redraw.
        /// Renderer positions may differ from logical positions because
        /// line caps shorten the visible line.
        /// </summary>
        [Test]
        public void TestExplicitlyClosedLineWithStoredLoopPreservesEndCap()
        {
            Vector3[] positions =
            {
        Vector3.zero,
        new Vector3(3.0f, 0.0f, 0.0f),
        new Vector3(3.0f, 3.0f, 0.0f),
        new Vector3(1.0f, 2.0f, 0.0f),
        Vector3.zero
    };

            LineConf malformed = LineConf.GetLine(line);

            malformed.ID = "MalformedExplicitlyClosedLine";
            malformed.RendererPositions = positions;
            malformed.OriginalStartAnchor = positions[0];
            malformed.OriginalEndAnchor = positions[^1];
            malformed.Loop = true;
            malformed.UsePreviousSegmentForEndCap = false;
            malformed.LineCapStart = LineCapConf.CreateNone();

            malformed.LineCapEnd =
                GameLineCapConfiguration.CreateLineCapConf(
                    malformed,
                    LineCapConf.CreateNone(),
                    LineCap.Arrowhead);

            GameObject restored =
                GameLineDrawer.ReDrawLine(surface, malformed);

            LineConf result = LineConf.GetLine(restored);

            Assert.That(result.Loop, Is.False);

            Assert.That(
                GameLineGeometry.GetOriginalLinePositions(restored),
                Is.EqualTo(positions));

            Assert.That(
                result.OriginalStartAnchor,
                Is.EqualTo(positions[0]));

            Assert.That(
                result.OriginalEndAnchor,
                Is.EqualTo(positions[^1]));

            Assert.That(
                result.LineCapEnd.CapKind,
                Is.EqualTo(LineCap.Arrowhead));

            LineRenderer renderer =
                restored.GetComponent<LineRenderer>();

            Assert.That(renderer.loop, Is.False);
            Assert.That(renderer.positionCount, Is.EqualTo(positions.Length));

            // The end cap shortens the visible endpoint.
            // This must not change the original logical endpoint.
            Assert.That(
                renderer.GetPosition(positions.Length - 1),
                Is.Not.EqualTo(positions[^1]));

            Assert.That(
                result.RendererPositions,
                Has.Length.EqualTo(positions.Length));

            List<GameObject> caps =
                GameLineCapRenderer.GetLineCapObjects(restored, false);

            Assert.That(caps, Has.Count.EqualTo(1));

            Transform cap = caps[0].transform;

            Assert.That(
                Vector3.Distance(cap.localPosition, positions[0]),
                Is.LessThan(0.0001f));

            Assert.That(
                Vector3.Angle(
                    cap.localRotation * Vector3.right,
                    positions[^1] - positions[^2]),
                Is.LessThan(0.01f));

            LineConf normalized = LineConf.GetLine(restored);
            normalized.ID = "NormalizedExplicitlyClosedLine";

            GameObject reloaded =
                GameLineDrawer.ReDrawLine(surface, normalized);

            LineConf reloadedConf = LineConf.GetLine(reloaded);

            Assert.That(reloadedConf.Loop, Is.False);

            Assert.That(
                GameLineGeometry.GetOriginalLinePositions(reloaded),
                Is.EqualTo(positions));

            Assert.That(
                reloadedConf.OriginalStartAnchor,
                Is.EqualTo(positions[0]));

            Assert.That(
                reloadedConf.OriginalEndAnchor,
                Is.EqualTo(positions[^1]));

            Assert.That(
                reloadedConf.LineCapEnd.CapKind,
                Is.EqualTo(LineCap.Arrowhead));

            Assert.That(
                reloaded.GetComponent<LineRenderer>().loop,
                Is.False);

            Assert.That(
                GameLineCapRenderer.GetLineCapObjects(reloaded, false),
                Has.Count.EqualTo(1));
        }

        /// <summary>
        /// Verifies that preserving an end cap on a three-point
        /// segment opened from a loop does not invalidate its start cap.
        /// Both caps must use their respective original or corrected
        /// directions while all logical positions remain unchanged.
        /// </summary>
        [Test]
        public void TestOpenedThreePointLoopSegmentPreservesBothCaps()
        {
            Vector3[] positions =
            {
        new Vector3(3.0f, 3.0f, 0.0f),
        new Vector3(0.02f, 0.0f, 0.0f),
        Vector3.zero
    };

            PrepareGeometry(positions);

            LineAnchorValueHolder anchorHolder =
                line.GetComponent<LineAnchorValueHolder>();

            anchorHolder.UsePreviousSegmentForEndCap = true;

            LineConf configuration = LineConf.GetLine(line);

            LineCapConf startCap =
                GameLineCapConfiguration.CreateLineCapConf(
                    configuration,
                    LineCapConf.CreateNone(),
                    LineCap.Arrowhead);

            LineCapConf endCap =
                GameLineCapConfiguration.CreateLineCapConf(
                    configuration,
                    LineCapConf.CreateNone(),
                    LineCap.Arrowhead);

            GameLineCapApplicator.ApplyLineCaps(
                line,
                startCap,
                endCap,
                useCapConfVisuals: true);

            LineConf result = LineConf.GetLine(line);

            Assert.That(result.Loop, Is.False);
            Assert.That(result.UsePreviousSegmentForEndCap, Is.True);

            Assert.That(
                GameLineGeometry.GetOriginalLinePositions(line),
                Is.EqualTo(positions));

            Assert.That(
                result.LineCapStart.CapKind,
                Is.EqualTo(LineCap.Arrowhead));

            Assert.That(
                result.LineCapEnd.CapKind,
                Is.EqualTo(LineCap.Arrowhead));

            List<GameObject> startCaps =
                GameLineCapRenderer.GetLineCapObjects(line, true);

            List<GameObject> endCaps =
                GameLineCapRenderer.GetLineCapObjects(line, false);

            Assert.That(
                startCaps,
                Has.Count.EqualTo(1),
                "The end-cap fallback must not suppress the start cap.");

            Assert.That(
                endCaps,
                Has.Count.EqualTo(1),
                "The original end cap must remain visible.");

            Vector3 startDirection =
                startCaps[0].transform.localRotation * Vector3.right;

            Vector3 endDirection =
                endCaps[0].transform.localRotation * Vector3.right;

            Assert.That(
                Vector3.Angle(
                    startDirection,
                    positions[0] - positions[1]),
                Is.LessThan(0.01f));

            Assert.That(
                Vector3.Angle(
                    endDirection,
                    positions[2] - positions[0]),
                Is.LessThan(0.01f));

            LineRenderer renderer =
                line.GetComponent<LineRenderer>();

            Assert.That(renderer.loop, Is.False);
            Assert.That(renderer.positionCount, Is.EqualTo(positions.Length));
        }
    }
}
