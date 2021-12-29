using System;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Dissonance.Audio.Playback
{
    internal interface IFrameSource
    {
        /// <summary>
        /// Number of samples this source provides in one read
        /// </summary>
        uint FrameSize { get; }

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
        /// <param name="frame">A buffer which FrameSize samples will be written into</param>
        /// <returns>true, if the source has completed (i.e. no more samples are available)</returns>
        bool Read(ArraySegment<float> frame);

        /// <summary>
        /// Reset this source to prepare to be read again
        /// </summary>
        void Reset();
    }
}