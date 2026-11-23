
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Drawable.Line
{
    /// <summary>
    /// Tests the logical geometry and cap ownership of segments
    /// produced by editing closed drawable lines.
    /// </summary>
    [TestFixture]
    public class TestGameLineSplitLoopGeometry
    {
        /// <summary>
        /// The first logical point of the test loop.
        /// </summary>
        private static readonly Vector3 A = new(0.0f, 0.0f, 0.0f);

        /// <summary>
        /// The second logical point of the test loop.
        /// </summary>
        private static readonly Vector3 B = new(2.0f, 3.0f, 0.0f);

        /// <summary>
        /// The third logical point of the test loop.
        /// </summary>
        private static readonly Vector3 C = new(4.0f, 0.0f, 0.0f);

        /// <summary>
        /// The fourth logical point of the test loop.
        /// </summary>
        private static readonly Vector3 D = new(2.0f, -1.0f, 0.0f);

        /// <summary>
        /// Returns a loop with four logical positions.
        /// The closing point is deliberately not duplicated.
        /// </summary>
        /// <returns>The original logical positions.</returns>
        private static List<Vector3> GetPositions()
        {
            return new List<Vector3> { A, B, C, D };
        }

        /// <summary>
        /// Verifies the geometry and ownership of both original caps.
        /// </summary>
        /// <param name="segment">The resulting segment.</param>
        /// <param name="startCap">Expected start-cap ownership.</param>
        /// <param name="endCap">Expected end-cap ownership.</param>
        /// <param name="expected">The expected ordered positions.</param>
        private static void AssertSegment(
            GameLineSplit.LoopSegment segment,
            bool startCap,
            bool endCap,
            params Vector3[] expected)
        {
            Assert.That(segment.Positions, Is.EqualTo(expected));
            Assert.That(segment.RetainsStartCap, Is.EqualTo(startCap));
            Assert.That(segment.RetainsEndCap, Is.EqualTo(endCap));
        }

        /// <summary>
        /// Splitting at C must preserve both sides of the loop.
        /// </summary>
        [Test]
        public void TestSplitAtC()
        {
            List<GameLineSplit.LoopSegment> segments =
                GameLineSplit.GetLoopSegments(
                    GetPositions(),
                    new[] { 2 },
                    GameLineSplit.LoopEditOperation.SplitPoint);

            Assert.That(segments, Has.Count.EqualTo(2));

            AssertSegment(segments[0], true, false, A, B, C);
            AssertSegment(segments[1], false, true, C, D, A);
        }

        /// <summary>
        /// Splitting at the shared start/end anchor opens the loop
        /// without removing any connection.
        /// </summary>
        [Test]
        public void TestSplitAtA()
        {
            List<GameLineSplit.LoopSegment> segments =
                GameLineSplit.GetLoopSegments(
                    GetPositions(),
                    new[] { 0 },
                    GameLineSplit.LoopEditOperation.SplitPoint);

            Assert.That(segments, Has.Count.EqualTo(1));

            AssertSegment(segments[0], true, true, A, B, C, D, A);
        }

        /// <summary>
        /// Multiple split points must produce ordered segments
        /// without dropping the closing connection.
        /// </summary>
        [Test]
        public void TestSplitAtBAndD()
        {
            List<GameLineSplit.LoopSegment> segments =
                GameLineSplit.GetLoopSegments(
                    GetPositions(),
                    new[] { 3, 1 },
                    GameLineSplit.LoopEditOperation.SplitPoint);

            Assert.That(segments, Has.Count.EqualTo(3));

            AssertSegment(segments[0], true, false, A, B);
            AssertSegment(segments[1], false, false, B, C, D);
            AssertSegment(segments[2], false, true, D, A);
        }

        /// <summary>
        /// Removing C must preserve the two disconnected
        /// remaining paths, including the closing connection.
        /// </summary>
        [Test]
        public void TestErasePointC()
        {
            List<GameLineSplit.LoopSegment> segments =
                GameLineSplit.GetLoopSegments(
                    GetPositions(),
                    new[] { 2 },
                    GameLineSplit.LoopEditOperation.ErasePoint);

            Assert.That(segments, Has.Count.EqualTo(2));

            AssertSegment(segments[0], true, false, A, B);
            AssertSegment(segments[1], false, true, D, A);
        }

        /// <summary>
        /// Removing A must remove both appearances of the
        /// shared start/end anchor and both original caps.
        /// </summary>
        [Test]
        public void TestErasePointA()
        {
            List<GameLineSplit.LoopSegment> segments =
                GameLineSplit.GetLoopSegments(
                    GetPositions(),
                    new[] { 0 },
                    GameLineSplit.LoopEditOperation.ErasePoint);

            Assert.That(segments, Has.Count.EqualTo(1));

            AssertSegment(segments[0], false, false, B, C, D);
        }

        /// <summary>
        /// Removing D must not remove A, B, or C.
        /// The end cap loses its incoming closing connection.
        /// </summary>
        [Test]
        public void TestErasePointD()
        {
            List<GameLineSplit.LoopSegment> segments =
                GameLineSplit.GetLoopSegments(
                    GetPositions(),
                    new[] { 3 },
                    GameLineSplit.LoopEditOperation.ErasePoint);

            Assert.That(segments, Has.Count.EqualTo(1));

            AssertSegment(segments[0], true, false, A, B, C);
        }

        /// <summary>
        /// Removing connection C-D must retain the path
        /// D-A and the original end cap.
        /// </summary>
        [Test]
        public void TestEraseConnectionCD()
        {
            List<GameLineSplit.LoopSegment> segments =
                GameLineSplit.GetLoopSegments(
                    GetPositions(),
                    new[] { 2 },
                    GameLineSplit.LoopEditOperation.EraseConnection);

            Assert.That(segments, Has.Count.EqualTo(2));

            AssertSegment(segments[0], true, false, A, B, C);
            AssertSegment(segments[1], false, true, D, A);
        }

        /// <summary>
        /// Removing the closing connection D-A must produce
        /// one open line with no original end cap.
        /// </summary>
        [Test]
        public void TestEraseClosingConnection()
        {
            List<GameLineSplit.LoopSegment> segments =
                GameLineSplit.GetLoopSegments(
                    GetPositions(),
                    new[] { 3 },
                    GameLineSplit.LoopEditOperation.EraseConnection);

            Assert.That(segments, Has.Count.EqualTo(1));

            AssertSegment(segments[0], true, false, A, B, C, D);
        }

        /// <summary>
        /// Removing connection A-B must preserve the incoming
        /// closing connection and its original end cap.
        /// </summary>
        [Test]
        public void TestEraseConnectionAB()
        {
            List<GameLineSplit.LoopSegment> segments =
                GameLineSplit.GetLoopSegments(
                    GetPositions(),
                    new[] { 0 },
                    GameLineSplit.LoopEditOperation.EraseConnection);

            Assert.That(segments, Has.Count.EqualTo(1));

            AssertSegment(segments[0], false, true, B, C, D, A);
        }

        /// <summary>
        /// Duplicate selected indices must not produce
        /// duplicate segments or remove additional points.
        /// </summary>
        [Test]
        public void TestDuplicateSelectedIndices()
        {
            List<GameLineSplit.LoopSegment> segments =
                GameLineSplit.GetLoopSegments(
                    GetPositions(),
                    new[] { 2, 2 },
                    GameLineSplit.LoopEditOperation.ErasePoint);

            Assert.That(segments, Has.Count.EqualTo(2));

            AssertSegment(segments[0], true, false, A, B);
            AssertSegment(segments[1], false, true, D, A);
        }

        /// <summary>
        /// Invalid logical indices must be rejected explicitly.
        /// </summary>
        [Test]
        public void TestInvalidLogicalIndex()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GameLineSplit.GetLoopSegments(
                    GetPositions(),
                    new[] { 4 },
                    GameLineSplit.LoopEditOperation.SplitPoint));
        }


        /// <summary>
        /// An explicitly repeated closing point must not be appended twice
        /// when the line is split.
        /// </summary>
        [Test]
        public void TestSplitWithExplicitClosingPoint()
        {
            List<Vector3> positions = new() { A, B, C, D, A };

            List<GameLineSplit.LoopSegment> segments =
                GameLineSplit.GetLoopSegments(
                    positions,
                    new[] { 2 },
                    GameLineSplit.LoopEditOperation.SplitPoint);

            Assert.That(segments, Has.Count.EqualTo(2));
            AssertSegment(segments[0], true, false, A, B, C);
            AssertSegment(segments[1], false, true, C, D, A);
        }

        /// <summary>
        /// Removing D from an explicitly closed loop must not create
        /// an additional degenerate A-A segment.
        /// </summary>
        [Test]
        public void TestErasePointWithExplicitClosingPoint()
        {
            List<Vector3> positions = new() { A, B, C, D, A };

            List<GameLineSplit.LoopSegment> segments =
                GameLineSplit.GetLoopSegments(
                    positions,
                    new[] { 3 },
                    GameLineSplit.LoopEditOperation.ErasePoint);

            Assert.That(segments, Has.Count.EqualTo(1));
            AssertSegment(segments[0], true, false, A, B, C);
        }

        /// <summary>
        /// The repeated closing position must refer to the first logical
        /// point when that point is erased.
        /// </summary>
        [Test]
        public void TestEraseRepeatedClosingPoint()
        {
            List<Vector3> positions = new() { A, B, C, D, A };

            List<GameLineSplit.LoopSegment> segments =
                GameLineSplit.GetLoopSegments(
                    positions,
                    new[] { 4 },
                    GameLineSplit.LoopEditOperation.ErasePoint);

            Assert.That(segments, Has.Count.EqualTo(1));
            AssertSegment(segments[0], false, false, B, C, D);
        }

        /// <summary>
        /// Selecting the repeated closing point for connection erasure
        /// must remove the closing connection, not the first connection.
        /// </summary>
        [Test]
        public void TestEraseClosingConnectionWithExplicitClosingPoint()
        {
            List<Vector3> positions = new() { A, B, C, D, A };

            List<GameLineSplit.LoopSegment> segments =
                GameLineSplit.GetLoopSegments(
                    positions,
                    new[] { 4 },
                    GameLineSplit.LoopEditOperation.EraseConnection);

            Assert.That(segments, Has.Count.EqualTo(1));
            AssertSegment(segments[0], true, false, A, B, C, D);
        }

    }
}
