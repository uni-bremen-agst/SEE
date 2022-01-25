using System;
using Dissonance.Audio.Codecs;
using NAudio.Wave;

namespace Dissonance.Audio.Playback
{
    internal struct FrameFormat
        : IEquatable<FrameFormat>
    {
        public readonly Codec Codec;
        public readonly WaveFormat WaveFormat;
        public readonly uint FrameSize;

        public FrameFormat(Codec codec, WaveFormat waveFormat, uint frameSize)
        {
            Codec = codec;
            WaveFormat = waveFormat;
            FrameSize = frameSize;
        }

        public override int GetHashCode()
        {
            var hash = 103577;

            unchecked
            {
                hash += ((int)Codec) + 17;
                hash *= 101117;

                hash += (WaveFormat.GetHashCode());
                hash *= 101117;

                hash += ((int)FrameSize);
                hash *= 101117;
            }

            return hash;
        }

        public bool Equals(FrameFormat other)
        {
            if (Codec != other.Codec)
                return false;

            if (FrameSize != other.FrameSize)
                return false;

            if (!WaveFormat.Equals(other.WaveFormat))
                return false;

            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is FrameFormat && Equals((FrameFormat)obj);
        }
    }
}