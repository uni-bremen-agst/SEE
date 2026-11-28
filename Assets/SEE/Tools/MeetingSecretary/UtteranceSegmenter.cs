using System;
using System.Collections.Generic;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Cuts a continuous stream of mono microphone samples into utterances
    /// (pieces of speech separated by silence).
    ///
    /// The samples are analyzed in short frames. A frame counts as speech if it is
    /// clearly louder than the background noise, which is measured while nobody speaks.
    /// </summary>
    /// <remarks>This class does not depend on any voice chat system and is not thread safe.</remarks>
    public class UtteranceSegmenter
    {
        #region Tuning constants
        /// <summary>
        /// The length of one analyzed frame in seconds.
        /// </summary>
        public const float FrameSeconds = 0.02f;

        /// <summary>
        /// How much audio before the first speech frame is added to an utterance (in seconds),
        /// so that the soft beginning of a word is not cut off.
        /// </summary>
        public const float PreRollSeconds = 0.3f;

        /// <summary>
        /// How long it must be silent until an utterance is considered finished (in seconds).
        /// Short breaks, e.g., for breathing, do not end an utterance.
        /// </summary>
        public const float EndSilenceSeconds = 0.7f;

        /// <summary>
        /// Utterances whose speech is shorter than this (in seconds) are dropped.
        /// These are mostly clicks or coughs, for which speech recognition invents text.
        /// </summary>
        public const float MinSpeechSeconds = 0.5f;

        /// <summary>
        /// An utterance longer than this (in seconds) is cut even if the speaker
        /// did not pause, because speech recognition handles only limited lengths.
        /// </summary>
        public const float MaxUtteranceSeconds = 20f;

        /// <summary>
        /// A frame is speech if its level is above the background noise level
        /// multiplied by this factor.
        /// </summary>
        public const float SpeechToNoiseRatio = 3f;

        /// <summary>
        /// A frame is never speech if its level is not above this value, no matter
        /// how quiet the background is. Levels are root mean squares of samples in [-1, 1].
        /// </summary>
        public const float MinSpeechLevel = 0.01f;

        /// <summary>
        /// The length of the initial phase (in seconds) in which only the background
        /// noise is measured and no speech is detected.
        /// </summary>
        public const float CalibrationSeconds = 0.3f;

        /// <summary>
        /// How quickly the background noise level follows a louder background
        /// (share of the difference taken over per frame). Slow, so that the soft
        /// beginning of speech does not raise the noise level much.
        /// </summary>
        public const float NoiseRiseRate = 0.02f;

        /// <summary>
        /// How quickly the background noise level follows a quieter background
        /// (share of the difference taken over per frame).
        /// </summary>
        public const float NoiseFallRate = 0.2f;
        #endregion

        /// <summary>
        /// Returned by <see cref="Feed(float[], int)"/> if no utterance was finished.
        /// </summary>
        private static readonly float[][] noUtterances = Array.Empty<float[]>();

        /// <summary>
        /// Number of samples of one frame.
        /// </summary>
        private readonly int frameSize;

        /// <summary>
        /// Number of samples of the pre-roll.
        /// </summary>
        private readonly int preRollSamples;

        /// <summary>
        /// Number of silent samples that end an utterance.
        /// </summary>
        private readonly int endSilenceSamples;

        /// <summary>
        /// Minimal number of speech samples of an utterance.
        /// </summary>
        private readonly int minSpeechSamples;

        /// <summary>
        /// Maximal number of samples of an utterance.
        /// </summary>
        private readonly int maxUtteranceSamples;

        /// <summary>
        /// Number of frames of the calibration phase.
        /// </summary>
        private readonly int calibrationFrames;

        /// <summary>
        /// The frame currently being filled.
        /// </summary>
        private readonly float[] frame;

        /// <summary>
        /// Number of samples already stored in <see cref="frame"/>.
        /// </summary>
        private int frameFill;

        /// <summary>
        /// The most recent samples received while nobody was speaking
        /// (at most <see cref="preRollSamples"/>).
        /// </summary>
        private readonly List<float> preRoll = new();

        /// <summary>
        /// The samples of the utterance currently being recorded.
        /// </summary>
        private readonly List<float> utterance = new();

        /// <summary>
        /// True while an utterance is being recorded.
        /// </summary>
        private bool speaking;

        /// <summary>
        /// Index in <see cref="utterance"/> where the speech starts (after the pre-roll).
        /// </summary>
        private int speechStart;

        /// <summary>
        /// Index in <see cref="utterance"/> just behind the last speech frame.
        /// </summary>
        private int speechEnd;

        /// <summary>
        /// Number of samples received since the last speech frame.
        /// </summary>
        private int silentSamples;

        /// <summary>
        /// Number of frames measured so far in the calibration phase.
        /// </summary>
        private int calibratedFrames;

        /// <summary>
        /// Sum of the levels of the frames measured in the calibration phase.
        /// </summary>
        private float calibrationSum;

        /// <summary>
        /// The estimated level of the background noise.
        /// </summary>
        public float NoiseLevel { get; private set; }

        /// <summary>
        /// The sample rate (in Hz) of the samples passed to <see cref="Feed(float[], int)"/>.
        /// </summary>
        public int SampleRate { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sampleRate">The sample rate (in Hz) of the samples that will be fed.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="sampleRate"/> is too
        /// low to form a frame.</exception>
        public UtteranceSegmenter(int sampleRate)
        {
            SampleRate = sampleRate;
            frameSize = ToSamples(FrameSeconds);
            if (frameSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate), "The sample rate is too low.");
            }
            preRollSamples = ToSamples(PreRollSeconds);
            endSilenceSamples = ToSamples(EndSilenceSeconds);
            minSpeechSamples = ToSamples(MinSpeechSeconds);
            maxUtteranceSamples = ToSamples(MaxUtteranceSeconds);
            calibrationFrames = (int)Math.Round(CalibrationSeconds / FrameSeconds);
            frame = new float[frameSize];
        }

        /// <summary>
        /// Returns the number of samples corresponding to the given duration.
        /// </summary>
        /// <param name="seconds">A duration in seconds.</param>
        /// <returns>The number of samples played in <paramref name="seconds"/>.</returns>
        private int ToSamples(float seconds)
        {
            return (int)Math.Round(SampleRate * seconds);
        }

        /// <summary>
        /// Processes all given <paramref name="samples"/>.
        /// </summary>
        /// <param name="samples">The next mono samples of the stream.</param>
        /// <returns>The utterances finished by these samples, in the order in which
        /// they were spoken; empty if there is none.</returns>
        public IReadOnlyList<float[]> Feed(float[] samples)
        {
            return Feed(samples, samples.Length);
        }

        /// <summary>
        /// Processes the first <paramref name="count"/> elements of <paramref name="samples"/>.
        /// The array is not kept; the caller may reuse it.
        /// </summary>
        /// <param name="samples">The next mono samples of the stream.</param>
        /// <param name="count">The number of elements of <paramref name="samples"/> to be processed.</param>
        /// <returns>The utterances finished by these samples, in the order in which
        /// they were spoken; empty if there is none.</returns>
        public IReadOnlyList<float[]> Feed(float[] samples, int count)
        {
            List<float[]> finished = null;
            for (int i = 0; i < count; i++)
            {
                frame[frameFill] = samples[i];
                frameFill++;
                if (frameFill == frameSize)
                {
                    frameFill = 0;
                    float[] result = ProcessFrame();
                    if (result != null)
                    {
                        finished ??= new List<float[]>();
                        finished.Add(result);
                    }
                }
            }
            if (finished == null)
            {
                return noUtterances;
            }
            return finished;
        }

        /// <summary>
        /// Discards all audio received so far, including an utterance that is
        /// currently being recorded. To be called when the stream is interrupted,
        /// e.g., when the player is muted.
        /// </summary>
        /// <remarks>The measured background noise level is kept, because the
        /// surroundings of the player are still the same afterwards.</remarks>
        public void Reset()
        {
            frameFill = 0;
            preRoll.Clear();
            utterance.Clear();
            speaking = false;
            speechStart = 0;
            speechEnd = 0;
            silentSamples = 0;
        }

        /// <summary>
        /// Processes the complete <see cref="frame"/>.
        /// </summary>
        /// <returns>The utterance finished by this frame or null if there is none.</returns>
        private float[] ProcessFrame()
        {
            float level = Level(frame);

            if (calibratedFrames < calibrationFrames)
            {
                calibrationSum += level;
                calibratedFrames++;
                NoiseLevel = calibrationSum / calibratedFrames;
                AddToPreRoll();
                return null;
            }

            float threshold = Math.Max(MinSpeechLevel, NoiseLevel * SpeechToNoiseRatio);
            bool isSpeech = level > threshold;

            if (!speaking)
            {
                if (isSpeech)
                {
                    StartUtterance();
                }
                else
                {
                    UpdateNoiseLevel(level);
                    AddToPreRoll();
                }
                return null;
            }

            utterance.AddRange(frame);
            if (isSpeech)
            {
                speechEnd = utterance.Count;
                silentSamples = 0;
            }
            else
            {
                silentSamples += frameSize;
            }

            if (silentSamples >= endSilenceSamples)
            {
                speaking = false;
                return TakeUtterance();
            }
            if (utterance.Count >= maxUtteranceSamples)
            {
                // The speaker is still talking: we stay in the speaking state, and
                // the next frames form a new utterance without pre-roll.
                return TakeUtterance();
            }
            return null;
        }

        /// <summary>
        /// Starts a new utterance consisting of the pre-roll and the current <see cref="frame"/>.
        /// </summary>
        private void StartUtterance()
        {
            utterance.Clear();
            utterance.AddRange(preRoll);
            preRoll.Clear();
            speechStart = utterance.Count;
            utterance.AddRange(frame);
            speechEnd = utterance.Count;
            silentSamples = 0;
            speaking = true;
        }

        /// <summary>
        /// Removes the samples recorded in <see cref="utterance"/> and returns them
        /// if they contain enough speech.
        /// </summary>
        /// <returns>The recorded utterance or null if its speech is too short.</returns>
        private float[] TakeUtterance()
        {
            float[] result = null;
            if (speechEnd - speechStart >= minSpeechSamples)
            {
                result = utterance.ToArray();
            }
            utterance.Clear();
            speechStart = 0;
            speechEnd = 0;
            return result;
        }

        /// <summary>
        /// Appends the current <see cref="frame"/> to <see cref="preRoll"/> and removes
        /// the oldest samples exceeding <see cref="preRollSamples"/>.
        /// </summary>
        private void AddToPreRoll()
        {
            preRoll.AddRange(frame);
            if (preRoll.Count > preRollSamples)
            {
                preRoll.RemoveRange(0, preRoll.Count - preRollSamples);
            }
        }

        /// <summary>
        /// Moves <see cref="NoiseLevel"/> towards the <paramref name="level"/> of a frame
        /// without speech.
        /// </summary>
        /// <param name="level">The level of a frame without speech.</param>
        private void UpdateNoiseLevel(float level)
        {
            float rate;
            if (level > NoiseLevel)
            {
                rate = NoiseRiseRate;
            }
            else
            {
                rate = NoiseFallRate;
            }
            NoiseLevel += (level - NoiseLevel) * rate;
        }

        /// <summary>
        /// Returns the level (root mean square) of the given <paramref name="samples"/>.
        /// </summary>
        /// <param name="samples">The samples to be measured.</param>
        /// <returns>The root mean square of <paramref name="samples"/>.</returns>
        private static float Level(float[] samples)
        {
            double sum = 0;
            foreach (float sample in samples)
            {
                sum += sample * sample;
            }
            return (float)Math.Sqrt(sum / samples.Length);
        }
    }
}
