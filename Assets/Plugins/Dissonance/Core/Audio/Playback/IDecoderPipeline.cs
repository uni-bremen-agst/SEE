using System;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Dissonance.Audio.Playback
{
    internal interface IDecoderPipeline
    {
        /// <summary>
        /// Number of buffers waiting for playback
        /// </summary>
        int BufferCount { get; }

        /// <summary>
        /// Total amount of time which is in buffers
        /// </summary>
        TimeSpan BufferTime { get; }

        /// <summary>
        /// Packet loss detected in playback (0-1)
        /// </summary>
        float PacketLoss { get; }

        /// <summary>
        /// Get the amount of time contained in a single frame at the input into this pipeline
        /// </summary>
        TimeSpan InputFrameTime { get; }

        /// <summary>
        /// Get the playback options to use for this audio stream
        /// </summary>
        PlaybackOptions PlaybackOptions { get; }

        /// <summary>
        /// Get the output format of data from this pipeline
        /// </summary>
        [NotNull] WaveFormat OutputFormat { get; }

        /// <summary>
        /// Before the first call to read this must be called to prepare the pipeline for reading.
        /// </summary>
        /// <param name="context"></param>
        void Prepare(SessionContext context);

        /// <summary>
        /// Get the current playback sync state from this pipeline
        /// </summary>
        SyncState SyncState { get; }

        /// <summary>
        /// Read data from the pipeline, completely filling the array segment with valid data
        /// </summary>
        /// <param name="samples">The array to read into, will always be filled with valid data</param>
        /// <returns>true, if the session is finished. Otherwise false.</returns>
        bool Read(ArraySegment<float> samples);

        /// <summary>
        /// Enable dynamic audio synchronisation.
        /// </summary>
        void EnableDynamicSync();

        /// <summary>
        /// If non-null, override automatic output sample rate determination and set it to a fixed value.
        /// If null, use automatic rate determination.
        /// </summary>
        /// <param name="rate"></param>
        void SetOutputSampleRate(int? rate);
    }
}