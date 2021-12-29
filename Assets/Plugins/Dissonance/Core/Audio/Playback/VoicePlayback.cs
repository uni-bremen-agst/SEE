using System.Collections.Generic;
using Dissonance.Networking;
using JetBrains.Annotations;
using NAudio.Wave;
using UnityEngine;

namespace Dissonance.Audio.Playback
{
    /// <summary>
    ///     Handles decoding and playing audio for a specific remote player.
    ///     Entities with this behaviour are created automatically by the DissonanceVoiceComms component.
    /// </summary>
    /// ReSharper disable once InheritdocConsiderUsage
    public class VoicePlayback
        : MonoBehaviour, IVoicePlaybackInternal, IVolumeProvider, IRemoteChannelProvider
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Playback, "Voice Playback Component");

        private Transform _transformCache;
        private Transform Transform
        {
            get
            {
                if (_transformCache == null)
                    _transformCache = transform;
                return _transformCache;
            }
        }

        private readonly SpeechSessionStream _sessions;

        private PlaybackOptions _cachedPlaybackOptions;

        // ReSharper disable once MemberCanBePrivate.Global (Justificiation: Public API)
        public AudioSource AudioSource { get; private set; }

        bool IVoicePlaybackInternal.AllowPositionalPlayback { get; set; }

        public bool IsActive
        {
            get { return isActiveAndEnabled; }
        }

        private SamplePlaybackComponent _player;
        private CodecSettings _codecSettings;
        private FrameFormat _frameFormat;
        private float? _savedSpatialBlend;

        /// <summary>
        /// Get the name of the player speaking through this component
        /// </summary>
        public string PlayerName
        {
            get { return _sessions.PlayerName; }
            internal set { _sessions.PlayerName = value; }
        }

        /// <summary>
        /// Get the codec settings used for playback for this player
        /// </summary>
        public CodecSettings CodecSettings
        {
            get { return _codecSettings; }
            internal set
            {
                _codecSettings = value;

                if (_frameFormat.Codec != _codecSettings.Codec
                    || _frameFormat.FrameSize != _codecSettings.FrameSize
                    || _frameFormat.WaveFormat == null
                    || _frameFormat.WaveFormat.SampleRate != _codecSettings.SampleRate)
                {
                    _frameFormat = new FrameFormat(_codecSettings.Codec, new WaveFormat(_codecSettings.SampleRate, 1), _codecSettings.FrameSize);
                }
            }
        }

        /// <inheritdoc />
        public bool IsSpeaking
        {
            get { return _player != null && _player.HasActiveSession; }
        }

        /// <inheritdoc />
        public float Amplitude
        {
            get { return _player == null ? 0 : _player.ARV; }
        }

        /// <summary>
        /// Get the current priority of audio being played through this component
        /// </summary>
        public ChannelPriority Priority
        {
            get
            {
                if (_player == null)
                    return ChannelPriority.None;

                var session = _player.Session;
                if (!session.HasValue)
                    return ChannelPriority.None;

                return _cachedPlaybackOptions.Priority;
            }
        }

        /// <inheritdoc />
        bool IVoicePlaybackInternal.IsMuted { get; set; }

        /// <inheritdoc />
        float IVoicePlaybackInternal.PlaybackVolume { get; set; }

        /// <summary>
        /// Get a value indicating if the playback component is doing basic spatialization itself (incompatible with other spatializers such as the oculus spatializer)
        /// </summary>
        private bool IsApplyingAudioSpatialization { get; set; }

        /// <inheritdoc />
        bool IVoicePlaybackInternal.IsApplyingAudioSpatialization
        {
            get { return IsApplyingAudioSpatialization; }
        }

        internal IPriorityManager PriorityManager { get; set; }

        float? IVoicePlayback.PacketLoss
        {
            get
            {
                var s = _player.Session;
                if (!s.HasValue)
                    return null;

                return s.Value.PacketLoss;
            }
        }

        float IVoicePlayback.Jitter { get { return ((IJitterEstimator)_sessions).Jitter; } }
        #endregion

        public VoicePlayback()
        {
            _sessions = new SpeechSessionStream(this);

            ((IVoicePlaybackInternal)this).PlaybackVolume = 1;
        }

        public void Awake()
        {
            AudioSource = GetComponent<AudioSource>();
            _player = GetComponent<SamplePlaybackComponent>();

            ((IVoicePlaybackInternal)this).Reset();
        }

#pragma warning disable UNT0006 // Incorrect message signature
        void IVoicePlaybackInternal.Reset()
