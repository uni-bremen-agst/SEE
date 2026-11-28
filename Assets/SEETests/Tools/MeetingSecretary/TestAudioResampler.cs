using NUnit.Framework;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Tests for <see cref="AudioResampler"/>.
    /// </summary>
    internal class TestAudioResampler
    {
        /// <summary>
        /// Tolerance for comparing sample values.
        /// </summary>
        private const float tolerance = 0.0001f;

        /// <summary>
        /// Samples that already have the target rate are returned unchanged.
        /// </summary>
        [Test]
        public void TestTargetRateIsUnchanged()
        {
            float[] samples = { 0.1f, 0.2f, 0.3f };
            Assert.That(AudioResampler.ToTargetSampleRate(samples, AudioResampler.TargetSampleRate), Is.SameAs(samples));
        }

        /// <summary>
        /// One second at 48 kHz becomes one second at 16 kHz.
        /// </summary>
        [Test]
        public void TestLengthFrom48000()
        {
            float[] result = AudioResampler.ToTargetSampleRate(new float[48000], 48000);
            Assert.That(result.Length, Is.EqualTo(16000));
        }

        /// <summary>
        /// At 48 kHz, each result sample is the average of three input samples.
        /// </summary>
        [Test]
        public void TestAverageFrom48000()
        {
            float[] samples = { 0.3f, 0.6f, 0.9f, -1f, 0f, 1f, 0.5f };
            float[] result = AudioResampler.ToTargetSampleRate(samples, 48000);
            // The incomplete group at the end is ignored.
            Assert.That(result.Length, Is.EqualTo(2));
            Assert.That(result[0], Is.EqualTo(0.6f).Within(tolerance));
            Assert.That(result[1], Is.EqualTo(0f).Within(tolerance));
        }

        /// <summary>
        /// One second at 44.1 kHz becomes one second at 16 kHz.
        /// </summary>
        [Test]
        public void TestLengthFrom44100()
        {
            float[] result = AudioResampler.ToTargetSampleRate(new float[44100], 44100);
            Assert.That(result.Length, Is.EqualTo(16000));
        }

        /// <summary>
        /// A constant signal stays constant when interpolated.
        /// </summary>
        [Test]
        public void TestInterpolationKeepsConstantSignal()
        {
            float[] samples = new float[4410];
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = 0.25f;
            }
            float[] result = AudioResampler.ToTargetSampleRate(samples, 44100);
            Assert.That(result.Length, Is.EqualTo(1600));
            Assert.That(result, Is.All.EqualTo(0.25f).Within(tolerance));
        }

        /// <summary>
        /// A lower sample rate than the target rate yields more samples.
        /// </summary>
        [Test]
        public void TestLengthFrom8000()
        {
            float[] result = AudioResampler.ToTargetSampleRate(new float[8000], 8000);
            Assert.That(result.Length, Is.EqualTo(16000));
        }

        /// <summary>
        /// Empty input yields empty output for all conversion paths.
        /// </summary>
        [Test]
        public void TestEmptyInput()
        {
            Assert.That(AudioResampler.ToTargetSampleRate(new float[0], 48000), Is.Empty);
            Assert.That(AudioResampler.ToTargetSampleRate(new float[0], 44100), Is.Empty);
            Assert.That(AudioResampler.MixToMono(new float[0], 2), Is.Empty);
        }

        /// <summary>
        /// Stereo samples are averaged per frame.
        /// </summary>
        [Test]
        public void TestMixStereoToMono()
        {
            float[] interleaved = { 0.2f, 0.4f, -1f, 1f, 0.5f, 0.5f };
            float[] mono = AudioResampler.MixToMono(interleaved, 2);
            Assert.That(mono.Length, Is.EqualTo(3));
            Assert.That(mono[0], Is.EqualTo(0.3f).Within(tolerance));
            Assert.That(mono[1], Is.EqualTo(0f).Within(tolerance));
            Assert.That(mono[2], Is.EqualTo(0.5f).Within(tolerance));
        }

        /// <summary>
        /// Mono samples are returned unchanged.
        /// </summary>
        [Test]
        public void TestMixMonoIsUnchanged()
        {
            float[] samples = { 0.1f, 0.2f, 0.3f };
            Assert.That(AudioResampler.MixToMono(samples, 1), Is.SameAs(samples));
        }
    }
}
