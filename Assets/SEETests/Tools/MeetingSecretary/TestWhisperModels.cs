using System.IO;
using NUnit.Framework;
using SEE.Utils;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Tests for <see cref="WhisperModels"/>.
    /// </summary>
    internal class TestWhisperModels
    {
        /// <summary>
        /// The SHA-256 checksum of the text "abc" (a standard test value of SHA-256).
        /// </summary>
        private const string checksumOfAbc = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

        /// <summary>
        /// Name of the temporary file whose checksum is computed. Will be created
        /// on <see cref="SetUp"/> and deleted on <see cref="TearDown"/>.
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
        /// Plain file names are accepted.
        /// </summary>
        [TestCase("ggml-small-q5_1.bin")]
        [TestCase("ggml-large-v3-turbo-q5_0.bin")]
        [TestCase("my model.bin")]
        [TestCase("ggml-base-q5_1.bin")]
        public void TestValidFileNames(string fileName)
        {
            Assert.That(WhisperModels.IsValidFileName(fileName), Is.True);
        }

        /// <summary>
        /// Empty names and names that could leave the folder of the models are rejected.
        /// </summary>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("../ggml-small-q5_1.bin")]
        [TestCase("..")]
        [TestCase("folder/ggml-small-q5_1.bin")]
        [TestCase("/etc/passwd")]
        [TestCase("folder\\ggml-small-q5_1.bin")]
        [TestCase("C:\\model.bin")]
        [TestCase("model..bin")]
        [TestCase("C:ggml.bin")]
        [TestCase("/tmp/x.bin")]
        [TestCase("x/y.bin")]
        public void TestInvalidFileNames(string fileName)
        {
            Assert.That(WhisperModels.IsValidFileName(fileName), Is.False);
        }

        /// <summary>
        /// The download URL points to the file in the repository of whisper.cpp.
        /// </summary>
        [Test]
        public void TestDownloadUrl()
        {
            Assert.That(WhisperModels.GetDownloadUrl("ggml-small-q5_1.bin"),
                        Is.EqualTo("https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small-q5_1.bin"));
        }

        /// <summary>
        /// The checksums of the models we offer are known and look like SHA-256 checksums.
        /// </summary>
        [TestCase("ggml-base-q5_1.bin")]
        [TestCase("ggml-small-q5_1.bin")]
        [TestCase("ggml-large-v3-turbo-q5_0.bin")]
        public void TestKnownChecksums(string fileName)
        {
            Assert.That(WhisperModels.TryGetChecksum(fileName, out string checksum), Is.True);
            Assert.That(checksum, Does.Match("^[0-9a-f]{64}$"));
        }

        /// <summary>
        /// The default model of the user settings has a known checksum.
        /// </summary>
        [Test]
        public void TestDefaultModelHasChecksum()
        {
            string defaultModel = new SEE.UserSettings.Transcription().ModelFileName;
            Assert.That(WhisperModels.TryGetChecksum(defaultModel, out string _), Is.True);
        }

        /// <summary>
        /// There is no checksum for an unknown model.
        /// </summary>
        [Test]
        public void TestUnknownChecksum()
        {
            Assert.That(WhisperModels.TryGetChecksum("unknown.bin", out string checksum), Is.False);
            Assert.That(checksum, Is.Null);
        }

        /// <summary>
        /// The checksum of a file is computed correctly.
        /// </summary>
        [Test]
        public void TestComputeChecksum()
        {
            File.WriteAllText(filename, "abc");
            Assert.That(WhisperModels.ComputeChecksum(filename), Is.EqualTo(checksumOfAbc));
        }

        /// <summary>
        /// A file is accepted with its own checksum (in any capitalization) and
        /// rejected if its content differs.
        /// </summary>
        [Test]
        public void TestHasChecksum()
        {
            File.WriteAllText(filename, "abc");
            Assert.That(WhisperModels.HasChecksum(filename, checksumOfAbc), Is.True);
            Assert.That(WhisperModels.HasChecksum(filename, checksumOfAbc.ToUpperInvariant()), Is.True);

            // A damaged or incomplete download.
            File.WriteAllText(filename, "ab");
            Assert.That(WhisperModels.HasChecksum(filename, checksumOfAbc), Is.False);
        }
    }
}
