using System;

namespace Dissonance.Audio.Codecs.Identity
{
    internal class IdentityEncoder
        : IVoiceEncoder
    {
        private readonly int _sampleRate;
        private readonly int _frameSize;

        public float PacketLoss
        {
            set { }
        }

        public int FrameSize
        {
            get { return _frameSize; }
        }

        public int SampleRate
        {
            get { return _sampleRate; }
        }

        public IdentityEncoder(int sampleRate, int frameSize)
        {
            _sampleRate = sampleRate;
            _frameSize = frameSize;
        }

        public ArraySegment<byte> Encode(ArraySegment<float> samples, ArraySegment<byte> array)
        {
            var inputArray = samples.Array;
            if (inputArray == null)
                throw new ArgumentNullException("samples");

            var outputArray = array.Array;
            if (outputArray == null)
                throw new ArgumentNullException("array");

            var bytes = samples.Count * sizeof(float);
            if (bytes > array.Count)
                throw new ArgumentException("output buffer is too small");

            Buffer.BlockCopy(inputArray, samples.Offset, outputArray, array.Offset, bytes);

            // ReSharper disable once AssignNullToNotNullAttribute
            return new ArraySegment<byte>(array.Array, array.Offset, bytes);
        }

        public void Reset()
        {
        }

        public void Dispose()
        {
        }
    }
}
