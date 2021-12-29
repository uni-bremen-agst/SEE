using System;

namespace Dissonance.Audio.Codecs
{
    public struct EncodedBuffer
    {
        /// <summary>
        /// A buffer of encoded data to decode.
        /// </summary>
        public readonly ArraySegment<byte>? Encoded;

        /// <summary>
        /// Indicates if the packet to decode was lost. The `Encoded` buffer may be null in this case.
        /// If not null it will be the _next_ buffer in the stream of encoded data.
        /// </summary>
        public readonly bool PacketLost;

        public EncodedBuffer(ArraySegment<byte>? encoded, bool packetLost)
        {
            Encoded = encoded;
            PacketLost = packetLost;
        }
    }
}
