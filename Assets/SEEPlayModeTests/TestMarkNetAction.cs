using System;
using System.Collections;
using NUnit.Framework;
using SEE.Game;
using SEE.GraphElementRefs;
using SEE.Net.Actions;
using SEE.Net.Actions.GraphElement;
using SEE.SceneManipulation;
using SEE.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEE.Net
{
    /// <summary>
    /// Tests marking-message serialization, remote execution, and deferred marker destruction.
    /// </summary>
    internal class TestMarkNetAction
    {
        /// <summary>
        /// Network manager required when constructing network actions.
        /// </summary>
        private GameObject networkManagerObject;

        /// <summary>
        /// Node used to apply incoming marking actions.
        /// </summary>
        private GameObject node;

        [SetUp]
        public void SetUp()
        {
            networkManagerObject = new GameObject("Mark test network manager");
            networkManagerObject.AddComponent<NetworkManager>();
            node = GameObject.CreatePrimitive(PrimitiveType.Cube);
            node.name = Guid.NewGuid().ToString();
            node.tag = Tags.Node;
            GraphElementIDMap.Add(node);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Destroyer.Destroy(node);
            Destroyer.Destroy(networkManagerObject);
            yield return null;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SerializationPreservesNodeIDAndMarkingState(bool marked)
        {
            MarkNetAction original = new MarkNetAction(node.name, marked);

            string serialized = ActionSerializer.Serialize(original);
            MarkNetAction restored = (MarkNetAction)ActionSerializer.Deserialize(serialized);

            Assert.That(restored.GraphElementID, Is.EqualTo(node.name));
            Assert.That(restored.Marked, Is.EqualTo(marked));
        }

        [Test]
        public void RemoteExecutionAppliesExplicitStateIdempotently()
        {
            MarkNetAction mark = new MarkNetAction(node.name, true);
            mark.ExecuteOnClient();
            Transform marker = node.transform.Find(GameNodeMarker.MarkerName);

            mark.ExecuteOnClient();

            Assert.That(GameNodeMarker.IsMarked(node), Is.True);
            Assert.That(node.transform.Find(GameNodeMarker.MarkerName), Is.EqualTo(marker));
            Assert.That(node.transform.childCount, Is.EqualTo(1));

            MarkNetAction unmark = new MarkNetAction(node.name, false);
            unmark.ExecuteOnClient();
            unmark.ExecuteOnClient();
            Assert.That(GameNodeMarker.IsMarked(node), Is.False);
            Assert.That(node.transform.childCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator MarkCanBeRemovedAndRecreatedInTheSameFrame()
        {
            GameNodeMarker.SetMarked(node, true);
            GameObject oldMarker = node.transform.Find(GameNodeMarker.MarkerName).gameObject;
            GameNodeMarker.SetMarked(node, false);
            Assert.That(oldMarker.activeSelf, Is.False);

            GameNodeMarker.SetMarked(node, true);

            Assert.That(GameNodeMarker.IsMarked(node), Is.True);
            Assert.That(node.transform.childCount, Is.EqualTo(1));
            Assert.That(node.transform.Find(GameNodeMarker.MarkerName).gameObject, Is.Not.EqualTo(oldMarker));
            yield return null;
            Assert.That(oldMarker == null, Is.True);
        }
    }
}
