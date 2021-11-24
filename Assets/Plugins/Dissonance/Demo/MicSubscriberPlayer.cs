using System;
using Dissonance.Audio.Capture;
using Dissonance.Audio.Playback;
using JetBrains.Annotations;
using NAudio.Wave;
using UnityEngine;
using Resampler = Dissonance.Audio.Playback.Resampler;

namespace Dissonance.Demo
{
    public class MicSubscriberPlayer
        : MonoBehaviour, IMicrophoneSubscriber
    {
        private BufferedSampleProvider _inputBuffer;
        private Resampler _output;
        private bool _playing;
        
        private void OnAudioFilterRead([NotNull] float[] data, int channels)
        {
            Array.Clear(data, 0, data.Length);

            var b = _inputBuffer;
            if (b == null)
                return;

            var ramp = false;
            if (!_playing && b.Count > 1000)
            {
                _playing = true;
                ramp = true;
            }

            if (_playing)
            {
                var samples = data.Length / channels;
                var tmp = new float[samples];
                _output.Read(new ArraySegment<float>(tmp, 0, samples));

                if (ramp)
                    for (var i = 0; i < tmp.Length; i++)
                        tmp[i] *= (float)i / tmp.Length;

                var idx = 0;
                for (var i = 0; i < tmp.Length; i++)
                    for (var c = 0; c < channels; c++)
                        data[idx++] = tmp[i];
            }
        }

        public void ReceiveMicrophoneData(ArraySegment<float> buffer, WaveFormat format)
        {
            if (_inputBuffer == null || !_inputBuffer.WaveFormat.Equals(format))
                return;

            _inputBuffer.Write(buffer);
        }

        void IMicrophoneSubscriber.Reset()
        {
            _playing = false;
        }

        public void SetFormat(WaveFormat format)
        {
            _playing = false;
            _inputBuffer = new BufferedSampleProvider(format, 12000);
            _output = new Resampler(new SourceWrapper(_inputBuffer), new ConstantRate());
            _playing = false;
        }

        private class SourceWrapper
            : ISampleSource
        {
            private readonly BufferedSampleProvider _provider;

            public SourceWrapper(BufferedSampleProvider provider)
            {
                _provider = provider;
            }

            public WaveFormat WaveFormat
            {
                get { return _provider.WaveFormat; }
            }

            public void Prepare(SessionContext context)
            {
            }

            public bool Read(ArraySegment<float> samples)
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                Array.Clear(samples.Array, samples.Offset, samples.Count);

                // ReSharper disable once AssignNullToNotNullAttribute
                _provider.Read(samples.Array, samples.Offset, samples.Count);

                return false;
            }

            public void Reset()
            {
                _provider.Reset();
            }
        }

        private class ConstantRate
            : IRateProvider
        {
            public float PlaybackRate
            {
                get { return 1; }
            }
        }
    }
}
