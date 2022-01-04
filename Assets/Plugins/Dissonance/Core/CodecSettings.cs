using Dissonance.Audio.Codecs;

namespace Dissonance
{
    public struct CodecSettings
    {
        private readonly Codec _codec;
        private readonly uint _frameSize;
        private readonly int _sampleRate;

        public CodecSettings(Codec codec, uint frameSize, int sampleRate)
        {
            _codec = codec;
            _frameSize = frameSize;
            _sampleRate = sampleRate;
        }

        public Codec Codec
        {
            get { return _codec; }
        }

        public uint FrameSize
        {
            get { return _frameSize; }
        }

        public int SampleRate
        {
            get { return _sampleRate; }
        }

        public override string ToString()
        {
            return string.Format("Codec: {0}, FrameSize: {1}, SampleRate: {2:##.##}kHz", Codec, FrameSize, SampleRate / 1000f);
        }
    }
}
