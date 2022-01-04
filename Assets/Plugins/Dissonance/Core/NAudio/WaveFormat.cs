// Justification: copied from NAudio, and we want to make the minimal changes possible
// ReSharper disable All

using System;

namespace NAudio.Wave
{
    public sealed class WaveFormat
    {
        private readonly int _channels;
        /// <summary>
        /// Returns the number of channels (1=mono,2=stereo etc)
        /// </summary>
        public int Channels
        {
            get
            {
                return _channels;
            }
        }

        private readonly int _sampleRate;
        /// <summary>
        /// Returns the sample rate (samples per second)
        /// </summary>
        public int SampleRate
        {
            get
            {
                return _sampleRate;
            }
        }

        public WaveFormat(int sampleRate, int channels)
        {
            if (channels > 64)
                throw new ArgumentOutOfRangeException("channels", "More than 64 channels");

            _channels = channels;
            _sampleRate = sampleRate;
        }

        public bool Equals(WaveFormat other)
        {
            return other.Channels == Channels
                && other.SampleRate == SampleRate;
        }

        /// <summary>
        /// Provides a Hashcode for this WaveFormat
        /// </summary>
        /// <returns>A hashcode</returns>
        public override int GetHashCode()
        {
            int hash = 1022251;

            unchecked
            {
                hash += _channels;
                hash *= 16777619;

                hash += _sampleRate;
                hash *= 16777619;
            }

            return hash;
        }

        public override string ToString()
        {
            return string.Format("(Channels:{0}, Rate:{1})", Channels, SampleRate);
        }


    }
}
