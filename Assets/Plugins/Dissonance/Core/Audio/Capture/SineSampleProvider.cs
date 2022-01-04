using System;
using NAudio.Wave;

namespace Dissonance.Audio.Capture
{
    internal class SineSampleProvider
        : ISampleProvider
    {
        private readonly WaveFormat _format;
        private readonly float _frequency;
        private readonly double _step;

        private const double TwoPi = Math.PI * 2;

        public float Frequency
        {
            get { return _frequency; }
        }

        public WaveFormat WaveFormat
        {
            get { return _format; }
        }

        private double _index;

        public SineSampleProvider(WaveFormat format, float frequency)
        {
            _format = format;
            _frequency = frequency;
            _step = TwoPi * _frequency / _format.SampleRate;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            for (var i = offset; i < count; i++)
            {
                //Slightly reduce amplitude to prevent minor clipping
                buffer[i] = (float)Math.Sin(_index) * 0.95f;

                //Stay within the 0 -> 2Pi range to prevent "_index" running out of precision
                _index = (_index + _step) % TwoPi;
            }

            return count;
        }
    }
}
