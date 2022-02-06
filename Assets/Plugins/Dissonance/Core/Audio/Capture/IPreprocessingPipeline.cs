using System;
using Dissonance.VAD;
using NAudio.Wave;

namespace Dissonance.Audio.Capture
{
    internal interface IPreprocessingPipeline
        : IDisposable, IMicrophoneSubscriber
    {
        /// <summary>
        /// Get the format of audio being output from the pipeline
        /// </summary>
        WaveFormat OutputFormat { get; }

        /// <summary>
        /// Get the amplitude of audio at the end of the pipeline
        /// </summary>
        float Amplitude { get; }

        /// <summary>
        /// Tell the preprocessor how much latency there is in the pipeline before the audio arrives at the preprocessor input
        /// </summary>
        TimeSpan UpstreamLatency { set; }

        /// <summary>
        /// Perform any startup work required by the pipeline before audio arrives
        /// </summary>
        void Start();

        /// <summary>
        /// Get the size of input frames
        /// </summary>
        int OutputFrameSize { get; }

        /// <summary>
        /// Set if the output stream is muted (i.e. audio passing through the preprocessor is not heard by anyone)
        /// </summary>
        bool IsOutputMuted { set; }

        /// <summary>
        /// Subscribe a new mic subscriber to this to receive processed audio data
        /// </summary>
        /// <param name="listener"></param>
        void Subscribe(IMicrophoneSubscriber listener);

        /// <summary>
        /// Unsubscribe a previously subscribed mic subscriber to this to stop receiving processed audio data
        /// </summary>
        /// <param name="listener"></param>
        bool Unsubscribe(IMicrophoneSubscriber listener);

        /// <summary>
        /// Subscribe a new VAD listener to this preprocessor
        /// </summary>
        /// <param name="listener"></param>
        void Subscribe(IVoiceActivationListener listener);

        /// <summary>
        /// Unsubscribe a previously subscribed VAD listener from this preprocessot
        /// </summary>
        /// <param name="listener"></param>
        /// <returns></returns>
        bool Unsubscribe(IVoiceActivationListener listener);
    }
}
