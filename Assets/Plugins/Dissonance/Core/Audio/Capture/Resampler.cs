using JetBrains.Annotations;
using NAudio.Dsp;
using NAudio.Wave;

namespace Dissonance.Audio.Capture
{
    /// <summary>
    /// Resample a signal from one sample rate to another
    /// </summary>
    /// <remarks>This is based on the NAudio resampler, but has the added available==0 check in the middle</remarks>
    internal class Resampler
        : ISampleProvider
    {
        private readonly WaveFormat _format;
        public WaveFormat WaveFormat
        {
            get { return _format; }
        }

        [CanBeNull] private readonly WdlResampler _resampler;
        private readonly ISampleProvider _source;

        public Resampler([NotNull] ISampleProvider source, int newSampleRate)
        {
            _source = source;
            _format = new WaveFormat(newSampleRate, source.WaveFormat.Channels);

            if (source.WaveFormat.SampleRate != newSampleRate)
            {
                _resampler = new WdlResampler();
                _resampler.SetMode(true, 2, false);
                _resampler.SetFilterParms();
                _resampler.SetFeedMode(false); // output driven
                _resampler.SetRates(source.WaveFormat.SampleRate, newSampleRate);
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            //If the resampler is null just read from upstream
            if (_resampler == null)
                return _source.Read(buffer, offset, count);

            //Early exit if no data is needed
            if (count == 0)
                return 0;

            var channels = _source.WaveFormat.Channels;

            float[] inBuffer;
            int inBufferOffset;
            var framesRequested = count / channels;
            var inNeeded = _resampler.ResamplePrepare(framesRequested, channels, out inBuffer, out inBufferOffset);
            var inAvailable = _source.Read(inBuffer, inBufferOffset, inNeeded * channels) / channels;

            //Resampler does not handle zero samples well! If we read nothing, return nothing
            if (inAvailable == 0)
                return 0;

            var outAvailable = _resampler.ResampleOut(buffer, offset, inAvailable, framesRequested, channels);
            return outAvailable * channels;
        }

        public void Reset()
        {
            if (_resampler != null)
                _resampler.Reset();
        }
    }
}
