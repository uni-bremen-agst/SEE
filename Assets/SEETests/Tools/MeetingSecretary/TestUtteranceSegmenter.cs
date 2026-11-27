using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Tests for <see cref="UtteranceSegmenter"/>.
    /// </summary>
    internal class TestUtteranceSegmenter
    {
        /// <summary>
        /// The sample rate used in all tests (in Hz).
        /// </summary>
        private const int sampleRate = 16000;

        /// <summary>
        /// The number of samples fed at once. Deliberately not a multiple of the
        /// frame size of the segmenter.
        /// </summary>
        private const int chunkSize = 1000;

        /// <summary>
        /// The amplitude of the tone standing in for speech.
        /// </summary>
        private const float toneAmplitude = 0.5f;

        /// <summary>
        /// The segmenter under test.
        /// </summary>
        private UtteranceSegmenter segmenter;

        [SetUp]
        public void SetUp()
        {
            segmenter = new UtteranceSegmenter(sampleRate);
        }

        /// <summary>
        /// Pure silence yields no utterance.
        /// </summary>
        [Test]
        public void TestSilence()
        {
            Assert.That(FeedInChunks(Silence(5f)), Is.Empty);
        }

        /// <summary>
        /// One second of a loud tone followed by silence yields exactly one utterance:
        /// the pre-roll, the tone and the silence that ended the utterance.
        /// </summary>
        [Test]
        public void TestToneThenSilence()
        {
            List<float[]> utterances = FeedInChunks(Concat(Silence(0.5f), Tone(1f), Silence(2f)));

            Assert.That(utterances.Count, Is.EqualTo(1));
            float[] utterance = utterances[0];
            int preRoll = Samples(UtteranceSegmenter.PreRollSeconds);
            int expectedLength = preRoll + Samples(1f) + Samples(UtteranceSegmenter.EndSilenceSeconds);
            Assert.That(utterance.Length, Is.EqualTo(expectedLength));

            // The pre-roll is the silence before the tone.
            for (int i = 0; i < preRoll; i++)
            {
                Assert.That(utterance[i], Is.EqualTo(0f), $"Sample {i} of the pre-roll.");
            }
            // The tone starts right after the pre-roll and is complete.
            Assert.That(LargestAmplitude(utterance, preRoll, Samples(1f)), Is.EqualTo(toneAmplitude).Within(0.01f));
        }

        /// <summary>
        /// A short click is dropped.
        /// </summary>
        [Test]
        public void TestClickIsDropped()
        {
            Assert.That(FeedInChunks(Concat(Silence(0.5f), Tone(0.04f), Silence(2f))), Is.Empty);
        }

        /// <summary>
        /// A tone of 25 seconds is cut after the maximal utterance length.
        /// The rest forms a second utterance.
        /// </summary>
        [Test]
        public void TestLongToneIsCut()
        {
            List<float[]> utterances = FeedInChunks(Concat(Silence(0.5f), Tone(25f), Silence(2f)));

            Assert.That(utterances.Count, Is.EqualTo(2));
            int maxLength = Samples(UtteranceSegmenter.MaxUtteranceSeconds);
            Assert.That(utterances[0].Length, Is.EqualTo(maxLength));

            // Nothing is lost: both utterances together are the pre-roll, the tone
            // and the silence that ended the second utterance.
            int totalLength = Samples(UtteranceSegmenter.PreRollSeconds) + Samples(25f)
                + Samples(UtteranceSegmenter.EndSilenceSeconds);
            Assert.That(utterances[1].Length, Is.EqualTo(totalLength - maxLength));
        }

        /// <summary>
        /// Constant background noise alone yields no utterance, even though
        /// it is louder than <see cref="UtteranceSegmenter.MinSpeechLevel"/>.
        /// </summary>
        [Test]
        public void TestBackgroundNoise()
        {
            float[] noise = Noise(10f, 0.05f);

            Assert.That(FeedInChunks(noise), Is.Empty);
            Assert.That(segmenter.NoiseLevel, Is.GreaterThan(UtteranceSegmenter.MinSpeechLevel));
        }

        /// <summary>
        /// A tone that is clearly louder than the background noise is detected.
        /// </summary>
        [Test]
        public void TestToneInBackgroundNoise()
        {
            float[] samples = Noise(4f, 0.05f);
            float[] tone = Tone(1f);
            int toneStart = Samples(1f);
            for (int i = 0; i < tone.Length; i++)
            {
                samples[toneStart + i] += tone[i];
            }

            Assert.That(FeedInChunks(samples).Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Two tones separated by a long silence yield two utterances.
        /// </summary>
        [Test]
        public void TestTwoUtterances()
        {
            float[] samples = Concat(Silence(0.5f), Tone(1f), Silence(1.5f), Tone(1f), Silence(1.5f));
            Assert.That(FeedInChunks(samples).Count, Is.EqualTo(2));
        }

        /// <summary>
        /// A break shorter than <see cref="UtteranceSegmenter.EndSilenceSeconds"/>
        /// does not split an utterance.
        /// </summary>
        [Test]
        public void TestShortBreakDoesNotSplit()
        {
            float[] samples = Concat(Silence(0.5f), Tone(1f), Silence(0.3f), Tone(1f), Silence(1.5f));
            Assert.That(FeedInChunks(samples).Count, Is.EqualTo(1));
        }

        /// <summary>
        /// <see cref="UtteranceSegmenter.Reset"/> discards the utterance being recorded.
        /// </summary>
        [Test]
        public void TestReset()
        {
            Assert.That(FeedInChunks(Concat(Silence(0.5f), Tone(1f))), Is.Empty);
            segmenter.Reset();
            Assert.That(FeedInChunks(Silence(2f)), Is.Empty);

            // The segmenter still works after a reset.
            Assert.That(FeedInChunks(Concat(Tone(1f), Silence(2f))).Count, Is.EqualTo(1));
        }

        #region Helpers
        /// <summary>
        /// Feeds <paramref name="samples"/> to the <see cref="segmenter"/> in chunks
        /// of <see cref="chunkSize"/>, as a microphone would deliver them.
        /// </summary>
        /// <param name="samples">The samples to be fed.</param>
        /// <returns>All utterances finished while feeding.</returns>
        private List<float[]> FeedInChunks(float[] samples)
        {
            List<float[]> utterances = new();
            float[] chunk = new float[chunkSize];
            int position = 0;
            while (position < samples.Length)
            {
                int count = Math.Min(chunkSize, samples.Length - position);
                Array.Copy(samples, position, chunk, 0, count);
                utterances.AddRange(segmenter.Feed(chunk, count));
                position += count;
            }
            return utterances;
        }

        /// <summary>
        /// Returns the number of samples corresponding to <paramref name="seconds"/>.
        /// </summary>
        /// <param name="seconds">A duration in seconds.</param>
        /// <returns>The number of samples played in <paramref name="seconds"/>.</returns>
        private static int Samples(float seconds)
        {
            return (int)Math.Round(sampleRate * seconds);
        }

        /// <summary>
        /// Returns silence of the given duration.
        /// </summary>
        /// <param name="seconds">The duration in seconds.</param>
        /// <returns>Samples that are all zero.</returns>
        private static float[] Silence(float seconds)
        {
            return new float[Samples(seconds)];
        }

        /// <summary>
        /// Returns a sine tone of 440 Hz with <see cref="toneAmplitude"/>.
        /// </summary>
        /// <param name="seconds">The duration in seconds.</param>
        /// <returns>The samples of the tone.</returns>
        private static float[] Tone(float seconds)
        {
            float[] samples = new float[Samples(seconds)];
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = toneAmplitude * (float)Math.Sin(2 * Math.PI * 440 * i / sampleRate);
            }
            return samples;
        }

        /// <summary>
        /// Returns reproducible random noise.
        /// </summary>
        /// <param name="seconds">The duration in seconds.</param>
        /// <param name="amplitude">The largest absolute value of the samples.</param>
        /// <returns>The samples of the noise.</returns>
        private static float[] Noise(float seconds, float amplitude)
        {
            // Fixed seed so that the test is deterministic.
            Random random = new(42);
            float[] samples = new float[Samples(seconds)];
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = amplitude * (float)(random.NextDouble() * 2 - 1);
            }
            return samples;
        }

        /// <summary>
        /// Returns the concatenation of the given <paramref name="parts"/>.
        /// </summary>
        /// <param name="parts">The sample arrays to be concatenated.</param>
        /// <returns>All samples of <paramref name="parts"/> in the given order.</returns>
        private static float[] Concat(params float[][] parts)
        {
            List<float> result = new();
            foreach (float[] part in parts)
            {
                result.AddRange(part);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Returns the largest absolute value in the given range of <paramref name="samples"/>.
        /// </summary>
        /// <param name="samples">The samples to be searched.</param>
        /// <param name="start">The index of the first sample of the range.</param>
        /// <param name="count">The number of samples of the range.</param>
        /// <returns>The largest absolute sample value in the range.</returns>
        private static float LargestAmplitude(float[] samples, int start, int count)
        {
            float largest = 0;
            for (int i = start; i < start + count; i++)
            {
                largest = Math.Max(largest, Math.Abs(samples[i]));
            }
            return largest;
        }
        #endregion
    }
}
