using NUnit.Framework;
using SEE.Net.Actions;
using SEE.Tools.MeetingSecretary;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEE.Net
{
    /// <summary>
    /// Tests for <see cref="TranscriptNetAction"/>.
    /// </summary>
    internal class TestTranscriptNetAction
    {
        /// <summary>
        /// The time used for the entries; its value does not matter.
        /// </summary>
        private static readonly DateTime time = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

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
        /// </summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            UnityEngine.Object.Destroy(networkManagerObject);
            yield return null;
        }

        /// <summary>
        /// All fields have the same values after the action was serialized and
        /// deserialized the way it is done for sending it over the network.
        /// </summary>
        [TestCase(TranscriptEntryKind.Speech, "Hello; this is \"a test\" with umlauts: äöüß.")]
        [TestCase(TranscriptEntryKind.Started, "")]
        [TestCase(TranscriptEntryKind.Stopped, "")]
        public void TestRoundTrip(TranscriptEntryKind kind, string text)
        {
            TranscriptNetAction original = new(new TranscriptEntry("Steve", text, time, kind));

            string serialized = ActionSerializer.Serialize(original);
            TranscriptNetAction deserialized = (TranscriptNetAction)ActionSerializer.Deserialize(serialized);

            Assert.That(deserialized.Speaker, Is.EqualTo("Steve"));
            Assert.That(deserialized.Text, Is.EqualTo(text));
            Assert.That(deserialized.Kind, Is.EqualTo(kind));
            Assert.That(deserialized.TimeUtcTicks, Is.EqualTo(time.Ticks));
            Assert.That(deserialized.Requester, Is.EqualTo(original.Requester));
        }

        /// <summary>
        /// The entry carried by a deserialized action equals the entry that was sent.
        /// </summary>
        [Test]
        public void TestToEntry()
        {
            TranscriptNetAction original = new(new TranscriptEntry("Steve", "Hello.", time, TranscriptEntryKind.Speech));
            TranscriptNetAction deserialized = (TranscriptNetAction)ActionSerializer.Deserialize(ActionSerializer.Serialize(original));

            TranscriptEntry entry = deserialized.ToEntry();

            Assert.That(entry.Speaker, Is.EqualTo("Steve"));
            Assert.That(entry.Text, Is.EqualTo("Hello."));
            Assert.That(entry.Kind, Is.EqualTo(TranscriptEntryKind.Speech));
            Assert.That(entry.Time, Is.EqualTo(time));
            Assert.That(entry.Time.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        /// <summary>
        /// A time that is not valid (it comes from the network) does not raise an exception.
        /// </summary>
        [Test]
        public void TestToEntryWithInvalidTime()
        {
            TranscriptNetAction action = new(new TranscriptEntry("Steve", "Hello.", time, TranscriptEntryKind.Speech))
            {
                TimeUtcTicks = -1
            };

            Assert.DoesNotThrow(() => action.ToEntry());
        }

        /// <summary>
        /// The transcript must not be replayed to players who join later.
        /// </summary>
        [Test]
        public void TestIsNotSentToNewClients()
        {
            TranscriptNetAction action = new(new TranscriptEntry("Steve", "Hello.", time, TranscriptEntryKind.Speech));
            Assert.That(action.ShouldBeSentToNewClient, Is.False);
        }
    }
}
