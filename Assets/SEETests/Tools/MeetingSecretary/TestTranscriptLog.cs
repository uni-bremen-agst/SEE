using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Tests for <see cref="TranscriptLog"/>.
    /// </summary>
    internal class TestTranscriptLog
    {
        /// <summary>
        /// The entries reported by <see cref="TranscriptLog.OnEntryAdded"/>.
        /// </summary>
        private readonly List<TranscriptEntry> reported = new();

        [SetUp]
        public void SetUp()
        {
            TranscriptLog.Clear();
            reported.Clear();
            TranscriptLog.OnEntryAdded += Report;
        }

        [TearDown]
        public void TearDown()
        {
            TranscriptLog.OnEntryAdded -= Report;
            TranscriptLog.Clear();
        }

        /// <summary>
        /// The transcript is empty at the beginning.
        /// </summary>
        [Test]
        public void TestEmpty()
        {
            Assert.That(TranscriptLog.Entries, Is.Empty);
        }

        /// <summary>
        /// Entries are kept in the order in which they were added.
        /// </summary>
        [Test]
        public void TestAddKeepsOrder()
        {
            TranscriptEntry first = NewEntry("Steve", "One.");
            TranscriptEntry second = NewEntry("Anna", "Two.");
            TranscriptEntry third = NewEntry("Steve", "Three.");

            TranscriptLog.Add(first);
            TranscriptLog.Add(second);
            TranscriptLog.Add(third);

            Assert.That(TranscriptLog.Entries, Is.EqualTo(new[] { first, second, third }));
        }

        /// <summary>
        /// <see cref="TranscriptLog.OnEntryAdded"/> is raised once for each added entry.
        /// </summary>
        [Test]
        public void TestAddRaisesEvent()
        {
            TranscriptEntry first = NewEntry("Steve", "One.");
            TranscriptEntry second = NewEntry("Anna", "Two.");

            TranscriptLog.Add(first);
            Assert.That(reported, Is.EqualTo(new[] { first }));

            TranscriptLog.Add(second);
            Assert.That(reported, Is.EqualTo(new[] { first, second }));
        }

        /// <summary>
        /// Null cannot be added.
        /// </summary>
        [Test]
        public void TestAddNull()
        {
            Assert.Throws<ArgumentNullException>(() => TranscriptLog.Add(null));
            Assert.That(TranscriptLog.Entries, Is.Empty);
            Assert.That(reported, Is.Empty);
        }

        /// <summary>
        /// <see cref="TranscriptLog.Clear"/> removes all entries; new ones can be added afterwards.
        /// </summary>
        [Test]
        public void TestClear()
        {
            TranscriptLog.Add(NewEntry("Steve", "One."));
            TranscriptLog.Add(NewEntry("Anna", "Two."));

            TranscriptLog.Clear();
            Assert.That(TranscriptLog.Entries, Is.Empty);

            TranscriptEntry third = NewEntry("Steve", "Three.");
            TranscriptLog.Add(third);
            Assert.That(TranscriptLog.Entries, Is.EqualTo(new[] { third }));
        }

        /// <summary>
        /// Stores <paramref name="entry"/> in <see cref="reported"/>.
        /// </summary>
        /// <param name="entry">The entry reported by the transcript.</param>
        private void Report(TranscriptEntry entry)
        {
            reported.Add(entry);
        }

        /// <summary>
        /// Returns a new speech entry.
        /// </summary>
        /// <param name="speaker">The name of the speaker.</param>
        /// <param name="text">What the speaker said.</param>
        /// <returns>The new entry.</returns>
        private static TranscriptEntry NewEntry(string speaker, string text)
        {
            return new TranscriptEntry(speaker, text, DateTime.UtcNow, TranscriptEntryKind.Speech);
        }
    }
}
