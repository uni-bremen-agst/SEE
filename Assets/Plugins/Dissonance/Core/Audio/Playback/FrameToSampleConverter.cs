using System;
using Dissonance.Extensions;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Dissonance.Audio.Playback
{
    /// <summary>
    ///     Allows an arbitrary number of samples be read from an <see cref="IFrameSource" />, reading frames as necessary.
    /// </summary>
    internal class FrameToSampleConverter : ISampleSource
    {
        private static readonly Log Log = Logs.Create(LogCategory.Playback, typeof (FrameToSampleConverter).Name);

        private readonly IFrameSource _source;
        private readonly float[] _temp;

        private bool _upstreamComplete;
        private int _firstSample;
        private int _lastSample;

        public FrameToSampleConverter([NotNull] IFrameSource source)
        {
            if (source == null) throw new ArgumentNullException("source");

            _source = source;
            _temp = new float[source.FrameSize * source.WaveFormat.Channels];
        }

        public WaveFormat WaveFormat
        {
            get { return _source.WaveFormat; }
        }

        public void Prepare(SessionContext context)
        {
            _source.Prepare(context);
        }

        public bool Read(ArraySegment<float> samples)
        {
            var offset = samples.Offset;
            var count = samples.Count;

            Log.Trace("{0} samples requested", samples.Count);

            while (count > 0)
            {
                // if we have data already buffered..
                if (_firstSample < _lastSample)
                {
                    // copy out what we need
                    var c = Math.Min(count, _lastSample - _firstSample);
                    Log.Trace("Transferring {0} buffered samples from previous read", c);
                    // ReSharper disable once AssignNullToNotNullAttribute (Justification: Array segment cannot be null)
                    Buffer.BlockCopy(_temp, _firstSample * sizeof (float), samples.Array, offset * sizeof (float), c * sizeof (float));

                    offset += c;
                    count -= c;
                    _firstSample += c;

                    // if that was the final frame, and we have read all of it
                    if (_upstreamComplete && _firstSample == _lastSample)
                    {
                        // pad the remainder with 0s if we didnt have enough
                        for (var i = offset; i < samples.Offset + samples.Count; i++)
                            samples.Array[i] = 0;

                        Log.Trace("Request satisfied ({0} samples provided with {1} samples 0-padded)", offset - samples.Offset, samples.Count - (offset - samples.Offset));

                        // return that we are complete
                        return true;
                    }
                }

                // break if we have read enough
                if (count == 0)
                    break;

                // if we get here, then we need to read another frame
                _firstSample = 0;
                _lastSample = _temp.Length;

                //If the upstream has already indicated that it is complete there's not a lot we can do!
                //Clear the buffer and return zeroes
                if (_upstreamComplete)
                {
                    Log.Warn(Log.PossibleBugMessage("Attempting to read from a stream which has already finished", "C88903DE-17D4-4341-9AC6-28EB50BCFC8A"));

                    samples.Clear();
                    return true;
                }

                Log.Trace("Reading frame ({0} samples)", _temp.Length);
                _upstreamComplete = _source.Read(new ArraySegment<float>(_temp));
            }

            Log.Trace("Request satisfied ({0} samples provided)", offset - samples.Offset);

            // return that there is more available
            return false;
        }

        public void Reset()
        {
            _firstSample = 0;
            _lastSample = 0;
            _upstreamComplete = false;

            _source.Reset();
        }
    }
}