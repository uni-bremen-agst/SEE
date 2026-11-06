using NUnit.Framework;
using SEE.Game.Drawable.ValueHolders;
using UnityEngine;

namespace SEE.Game.Drawable.Line
{
    /// <summary>
    /// Tests the geometry-related functionality for drawable lines.
    /// </summary>
    [TestFixture]
    public class TestGameLineGeometry
    {
        /// <summary>
        /// The temporary line object used by the tests.
        /// </summary>
        private GameObject line;

        /// <summary>
        /// Creates the temporary line object.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            line = new GameObject("GameLineGeometryTest");
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
        /// Verifies that duplicate positions are counted only once.
        /// </summary>
        [Test]
        public void TestDifferentPositionCounter()
        {
            Vector3[] positions =
            {
                new(1.0f, 2.0f, 0.0f),
                new(3.0f, 4.0f, 0.0f),
                new(1.0f, 2.0f, 0.0f),
                new(5.0f, 6.0f, 0.0f)
            };

            Assert.That(
                GameLineGeometry.DifferentPositionCounter(positions),
                Is.EqualTo(3));
        }

        /// <summary>
        /// Verifies that updating z positions preserves x and y while setting z to zero.
        /// </summary>
        [Test]
        public void TestUpdateZPositions()
        {
            Vector3[] positions =
            {
                new(1.0f, 2.0f, 3.0f),
                new(4.0f, 5.0f, -6.0f)
            };

            GameLineGeometry.UpdateZPositions(ref positions);

            Assert.That(
                positions[0],
                Is.EqualTo(new Vector3(1.0f, 2.0f, 0.0f)));

            Assert.That(
                positions[1],
                Is.EqualTo(new Vector3(4.0f, 5.0f, 0.0f)));
        }

        /// <summary>
        /// Verifies that updating original anchors creates and initializes
        /// a line-anchor value holder.
        /// </summary>
        [Test]
        public void TestUpdateOriginalAnchorsCreatesHolder()
        {
            Vector3[] positions =
            {
                new(1.0f, 2.0f, 3.0f),
                new(4.0f, 5.0f, 6.0f),
                new(7.0f, 8.0f, 9.0f)
            };

            GameLineGeometry.UpdateOriginalAnchors(
                line,
                positions);

            LineAnchorValueHolder holder =
                line.GetComponent<LineAnchorValueHolder>();

            Assert.That(holder, Is.Not.Null);
            Assert.That(holder.HasOriginalAnchors, Is.True);

            Assert.That(
                holder.OriginalStartAnchor,
                Is.EqualTo(new Vector3(1.0f, 2.0f, 0.0f)));

            Assert.That(
                holder.OriginalEndAnchor,
                Is.EqualTo(new Vector3(7.0f, 8.0f, 0.0f)));
        }

        /// <summary>
        /// Verifies that existing original anchors are overwritten when new
        /// line positions are supplied.
        /// </summary>
        [Test]
        public void TestUpdateOriginalAnchorsOverwritesExistingValues()
        {
            LineAnchorValueHolder holder =
                line.AddComponent<LineAnchorValueHolder>();

            holder.OriginalStartAnchor =
                new Vector3(10.0f, 10.0f, 10.0f);

            holder.OriginalEndAnchor =
                new Vector3(20.0f, 20.0f, 20.0f);

            Vector3[] positions =
            {
                new(-1.0f, -2.0f, 3.0f),
                new(4.0f, 5.0f, 6.0f)
            };

            GameLineGeometry.UpdateOriginalAnchors(
                line,
                positions);

            Assert.That(
                holder.OriginalStartAnchor,
                Is.EqualTo(new Vector3(-1.0f, -2.0f, 0.0f)));

            Assert.That(
                holder.OriginalEndAnchor,
                Is.EqualTo(new Vector3(4.0f, 5.0f, 0.0f)));

            Assert.That(holder.HasOriginalAnchors, Is.True);
        }

        /// <summary>
        /// Verifies that insufficient position data does not create an anchor holder.
        /// </summary>
        [Test]
        public void TestUpdateOriginalAnchorsIgnoresInsufficientPositions()
        {
            Vector3[] positions =
            {
                new(1.0f, 2.0f, 3.0f)
            };

            GameLineGeometry.UpdateOriginalAnchors(
                line,
                positions);

            Assert.That(
                line.GetComponent<LineAnchorValueHolder>(),
                Is.Null);
        }
    }
}
