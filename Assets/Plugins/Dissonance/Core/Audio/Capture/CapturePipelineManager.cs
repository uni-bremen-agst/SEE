using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dissonance.Networking;
using Dissonance.VAD;
using JetBrains.Annotations;
using NAudio.Wave;
using UnityEngine;
using UnityEngine.Profiling;

namespace Dissonance.Audio.Capture
{
    internal class CapturePipelineManager
        : IAmplitudeProvider, ILossEstimator
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Recording, typeof(CapturePipelineManager).Name);

        private bool _isMobilePlatform;

        private readonly CodecSettingsLoader _codecSettingsLoader;
        private readonly RoomChannels _roomChannels;
        private readonly PlayerChannels _playerChannels;
        private readonly PacketLossMonitor _receivingPacketLossMonitor;
        [CanBeNull] private ICommsNetwork _network;

        private IMicrophoneCapture _microphone;
        private IPreprocessingPipeline _preprocessor;
        private EncoderPipeline _encoder;

        private bool _encounteredFatalException;
        private bool _netModeRequiresPipeline;
        private bool _cannotStartMic;
        private bool _encoderSubscribed;

        private int _startupDelay;

        private FrameSkipDetector _skipDetector = new FrameSkipDetector(
            maxFrameTime: TimeSpan.FromMilliseconds(150),
            minimumBreakerDuration: TimeSpan.FromMilliseconds(350),
            maxBreakerDuration: TimeSpan.FromMilliseconds(10000),
            breakerResetPerSecond: TimeSpan.FromMilliseconds(250)
        );

        private readonly List<IVoiceActivationListener> _activationListeners = new List<IVoiceActivationListener>();
        private readonly List<IMicrophoneSubscriber> _audioListeners = new List<IMicrophoneSubscriber>();

        [CanBeNull] public IMicrophoneCapture Microphone
        {
            get { return _microphone; }
        }

        private string _micName;
        public string MicrophoneName
        {
            get { return _micName; }
            set
            {
                //Early exit if the value isn't actually changing
                // - If the values are the same then obviously there's no changes
                // - Null strings and empty strings both indicate the default mic, so we also consider those equivalent
                if (_micName == value || (string.IsNullOrEmpty(_micName) && string.IsNullOrEmpty(value)))
                {
                    Log.Debug("Not changing microphone device from '{0}' to '{1}' (equivalent devices)", _micName, value);
                    return;
                }

                if (_microphone != null && _microphone.IsRecording)
                    Log.Info("Changing microphone device from '{0}' to '{1}'", _micName, value);
                else
                    Log.Debug("Setting microphone device to '{0}'", value);

                //Save the mic name and force a reset, this will pick up the new name as part of the reset
                _micName = value;
                RestartTransmissionPipeline("Microphone name changed");
            }
        }

        public float PacketLoss
        {
            get { return _receivingPacketLossMonitor.PacketLoss; }
        }

        public float Amplitude
        {
            get { return _preprocessor == null ? 0 : _preprocessor.Amplitude; }
        }

        private bool _pendingResetRequest;
        #endregion

        #region constructor
        public CapturePipelineManager([NotNull] CodecSettingsLoader codecSettingsLoader, [NotNull] RoomChannels roomChannels, [NotNull] PlayerChannels playerChannels, [NotNull] ReadOnlyCollection<VoicePlayerState> players, int startupDelay = 0)
        {
            if (codecSettingsLoader == null) throw new ArgumentNullException("codecSettingsLoader");
            if (roomChannels == null) throw new ArgumentNullException("roomChannels");
            if (playerChannels == null) throw new ArgumentNullException("playerChannels");
            if (players == null) throw new ArgumentNullException("players");

            _codecSettingsLoader = codecSettingsLoader;
            _roomChannels = roomChannels;
            _playerChannels = playerChannels;
            _receivingPacketLossMonitor = new PacketLossMonitor(players);
            _startupDelay = startupDelay;
        }
        #endregion

        public void Start([NotNull] ICommsNetwork network, [NotNull] IMicrophoneCapture microphone)
        {
            if (network == null) throw new ArgumentNullException("network");
            if (microphone == null) throw new ArgumentNullException("microphone");

            _microphone = microphone;
            _network = network;
            AudioSettingsWatcher.Instance.Start();

            Net_ModeChanged(network.Mode);
            network.ModeChanged += Net_ModeChanged;

            AudioSettings.OnAudioConfigurationChanged += OnAudioDeviceChanged;

            _isMobilePlatform = IsMobilePlatform();
        }

        private void OnAudioDeviceChanged(bool devicewaschanged)
        {
            if (devicewaschanged)
                ForceReset();
        }

        private static bool IsMobilePlatform()
        {
            #if UNITY_EDITOR
                // Editor is never a mobile platform, obviously.
                return false;
            #else
                // Override the logic for specific devices
                switch (SystemInfo.deviceModel)
                {
                    // Override Quest 1 and 2 to treat them as desktops. They're
                    // very powerful devices that can handle the desktop audio processing.
                    case "Oculus Quest":
                        return false;
                }

                #if UNITY_ANDROID || UNITY_IOS || UNITY_IPHONE || UNITY_BLACKBERRY || UNITY_WP8 || UNITY_WII || UNITY_TVOS || UNITY_TIZEN || UNITY_WEBGL || PLATFORM_LUMIN
                    //Platforms which we explicitly know are mobile are conditionally compiled to return true
                    // - Wii is included here because it's an old an underpowered platform, so it probably needs to be considered a mobile
                    // - We have no idea what webGL is running on (and the runtime check probably won't help), so we've got assume it's mobile
                    return true;
                #elif UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_PS4 || UNITY_XBOXONE
                    //Platforms which we explicitly know are desktop (or equivalent in power) are conditionally compiled to return false
                    return false;
                #else
                    //We don't know if this is a mobile platform or a desktop platform. Perform a runtime check
                    return UnityEngine.SystemInfo.deviceType == UnityEngine.DeviceType.Handheld;
                #endif
            #endif
        }

        public void Destroy()
        {
            Log.Debug("Destroying");

            if (!ReferenceEquals(_network, null))
                _network.ModeChanged -= Net_ModeChanged;

            AudioSettings.OnAudioConfigurationChanged -= OnAudioDeviceChanged;

            StopTransmissionPipeline();
        }

        // ReSharper disable once InconsistentNaming
        private void Net_ModeChanged(NetworkMode mode)
        {
            _netModeRequiresPipeline = mode.IsClientEnabled();
        }

        public void Update(bool muted, float deltaTime)
        {
            //Delay the initial startup of the capture pipeline. This spreads out the cost of initialising Dissonance over more frames, preventing very large spikes caused by everything being set up at once
            _startupDelay--;
            if (_startupDelay > 0)
                return;

			_receivingPacketLossMonitor.Update();

            //Early exit if we don't need to record audio. Either because:
            // - Netmode doesn't require audio (i.e. dedicated server)
            // - A fatal exception occurred, permanently killing capture
            if (!_netModeRequiresPipeline || _encounteredFatalException || _cannotStartMic)
            {
                StopTransmissionPipeline();
                return;
            }

            //Update microphone and reset it either if there is a frame skip or the microphone requests a reset
            var skipped = _skipDetector.IsFrameSkip(deltaTime);
            var request = _microphone.IsRecording && _microphone.UpdateSubscribers();
            var netmode = _netModeRequiresPipeline && _encoder == null;
            if (skipped || request || _pendingResetRequest || netmode)
            {
                var reason = skipped ? "Detected a frame skip, forcing capture pipeline reset"
                             : netmode ? "Network mode changed"
                             : _pendingResetRequest ? "Applying external reset request"
                             : "Microphone requested a pipeline reset";

                if (skipped)
                {
                    //If warn level logging is turned on show some extra information in the reason (at the cost of a string allocation)
                    if (Log.IsWarn)
                        reason = string.Format("Detected a frame skip, forcing capture pipeline reset (Delta Time:{0})", deltaTime);
                    Log.Warn(reason);
                }

                // Try to start pipeline, early exit if it fails
                RestartTransmissionPipeline(reason);
                if (_preprocessor == null || _cannotStartMic)
                    return;
            }

            // Tell the preprocessor if the output stream is going anywhere (based on if the encoder is subscribed)
            _preprocessor.IsOutputMuted = !_encoderSubscribed;

            if (_encoder != null)
            {
                //If the encoder is finally stopped and still subscribed then unsubscribe and reset it. This puts it into a state ready to be used again.
                if (_encoder.Stopped && _encoderSubscribed)
                {
                    Log.Debug("Unsubscribing encoder from preprocessor");

                    _preprocessor.Unsubscribe(_encoder);
                    _encoder.Reset();
                    _encoderSubscribed = false;
                }

                //Determine if the encoder should be subscribed:
                // - If encoder is stopping (but has not yet stopped) then do not sub
                // - If mute is explicitly set then do not sub
                // - if there are open channels then sub
                var shouldSub = !(_encoder.Stopping && !_encoder.Stopped)
                             && !muted
                             && (_roomChannels.Count + _playerChannels.Count) > 0;

                //Change the encoder state to the desired state
                if (shouldSub != _encoderSubscribed)
                {
                    if (shouldSub)
                    {
                        Log.Debug("Subscribing encoder to preprocessor");

                        _encoder.Reset();
                        _preprocessor.Subscribe(_encoder);
                        _encoderSubscribed = true;
                    }
                    else
                    {
                        //If the encoder has not been told to stop, tell it now
                        if (!_encoder.Stopping)
                        {
                            //Set the encoder state to stopping - it will stop after it sends one final packet to end the stream
                            _encoder.Stop();
                            Log.Debug("Stopping encoder");
                        }
                        else
                        {
                            Log.Debug("Waiting for encoder to send last packet");
                        }
                    }
                }
                else
                {
                    //Log.Debug("Should Sub - Stopping:{0} Stopped:{1} Muted:{1}", _encoder.Stopping, _encoder.Stopped, muted);
                }

                //Propogate measured *incoming* packet loss to encoder as expected *outgoing* packet loss
                if (_encoder != null)
                    _encoder.TransmissionPacketLoss = _receivingPacketLossMonitor.PacketLoss;
            }
        }

        /// <summary>
        /// Immediately stop the entire transmission system
        /// </summary>
        private void StopTransmissionPipeline()
        {
#if !NCRUNCH
            Profiler.BeginSample("CapturePipelineManager: StopTransmissionPipeline");
#endif

            //Stop microphone
            if (_microphone != null && _microphone.IsRecording)
                _microphone.StopCapture();

            //Dispose preprocessor and encoder
            if (_preprocessor != null)
            {
                if (_microphone != null)
                    _microphone.Unsubscribe(_preprocessor);
                if (_encoder != null)
                    _preprocessor.Unsubscribe(_encoder);

                _preprocessor.Dispose();
                _preprocessor = null;
            }

            if (_encoder != null)
            {
                _encoder.Dispose();
                _encoder = null;
            }

            _encoderSubscribed = false;
            
#if !NCRUNCH
            Profiler.EndSample();
#endif
        }

        /// <summary>
        /// (Re)Start the transmission pipeline, getting to a state where we *can* send voice (but aren't yet)
        /// </summary>
        private void RestartTransmissionPipeline(string reason)
        {
            Log.Debug("Restarting transmission pipeline: '{0}'", reason);

            StopTransmissionPipeline();

            //If capture has been specifically disabled, exit out of starting it
            if (_encounteredFatalException)
                return;

#if !NCRUNCH
            Profiler.BeginSample("CapturePipelineManager: RestartTransmissionPipeline");
#endif

            try
            {
                //Clear the flag for requesting an explicit reset.
                //We're about to apply a reset so the request will be satisfied.
                _pendingResetRequest = false;

                //No point starting a transmission pipeline if the network is not a client
                if (_network == null || !_network.Mode.IsClientEnabled())
                    return;

#if UNITY_ANDROID && UNITY_2018_4_OR_NEWER && !UNITY_EDITOR
                // Check if we have permission to use the microphone (android only)
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
                {
                    _cannotStartMic = true;
                    return;
                }
#endif

                //Create new mic capture system
                var format = _microphone.StartCapture(_micName);

                //If we created a mic (can be null if e.g. there is no mic)
                if (format != null)
                {
                    //Close and re-open all channels, propogating this restart to the receiving end
                    _roomChannels.Refresh();
                    _playerChannels.Refresh();

                    //Create preprocessor and subscribe it to microphone (webrtc preprocessor always wants audio to drive VAD+AEC)
                    _preprocessor = CreatePreprocessor(format);
                    _preprocessor.UpstreamLatency = _microphone.Latency;
                    _preprocessor.Start();
                    _microphone.Subscribe(_preprocessor);

                    //Sub VAD listeners to preprocessor
                    for (var i = 0; i < _activationListeners.Count; i++)
                        _preprocessor.Subscribe(_activationListeners[i]);

                    //Sub audio listeners to the preprocessor output
                    for (var i = 0; i < _audioListeners.Count; i++)
                    {
                        var al = _audioListeners[i];
                        al.Reset();
                        _preprocessor.Subscribe(al);
                    }

                    //Create encoder (not yet subscribed to receive audio data, we'll do that later)
                    Log.AssertAndThrowPossibleBug(_network != null, "5F33336B-15B5-4A85-9B54-54352C74768E", "Network object is unexpectedly null");
                    _encoder = new EncoderPipeline(_preprocessor.OutputFormat, _codecSettingsLoader.CreateEncoder(), _network);
                }
                else
                {
                    Log.Warn("Failed to start microphone capture; local voice transmission will be disabled.");
                    _cannotStartMic = true;
                }
            }
            catch (Exception ex)
            {
                //We don't know what happened, but something went wrong. As a precaution kill the transmission pipeline (it will be restarted if necessary)
                StopTransmissionPipeline();

                Log.Error("Unexpected exception encountered starting microphone capture; local voice transmission will be disabled: {0}", ex);
                _encounteredFatalException = true;
            }
            finally
            {
#if !NCRUNCH
                Profiler.EndSample();
#endif
            }
        }

        // ncrunch: no coverage start
        // Justification: we don't want to load the webrtc preprocessing DLL into tests so we're faking a preprocessor in a derived test class)
        [NotNull] protected virtual IPreprocessingPipeline CreatePreprocessor([NotNull] WaveFormat format)
        {
            return new WebRtcPreprocessingPipeline(format, _isMobilePlatform);
            //return new EmptyPreprocessingPipeline(format);
        }
        //ncrunch: no coverage end

        #region VAD subscribers
        public void Subscribe([NotNull] IVoiceActivationListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException("listener", "Cannot subscribe with a null listener");

            _activationListeners.Add(listener);

            if (_preprocessor != null)
                _preprocessor.Subscribe(listener);
        }

        public void Unsubscribe([NotNull] IVoiceActivationListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException("listener", "Cannot unsubscribe with a null listener");

            _activationListeners.Remove(listener);

            if (_preprocessor != null)
                _preprocessor.Unsubscribe(listener);
        }
        #endregion

        #region Audio subscribers
        public void Subscribe([NotNull] IMicrophoneSubscriber listener)
        {
            if (listener == null)
                throw new ArgumentNullException("listener", "Cannot subscribe with a null listener");

            _audioListeners.Add(listener);

            if (_preprocessor != null)
                _preprocessor.Subscribe(listener);
        }

        public void Unsubscribe([NotNull] IMicrophoneSubscriber listener)
        {
            if (listener == null)
                throw new ArgumentNullException("listener", "Cannot unsubscribe with a null listener");

            _audioListeners.Remove(listener);

            if (_preprocessor != null)
                _preprocessor.Unsubscribe(listener);
        }
        #endregion

        // We obviously can't run a realtime audio pipeline while the editor is paused. Encoding happens on...
        // ...another thread but we have to get microphone data on the main thread and that's not going...
        // ...to happen. Stop the pipeline until the editor is unpaused and then resume the pipeline.

        //ncrunch: no coverage start
        internal void Pause()
        {
            StopTransmissionPipeline();
        }

        internal void Resume([CanBeNull] string reason = null)
        {
            RestartTransmissionPipeline(reason ?? "Editor resumed from pause");
        }
        //ncrunch: no coverage end

        public void ForceReset()
        {
            Log.Warn("Forcing capture pipeline reset");

            //Set a flag to request a reset. Netx time the capture pipeline updates it will check and apply this flag
            _pendingResetRequest = true;

            //Clear status flags
            _cannotStartMic = false;
            _encounteredFatalException = false;
        }
    }
}