#pragma warning restore UNT0006 // Incorrect message signature
        {
            ((IVoicePlaybackInternal)this).IsMuted = false;
            ((IVoicePlaybackInternal)this).PlaybackVolume = 1;
        }

        public void OnEnable()
        {
            AudioSource.Stop();

            // There is no low-latency way to play back audio into a spatialized AudioSource. Disable spatialization for this source if it's enabled.
            if (AudioSource.spatialize)
            {
                Log.Debug("spatialized AudioSource not supported for voice playback. Setting `spatialize=false`.");
                AudioSource.spatialize = false;
            }

            // Play back a flatline of 1.0 through the source and then multiply the voice signal by that to achieve spatial blending of voice.
            IsApplyingAudioSpatialization = true;
            AudioSource.clip = AudioClip.Create("Flatline", 4096, 1, AudioSettings.outputSampleRate, false, buf =>
            {
                for (var i = 0; i < buf.Length; i++)
                    buf[i] = 1.0f;
            });

            // Set all of the audio source settings that are not allowed to changed
            AudioSource.loop = true;        // Audio must play forever
            AudioSource.pitch = 1;          // Pitch has no effect on the audio
            AudioSource.dopplerLevel = 0;   // Pitch cannot be changed, so doppler makes no sense
            AudioSource.mute = false;       // Muting should be done through the player object, not the source
            AudioSource.priority = 0;       // 0 is the **maximum** priority! Dissonance cannot handle...
                                            // ...virtualised AudioSources, this makes sure it's very unlikely to happen.
        }

        public void OnDisable()
        {
            _sessions.StopSession(false);

            if (AudioSource != null && AudioSource.clip != null)
            {
                var c = AudioSource.clip;
                AudioSource.clip = null;
                Destroy(c);
            }
        }

        public void Update()
        {
            if (!_player.HasActiveSession)
            {
                // We're not playing anything at the moment. Try to get a session to play.
                var s = _sessions.TryDequeueSession();
                if (s.HasValue)
                {
                    _cachedPlaybackOptions = s.Value.PlaybackOptions;
                    _player.Play(s.Value);
                    AudioSource.Play();
                }
                else
                {
                    // No session was available to start playing. Stop the audio source playing to preserve
                    // limited "real voices" in the Unity audio mixer.
                    if (AudioSource.isPlaying)
                        AudioSource.Stop();
                }
            }

            //Sanity check that the AudioSource has not been muted. Doing this will stop the playback pipeline from running, causing encoded audio to backup as it waits for playback.
            if (AudioSource.mute)
            {
                Log.Warn("Voice AudioSource was muted, unmuting source. " +
                         "To mute a specific Dissonance player see: https://placeholder-software.co.uk/dissonance/docs/Reference/Other/VoicePlayerState.html#islocallymuted-bool");
                AudioSource.mute = false;
            }

            //Enable or disable positional playback depending upon if it's avilable for this speaker
            UpdatePositionalPlayback();
        }

        private void UpdatePositionalPlayback()
        {
            var session = _player.Session;
            if (session.HasValue)
            {
                //Unconditionally copy across the playback options into the cache once a frame.
                _cachedPlaybackOptions = session.Value.PlaybackOptions;

                if (((IVoicePlaybackInternal)this).AllowPositionalPlayback && _cachedPlaybackOptions.IsPositional)
                {
                    if (_savedSpatialBlend.HasValue)
                    {
                        Log.Debug("Changing to positional playback for {0}", PlayerName);
                        AudioSource.spatialBlend = _savedSpatialBlend.Value;
                        _savedSpatialBlend = null;
                    }
                }
                else
                {
                    if (!_savedSpatialBlend.HasValue)
                    {
                        Log.Debug("Changing to non-positional playback for {0}", PlayerName);
                        _savedSpatialBlend = AudioSource.spatialBlend;
                        AudioSource.spatialBlend = 0;
                    }
                }
            }
        }

        void IVoicePlaybackInternal.SetTransform(Vector3 pos, Quaternion rot)
        {
            var t = Transform;
            t.position = pos;
            t.rotation = rot;
        }

        void IVoicePlaybackInternal.StartPlayback()
        {
            _sessions.StartSession(_frameFormat);
        }

        void IVoicePlaybackInternal.StopPlayback()
        {
            _sessions.StopSession();
        }

        void IVoicePlaybackInternal.ReceiveAudioPacket(VoicePacket packet)
        {
            _sessions.ReceiveFrame(packet);
        }

        public void ForceReset()
        {
            _sessions.ForceReset();
        }

        /// <summary>
        /// Upstream volume setting (if null assume 1)
        /// </summary>
        [CanBeNull] internal IVolumeProvider VolumeProvider
        {
            get;
            set;
        }

        float IVolumeProvider.TargetVolume
        {
            get
            {
                //Mute if explicitly muted
                if (((IVoicePlaybackInternal)this).IsMuted)
                    return 0;

                //Mute if the top priority is greater than this priority
                if (PriorityManager != null && PriorityManager.TopPriority > Priority)
                    return 0;

                //Get the upstream volume setting (if there is one - default to 1 otherwise)
                var v = VolumeProvider;
                var upstream = v == null ? 1 : v.TargetVolume;

                //No muting applied, so play at chosen volume
                return ((IVoicePlaybackInternal)this).PlaybackVolume * upstream;
            }
        }

        void IRemoteChannelProvider.GetRemoteChannels(List<RemoteChannel> output)
        {
            output.Clear();

            if (_player == null)
                return;

            var s = _player.Session;
            if (!s.HasValue)
                return;

            s.Value.Channels.GetRemoteChannels(output);
        }
    }
}