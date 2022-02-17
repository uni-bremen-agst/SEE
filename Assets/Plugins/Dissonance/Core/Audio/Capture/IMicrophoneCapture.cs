using System;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Dissonance.Audio.Capture
{
    /// <summary>
    /// Interface for capturing microphone samples
    /// </summary>
    public interface IMicrophoneCapture
    {
        /// <summary>
        /// Indicates if Start has successfully been called more recently than Stop
        /// </summary>
        bool IsRecording { get; }

        /// <summary>
        /// The device which is currently being used to record audio. Null if not recording.
        /// </summary>
        [CanBeNull] string Device { get; }

        /// <summary>
        /// Total latency from audio arriving at the microphone to being delivered to mic subscribers
        /// </summary>
        TimeSpan Latency { get; }

        /// <summary>
        /// Begin capturing PCM data from the microphone
        /// </summary>
        /// <param name="name">Name of the microphone to capture from. Null indicates the default microphone</param>
        /// <returns>Format of captured data, null if capture did not start</returns>
        [CanBeNull] WaveFormat StartCapture([CanBeNull] string name);

        /// <summary>
        /// Stop capturing microphone samples, discard any buffered data
        /// </summary>
        void StopCapture();

        /// <summary>
        /// Subscribe a handler to raw PCM data from the microphone
        /// </summary>
        /// <param name="listener"></param>
        void Subscribe([NotNull] IMicrophoneSubscriber listener);

        /// <summary>
        /// Unsubscribe a handler from receiving raw PCM data
        /// </summary>
        /// <param name="listener"></param>
        /// <returns>true; if the subscriber was unsubscribed. False if the given subscriber was not found</returns>
        bool Unsubscribe([NotNull] IMicrophoneSubscriber listener);

        /// <summary>
        /// Capture frames of microphone input and send them on to subscribers
        /// </summary>
        /// <returns>true if the microphone requires resetting for any reason</returns>
        bool UpdateSubscribers();
    }
}
