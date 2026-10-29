using System;
using NUnit.Framework;
using UnityEngine;
using SEE.Extensions;
using SEE.Utils;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Tests the geometry and lifecycle of node markings.
    /// </summary>
    public class TestGameNodeMarker
    {
        /// <summary>
        /// Root of all objects created by a test.
        /// </summary>
        private GameObject root;

        /// <summary>
        /// Creates a fresh hierarchy for each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Marker test root");
        }

        /// <summary>
        /// Removes all objects created by a test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Destroyer.Destroy(root);
        }

        /// <summary>
        /// Checks the diameter, center, and roof contact under nonuniform parent scaling.
        /// </summary>
        /// <param name="width">Local width of the node.</param>
        /// <param name="depth">Local depth of the node.</param>
        [TestCase(2f, 5f)]
        [TestCase(5f, 1f)]
        public void SphereFitsNode(float width, float depth)
        {
            root.transform.position = new Vector3(3f, 4f, 5f);
            root.transform.localScale = new Vector3(2f, 3f, 4f);
            GameObject node = CreateBlock(root.transform, new Vector3(width, 2f, depth));
            Physics.SyncTransforms();
            node.WorldSpaceSize(out Vector3 size, out Vector3 center);
            float diameter = Mathf.Min(size.x, size.z);
            float roof = node.GetTop().y;

            GameNodeMarker.SetMarked(node, true);

            Assert.That(GameNodeMarker.IsMarked(node), Is.True);
            Assert.That(node.transform.childCount, Is.EqualTo(1));
            GameObject sphere = node.transform.GetChild(0).gameObject;
            Bounds bounds = sphere.GetComponent<Renderer>().bounds;
            Assert.That(bounds.size.x, Is.EqualTo(diameter).Within(0.0001f));
            Assert.That(bounds.size.y, Is.EqualTo(diameter).Within(0.0001f));
            Assert.That(bounds.size.z, Is.EqualTo(diameter).Within(0.0001f));
            Assert.That(bounds.center.x, Is.EqualTo(center.x).Within(0.0001f));
            Assert.That(bounds.center.z, Is.EqualTo(center.z).Within(0.0001f));
            Assert.That(bounds.min.y, Is.EqualTo(roof).Within(0.0001f));
            Collider collider = sphere.GetComponent<Collider>();
            Assert.That(collider == null || !collider.enabled, Is.True);
        }

        /// <summary>
        /// Repeated state assignments neither duplicate markings nor prevent recreation.
        /// </summary>
        [Test]
        public void RepeatedStatesAndImmediateRecreation()
        {
            GameObject node = CreateBlock(root.transform, Vector3.one);
            Physics.SyncTransforms();
            Assert.That(GameNodeMarker.IsMarked(node), Is.False);
            GameNodeMarker.SetMarked(node, false);
            GameNodeMarker.SetMarked(node, true);
            Transform firstSphere = node.transform.GetChild(0);
            GameNodeMarker.SetMarked(node, true);
            Assert.That(node.transform.childCount, Is.EqualTo(1));
            Assert.That(node.transform.GetChild(0), Is.EqualTo(firstSphere));

            GameNodeMarker.SetMarked(node, false);
            Assert.That(GameNodeMarker.IsMarked(node), Is.False);
            Assert.That(node.transform.childCount, Is.Zero);
            GameNodeMarker.SetMarked(node, false);
            GameNodeMarker.SetMarked(node, true);
            Assert.That(GameNodeMarker.IsMarked(node), Is.True);
            Assert.That(node.transform.childCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Inner nodes account for decorations but ignore markings on their children.
        /// </summary>
        [Test]
        public void InnerNodeRoofIncludesDecorationButExcludesChildMarking()
        {
            GameObject node = CreateBlock(root.transform, new Vector3(4f, 1f, 4f));
            GameObject child = CreateBlock(node.transform, Vector3.one);
            child.transform.position = new Vector3(0f, 2f, 0f);
            GameObject antenna = CreateBlock(child.transform, new Vector3(0.1f, 2f, 0.1f));
            antenna.transform.position = new Vector3(0f, 4f, 0f);
            Physics.SyncTransforms();
            float roof = node.GetTop().y;
            GameNodeMarker.SetMarked(child, true);
            Assert.That(GameNodeMarker.IsMarked(node), Is.False);

            GameNodeMarker.SetMarked(node, true);

            Bounds sphere = node.transform.GetChild(1).GetComponent<Renderer>().bounds;
            Assert.That(sphere.min.y, Is.EqualTo(roof).Within(0.0001f));
            GameNodeMarker.SetMarked(node, false);
            Assert.That(GameNodeMarker.IsMarked(child), Is.True);
            Assert.That(antenna != null, Is.True);
        }

        /// <summary>
        /// Missing nodes are rejected explicitly.
        /// </summary>
        [Test]
        public void NullNodeIsRejected()
        {
            Assert.That(() => GameNodeMarker.IsMarked(null), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => GameNodeMarker.SetMarked(null, true), Throws.TypeOf<ArgumentNullException>());
        }

        /// <summary>
        /// Creates a block in the test hierarchy.
        /// </summary>
        /// <param name="parent">Parent of the new block.</param>
        /// <param name="size">Local dimensions of the block.</param>
        /// <returns>The new block.</returns>
        private static GameObject CreateBlock(Transform parent, Vector3 size)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.transform.SetParent(parent, false);
            block.transform.localScale = size;
            return block;
        }
    }
}
