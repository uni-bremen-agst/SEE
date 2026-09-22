using NUnit.Framework;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// Tests fill-out management for drawable lines.
    /// </summary>
    [TestFixture]
    public class TestGameLineFillOut
    {
        /// <summary>
        /// The temporary shape used by the tests.
        /// </summary>
        private GameObject shape;

        /// <summary>
        /// Creates the temporary shape.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            shape = new GameObject("GameLineFillOutTest");
        }

        /// <summary>
        /// Destroys the temporary shape.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(shape);
        }

        /// <summary>
        /// Verifies that null has no fill-out object.
        /// </summary>
        [Test]
        public void TestGetOwnFillOutObjectWithNull()
        {
            Assert.That(
                GameLineFillOut.GetOwnFillOutObject(null),
                Is.Null);
        }

        /// <summary>
        /// Verifies that the direct fill-out child is returned.
        /// </summary>
        [Test]
        public void TestGetOwnFillOutObjectReturnsDirectChild()
        {
            GameObject fillOut =
                new GameObject(ValueHolder.FillOut);

            fillOut.transform.SetParent(shape.transform);

            Assert.That(
                GameLineFillOut.GetOwnFillOutObject(shape),
                Is.SameAs(fillOut));
        }

        /// <summary>
        /// Verifies that nested fill-out objects are not treated as the
        /// shape's own fill-out object.
        /// </summary>
        [Test]
        public void TestGetOwnFillOutObjectIgnoresNestedChild()
        {
            GameObject child =
                new GameObject("Child");

            child.transform.SetParent(shape.transform);

            GameObject fillOut =
                new GameObject(ValueHolder.FillOut);

            fillOut.transform.SetParent(child.transform);

            Assert.That(
                GameLineFillOut.GetOwnFillOutObject(shape),
                Is.Null);
        }
    }
}
