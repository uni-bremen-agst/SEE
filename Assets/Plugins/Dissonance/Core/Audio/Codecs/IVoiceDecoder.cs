using System;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Dissonance.Audio.Codecs
{
    internal interface IVoiceDecoder
        : IDisposable
    {
        [NotNull] WaveFormat Format { get; }

        void Reset();

        int Decode(EncodedBuffer input, ArraySegment<float> output);
    }
}
