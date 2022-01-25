using System;
using System.Collections.Generic;
using System.Linq;
using Dissonance.Config;
using Dissonance.Datastructures;
using JetBrains.Annotations;
using NAudio.Wave;
using UnityEngine;
using UnityEngine.Profiling;

namespace Dissonance.Audio.Capture
{
    /// <summary>
    /// Captures audio from the microphone using the Unity microphone API
    /// </summary>
    // ReSharper disable once InheritdocConsiderUsage
    public class BasicMicrophoneCapture
        : MonoBehaviour, IMicrophoneCapture, IMicrophoneDeviceList
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Recording, typeof(BasicMicrophoneCapture).Name);

        private byte _maxReadBufferPower;
        private readonly POTBuffer _readBuffer = new POTBuffer(10);
        private BufferedSampleProvider _rawMicSamples;
        private IFrameProvider _rawMicFrames;
        private float[] _frame;

        private WaveFormat _format;
        private AudioClip _clip;
        private int _readHead;
        private bool _started;
        private string _micName;

        private bool _audioDeviceChanged;

        private AudioFileWriter _microphoneDiagnosticOutput;

        private readonly List<IMicrophoneSubscriber> _subscribers = new List<IMicrophoneSubscriber>();

        public string Device
        {
            get { return _micName; }
        }

        public TimeSpan Latency { get; private set; }

        public bool IsRecording
        {
            get { return _clip != null; }
        }
        #endregion

        #region start capture
        public virtual WaveFormat StartCapture(string inputMicName)
        {
            _micName = null;

            try
            {
#if !NCRUNCH
                Profiler.BeginSample("BasicMicrophoneCapture: StartCapture", this);
#endif

                //Sanity checks
                Log.AssertAndThrowPossibleBug(_clip == null, "1BAD3E74-B451-4B7D-A9B9-35225BE55364", "Attempted to Start microphone capture, but capture is already running");

                //Early exit if there are no microphones connected
                if (Log.AssertAndLogWarn(Microphone.devices.Length > 0, "No microphone detected; disabling voice capture"))
                    return null;

                //Check the micName and default to null if it's invalid (all whitespace or not a known device)
                _micName = ChooseMicName(inputMicName);

#if !NCRUNCH
                Profiler.BeginSample("BasicMicrophoneCapture: GetDeviceCaps", this);
#endif
                int sampleRate;
                try
                {
                    //Get device capabilities and choose a sample rate as close to 48000 as possible.
                    //If min and max are both zero that indicates we can use any sample rate
                    int minFreq;
                    int maxFreq;
                    Microphone.GetDeviceCaps(_micName, out minFreq, out maxFreq);
                    sampleRate = minFreq == 0 && maxFreq == 0 ? 48000 : Mathf.Clamp(48000, minFreq, maxFreq);
                    Log.Debug("GetDeviceCaps name=`{0}` min=`{1}` max=`{2}`", _micName, minFreq, maxFreq);
                }
                finally
                {
#if !NCRUNCH
                    Profiler.EndSample();
#endif
                }

#if !NCRUNCH
                Profiler.BeginSample("BasicMicrophoneCapture: Microphone.Start", this);
#endif
                try
                {
                    if (_clip != null)
                    {
                        Destroy(_clip);
                        _clip = null;
                    }

                    //Get the audioclip from Unity for this microphone (with a fairly large internal buffer)
                    _clip = Microphone.Start(_micName, true, 10, sampleRate);
                    if (_clip == null)
                    {
                        Log.Error("Failed to start microphone capture");
                        _micName = null;
                        return null;
                    }
                }
                finally
                {
#if !NCRUNCH
                    Profiler.EndSample();
#endif
                }

                //Setup buffers for capture
                _format = new WaveFormat(_clip.frequency, 1);
                _maxReadBufferPower = (byte)Math.Ceiling(Math.Log(0.1f * _clip.frequency, 2));

                // Create/resize the audio buffers to contain 20ms frames of data. Any frame size will work (the pipeline will buffer/split them as necessary) but 20ms is
                // optimal because that's native frame size the preprocessor works at so it has to do no extra work to assemble the frames at it's desired size.
                var frameSize = (int)(0.02 * _clip.frequency);
                if (_rawMicSamples == null || _rawMicSamples.WaveFormat != _format || _rawMicSamples.Capacity != frameSize || _rawMicFrames.FrameSize != frameSize)
                {
                    _rawMicSamples = new BufferedSampleProvider(_format, frameSize * 4);
                    _rawMicFrames = new SampleToFrameProvider(_rawMicSamples, (uint)frameSize);
                }

                if (_frame == null || _frame.Length != frameSize)
                    _frame = new float[frameSize];

                //watch for device changes - we need to reset if the audio device changes
                AudioSettings.OnAudioConfigurationChanged += OnAudioDeviceChanged;
                _audioDeviceChanged = false;

                //Reset subscribers to prepare them for another stream of data
                for (var i = 0; i < _subscribers.Count; i++)
                    _subscribers[i].Reset();

                Latency = TimeSpan.FromSeconds(frameSize / (float)_format.SampleRate);
                Log.Info("Began mic capture (SampleRate:{0}Hz, FrameSize:{1}, Buffer Limit:2^{2}, Latency:{3}ms, Device:'{4}')", _clip.frequency, frameSize, _maxReadBufferPower, Latency.TotalMilliseconds, _micName);
                return _format;
            }
            finally
            {
#if !NCRUNCH
                Profiler.EndSample();
#endif
            }
        }

        [CanBeNull] private static string ChooseMicName([CanBeNull] string micName)
        {
            if (string.IsNullOrEmpty(micName))
                return null;

            if (!Microphone.devices.Contains(micName))
            {
                Log.Warn("Cannot find microphone '{0}', using default mic", micName);
                return null;
            }

            return micName;
        }
        #endregion

        #region stop capture
        private void OnDestroy()
        {
            if (_clip != null)
            {
                Destroy(_clip);
                _clip = null;
            }
        }

        public virtual void StopCapture()
        {
            Log.AssertAndThrowPossibleBug(_clip != null, "CDDAE69D-44DC-487F-9B69-4703B779400E", "Attempted to stop microphone capture, but it is already stopped");

            //Stop diagnostic output
            if (_microphoneDiagnosticOutput != null)
            {
                _microphoneDiagnosticOutput.Dispose();
                _microphoneDiagnosticOutput = null;
            }

            //Stop capture
            Microphone.End(_micName);
            _format = null;
            _readHead = 0;
            _started = false;
            _micName = null;

            // Destroy the clip
            if (_clip != null)
            {
                Destroy(_clip);
                _clip = null;
            }

            //Clean the buffers
            _rawMicSamples.Reset();
            _rawMicFrames.Reset();

            //Stop watching for device changes
            AudioSettings.OnAudioConfigurationChanged -= OnAudioDeviceChanged;
            _audioDeviceChanged = false;
        }
        #endregion

        private void OnAudioDeviceChanged(bool deviceWasChanged)
        {
            _audioDeviceChanged |= deviceWasChanged;
        }

        #region audio pumping
        //These methods run on the main thread, they drain audio from the mic and send it across to subscribers
        //
        // UpdateSubscribers - Either discard mic samples (if no one is subscribed to mic data) or call DrainMicSamples
        // DrainMicSamples   - Read as many samples as possible from the mic (using a set of pow2 sized buffers to get as
        //                     much as possible), passes samples to ConsumeSamples
        // ConsumeMicSamples - Take some number of samples from produced by DrainMicSamples and buffer them up, call SendFrame
        //                     every time some samples are added to the buffer
        // SendFrame         - Read as many frames as possible from the buffer and send them to the subscribers

        public bool UpdateSubscribers()
        {
            //Don't deliver any audio at all until microphone has initialised (i.e. delivered at least one sample)
            if (!_started)
            {
                _readHead = Microphone.GetPosition(_micName);
                _started = _readHead > 0;

                if (!_started)
                    return false;
            }

            // If the clip capacity is zero then something has gone horribly wrong! This can happen if the microphone hardware fails...
            // ...but we don't get an audio device reset event for some reason. Force the mic to reinitialize (hope it fixes the problem).
            if (_clip.samples == 0)
            {
                Log.Error("Unknown microphone capture error (zero length clip) - restarting mic");
                return true;
            }

            // If the audio device changes then we really don't know what state we're in any more (e.g. the mic could have just been unplugged).
            // Force the mic to reinitialize (setting us back to a known state).
            if (_audioDeviceChanged)
            {
                Log.Debug("Audio device changed - restarting mic");
                return true;
            }

            // If the microphone simply isn't recording at this point something has gone wrong, for example an external script has called Microphone.End
            // or something else has taken exclusive control of the mic. Force a reset and hope that fixes the issue.
            if (!Microphone.IsRecording(_micName))
            {
                Log.Warn("Microphone stopped recording for an unknown reason (possibly due to an external script calling `Microphone.End`");
                return true;
            }

            if (_subscribers.Count > 0)
            {
                //There are subscribers - drain data from mic and pass it on to the subscribers
                DrainMicSamples();
            }
            else
            {
                //No subscribers - discard the data in the mic input buffer
                _readHead = Microphone.GetPosition(_micName);
                _rawMicSamples.Reset();
                _rawMicFrames.Reset();

                if (_microphoneDiagnosticOutput != null)
                {
                    _microphoneDiagnosticOutput.Dispose();
                    _microphoneDiagnosticOutput = null;
                }
            }

            return false;
        }

        private void DrainMicSamples()
        {
            // How many samples has the mic moved since the last time we read from it?
            var writeHead = Microphone.GetPosition(_micName);
            var samplesToRead = (uint)((_clip.samples + writeHead - _readHead) % _clip.samples);

            //Early exit if no samples are available
            if (samplesToRead == 0)
                return;

            //If we're trying to read more data than we have buffer space expand the buffer (up to a max limit)
            //If we're at the max limit, just clamp to buffer size and discard the extra samples
            while (samplesToRead > _readBuffer.MaxCount)
            {
                //absolute max buffer size, we will refuse to expand beyond this
                if (_readBuffer.Pow2 > _maxReadBufferPower || !_readBuffer.Expand())
                {
                    //Work out how many samples we need to drop
                    var skip = Mathf.Min(_clip.samples, samplesToRead - _readBuffer.MaxCount);
                    Log.Warn("Insufficient buffer space, requested {0}, clamped to {1} (dropping {2} samples)", samplesToRead, _readBuffer.MaxCount, skip);

                    //Read as many samples as possible with the limited buffer space available.
                    samplesToRead = _readBuffer.MaxCount;

                    //Skip the head forwards to skip samples we can't read
                    _readHead = (int)((_readHead + skip) % _clip.samples);

                    break;
                }
                else
                {
                    Log.Debug("Trying to read {0} samples, growing read buffer space to {1}", samplesToRead, _readBuffer.MaxCount);
                }
            }

            //Inform the buffer how many samples we want to read
            _readBuffer.Alloc(samplesToRead);
            try
            {
                while (samplesToRead > 0)
                {
                    //Read from mic
                    var buffer = _readBuffer.GetBuffer(ref samplesToRead, true);
                    _clip.GetData(buffer, _readHead);
                    _readHead = (_readHead + buffer.Length) % _clip.samples;

                    //Send samples downstream
                    ConsumeSamples(new ArraySegment<float>(buffer, 0, buffer.Length));
                }
            }
            finally
            {
                _readBuffer.Free();
            }
        }

        /// <summary>
        /// Given some samples consume them (as many as possible at a time) and send frames downstream (as frequently as possible)
        /// </summary>
        /// <param name="samples"></param>
        private void ConsumeSamples(ArraySegment<float> samples)
        {
            if (samples.Array == null)
                throw new ArgumentNullException("samples");

            while (samples.Count > 0)
            {
                //Write as many samples as possible (up to capacity of buffer)
                var written = _rawMicSamples.Write(samples);

                //Shrink the input segment to exclude the samples we just wrote
                // ReSharper disable once AssignNullToNotNullAttribute (Justification: Array segment cannot be null)
                samples = new ArraySegment<float>(samples.Array, samples.Offset + written, samples.Count - written);

                //Drain as many of those samples as possible in frame sized chunks
                SendFrame();
            }
        }

        /// <summary>
        /// Read as many frames as possible from the mic sample buffer and pass them to the encoding thread
        /// </summary>
        private void SendFrame()
        {
            //Drain as many frames as possible
            while (_rawMicSamples.Count > _rawMicFrames.FrameSize)
            {
                //Try to get a frame
                var segment = new ArraySegment<float>(_frame);
                var available = _rawMicFrames.Read(segment);
                if (!available)
                    break;

                //Create diagnostic writer (if necessary)
                if (DebugSettings.Instance.EnableRecordingDiagnostics && DebugSettings.Instance.RecordMicrophoneRawAudio)
                {
                    if (_microphoneDiagnosticOutput == null)
                    {
                        var filename = string.Format("Dissonance_Diagnostics/MicrophoneRawAudio_{0}", DateTime.UtcNow.ToFileTime());
                        _microphoneDiagnosticOutput = new AudioFileWriter(filename, _format);
                    }
                }
                else if (_microphoneDiagnosticOutput != null)
                {
                    _microphoneDiagnosticOutput.Dispose();
                    _microphoneDiagnosticOutput = null;
                }

                //Write out the diagnostic info
                if (_microphoneDiagnosticOutput != null)
                {
                    _microphoneDiagnosticOutput.WriteSamples(segment);
                    _microphoneDiagnosticOutput.Flush();
                }

                //Send frame to subscribers
                for (var i = 0; i < _subscribers.Count; i++)
                    _subscribers[i].ReceiveMicrophoneData(segment, _format);
            }
        }
        #endregion

        #region subscribers
        public void Subscribe(IMicrophoneSubscriber listener)
        {
            if (listener == null) throw new ArgumentNullException("listener");

            _subscribers.Add(listener);
        }

        public bool Unsubscribe(IMicrophoneSubscriber listener)
        {
            if (listener == null) throw new ArgumentNullException("listener");

            return _subscribers.Remove(listener);
        }
        #endregion

        public void GetDevices([NotNull] List<string> output)
        {
            output.AddRange(Microphone.devices);
        }
    }
}
