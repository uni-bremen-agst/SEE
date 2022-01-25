using System;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Dissonance.Audio.Capture
{
    /// <summary>
    /// Provides fixed size frames of PCM data in a given format
    /// </summary>
    internal interface IFrameProvider
    {
        /// <summary>
        /// Format of data read from this provider
        /// </summary>
        [NotNull] WaveFormat WaveFormat { get; }

        /// <summary>
        /// Number of samples per frame
        /// </summary>
        uint FrameSize { get; }

        /// <summary>
        /// Read a frame of audio into the outbuffer, starting at the given offset
        /// </summary>
        /// <param name="outBuffer">buffer to write samples into</param>
        /// <returns>true, if a frame was read, otherwise false</returns>
        bool Read(ArraySegment<float> outBuffer);

        /// <summary>
        /// Clear all data from the provider and reset back to a state ready to be used
        /// </summary>
        void Reset();
    }
}
