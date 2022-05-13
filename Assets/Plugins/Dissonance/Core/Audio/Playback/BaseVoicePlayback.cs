using System.Collections.Generic;
using Dissonance.Networking;
using NAudio.Wave;
using UnityEngine;

namespace Dissonance.Audio.Playback
{
    public abstract class BaseVoicePlayback
        : MonoBehaviour, IVoicePlaybackInternal, IVolumeProvider
    {
        private IPriorityManager _priorityManager;
        private IVolumeProvider _volumeProvider;

        private readonly SpeechSessionStream _sessions;
        
        protected PlaybackOptions? LatestPlaybackOptions => TryGetActiveSession()?.PlaybackOptions;

        private FrameFormat _frameFormat;
        private CodecSettings _codecSettings;

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

        public bool IsActive => isActiveAndEnabled;

        public bool AllowPositionalPlayback { get; set; }

        public bool IsMuted { get; set; }

        public float PlaybackVolume { get; set; }

        public string PlayerName
        {
            get => _sessions.PlayerName;
            set => _sessions.PlayerName = value;
        }

        /// <summary>
        /// Get the current priority of audio being played through this component
        /// </summary>
        ChannelPriority IVoicePlayback.Priority
        {
            get
            {
                var session = LatestPlaybackOptions;
                if (!session.HasValue)
                    return ChannelPriority.None;

                return session.Value.Priority;
            }
        }

        float IVolumeProvider.TargetVolume
        {
            get
            {
                //Mute if explicitly muted
                if (((IVoicePlaybackInternal)this).IsMuted)
                    return 0;

                //Mute if the top priority is greater than this priority
                var pm = _priorityManager;
                if (pm != null && pm.TopPriority > ((IVoicePlayback)this).Priority)
                    return 0;

                //Get the upstream volume setting (if there is one - default to 1 otherwise)
                var v = _volumeProvider;
                var upstream = v?.TargetVolume ?? 1;

                //No muting applied, so play at chosen volume
                return ((IVoicePlaybackInternal)this).PlaybackVolume * upstream;
            }
        }

        public float Jitter => ((IJitterEstimator)_sessions).Jitter;

        public float? PacketLoss => TryGetActiveSession()?.PacketLoss;

        public bool IsSpeaking => TryGetActiveSession().HasValue;

        /// <summary>
        /// Get the codec settings used for playback for this player
        /// </summary>
        CodecSettings IVoicePlaybackInternal.CodecSettings
        {
            get => _codecSettings;
            set
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

        protected BaseVoicePlayback()
        {
            _sessions = new SpeechSessionStream(this);
            PlaybackVolume = 1;
        }

        public virtual void Setup(IPriorityManager priority, IVolumeProvider volume)
        {
            _priorityManager = priority;
            _volumeProvider = volume;
        }

        protected virtual void Start()
        {
        }

        protected virtual void OnDestroy()
        {
        }

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
            _sessions.StopSession(false);
        }

        protected virtual void Update()
        {
        }

        public void GetRemoteChannels(List<RemoteChannel> output)
        {
            output.Clear();
            TryGetActiveSession()?.Channels.GetRemoteChannels(output);
        }

        void IVoicePlaybackInternal.Reset()
        {
            ((IVoicePlaybackInternal)this).IsMuted = false;
            ((IVoicePlaybackInternal)this).PlaybackVolume = 1;
        }

        void IVoicePlaybackInternal.SetTransform(Vector3 pos, Quaternion rot)
        {
            SetTransform(pos, rot);
        }

        protected virtual void SetTransform(Vector3 pos, Quaternion rot)
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

        void IVoicePlaybackInternal.ForceReset()
        {
            _sessions.ForceReset();
        }

        protected SpeechSession? TryDequeueSession(int? outputRate = null)
        {
            _sessions.SetFixedOutputRate(outputRate);
            return _sessions.TryDequeueSession();
        }

        protected abstract SpeechSession? TryGetActiveSession();

        /// <summary>
        /// Get the live amplitude of voice playback (ARV)
        /// </summary>
        public abstract float Amplitude { get; }
    }
}
