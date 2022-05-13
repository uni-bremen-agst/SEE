using System;
using Dissonance.Audio.Playback;
using Dissonance.Datastructures;
using JetBrains.Annotations;
using UnityEngine;

namespace Dissonance
{
    internal class PlaybackPool
    {
        private readonly Pool<IVoicePlaybackInternal> _pool;
        
        [NotNull] private readonly IPriorityManager _priority;
        [NotNull] private readonly IVolumeProvider _volume;

        private GameObject _prefab;
        private Transform _parent;

        public PlaybackPool([NotNull] IPriorityManager priority, [NotNull] IVolumeProvider volume)
        {
            _priority = priority ?? throw new ArgumentNullException(nameof(priority));
            _volume = volume ?? throw new ArgumentNullException(nameof(volume));

            _pool = new Pool<IVoicePlaybackInternal>(10, CreatePlayback);
        }

        public void Start([NotNull] GameObject playbackPrefab, [NotNull] Transform transform)
        {
            _prefab = playbackPrefab ?? throw new ArgumentNullException(nameof(playbackPrefab));
            _parent = transform ?? throw new ArgumentNullException(nameof(transform));
        }

        [NotNull] private IVoicePlaybackInternal CreatePlayback()
        {
            // The game object must be inactive when it's added to the scene (so it can be edited before it activates)
            _prefab.gameObject.SetActive(false);

            // Create an instance (currently inactive)
            var entity = UnityEngine.Object.Instantiate(_prefab.gameObject);
            entity.transform.parent = _parent;

            // Perform one-time setup on the playback component
            var playback = entity.GetComponent<IVoicePlaybackInternal>();
            playback.Setup(_priority, _volume);

            return playback;
        }

        [NotNull] public IVoicePlaybackInternal Get([NotNull] string playerId)
        {
            if (playerId == null)
                throw new ArgumentNullException(nameof(playerId));

            var instance = _pool.Get();

            var go = ((MonoBehaviour)instance).gameObject;
            go.name = $"Player {playerId} voice comms";

            instance.PlayerName = playerId;

            return instance;
        }

        public void Put([NotNull] IVoicePlayback playback)
        {
            if (playback == null)
                throw new ArgumentNullException(nameof(playback));

            var go = ((MonoBehaviour)playback).gameObject;
            go.SetActive(false);
            go.name = "Spare voice comms";

            var pi = (IVoicePlaybackInternal)playback;
            pi.PlayerName = null;

            if (!_pool.Put(pi))
                UnityEngine.Object.Destroy(go);
        }
    }
}
