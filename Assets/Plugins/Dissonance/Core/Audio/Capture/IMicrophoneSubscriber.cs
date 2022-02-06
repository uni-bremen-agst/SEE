using System;
using NAudio.Wave;

namespace Dissonance.Audio.Capture
{
    /// <summary>
    /// Interface for things which receive microphone data
    /// </summary>
    public interface IMicrophoneSubscriber
    {
        /// <summary>
        /// Receives PCM data from the microphone
        /// </summary>
        /// <param name="buffer">A buffer of PCM data</param>
        /// <param name="format">The format of the data in the buffer</param>
        void ReceiveMicrophoneData(ArraySegment<float> buffer, WaveFormat format);

        /// <summary>
        /// Reset the subscriber (ending the last stream of data, preparing to start a new one)
        /// </summary>
        void Reset();
    }
}
