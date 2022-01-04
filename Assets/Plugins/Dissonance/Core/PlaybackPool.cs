using System;
using Dissonance.Audio.Playback;
using Dissonance.Datastructures;
using JetBrains.Annotations;
using UnityEngine;

namespace Dissonance
{
    internal class PlaybackPool
    {
        private readonly Pool<VoicePlayback> _pool;
        
        [NotNull] private readonly IPriorityManager _priority;
        [NotNull] private readonly IVolumeProvider _volume;

        private GameObject _prefab;
        private Transform _parent;

        public PlaybackPool([NotNull] IPriorityManager priority, [NotNull] IVolumeProvider volume)
        {
            if (priority == null) throw new ArgumentNullException("priority");
            if (volume == null) throw new ArgumentNullException("volume");

            _priority = priority;
            _volume = volume;
            _pool = new Pool<VoicePlayback>(10, CreatePlayback);
        }

        public void Start([NotNull] GameObject playbackPrefab, [NotNull] Transform transform)
        {
            if (playbackPrefab == null) throw new ArgumentNullException("playbackPrefab");
            if (transform == null) throw new ArgumentNullException("transform");

            _prefab = playbackPrefab;
            _parent = transform;
        }

        [NotNull] private VoicePlayback CreatePlayback()
        {
            //The game object must be inactive when it's added to the scene (so it can be edited before it activates)
            _prefab.gameObject.SetActive(false);

            //Create an instance (currently inactive)
            var entity = UnityEngine.Object.Instantiate(_prefab.gameObject);
            entity.transform.parent = _parent;

            //Configure (and add, if necessary) audio source
            var audioSource = entity.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = entity.AddComponent<AudioSource>();
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
            var player = entity.GetComponent<SamplePlaybackComponent>();
            if (player == null)
                entity.AddComponent<SamplePlaybackComponent>();

            //Configure VoicePlayback component
            var playback = entity.GetComponent<VoicePlayback>();
            playback.PriorityManager = _priority;
            playback.VolumeProvider = _volume;

            return playback;
        }

        [NotNull] public VoicePlayback Get([NotNull] string playerId)
        {
            if (playerId == null)
                throw new ArgumentNullException("playerId");

            var instance = _pool.Get();

            instance.gameObject.name = string.Format("Player {0} voice comms", playerId);
            instance.PlayerName = playerId;

            return instance;
        }

        public void Put([NotNull] VoicePlayback playback)
        {
            if (playback == null)
                throw new ArgumentNullException("playback");

            playback.gameObject.SetActive(false);
            playback.gameObject.name = "Spare voice comms";
            playback.PlayerName = null;

            if (!_pool.Put(playback))
                UnityEngine.Object.Destroy(playback.gameObject);
        }
    }
}
