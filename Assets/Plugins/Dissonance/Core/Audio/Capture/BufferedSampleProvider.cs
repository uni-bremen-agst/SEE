using System;
using Dissonance.Datastructures;
using NAudio.Wave;

namespace Dissonance.Audio.Capture
{
    /// <summary>
    /// A sample provider which reads from an internal buffer of samples
    /// </summary>
    internal class BufferedSampleProvider
        : ISampleProvider
    {
        public int Count
        {
            get { return _samples.EstimatedUnreadCount; }
        }

        public int Capacity
        {
            get { return _samples.Capacity; }
        }

        private readonly WaveFormat _format;
        /// <inheritdoc />
        public WaveFormat WaveFormat
        {
            get { return _format; }
        }

        private readonly TransferBuffer<float> _samples;

        public BufferedSampleProvider(WaveFormat format, int bufferSize)
        {
            _format = format;
            _samples = new TransferBuffer<float>(bufferSize);
        }

        /// <inheritdoc />
        public int Read(float[] buffer, int offset, int count)
        {
            if (!_samples.Read(new ArraySegment<float>(buffer, offset, count)))
                return 0;
            return count;
        }

        /// <summary>
        /// Write data into the buffer
        /// </summary>
        /// <param name="data"></param>
        /// <returns>The amount of data written into the buffer</returns>
        public int Write(ArraySegment<float> data)
        {
            if (data.Array == null)
                throw new ArgumentNullException("data");

            return _samples.WriteSome(data);
        }

        public void Reset()
        {
            _samples.Clear();
        }
    }
}
