using NUnit.Framework;
using SEE.Graphs.Config;
using SEE.Utils;
using System.IO;

namespace SEE.UserSettings
{
    /// <summary>
    /// Test cases for input/output of <see cref="Transcription"/>.
    /// </summary>
    internal class TestTranscription : AbstractTestConfigIO
    {
        /// <summary>
        /// The label under which the transcription settings are saved.
        /// </summary>
        private const string transcriptionLabel = "transcription";

        /// <summary>
        /// Name of the temporary file to which the settings will be written
        /// and from which they will be read. Will be created on <see cref="SetUp"/>
        /// and deleted on <see cref="TearDown"/>.
        /// </summary>
        private string filename;

        [SetUp]
        public void SetUp()
        {
            filename = Path.GetTempFileName();
        }

        [TearDown]
        public void TearDown()
        {
            FileIO.DeleteIfExists(filename);
        }

        /// <summary>
        /// Saved settings must be restored with the same values.
        /// </summary>
        [Test]
        public void TestRoundTrip()
        {
            Transcription saved = new()
            {
                Language = TranscriptionLanguage.English,
                ModelFileName = "ggml-base-q5_1.bin"
            };
            Save(saved, transcriptionLabel);

            Transcription loaded = Load(transcriptionLabel);

            Assert.That(loaded.Language, Is.EqualTo(saved.Language));
            Assert.That(loaded.ModelFileName, Is.EqualTo(saved.ModelFileName));
        }

        /// <summary>
        /// If the configuration file has no group for the transcription,
        /// the default values must be kept.
        /// </summary>
        [Test]
        public void TestMissingGroupKeepsDefaults()
        {
            Transcription saved = new()
            {
                Language = TranscriptionLanguage.English,
                ModelFileName = "ggml-base-q5_1.bin"
            };
            // Saved under a different label so that the expected group is missing.
            Save(saved, "other");

            Transcription loaded = Load(transcriptionLabel);

            Assert.That(loaded.Language, Is.EqualTo(TranscriptionLanguage.German));
            Assert.That(loaded.ModelFileName, Is.EqualTo("ggml-small-q5_1.bin"));
        }

        /// <summary>
        /// Saves <paramref name="saved"/> under <paramref name="label"/> in <see cref="filename"/>.
        /// </summary>
        /// <param name="saved">The settings to be saved.</param>
        /// <param name="label">The label under which to group the settings.</param>
        private void Save(Transcription saved, string label)
        {
            using ConfigWriter writer = new(filename);
            saved.Save(writer, label);
        }

        /// <summary>
        /// Returns new settings restored from the group <paramref name="label"/> in <see cref="filename"/>.
        /// </summary>
        /// <param name="label">The label under which to look up the settings.</param>
        /// <returns>The restored settings.</returns>
        private Transcription Load(string label)
        {
            using ConfigReader stream = new(filename);
            Transcription loaded = new();
            loaded.Restore(stream.Read(), label);
            return loaded;
        }
    }
}
