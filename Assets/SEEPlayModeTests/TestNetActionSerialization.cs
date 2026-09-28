using NUnit.Framework;
using SEE.Net.Actions;
using SEE.Net.Actions.Animation;
using SEE.Net.Actions.GraphElement;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEE.Net
{
    /// <summary>
    /// Tests serialization and deserialization of network actions.
    /// </summary>
    internal class TestNetActionSerialization
    {
        /// <summary>
        /// Game object providing the <see cref="NetworkManager"/> required for constructing network actions.
        /// </summary>
        private GameObject networkManagerObject;

        /// <summary>
        /// Creates the <see cref="NetworkManager"/> required by <see cref="AbstractNetAction"/>.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            networkManagerObject = new GameObject("NetworkManager");
            networkManagerObject.AddComponent<NetworkManager>();
        }

        /// <summary>
        /// Destroys the <see cref="NetworkManager"/> created for the test.
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(networkManagerObject);
            yield return null;
        }

        /// <summary>
        /// Tests that <see cref="AnimationNetAction.GameObjectID"/> is preserved during serialization
        /// and deserialization.
        /// </summary>
        [Test]
        public void AnimationNetActionPreservesGameObjectID()
        {
            const string gameObjectID = "TestGameObject";

            PressPlayNetAction original = new(gameObjectID);

            string serialized = ActionSerializer.Serialize(original);
            PressPlayNetAction deserialized = (PressPlayNetAction)ActionSerializer.Deserialize(serialized);

            TestContext.WriteLine($"Serialized JSON: {serialized}");

            Assert.That(deserialized.GameObjectID, Is.EqualTo(gameObjectID));
        }

        /// <summary>
        /// Tests that <see cref="ShowInCityNetAction.Duration"/> is preserved during serialization
        /// and deserialization.
        /// </summary>
        [Test]
        public void ShowInCityNetActionPreservesDuration()
        {
            const string graphElementID = "TestGraphElement";
            const float duration = 3.5f;

            ShowInCityNetAction original = new(graphElementID, duration);

            string serialized = ActionSerializer.Serialize(original);
            ShowInCityNetAction deserialized = (ShowInCityNetAction)ActionSerializer.Deserialize(serialized);

            TestContext.WriteLine($"Serialized JSON: {serialized}");

            Assert.That(deserialized.GraphElementID, Is.EqualTo(graphElementID));
            Assert.That(deserialized.Duration, Is.EqualTo(duration));
        }
    }
}
