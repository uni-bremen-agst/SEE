using System;
using Dissonance.Extensions;
using NAudio.Wave;
using UnityEngine;

namespace Dissonance.Audio.Playback
{
    /// <summary>
    ///     Ramps volume up over the first frame, and down across the final frame.
    /// </summary>
    internal class VolumeRampedFrameSource : IFrameSource
    {
        private readonly IFrameSource _source;
        private readonly IVolumeProvider _volumeProvider;

        private float _targetVolume;
        private float _currentVolume;

        public VolumeRampedFrameSource(IFrameSource source, IVolumeProvider volumeProvider)
        {
            _source = source;
            _volumeProvider = volumeProvider;
        }

        public uint FrameSize
        {
            get { return _source.FrameSize; }
        }

        public WaveFormat WaveFormat
        {
            get { return _source.WaveFormat; }
        }

        public void Prepare(SessionContext context)
        {
            _source.Prepare(context);
        }

        public bool Read(ArraySegment<float> frame)
        {
            var complete = _source.Read(frame);

            _targetVolume = complete ? 0 : _volumeProvider.TargetVolume;

            // ReSharper disable once CompareOfFloatsByEqualityOperator (Justification: exact float equality is what we want)
            if (_targetVolume == _currentVolume)
                ApplyFlatAttenuation(frame, _currentVolume);
            else
                ApplyRampedAttenuation(frame, _currentVolume, _targetVolume);

            _currentVolume = _targetVolume;

            return complete;
        }

        private static void ApplyFlatAttenuation(ArraySegment<float> frame, float volume)
        {
            if (frame.Array == null)
                throw new ArgumentNullException("frame");

            //Early exit in the (very common) case that volume is exactly 1
            // ReSharper disable once CompareOfFloatsByEqualityOperator (Justification: exact float equality is what we want)
            if (volume == 1)
                return;

            //Just clear the entire array in the (relatively common) case that volume is 0
            // ReSharper disable once CompareOfFloatsByEqualityOperator (Justification: exact float equality is what we want)
            if (volume == 0)
            {
                frame.Clear();
                return;
            }

            //Apply the attenuation to the entire frame
            for (int i = 0; i < frame.Count; i++)
                frame.Array[frame.Offset + i] *= volume;
        }

        private static void ApplyRampedAttenuation(ArraySegment<float> frame, float start, float end)
        {
            if (frame.Array == null)
                throw new ArgumentNullException("frame");

            var step = (end - start) / frame.Count;
            var mul = start;

            for (var i = frame.Offset; i < frame.Offset + frame.Count; i++)
            {
                frame.Array[i] *= mul;
                mul = Mathf.Clamp(mul + step, 0, 1);
            }
        }

        public void Reset()
        {
            _source.Reset();

            //Setting current volume to 0 will cause the very first frame to ramp up to full volume, exactly what we want!
            _currentVolume = 0;
        }
    }

    internal interface IVolumeProvider
    {
        float TargetVolume { get; }
    }
}