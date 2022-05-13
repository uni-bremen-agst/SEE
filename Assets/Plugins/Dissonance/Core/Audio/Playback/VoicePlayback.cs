using UnityEngine;

namespace Dissonance.Audio.Playback
{
    /// <summary>
    ///     Handles decoding and playing audio for a specific remote player.
    ///     Entities with this behaviour are created automatically by the DissonanceVoiceComms component.
    /// </summary>
    /// ReSharper disable once InheritdocConsiderUsage
    public class VoicePlayback
        : BaseVoicePlayback
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Playback, "Voice Playback Component");

        public AudioSource AudioSource { get; private set; }

        private SamplePlaybackComponent _player;
        private float? _savedSpatialBlend;

        public override float Amplitude => _player == null ? 0 : _player.ARV;
        #endregion

        public void Awake()
        {
            AudioSource = GetComponent<AudioSource>();
            _player = GetComponent<SamplePlaybackComponent>();

            ((IVoicePlaybackInternal)this).Reset();
        }

        public override void Setup(IPriorityManager priority, IVolumeProvider volume)
        {
            base.Setup(priority, volume);

            //Configure (and add, if necessary) audio source
            var audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.bypassReverbZones = true;
            }
            audioSource.loop = true;
            audioSource.pitch = 1;
            audioSource.clip = null;
            audioSource.playOnAwake = false;
            audioSource.ignoreListenerPause = true;
            audioSource.Stop();

            //Configure (and add, if necessary) sample player
            //Because the audio source has no clip, this filter will be "played" instead
            var player = gameObject.GetComponent<SamplePlaybackComponent>();
            if (player == null)
                gameObject.AddComponent<SamplePlaybackComponent>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            AudioSource.Stop();

            // There is no low-latency way to play back audio into a spatialized AudioSource. Disable spatialization for this source if it's enabled.
            if (AudioSource.spatialize)
            {
                Log.Debug("spatialized AudioSource not supported for voice playback. Setting `spatialize=false`.");
                AudioSource.spatialize = false;
            }

            // Play back a flatline of 1.0 through the source and then multiply the voice signal by that to achieve spatial blending of voice.
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

        protected override void OnDisable()
        {
            base.OnDisable();

            if (AudioSource != null && AudioSource.clip != null)
            {
                var c = AudioSource.clip;
                AudioSource.clip = null;
                Destroy(c);
            }
        }

        protected override void Update()
        {
            base.Update();

            if (!_player.HasActiveSession)
            {
                // We're not playing anything at the moment. Try to get a session to play.
                var s = TryDequeueSession();
                if (s.HasValue)
                {
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
                var playbackOptions = LatestPlaybackOptions;
                var isPositional = playbackOptions?.IsPositional ?? false;

                if (((IVoicePlaybackInternal)this).AllowPositionalPlayback && isPositional)
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

        protected override SpeechSession? TryGetActiveSession()
        {
            return _player == null ? null : _player.Session;
        }
    }
}