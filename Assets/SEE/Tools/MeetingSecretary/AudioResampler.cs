using System;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Converts audio samples into the format needed for speech recognition:
    /// mono samples at <see cref="TargetSampleRate"/>.
    /// </summary>
    public static class AudioResampler
    {
        /// <summary>
        /// The sample rate (in Hz) expected by the speech recognition.
        /// </summary>
        public const int TargetSampleRate = 16000;

        /// <summary>
        /// Converts the mono <paramref name="samples"/> recorded at <paramref name="sampleRate"/>
        /// to <see cref="TargetSampleRate"/>.
        /// </summary>
        /// <param name="samples">The mono samples to be converted.</param>
        /// <param name="sampleRate">The sample rate of <paramref name="samples"/> in Hz.</param>
        /// <returns>The samples at <see cref="TargetSampleRate"/>. If <paramref name="sampleRate"/>
        /// is already <see cref="TargetSampleRate"/>, <paramref name="samples"/> itself is returned.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="samples"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="sampleRate"/> is not positive.</exception>
        public static float[] ToTargetSampleRate(float[] samples, int sampleRate)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }
            if (sampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate), "The sample rate must be positive.");
            }

            if (sampleRate == TargetSampleRate)
            {
                return samples;
            }
            if (sampleRate % TargetSampleRate == 0)
            {
                return Average(samples, sampleRate / TargetSampleRate);
            }
            return Interpolate(samples, sampleRate);
        }

        /// <summary>
        /// Mixes interleaved multi-channel samples down to mono by averaging
        /// the channels of each frame.
        /// </summary>
        /// <param name="interleaved">The interleaved samples (channel 1, channel 2, ..., channel 1, ...).</param>
        /// <param name="channels">The number of channels in <paramref name="interleaved"/>.</param>
        /// <returns>The mono samples. If <paramref name="channels"/> is 1,
        /// <paramref name="interleaved"/> itself is returned.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="interleaved"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="channels"/> is not positive.</exception>
        public static float[] MixToMono(float[] interleaved, int channels)
        {
            if (interleaved == null)
            {
                throw new ArgumentNullException(nameof(interleaved));
            }
            if (channels <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(channels), "The number of channels must be positive.");
            }

            if (channels == 1)
            {
                return interleaved;
            }

            // An incomplete frame at the end is ignored.
            float[] mono = new float[interleaved.Length / channels];
            for (int frame = 0; frame < mono.Length; frame++)
            {
                float sum = 0;
                for (int channel = 0; channel < channels; channel++)
                {
                    sum += interleaved[frame * channels + channel];
                }
                mono[frame] = sum / channels;
            }
            return mono;
        }

        /// <summary>
        /// Reduces the sample rate by the integer <paramref name="factor"/>. Each result
        /// sample is the average of <paramref name="factor"/> consecutive input samples.
        /// </summary>
        /// <param name="samples">The samples to be reduced.</param>
        /// <param name="factor">How many input samples form one result sample.</param>
        /// <returns>The reduced samples.</returns>
        private static float[] Average(float[] samples, int factor)
        {
            // Averaging (instead of just picking every n-th sample) dampens the
            // high frequencies that the lower sample rate cannot represent.
            float[] result = new float[samples.Length / factor];
            for (int i = 0; i < result.Length; i++)
            {
                float sum = 0;
                for (int j = 0; j < factor; j++)
                {
                    sum += samples[i * factor + j];
                }
                result[i] = sum / factor;
            }
            return result;
        }

        /// <summary>
        /// Converts <paramref name="samples"/> from <paramref name="sampleRate"/> to
        /// <see cref="TargetSampleRate"/> by linear interpolation between neighboring samples.
        /// </summary>
        /// <param name="samples">The samples to be converted.</param>
        /// <param name="sampleRate">The sample rate of <paramref name="samples"/> in Hz.</param>
        /// <returns>The converted samples.</returns>
        private static float[] Interpolate(float[] samples, int sampleRate)
        {
            float[] result = new float[(long)samples.Length * TargetSampleRate / sampleRate];
            double step = (double)sampleRate / TargetSampleRate;
            for (int i = 0; i < result.Length; i++)
            {
                double position = i * step;
                int left = (int)position;
                int right = Math.Min(left + 1, samples.Length - 1);
                float weight = (float)(position - left);
                result[i] = samples[left] * (1 - weight) + samples[right] * weight;
            }
            return result;
        }
    }
}
