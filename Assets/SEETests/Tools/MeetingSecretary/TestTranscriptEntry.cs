using System;
using NUnit.Framework;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Tests for <see cref="TranscriptEntry"/>.
    /// </summary>
    internal class TestTranscriptEntry
    {
        /// <summary>
        /// The time used for the entries; its value does not matter.
        /// </summary>
        private static readonly DateTime time = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        /// <summary>
        /// Speech is shown with the name of the speaker and the marker "(voice)".
        /// </summary>
        [Test]
        public void TestDisplayTextOfSpeech()
        {
            TranscriptEntry entry = new("Steve", "Hello, world.", time, TranscriptEntryKind.Speech);
            Assert.That(entry.ToDisplayText(), Is.EqualTo("Steve (voice): Hello, world."));
        }

        /// <summary>
        /// The notice that a player started the transcription.
        /// </summary>
        [Test]
        public void TestDisplayTextOfStarted()
        {
            TranscriptEntry entry = new("Steve", "", time, TranscriptEntryKind.Started);
            Assert.That(entry.ToDisplayText(), Is.EqualTo("Steve started the transcription"));
        }

        /// <summary>
        /// The notice that a player stopped the transcription.
        /// </summary>
        [Test]
        public void TestDisplayTextOfStopped()
        {
            TranscriptEntry entry = new("Steve", "", time, TranscriptEntryKind.Stopped);
            Assert.That(entry.ToDisplayText(), Is.EqualTo("Steve stopped the transcription"));
        }

        /// <summary>
        /// The values passed to the constructor are kept; null becomes empty.
        /// </summary>
        [Test]
        public void TestConstructor()
        {
            TranscriptEntry entry = new("Steve", "Hello.", time, TranscriptEntryKind.Speech);
            Assert.That(entry.Speaker, Is.EqualTo("Steve"));
            Assert.That(entry.Text, Is.EqualTo("Hello."));
            Assert.That(entry.Time, Is.EqualTo(time));
            Assert.That(entry.Kind, Is.EqualTo(TranscriptEntryKind.Speech));

            TranscriptEntry withoutTexts = new(null, null, time, TranscriptEntryKind.Started);
            Assert.That(withoutTexts.Speaker, Is.EqualTo(""));
            Assert.That(withoutTexts.Text, Is.EqualTo(""));
        }
    }
}
