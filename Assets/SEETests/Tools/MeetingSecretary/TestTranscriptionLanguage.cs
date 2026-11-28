using NUnit.Framework;
using SEE.UserSettings;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Tests for <see cref="TranscriptionLanguageExtensions"/>.
    /// </summary>
    internal class TestTranscriptionLanguage
    {
        /// <summary>
        /// Each language is mapped onto the code expected by Whisper.
        /// </summary>
        [Test]
        public void TestToWhisperCode()
        {
            Assert.That(TranscriptionLanguage.German.ToWhisperCode(), Is.EqualTo("de"));
            Assert.That(TranscriptionLanguage.English.ToWhisperCode(), Is.EqualTo("en"));
            Assert.That(TranscriptionLanguage.Auto.ToWhisperCode(), Is.EqualTo("auto"));
        }

        /// <summary>
        /// No language is forgotten in the mapping.
        /// </summary>
        [Test]
        public void TestAllLanguagesAreMapped()
        {
            foreach (TranscriptionLanguage language in System.Enum.GetValues(typeof(TranscriptionLanguage)))
            {
                Assert.That(language.ToWhisperCode(), Is.Not.Empty, language.ToString());
            }
        }
    }
}
