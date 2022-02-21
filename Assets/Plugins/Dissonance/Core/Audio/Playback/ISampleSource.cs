using System;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Dissonance.Audio.Playback
{
    internal interface ISampleSource
    {
        /// <summary>
        /// Format of the samples this source provides
        /// </summary>
        [NotNull] WaveFormat WaveFormat { get; }

        /// <summary>
        /// Prepare for providing samples (called before read)
        /// </summary>
        /// <param name="context"></param>
        void Prepare(SessionContext context);

        /// <summary>
        /// Read some samples into the provided buffer
        /// </summary>
        /// <param name="samples">A buffer which will be filled with samples</param>
        /// <returns>true, if the source has completed (i.e. no more samples are available)</returns>
        bool Read(ArraySegment<float> samples);

        /// <summary>
        /// Reset this source to prepare to be read again
        /// </summary>
        void Reset();
    }
}