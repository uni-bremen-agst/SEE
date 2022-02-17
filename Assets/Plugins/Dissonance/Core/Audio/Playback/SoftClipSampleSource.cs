using System;
using Dissonance.Audio.Codecs.Opus;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Dissonance.Audio.Playback
{
    internal class SoftClipSampleSource
        : ISampleSource
    {
        private readonly ISampleSource _upstream;
        private readonly OpusNative.OpusSoftClip _clipper;

        public WaveFormat WaveFormat
        {
            get { return _upstream.WaveFormat; }
        }

        public SoftClipSampleSource([NotNull] ISampleSource upstream)
        {
            if (upstream == null) throw new ArgumentNullException("upstream");

            _upstream = upstream;
            _clipper = new OpusNative.OpusSoftClip(upstream.WaveFormat.Channels);
        }

        public void Prepare(SessionContext context)
        {
            _upstream.Prepare(context);
        }

        public bool Read(ArraySegment<float> samples)
        {
            var result = _upstream.Read(samples);
            _clipper.Clip(samples);
            return result;
        }

        public void Reset()
        {
            _clipper.Reset();

            _upstream.Reset();
        }
    }
}
