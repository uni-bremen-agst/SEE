using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// Tests the resolution of selectable drawable objects from nested child objects.
    /// </summary>
    [TestFixture]
    public class TestSelector
    {
        /// <summary>
        /// Holds the created objects so that they can be destroyed after each test.
        /// </summary>
        private readonly List<GameObject> objects = new();

        /// <summary>
        /// Destroys all GameObjects created by a test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in objects)
            {
                if (gameObject != null)
                {
                    Object.DestroyImmediate(gameObject);
                }
            }

            objects.Clear();
        }

        /// <summary>
        /// Verifies that a drawable object hit directly resolves to itself.
        /// </summary>
        [Test]
        public void TestDirectDrawableResolvesToItself()
        {
            GameObject line = CreateObject("Line", Tags.Line);

            GameObject result = Selector.ResolveSelectableObject(
                line,
                isDrawableType: true,
                allowSelectViaChild: true);

            Assert.That(result, Is.SameAs(line));
        }

        /// <summary>
        /// Verifies that a direct child of a drawable object resolves to its parent drawable.
        /// </summary>
        [Test]
        public void TestDirectChildResolvesToDrawableParent()
        {
            GameObject line = CreateObject("Line", Tags.Line);
            GameObject fillOut = CreateObject("FillOut");
            fillOut.transform.SetParent(line.transform);

            GameObject result = Selector.ResolveSelectableObject(
                fillOut,
                isDrawableType: true,
                allowSelectViaChild: true);

            Assert.That(result, Is.SameAs(line));
        }

        /// <summary>
        /// Verifies that a fill-out belonging to a line cap resolves through
        /// the line cap to the owning line.
        /// </summary>
        [Test]
        public void TestLineCapFillOutResolvesToOwningLine()
        {
            GameObject line = CreateObject("Line", Tags.Line);
            GameObject lineCap = CreateObject("LineCap", Tags.LineCap);
            GameObject fillOut = CreateObject("FillOut");

            lineCap.transform.SetParent(line.transform);
            fillOut.transform.SetParent(lineCap.transform);

            GameObject result = Selector.ResolveSelectableObject(
                fillOut,
                isDrawableType: true,
                allowSelectViaChild: true);

            Assert.That(result, Is.SameAs(line));
        }

        /// <summary>
        /// Verifies that selecting a line cap itself resolves to its owning line.
        /// </summary>
        [Test]
        public void TestLineCapResolvesToOwningLine()
        {
            GameObject line = CreateObject("Line", Tags.Line);
            GameObject lineCap = CreateObject("LineCap", Tags.LineCap);
            lineCap.transform.SetParent(line.transform);

            GameObject result = Selector.ResolveSelectableObject(
                lineCap,
                isDrawableType: true,
                allowSelectViaChild: true);

            Assert.That(result, Is.SameAs(line));
        }

        /// <summary>
        /// Verifies that disabling child selection keeps the directly hit object.
        /// </summary>
        [Test]
        public void TestDisabledChildSelectionKeepsHitObject()
        {
            GameObject line = CreateObject("Line", Tags.Line);
            GameObject fillOut = CreateObject("FillOut");
            fillOut.transform.SetParent(line.transform);

            GameObject result = Selector.ResolveSelectableObject(
                fillOut,
                isDrawableType: true,
                allowSelectViaChild: false);

            Assert.That(result, Is.SameAs(fillOut));
        }

        /// <summary>
        /// Verifies that selection which does not require a drawable type retains
        /// the previous one-level child-selection behavior.
        /// </summary>
        [Test]
        public void TestNonDrawableTypeSelectionUsesDirectParent()
        {
            GameObject line = CreateObject("Line", Tags.Line);
            GameObject lineCap = CreateObject("LineCap", Tags.LineCap);
            GameObject fillOut = CreateObject("FillOut");

            lineCap.transform.SetParent(line.transform);
            fillOut.transform.SetParent(lineCap.transform);

            GameObject result = Selector.ResolveSelectableObject(
                fillOut,
                isDrawableType: false,
                allowSelectViaChild: true);

            Assert.That(result, Is.SameAs(lineCap));
        }

        /// <summary>
        /// Creates and tracks a GameObject with the given name and optional tag.
        /// </summary>
        /// <param name="name">The name of the created object.</param>
        /// <param name="tag">The tag of the created object.</param>
        /// <returns>The created GameObject.</returns>
        private GameObject CreateObject(string name, string tag = Tags.Untagged)
        {
            GameObject gameObject = new(name)
            {
                tag = tag
            };

            objects.Add(gameObject);
            return gameObject;
        }
    }
}
