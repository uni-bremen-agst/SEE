using NUnit.Framework;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// Tests fill-out behavior while line geometry is updated.
    /// </summary>
    [TestFixture]
    public class TestGameDrawerFillOut
    {
        /// <summary>
        /// The line created for the current test.
        /// </summary>
        private GameObject line;

        /// <summary>
        /// Creates a closed line with a fill-out before each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            line = new GameObject("Line")
            {
                tag = Tags.Line
            };

            LineRenderer renderer = line.AddComponent<LineRenderer>();
            renderer.positionCount = 4;
            renderer.SetPositions(GetPositions());

            Assert.That(GameDrawer.FillOut(line, Color.red), Is.True);
        }

        /// <summary>
        /// Destroys the line and its fill-out after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (line != null)
            {
                Object.DestroyImmediate(line);
            }
        }

        /// <summary>
        /// Verifies that the normal drawing behavior disables the fill-out collider.
        /// This is required while a line is still being interactively drawn.
        /// </summary>
        [Test]
        public void TestDrawingDisablesFillOutColliderByDefault()
        {
            MeshCollider collider = GetFillOutCollider();
            collider.enabled = true;

            GameDrawer.Drawing(line, GetPositions(), Color.red);

            Assert.That(collider.enabled, Is.False);
        }

        /// <summary>
        /// Verifies that an enabled fill-out collider remains enabled when its
        /// current state should be preserved.
        /// </summary>
        [Test]
        public void TestDrawingPreservesEnabledFillOutCollider()
        {
            MeshCollider collider = GetFillOutCollider();
            collider.enabled = true;

            GameDrawer.Drawing(
                line,
                GetPositions(),
                Color.red,
                preserveFillOutColliderState: true);

            Assert.That(collider.enabled, Is.True);
        }

        /// <summary>
        /// Verifies that a disabled fill-out collider remains disabled when its
        /// current state should be preserved.
        /// </summary>
        [Test]
        public void TestDrawingPreservesDisabledFillOutCollider()
        {
            MeshCollider collider = GetFillOutCollider();
            collider.enabled = false;

            GameDrawer.Drawing(
                line,
                GetPositions(),
                Color.red,
                preserveFillOutColliderState: true);

            Assert.That(collider.enabled, Is.False);
        }

        /// <summary>
        /// Returns the fill-out collider of the test line.
        /// </summary>
        /// <returns>The fill-out mesh collider.</returns>
        private MeshCollider GetFillOutCollider()
        {
            GameObject fillOut = GameDrawer.GetOwnFillOutObject(line);

            Assert.That(fillOut, Is.Not.Null);

            MeshCollider collider = fillOut.GetComponent<MeshCollider>();

            Assert.That(collider, Is.Not.Null);
            return collider;
        }

        /// <summary>
        /// Returns the positions used for the test line.
        /// </summary>
        /// <returns>Four distinct positions forming a closed area.</returns>
        private static Vector3[] GetPositions()
        {
            return new[]
            {
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(1.0f, 0.0f, 0.0f),
                new Vector3(1.0f, 1.0f, 0.0f),
                new Vector3(0.0f, 1.0f, 0.0f)
            };
        }
    }
}
