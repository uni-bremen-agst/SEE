using System;
using System.Collections.Generic;
using System.Threading;
using Dissonance.Config;
using Dissonance.Threading;
using Dissonance.VAD;
using JetBrains.Annotations;
using NAudio.Wave;
using UnityEngine.Profiling;

namespace Dissonance.Audio.Capture
{
    internal abstract class BasePreprocessingPipeline
        : IPreprocessingPipeline
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Recording, typeof(BasePreprocessingPipeline).Name);

        private ArvCalculator _arv = new ArvCalculator();
        public float Amplitude
        {
            get { return _arv.ARV; }
        }

        private int _droppedSamples;

        private readonly object _inputWriteLock = new object();
        private readonly BufferedSampleProvider _resamplerInput;
        private readonly Resampler _resampler;
        private readonly SampleToFrameProvider _resampledOutput;
        private readonly float[] _intermediateFrame;

        private AudioFileWriter _diagnosticOutputRecorder;

        private readonly int _outputFrameSize;
        public int OutputFrameSize
        {
            get { return _outputFrameSize; }
        }

        public abstract bool IsOutputMuted { set; }

        private readonly WaveFormat _outputFormat;
        [NotNull] public WaveFormat OutputFormat { get { return _outputFormat; } }

        private bool _resetApplied;
        private int _resetRequested;

        private volatile bool _runThread;
        private readonly DThread _thread;
        private readonly AutoResetEvent _threadEvent;

        private readonly ReadonlyLockedValue<List<IMicrophoneSubscriber>> _micSubscriptions = new ReadonlyLockedValue<List<IMicrophoneSubscriber>>(new List<IMicrophoneSubscriber>());
        private int _micSubscriptionCount;

        private readonly ReadonlyLockedValue<List<IVoiceActivationListener>> _vadSubscriptions = new ReadonlyLockedValue<List<IVoiceActivationListener>>(new List<IVoiceActivationListener>());
        private int _vadSubscriptionCount;

        protected abstract bool VadIsSpeechDetected { get; }

        private int _upstreamLatencyMs;
        /// <inheritdoc />
        public TimeSpan UpstreamLatency
        {
            get { return TimeSpan.FromMilliseconds(_upstreamLatencyMs); }
            set { _upstreamLatencyMs = (int)value.TotalMilliseconds; }
        }

        private readonly int _estimatedPreprocessorLatencyMs;
        /// <summary>
        /// Latency (in milliseconds) from audio arriving at the microphone to it being preprocessed
        /// </summary>
        protected int PreprocessorLatencyMs
        {
            get { return _upstreamLatencyMs + _estimatedPreprocessorLatencyMs; }
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputFormat">format of audio supplied to `Send`</param>
        /// <param name="intermediateFrameSize">Size of frames which should be passed into `PreprocessAudioFrame`</param>
        /// <param name="intermediateSampleRate">Sample rate which should be passed into `PreprocessAudioFrame`</param>
        /// <param name="outputFrameSize">Size of frames which will be provided sent to `SendSamplesToSubscribers`</param>
        /// <param name="outputSampleRate"></param>
        protected BasePreprocessingPipeline([NotNull] WaveFormat inputFormat, int intermediateFrameSize, int intermediateSampleRate, int outputFrameSize, int outputSampleRate)
        {
            if (inputFormat == null) throw new ArgumentNullException("inputFormat");
            if (intermediateFrameSize < 0) throw new ArgumentOutOfRangeException("intermediateFrameSize", "Intermediate frame size cannot be less than zero");
            if (intermediateSampleRate < 0) throw new ArgumentOutOfRangeException("intermediateSampleRate", "Intermediate sample rate cannot be less than zero");
            if (outputFrameSize < 0) throw new ArgumentOutOfRangeException("outputFrameSize", "Output frame size cannot be less than zero");
            if (outputSampleRate < 0) throw new ArgumentOutOfRangeException("outputSampleRate", "Output sample rate cannot be less than zero");

            _outputFrameSize = outputFrameSize;
            _outputFormat = new WaveFormat(outputSampleRate, 1);

            //Create resampler to resample input to intermediate rate
            _resamplerInput = new BufferedSampleProvider(inputFormat, intermediateFrameSize * 16);
            _resampler = new Resampler(_resamplerInput, 48000);
            _resampledOutput = new SampleToFrameProvider(_resampler, (uint)OutputFrameSize);
            _intermediateFrame = new float[intermediateFrameSize];

            //Create a thread to drive the audio processing
            _threadEvent = new AutoResetEvent(false);
            _thread = new DThread(ThreadEntry);

            // We don't want to overestimate the latency. It's hard to come up with a reasonable number for this because if the input frames are >= the...
            // ...intermediate frames there will actually be no buffering delay in the preprocessor (it'll pick up a complete frame and process it right away).
            _estimatedPreprocessorLatencyMs = 0;
        }

        public virtual void Dispose()
        {
            _runThread = false;
            _threadEvent.Set();
            if (_thread.IsStarted)
                _thread.Join();

            //ncrunch: no coverage start
            if (_diagnosticOutputRecorder != null)
            {
                _diagnosticOutputRecorder.Dispose();
                _diagnosticOutputRecorder = null;
            }
            //ncrunch: no coverage end

            using (var subscriptions = _micSubscriptions.Lock())
                while (subscriptions.Value.Count > 0)
                    Unsubscribe(subscriptions.Value[0]);

            using (var subscriptions = _vadSubscriptions.Lock())
                while (subscriptions.Value.Count > 0)
                    Unsubscribe(subscriptions.Value[0]);

            Log.Debug("Disposed pipeline");
        }

        public void Reset()
        {
            Log.Debug("Reset Requested");

            Interlocked.Exchange(ref _resetRequested, 1);

            //Set the thread event, prompting it to immediately apply the reset
            _threadEvent.Set();
        }

        protected virtual void ApplyReset()
        {
            Log.Debug("Resetting preprocessing pipeline");

            lock (_inputWriteLock)
            {
                _resamplerInput.Reset();
            }

            _resampler.Reset();
            _resampledOutput.Reset();

            _arv.Reset();
            _droppedSamples = 0;

            SendResetToSubscribers();

            _resetApplied = true;
        }

        void IMicrophoneSubscriber.ReceiveMicrophoneData(ArraySegment<float> data, [NotNull] WaveFormat format)
        {
            if (data.Array == null) throw new ArgumentNullException("data");

            // ReSharper disable once InconsistentlySynchronizedField (Justification: `_resamplerInput` is itself thread safe, so using it with synchronisation is unnecessary)
            if (!format.Equals(_resamplerInput.WaveFormat)) throw new ArgumentException("Incorrect format supplied to preprocessor", "format");

            lock (_inputWriteLock)
            {
                // Write as much data into the buffer as possible
                var written = _resamplerInput.Write(data);

                // If not everything was written it means the input buffer is full! The only thing to do is throw
                // away the excess audio and to keep track of exactly how much was lost. The lost samples will be injected
                // as silence, to keep everything in sync.
                if (written < data.Count)
                {
                    var lost = data.Count - written;

                    Interlocked.Add(ref _droppedSamples, lost);
                    Log.Warn("Lost {0} samples in the preprocessor (buffer full), injecting silence to compensate", lost);
                }
            }

            //Wake up the processing thread
            _threadEvent.Set();
        }

        public void Start()
        {
#if !NCRUNCH
            Profiler.BeginSample("BasePreprocessingPipeline: Start");
#endif
            try
            {
                _runThread = true;
                _thread.Start();
            }
            finally
            {

#if !NCRUNCH
                Profiler.EndSample();
#endif
            }

            Log.Debug("Started preprocessor thread. Internal latency: {0}ms, Total stream latency:{1}ms", _estimatedPreprocessorLatencyMs, PreprocessorLatencyMs);
        }

        private void ThreadEntry()
        {
            try
            {
                ThreadStart();

                ApplyReset();

                while (_runThread)
                {
                    //Wait for an event(s) to arrive which we need to process.
                    // ReSharper disable once InconsistentlySynchronizedField (Justification: `_resamplerInput` is itself thread safe, so using it with synchronisation is unnecessary)
                    var resamplerInputCount = _resamplerInput.Count;
                    if (resamplerInputCount < _intermediateFrame.Length)
                        _threadEvent.WaitOne(100);

                    //Record when we started this iteration
                    var startTime = Environment.TickCount;

                    //Apply a pipeline reset if one has been requested, but one has not yet been applied
                    if (Interlocked.Exchange(ref _resetRequested, 0) == 1 && !_resetApplied)
                        ApplyReset();

                    //We're about to process some audio, meaning the pipeline is no longer in a reset state
                    _resetApplied = false;

                    //Reset the external dropped samples counter, we'll inject the appropriate number of samples to compensate
                    var missedSamples = Interlocked.Exchange(ref _droppedSamples, 0);

                    //Pull input data through the resampler and push it through the preprocessor
                    var frameCount = 0;
                    var preSpeech = VadIsSpeechDetected;
                    while (_resampledOutput.Read(new ArraySegment<float>(_intermediateFrame, 0, _intermediateFrame.Length)))
                    {
                        _arv.Update(new ArraySegment<float>(_intermediateFrame));
                        PreprocessAudioFrame(_intermediateFrame);
                        frameCount++;
                    }
                    var postSpeech = VadIsSpeechDetected;

                    //Update the VAD subscribers if necessary
                    if (preSpeech ^ postSpeech)
                    {
                        if (preSpeech)
                            SendStoppedTalking();
                        else
                            SendStartedTalking();
                    }

                    //Preprocess silent frames to make up the missed samples
                    if (missedSamples > 0)
                    {
                        //Inject silent frames directly into the preprocessor
                        Array.Clear(_intermediateFrame, 0, _intermediateFrame.Length);
                        while (missedSamples >= _intermediateFrame.Length)
                        {
                            PreprocessAudioFrame(_intermediateFrame);
                            missedSamples -= _intermediateFrame.Length;
                        }

                        //Add the remaining samples back onto the missed counter (in case we didn't lose exactly one frame)
                        if (missedSamples > 0)
                            Interlocked.Add(ref _droppedSamples, missedSamples);
                    }

                    //Record end time, warn if it took a long time to process the frames (don't warn if wraparound of time occured, i.e. end < start)
                    var endTime = Environment.TickCount;
                    var elapsed = endTime - startTime;
                    if (endTime > startTime && elapsed > (50 + frameCount * 5))
                        Log.Warn("Preprocessor running slow! Iteration took:{0}ms for {1} frames", elapsed, frameCount);
                }
            }
            //ncrunch: no coverage start (Justification: this should never happen, it's just a log in case of sanity failure
            catch (Exception e)
            {
                Log.Error(Log.PossibleBugMessage("Unhandled exception killed audio preprocessor thread: " + e, "02EB75C0-1E12-4109-BFD2-64645C14BD5F"));
            }
            //ncrunch: no coverage end
        }

        protected virtual void ThreadStart()
        {
        }

        /// <summary>
        /// Process a frame of audio. Audio passed to this method arrives in frames of intermediate size/rate (passed into the constructor).
        /// Frames of output size/rate must be passed to SendSamplesToSubscribers.
        /// </summary>
        /// <param name="frame"></param>
        protected abstract void PreprocessAudioFrame([NotNull] float[] frame);

        #region mic subscriptions
        protected void SendSamplesToSubscribers([NotNull] float[] buffer)
        {
            //Write processed audio to file (for diagnostic purposes)
            //ncrunch: no coverage start (Justification: don't want to have to write to disk in a test)
            if (DebugSettings.Instance.EnableRecordingDiagnostics && DebugSettings.Instance.RecordPreprocessorOutput)
            {
                if (_diagnosticOutputRecorder == null)
                {
                    var filename = string.Format("Dissonance_Diagnostics/PreprocessorOutputAudio_{0}", DateTime.UtcNow.ToFileTime());
                    _diagnosticOutputRecorder = new AudioFileWriter(filename, OutputFormat);
                }

                _diagnosticOutputRecorder.WriteSamples(new ArraySegment<float>(buffer));
            }
            //ncrunch: no coverage end

            //Send the audio to subscribers
            using (var unlocker = _micSubscriptions.Lock())
            {
                var subs = unlocker.Value;
                for (var i = 0; i < subs.Count; i++)
                {
                    try
                    {
                        subs[i].ReceiveMicrophoneData(new ArraySegment<float>(buffer), OutputFormat);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Microphone subscriber '{0}' threw: {1}", subs[i].GetType().Name, ex);
                    }
                }
            }
        }

        private void SendResetToSubscribers()
        {
            using (var unlocker = _micSubscriptions.Lock())
            {
                var subs = unlocker.Value;
                for (var i = 0; i < subs.Count; i++)
                {
                    try
                    {
                        subs[i].Reset();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Microphone subscriber '{0}' Reset threw: {1}", subs[i].GetType().Name, ex);
                    }
                }
            }
        }

        public virtual void Subscribe([NotNull] IMicrophoneSubscriber listener)
        {
            if (listener == null) throw new ArgumentNullException("listener");

            using (var subscriptions = _micSubscriptions.Lock())
            {
                subscriptions.Value.Add(listener);
                Interlocked.Increment(ref _micSubscriptionCount);
            }
        }

        public virtual bool Unsubscribe([NotNull] IMicrophoneSubscriber listener)
        {
            if (listener == null) throw new ArgumentNullException("listener");

            using (var subscriptions = _micSubscriptions.Lock())
            {
                var removed = subscriptions.Value.Remove(listener);
                if (removed)
                    Interlocked.Decrement(ref _micSubscriptionCount);
                return removed;
            }
        }
        #endregion

        #region vad subscriptions
        private void SendStoppedTalking()
        {
            using (var unlocker = _vadSubscriptions.Lock())
            {
                var subs = unlocker.Value;
                for (var i = 0; i < subs.Count; i++)
                    SendStoppedTalking(subs[i]);
            }
        }

        private static void SendStoppedTalking([NotNull] IVoiceActivationListener listener)
        {
            try
            {
                listener.VoiceActivationStop();
            }
            catch (Exception ex)
            {
                Log.Error("VAD subscriber '{0}' threw: {1}", listener.GetType().Name, ex);
            }
        }

        private void SendStartedTalking()
        {
            using (var unlocker = _vadSubscriptions.Lock())
            {
                var subs = unlocker.Value;
                for (var i = 0; i < subs.Count; i++)
                    SendStartedTalking(subs[i]);
            }
        }

        private static void SendStartedTalking([NotNull] IVoiceActivationListener listener)
        {
            try
            {
                listener.VoiceActivationStart();
            }
            catch (Exception ex)
            {
                Log.Error("VAD subscriber '{0}' threw: {1}", listener.GetType().Name, ex);
            }
        }

        public virtual void Subscribe([NotNull] IVoiceActivationListener listener)
        {
            if (listener == null) throw new ArgumentNullException("listener");

            using (var subs = _vadSubscriptions.Lock())
            {
                subs.Value.Add(listener);
                Interlocked.Increment(ref _vadSubscriptionCount);

                if (VadIsSpeechDetected)
                    SendStartedTalking(listener);
            }
        }

        public virtual bool Unsubscribe([NotNull] IVoiceActivationListener listener)
        {
            if (listener == null) throw new ArgumentNullException("listener");

            using (var subs = _vadSubscriptions.Lock())
            {
                var removed = subs.Value.Remove(listener);
                if (removed)
                {
                    Interlocked.Decrement(ref _vadSubscriptionCount);

                    if (VadIsSpeechDetected)
                        SendStoppedTalking(listener);
                }
                return removed;
            }
        }
        #endregion
    }
}
