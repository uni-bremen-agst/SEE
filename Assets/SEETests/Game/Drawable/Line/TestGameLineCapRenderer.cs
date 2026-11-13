using NUnit.Framework;
using UnityEngine;

namespace SEE.Game.Drawable.Line
{
    /// <summary>
    /// Tests management of generated line-cap objects.
    /// </summary>
    [TestFixture]
    public class TestGameLineCapRenderer
    {
        /// <summary>
        /// The parent line used by the tests.
        /// </summary>
        private GameObject line;

        /// <summary>
        /// Creates a temporary line object.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            line = new GameObject(ValueHolder.LinePrefix + "42");
        }

        /// <summary>
        /// Destroys the temporary line object.
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
        /// Verifies that start-cap objects are returned independently
        /// from end-cap objects.
        /// </summary>
        [Test]
        public void TestGetLineCapObjectsDistinguishesStartAndEndCaps()
        {
            GameObject startCap =
                CreateChild(ValueHolder.LineStartCapPrefix + "42_0");

            GameObject endCap =
                CreateChild(ValueHolder.LineEndCapPrefix + "42_0");

            Assert.That(
                GameLineCapRenderer.GetLineCapObjects(line, true),
                Is.EquivalentTo(new[] { startCap }));

            Assert.That(
                GameLineCapRenderer.GetLineCapObjects(line, false),
                Is.EquivalentTo(new[] { endCap }));
        }

        /// <summary>
        /// Verifies that generated start and end caps are removed while
        /// unrelated child objects remain untouched.
        /// </summary>
        [Test]
        public void TestRemoveLineCapsKeepsUnrelatedChildren()
        {
            GameObject startCap =
                CreateChild(ValueHolder.LineStartCapPrefix + "42_0");

            GameObject endCap =
                CreateChild(ValueHolder.LineEndCapPrefix + "42_0");

            GameObject unrelated =
                CreateChild("UnrelatedChild");

            GameLineCapRenderer.RemoveLineCaps(line);

            Assert.That(startCap == null, Is.True);
            Assert.That(endCap == null, Is.True);
            Assert.That(unrelated == null, Is.False);
            Assert.That(unrelated.transform.parent, Is.SameAs(line.transform));
        }

        /// <summary>
        /// Creates a child object below the test line.
        /// </summary>
        /// <param name="name">The child object's name.</param>
        /// <returns>The created child object.</returns>
        private GameObject CreateChild(string name)
        {
            GameObject child = new(name);
            child.transform.SetParent(line.transform);

            return child;
        }
    }
}
