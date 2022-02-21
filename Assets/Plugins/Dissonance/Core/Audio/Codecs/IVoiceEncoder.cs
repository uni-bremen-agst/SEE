using System;

namespace Dissonance.Audio.Codecs
{
    internal interface IVoiceEncoder : IDisposable
    {
        /// <summary>
        /// Inform the codec of the current packet loss percentage (0 to 1)
        /// </summary>
        float PacketLoss { set; }

        /// <summary>
        /// Get the frame size this codec expects
        /// </summary>
        int FrameSize { get; }

        /// <summary>
        /// Get the same rate which this codec expects
        /// </summary>
        int SampleRate { get; }

        /// <summary>
        /// Encode a frame of samples (size = FrameSize, rate = SampleRate)
        /// </summary>
        /// <param name="samples">Input samples</param>
        /// <param name="array">Some space to write the result into</param>
        /// <returns>A sub-slice of the output array with the encoded data</returns>
        ArraySegment<byte> Encode(ArraySegment<float> samples, ArraySegment<byte> array);

        /// <summary>
        /// Reset the state of the encoder
        /// </summary>
        void Reset();
    }
}
