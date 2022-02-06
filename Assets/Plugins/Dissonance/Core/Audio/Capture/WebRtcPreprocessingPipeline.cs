using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Dissonance.Config;
using NAudio.Wave;
using Dissonance.Threading;
using JetBrains.Annotations;

namespace Dissonance.Audio.Capture
{
    internal class WebRtcPreprocessingPipeline
        : BasePreprocessingPipeline
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Recording, typeof(WebRtcPreprocessingPipeline).Name);

        private bool _isVadDetectingSpeech;
        protected override bool VadIsSpeechDetected
        {
            get { return _isVadDetectingSpeech; }
        }

        private readonly bool _isMobilePlatform;

        private WebRtcPreprocessor _preprocessor;
        private RnnoisePreprocessor _rnnoise;

        private bool _isOutputMuted;
        public override bool IsOutputMuted
        {
            set
            {
                _isOutputMuted = value;
            }
        }
        #endregion

        #region construction
        public WebRtcPreprocessingPipeline([NotNull] WaveFormat inputFormat, bool mobilePlatform)
            : base(inputFormat, 480, 48000, 480, 48000)
        {
            _isMobilePlatform = mobilePlatform;
        }
        #endregion

        protected override void ThreadStart()
        {
            _preprocessor = new WebRtcPreprocessor(_isMobilePlatform);
            _rnnoise = new RnnoisePreprocessor();

            base.ThreadStart();
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_preprocessor != null)
                _preprocessor.Dispose();
            if (_rnnoise != null)
                _rnnoise.Dispose();
        }

        protected override void ApplyReset()
        {
            if (_preprocessor != null)
                _preprocessor.Reset();
            if (_rnnoise != null)
                _rnnoise.Reset();

            base.ApplyReset();
        }

        protected override void PreprocessAudioFrame(float[] frame)
        {
            var config = AudioSettingsWatcher.Instance.Configuration;

            var captureLatencyMs = PreprocessorLatencyMs;
            var playbackLatencyMs = (int)(1000 * ((float)config.dspBufferSize / config.sampleRate));
            var latencyMs = captureLatencyMs + playbackLatencyMs;

            // Process through rnnoise to remove background sounds
            _rnnoise.Process(AudioPluginDissonanceNative.SampleRates.SampleRate48KHz, frame, frame);

            // Process through webrtc to do the rest of the audio processing
            _isVadDetectingSpeech = _preprocessor.Process(AudioPluginDissonanceNative.SampleRates.SampleRate48KHz, frame, frame, latencyMs, _isOutputMuted);

            SendSamplesToSubscribers(frame);
        }

        internal static AudioPluginDissonanceNative.FilterState GetAecFilterState()
        {
            return (AudioPluginDissonanceNative.FilterState)AudioPluginDissonanceNative.Dissonance_GetFilterState();
        }

        internal int GetBackgroundNoiseRemovalGains(float[] output)
        {
            var r = _rnnoise;
            if (r != null)
                return r.GetGains(output);
            return 0;
        }

        internal sealed class WebRtcPreprocessor
            : IDisposable
        {
            #region properties and fields
            private readonly LockedValue<IntPtr> _handle;

            private readonly List<PropertyChangedEventHandler> _subscribed = new List<PropertyChangedEventHandler>();

            private readonly bool _useMobileAec;

            private NoiseSuppressionLevels _nsLevel;
            private NoiseSuppressionLevels NoiseSuppressionLevel
            {
                get { return _nsLevel; }
                set
                {
                    using (var handle = _handle.Lock())
                    {
                        //Lumin (magic leap) has built in noise suppression applied to the mic signal before we even get it. This disables the Dissonance Noise suppressor.
                        #if PLATFORM_LUMIN && !UNITY_EDITOR
                            _nsLevel = NoiseSuppressionLevels.Disabled;
                            Log.Debug("`NoiseSuppressionLevel` was set to `{0}` but PLATFORM_LUMIN is defined, overriding to `Disabled`");
                        #else
                            _nsLevel = value;
                            if (handle.Value != IntPtr.Zero)
                                AudioPluginDissonanceNative.Dissonance_ConfigureNoiseSuppression(handle.Value, _nsLevel);
                        #endif
                    }
                }
            }

            private VadSensitivityLevels _vadlevel;
            private VadSensitivityLevels VadSensitivityLevel
            {
                get { return _vadlevel; }
                set
                {
                    using (var handle = _handle.Lock())
                    {
                        _vadlevel = value;
                        if (handle.Value != IntPtr.Zero)
                            AudioPluginDissonanceNative.Dissonance_ConfigureVadSensitivity(handle.Value, _vadlevel);
                    }
                }
            }

            private AecSuppressionLevels _aecLevel;
            private AecSuppressionLevels AecSuppressionLevel
            {
                get { return _aecLevel; }
                set
                {
                    using (var handle = _handle.Lock())
                    {
                        // Lumin (magic leap) has built in AEC applied to the mic signal before we even get it. This disables the Dissonance AEC.
                        // Technically this is the desktop AEC so it shouldn't even be running on the magic leap anyway.
                        #if PLATFORM_LUMIN && !UNITY_EDITOR
                            value = AecSuppressionLevels.Disabled;
                            Log.Debug("`AecSuppressionLevel` was set to `{0}` but PLATFORM_LUMIN is defined, overriding to `Disabled`");
                        #else
                            _aecLevel = value;
                            if (!_useMobileAec)
                            {
                                if (handle.Value != IntPtr.Zero)
                                    AudioPluginDissonanceNative.Dissonance_ConfigureAecSuppression(handle.Value, _aecLevel, AecmRoutingMode.Disabled);
                            }
                        #endif
                    }
                }
            }

            private AecmRoutingMode _aecmLevel;
            private AecmRoutingMode AecmSuppressionLevel
            {
                get { return _aecmLevel; }
                set
                {
                    using (var handle = _handle.Lock())
                    {
                        //Lumin (magic leap) has built in AEC applied to the mic signal before we even get it. This disables the Dissonance AECM.
                        #if PLATFORM_LUMIN && !UNITY_EDITOR
                            value = AecmRoutingMode.Disabled;
                            Log.Debug("`AecmSuppressionLevel` was set to `{0}` but PLATFORM_LUMIN is defined, overriding to `Disabled`");
                        #else
                            _aecmLevel = value;
                            if (_useMobileAec)
                            {
                                if (handle.Value != IntPtr.Zero)
                                    AudioPluginDissonanceNative.Dissonance_ConfigureAecSuppression(handle.Value, AecSuppressionLevels.Disabled, _aecmLevel);
                            }
                        #endif
                    }
                }
            }
            #endregion

            public WebRtcPreprocessor(bool useMobileAec)
            {
                _useMobileAec = useMobileAec;
                _handle = new LockedValue<IntPtr>(IntPtr.Zero);

                NoiseSuppressionLevel = VoiceSettings.Instance.DenoiseAmount;
                AecSuppressionLevel = VoiceSettings.Instance.AecSuppressionAmount;
                AecmSuppressionLevel = VoiceSettings.Instance.AecmRoutingMode;
                VadSensitivityLevel = VoiceSettings.Instance.VadSensitivity;
            }

            public bool Process(AudioPluginDissonanceNative.SampleRates inputSampleRate, float[] input, float[] output, int estimatedStreamDelay, bool isOutputMuted)
            {
                using (var handle = _handle.Lock())
                {
                    if (handle.Value == IntPtr.Zero)
                        throw Log.CreatePossibleBugException("Attempted  to access a null WebRtc Preprocessor encoder", "5C97EF6A-353B-4B96-871F-1073746B5708");

                    AudioPluginDissonanceNative.Dissonance_SetAgcIsOutputMutedState(handle.Value, isOutputMuted);
                    Log.Trace("Set IsOutputMuted to `{0}`", isOutputMuted);

                    var result = AudioPluginDissonanceNative.Dissonance_PreprocessCaptureFrame(handle.Value, (int)inputSampleRate, input, output, estimatedStreamDelay);
                    if (result != AudioPluginDissonanceNative.ProcessorErrors.Ok)
                        throw Log.CreatePossibleBugException(string.Format("Preprocessor error: '{0}'", result), "0A89A5E7-F527-4856-BA01-5A19578C6D88");

                    return AudioPluginDissonanceNative.Dissonance_GetVadSpeechState(handle.Value);
                }
            }

            public void Reset()
            {
                using (var handle = _handle.Lock())
                {
                    Log.Debug("Resetting WebRtcPreprocessor");

                    if (handle.Value != IntPtr.Zero)
                    {
                        //Clear from playback filter. This internally acquires a lock and will not complete until it is safe to (i.e. no one else is using the preprocessor concurrently).
                        ClearFilterPreprocessor();

                        //Destroy it
                        AudioPluginDissonanceNative.Dissonance_DestroyPreprocessor(handle.Value);
                        handle.Value = IntPtr.Zero;
                    }

                    //Create a new one
                    handle.Value = CreatePreprocessor();

                    //Associate with playback filter
                    SetFilterPreprocessor(handle.Value);
                }
            }

            private IntPtr CreatePreprocessor()
            {
                var instance = VoiceSettings.Instance;

                //Disable one of the echo cancellers, depending upon platform
                var pcLevel = AecSuppressionLevel;
                var mobLevel = AecmSuppressionLevel;
                if (_useMobileAec)
                    pcLevel = AecSuppressionLevels.Disabled;
                else
                    mobLevel = AecmRoutingMode.Disabled;

                Log.Debug("Creating new preprocessor instance - Mob:{0} NS:{1} AEC:{2} DelayAg:{3} Ext:{4}, Refined:{5} Aecm:{6}, Comfort:{7}",
                    _useMobileAec,
                    NoiseSuppressionLevel,
                    pcLevel,
                    instance.AecDelayAgnostic,
                    instance.AecExtendedFilter,
                    instance.AecRefinedAdaptiveFilter,
                    mobLevel,
                    instance.AecmComfortNoise
                );

                var handle = AudioPluginDissonanceNative.Dissonance_CreatePreprocessor(
                    NoiseSuppressionLevel,
                    pcLevel,
                    instance.AecDelayAgnostic,
                    instance.AecExtendedFilter,
                    instance.AecRefinedAdaptiveFilter,
                    mobLevel,
                    instance.AecmComfortNoise
                );

                AudioPluginDissonanceNative.Dissonance_ConfigureVadSensitivity(handle, instance.VadSensitivity);

                return handle;
            }

            private void SetFilterPreprocessor(IntPtr preprocessor)
            {
                using (var handle = _handle.Lock())
                {
                    if (handle.Value == IntPtr.Zero)
                        throw Log.CreatePossibleBugException("Attempted  to access a null WebRtc Preprocessor encoder", "3BA66D46-A7A6-41E8-BE38-52AFE5212ACD");

                    Log.Debug("Exchanging preprocessor instance in playback filter...");

                    if (!AudioPluginDissonanceNative.Dissonance_PreprocessorExchangeInstance(IntPtr.Zero, handle.Value))
                        throw Log.CreatePossibleBugException("Cannot associate preprocessor with Playback filter - one already exists", "D5862DD2-B44E-4605-8D1C-29DD2C72A70C");

                    Log.Debug("...Exchanged preprocessor instance in playback filter");

                    var state = (AudioPluginDissonanceNative.FilterState)AudioPluginDissonanceNative.Dissonance_GetFilterState();
                    if (state == AudioPluginDissonanceNative.FilterState.FilterNotRunning)
                        Log.Debug("Associated preprocessor with playback filter - but filter is not running");

                    Bind(s => s.DenoiseAmount, "DenoiseAmount", v => NoiseSuppressionLevel = (NoiseSuppressionLevels)v);
                    Bind(s => s.AecSuppressionAmount, "AecSuppressionAmount", v => AecSuppressionLevel = (AecSuppressionLevels)v);
                    Bind(s => s.AecmRoutingMode, "AecmRoutingMode", v => AecmSuppressionLevel = (AecmRoutingMode)v);
                    Bind(s => s.VadSensitivity, "VadSensitivity", v => VadSensitivityLevel = v);
                }
            }

            private void Bind<T>(Func<VoiceSettings, T> getValue, string propertyName, Action<T> setValue)
            {
                var settings = VoiceSettings.Instance;

                //Bind for value changes in the future
                PropertyChangedEventHandler subbed;
                settings.PropertyChanged += subbed = (sender, args) => {
                    if (args.PropertyName == propertyName)
                        setValue(getValue(settings));
                };

                //Save this subscription so we can *unsub* later
                _subscribed.Add(subbed);

                //Invoke immediately to pull the current value
                subbed.Invoke(settings, new PropertyChangedEventArgs(propertyName));
            }

            private bool ClearFilterPreprocessor(bool throwOnError = true)
            {
                using (var handle = _handle.Lock())
                {
                    if (handle.Value == IntPtr.Zero)
                        throw Log.CreatePossibleBugException("Attempted to access a null WebRtc Preprocessor encoder", "2DBC7779-F1B9-45F2-9372-3268FD8D7EBA");

                    Log.Debug("Clearing preprocessor instance in playback filter...");

                    //Clear binding in native code
                    if (!AudioPluginDissonanceNative.Dissonance_PreprocessorExchangeInstance(handle.Value, IntPtr.Zero))
                    {
                        if (throwOnError)
                            throw Log.CreatePossibleBugException("Cannot clear preprocessor from Playback filter. Editor restart required!", "6323106A-04BD-4217-9ECA-6FD49BF04FF0");
                        else
                            Log.Error("Failed to clear preprocessor from playback filter. Editor restart required!", "CBC6D727-BE07-4073-AA5A-F750A0CC023D");

                        return false;
                    }

                    //Clear event handlers from voice settings
                    var settings = VoiceSettings.Instance;
                    for (var i = 0; i < _subscribed.Count; i++)
                        settings.PropertyChanged -= _subscribed[i];
                    _subscribed.Clear();

                    Log.Debug("...Cleared preprocessor instance in playback filter");
                    return true;
                }
            }

            #region dispose
            private void ReleaseUnmanagedResources()
            {
                using (var handle = _handle.Lock())
                {
                    if (handle.Value != IntPtr.Zero)
                    {
                        ClearFilterPreprocessor(throwOnError: false);

                        AudioPluginDissonanceNative.Dissonance_DestroyPreprocessor(handle.Value);
                        handle.Value = IntPtr.Zero;
                    }
                }
            }

            public void Dispose()
            {
                ReleaseUnmanagedResources();
                GC.SuppressFinalize(this);
            }

            ~WebRtcPreprocessor()
            {
                ReleaseUnmanagedResources();
            }
            #endregion
        }

        internal sealed class RnnoisePreprocessor
            : IDisposable
        {
            private bool _enabled;
            private bool Enabled
            {
                get { return _enabled; }
                set
                {
                    if (_enabled == value)
                        return;

                    using (var handle = _handle.Lock())
                    {
                        if (value)
                        {
                            if (handle.Value == IntPtr.Zero)
                                handle.Value = AudioPluginDissonanceNative.Dissonance_CreateRnnoiseState();
                        }
                        else
                        {
                            if (handle.Value != IntPtr.Zero)
                            {
                                AudioPluginDissonanceNative.Dissonance_DestroyRnnoiseState(handle.Value);
                                handle.Value = IntPtr.Zero;
                            }
                        }

                        //Lumin (magic leap) has built in noise suppression applied to the mic signal before we even get it. This disables the Dissonance Noise suppressor.
                        #if PLATFORM_LUMIN && !UNITY_EDITOR
                            _enabled = false;
                            Log.Debug("`Background Sound Suppression` was set to `{0}` but PLATFORM_LUMIN is defined, overriding to `false`");
                        #else
                            _enabled = value;
                        #endif
                    }
                }
            }

            private float _wetMix;

            private readonly LockedValue<IntPtr> _handle;
            private readonly List<PropertyChangedEventHandler> _subscribed = new List<PropertyChangedEventHandler>();

            private float[] _temp;

            public RnnoisePreprocessor()
            {
                _handle = new LockedValue<IntPtr>(IntPtr.Zero);

                Bind(v => v.BackgroundSoundRemovalEnabled, "BackgroundSoundRemovalEnabled", a => Enabled = a);
                Bind(v => v.BackgroundSoundRemovalAmount, "BackgroundSoundRemovalAmount", a => _wetMix = a);
            }

            private void Bind<T>(Func<VoiceSettings, T> getValue, string propertyName, Action<T> setValue)
            {
                var settings = VoiceSettings.Instance;

                //Bind for value changes in the future
                PropertyChangedEventHandler subbed;
                settings.PropertyChanged += subbed = (sender, args) => {
                    if (args.PropertyName == propertyName)
                        setValue(getValue(settings));
                };

                //Save this subscription so we can *unsub* later
                _subscribed.Add(subbed);

                //Invoke immediately to pull the current value
                subbed.Invoke(settings, new PropertyChangedEventArgs(propertyName));
            }

            public void Reset()
            {
                using (var handle = _handle.Lock())
                {
                    Log.Debug("Resetting RnnoisePreprocessor");

                    if (handle.Value != IntPtr.Zero)
                    {
                        //Destroy it
                        AudioPluginDissonanceNative.Dissonance_DestroyRnnoiseState(handle.Value);
                        handle.Value = IntPtr.Zero;
                    }

                    //Create a new one
                    handle.Value = AudioPluginDissonanceNative.Dissonance_CreateRnnoiseState();
                }
            }

            public void Process(AudioPluginDissonanceNative.SampleRates inputSampleRate, float[] input, float[] output)
            {
                if (Enabled)
                {
                    using (var handle = _handle.Lock())
                    {
                        if (handle.Value == IntPtr.Zero)
                            throw Log.CreatePossibleBugException("Attempted to access a null WebRtc Rnnoise", "1014ecad-f1cf-4377-a2cd-31e46df55b08");

                        // Allocate a temporary buffer to store the output of rnnoise
                        if (_temp == null || _temp.Length != output.Length)
                            _temp = new float[output.Length];

                        if (!AudioPluginDissonanceNative.Dissonance_RnnoiseProcessFrame(handle.Value, input.Length, (int)inputSampleRate, input, _temp))
                            Log.Warn("Dissonance_RnnoiseProcessFrame returned false");

                        // Linear crossfade between input(dry) and _temp(wet). This is a linear crossfade instead of an
                        // equal power crossfade because input and wet are very highly correlated in most cases. Also
                        // the AGC runs after this, so a slight drop in amplitude won't really matter.
                        var wetmix = _wetMix;
                        var drymix = 1 - wetmix;
                        for (var i = 0; i < input.Length; i++)
                            output[i] = input[i] * drymix + _temp[i] * wetmix;
                    }
                }
                else
                {
                    if (input != output)
                        Array.Copy(input, output, input.Length);
                }
            }

            public int GetGains(float[] output)
            {
                using (var handle = _handle.Lock())
                {
                    if (handle.Value == IntPtr.Zero)
                        return 0;

                    return AudioPluginDissonanceNative.Dissonance_RnnoiseGetGains(handle.Value, output, output.Length);
                }
            }

            #region dispose
            private void ReleaseUnmanagedResources()
            {
                using (var handle = _handle.Lock())
                {
                    if (handle.Value != IntPtr.Zero)
                    {
                        AudioPluginDissonanceNative.Dissonance_DestroyRnnoiseState(handle.Value);
                        handle.Value = IntPtr.Zero;
                    }
                }
            }

            public void Dispose()
            {
                //Clear event handlers from voice settings
                var settings = VoiceSettings.Instance;
                for (var i = 0; i < _subscribed.Count; i++)
                    settings.PropertyChanged -= _subscribed[i];
                _subscribed.Clear();

                ReleaseUnmanagedResources();
                GC.SuppressFinalize(this);
            }

            ~RnnoisePreprocessor()
            {
                ReleaseUnmanagedResources();
            }
            #endregion
        }
    }
}
