using System;
using NUnit.Framework;
using SEE.Extensions;
using SEE.Game;
using SEE.GraphElementRefs;
using SEE.SceneManipulation;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.ReversibleActions
{
    /// <summary>
    /// Tests marking geometry, action history, and independent action instances.
    /// </summary>
    internal class TestMarkAction
    {
        /// <summary>
        /// Node registered in the graph-element map for the current test.
        /// </summary>
        private GameObject node;

        /// <summary>
        /// Optional parent used to exercise city scaling.
        /// </summary>
        private GameObject parent;

        [SetUp]
        public void SetUp()
        {
            node = CreateNode(Guid.NewGuid().ToString());
            parent = null;
        }

        [TearDown]
        public void TearDown()
        {
            Destroyer.Destroy(parent != null ? parent : node);
        }

        /// <summary>
        /// Creates a node with a unique ID in the graph-element map.
        /// </summary>
        /// <param name="id">ID of the node to create.</param>
        /// <returns>The registered game node.</returns>
        private static GameObject CreateNode(string id)
        {
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.name = id;
            result.tag = Tags.Node;
            GraphElementIDMap.Add(result);
            return result;
        }

        [TestCase(4.0f, 2.0f)]
        [TestCase(2.0f, 4.0f)]
        [TestCase(3.0f, 3.0f)]
        public void SphereFitsNodeAndTouchesRoof(float width, float depth)
        {
            node.transform.position = new Vector3(5.0f, 2.0f, -7.0f);
            node.transform.localScale = new Vector3(width, 3.0f, depth);
            Physics.SyncTransforms();

            MarkAction action = new MarkAction();
            action.ToggleMark(node);

            Bounds bounds = node.transform.Find(GameNodeMarker.MarkerName).GetComponent<Renderer>().bounds;
            float diameter = Mathf.Min(width, depth);
            Assert.That(bounds.size.x, Is.EqualTo(diameter).Within(0.0001f));
            Assert.That(bounds.size.y, Is.EqualTo(diameter).Within(0.0001f));
            Assert.That(bounds.size.z, Is.EqualTo(diameter).Within(0.0001f));
            Assert.That(bounds.center.x, Is.EqualTo(5.0f).Within(0.0001f));
            Assert.That(bounds.center.z, Is.EqualTo(-7.0f).Within(0.0001f));
            Assert.That(bounds.min.y, Is.EqualTo(3.5f).Within(0.0001f));
        }

        [Test]
        public void SphereRemainsSphericalUnderNonUniformParentScale()
        {
            parent = new GameObject("Scaled city");
            parent.transform.position = new Vector3(3.0f, 2.0f, 1.0f);
            parent.transform.localScale = new Vector3(2.0f, 3.0f, 0.5f);
            node.transform.SetParent(parent.transform, worldPositionStays: false);
            node.transform.localScale = new Vector3(3.0f, 2.0f, 8.0f);
            Physics.SyncTransforms();

            GameNodeMarker.SetMarked(node, true);

            Bounds bounds = node.transform.Find(GameNodeMarker.MarkerName).GetComponent<Renderer>().bounds;
            Assert.That(bounds.size.x, Is.EqualTo(4.0f).Within(0.0001f));
            Assert.That(bounds.size.y, Is.EqualTo(4.0f).Within(0.0001f));
            Assert.That(bounds.size.z, Is.EqualTo(4.0f).Within(0.0001f));
            Assert.That(bounds.min.y, Is.EqualTo(5.0f).Within(0.0001f));
        }

        [Test]
        public void MarkingClearsRoofDecoration()
        {
            GameObject antenna = GameObject.CreatePrimitive(PrimitiveType.Cube);
            antenna.transform.SetParent(node.transform, worldPositionStays: false);
            antenna.transform.localPosition = Vector3.up;
            antenna.transform.localScale = new Vector3(0.1f, 1.0f, 0.1f);
            Physics.SyncTransforms();

            GameNodeMarker.SetMarked(node, true);

            Bounds bounds = node.transform.Find(GameNodeMarker.MarkerName).GetComponent<Renderer>().bounds;
            Assert.That(bounds.min.y, Is.EqualTo(1.5f).Within(0.0001f));
        }

        [Test]
        public void InnerNodeMarkingIgnoresDescendantMarkings()
        {
            GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.transform.SetParent(node.transform, worldPositionStays: false);
            child.transform.localPosition = Vector3.up;
            child.transform.localScale = new Vector3(0.5f, 1.0f, 0.5f);
            Physics.SyncTransforms();
            GameNodeMarker.SetMarked(child, true);

            GameNodeMarker.SetMarked(node, true);

            Bounds bounds = node.transform.Find(GameNodeMarker.MarkerName).GetComponent<Renderer>().bounds;
            Assert.That(bounds.min.y, Is.EqualTo(1.5f).Within(0.0001f));
            GameNodeMarker.SetMarked(node, false);
            Assert.That(GameNodeMarker.IsMarked(child), Is.True);
        }

        [Test]
        public void RepeatedRequestsDoNotDuplicateMarkersOrChangeTheNodeMaterial()
        {
            Material original = node.GetComponent<Renderer>().sharedMaterial;
            GameNodeMarker.SetMarked(node, true);
            Transform marker = node.transform.Find(GameNodeMarker.MarkerName);

            GameNodeMarker.SetMarked(node, true);

            Assert.That(node.transform.childCount, Is.EqualTo(1));
            Assert.That(node.transform.Find(GameNodeMarker.MarkerName), Is.EqualTo(marker));
            Assert.That(marker.GetComponent<Collider>(), Is.Null);
            Assert.That(marker.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("Ignore Raycast")));
            Assert.That(node.GetComponent<Renderer>().sharedMaterial, Is.SameAs(original));

            GameNodeMarker.SetMarked(node, false);
            GameNodeMarker.SetMarked(node, false);
            Assert.That(GameNodeMarker.IsMarked(node), Is.False);
            Assert.That(node.transform.childCount, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UndoAndRedoRestoreBothInitialStates(bool initiallyMarked)
        {
            GameNodeMarker.SetMarked(node, initiallyMarked);
            MarkAction action = new MarkAction();

            action.ToggleMark(node);

            Assert.That(GameNodeMarker.IsMarked(node), Is.EqualTo(!initiallyMarked));
            Assert.That(action.CurrentProgress(), Is.EqualTo(IReversibleAction.Progress.Completed));
            Assert.That(action.GetChangedObjects(), Is.EquivalentTo(new[] { node.name }));

            action.Undo();
            Assert.That(GameNodeMarker.IsMarked(node), Is.EqualTo(initiallyMarked));
            action.Redo();
            Assert.That(GameNodeMarker.IsMarked(node), Is.EqualTo(!initiallyMarked));
        }

        [Test]
        public void HistoryResolvesRecreatedNodeByID()
        {
            MarkAction action = new MarkAction();
            action.ToggleMark(node);
            string id = node.name;
            Destroyer.Destroy(node);
            node = CreateNode(id);
            GameNodeMarker.SetMarked(node, true);

            action.Undo();
            Assert.That(GameNodeMarker.IsMarked(node), Is.False);
            action.Redo();
            Assert.That(GameNodeMarker.IsMarked(node), Is.True);
        }

        [Test]
        public void NewInstanceHasIndependentHistoryAndCorrectMenuEntry()
        {
            MarkAction action = new MarkAction();
            action.ToggleMark(node);

            IReversibleAction next = action.NewInstance();

            Assert.That(next, Is.TypeOf<MarkAction>());
            Assert.That(next.GetId(), Is.Not.EqualTo(action.GetId()));
            Assert.That(next.GetChangedObjects(), Is.Empty);
            Assert.That(next.CurrentProgress(), Is.EqualTo(IReversibleAction.Progress.NoEffect));
            Assert.That(next.GetActionStateType(), Is.SameAs(ActionStateTypes.Mark));
        }
    }
}
