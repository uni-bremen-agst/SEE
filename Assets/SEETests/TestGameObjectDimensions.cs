using NUnit.Framework;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Tests for <see cref="SEE.GO.GameObjectExtensions"/> regarding dimensions.
    /// </summary>
    internal class TestGameObjectDimensions
    {
        [Test]
        public void TestGetRelativeTop()
        {
            GameObject grandparent = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grandparent.transform.position = new Vector3(1, 2, 3);
            grandparent.transform.localScale = Vector3.one / 2f;

            GameObject parent = GameObject.CreatePrimitive(PrimitiveType.Cube);
            parent.transform.localScale = Vector3.one / 2f;
            parent.transform.SetParent(grandparent.transform);
            parent.transform.localPosition = new Vector3(0, 1, 0);

            GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.transform.localScale = Vector3.one / 2f;
            child.transform.SetParent(parent.transform);
            child.transform.localPosition = new Vector3(0, 1, 0);

            // Child is immediately above parent and both have half the default cube size, i.e., 0.5.
            float heightOfChildren = parent.transform.lossyScale.y
                + child.transform.lossyScale.y;
            Assert.That(heightOfChildren, Is.EqualTo(1.0f));

            // Grandparent has half the default cube size, i.e., 0.5. Extent is half of the size.
            float grandparentExtent = grandparent.transform.lossyScale.y / 2.0f;
            Assert.That(grandparentExtent, Is.EqualTo(0.25f));

            // Grandparent, parent, and child are immediately on top of each other; no gap.
            float height = grandparentExtent + heightOfChildren;
            Assert.That(height, Is.EqualTo(1.25f));

            // The world-space top.
            float expectedTop = grandparent.transform.position.y + height;

            Assert.That(grandparent.GetTop(), Is.EqualTo(new Vector3(1, expectedTop, 3)));
            Assert.That(grandparent.GetRelativeTop(), Is.EqualTo(height));
        }
    }
}
