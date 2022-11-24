using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SEE.Audio.AudioManager;
using static SEE.Audio.GameStateManager;

namespace SEE.Audio
{
    public class AudioManagerImpl : MonoBehaviour, AudioManager
    {
        public AudioClip lobbyMusic;
        public AudioClip clickSoundEffect;
        public AudioClip dropSoundEffect;
        public AudioClip footstepSoundEffect;
        public AudioClip messagePopAlert;
        public AudioClip pickSoundEffect;
        public AudioClip switchSoundEffekt;
        public AudioClip warningSoundEffect;

        /// <summary>
        /// Contains a list of Game Objects that had an AudioSource attached to them to play a sound effect.
        /// </summary>
        private readonly HashSet<AudioGameObject> soundEffectGameObjects = new HashSet<AudioGameObject>();

        /// <summary>
        /// The player's GameObject, used to play music (which is in an ambient environment, rather than being directional).
        /// </summary>
        public GameObject playerObject;

        /// <summary>
        /// Publicly accessible default for music volume. Can be set by Unity properties.
        /// </summary>
        [Range(0,1)]
        public float defaultMusicVolume;

        /// <summary>
        /// Publicly accessible default for sound effect volume. Can be set by Unity properties.
        /// </summary>
        [Range(0, 1)]
        public float defaultSoundEffectVolume;

        /// <summary>
        /// Memento that stores the music volume before the music was muted.
        /// </summary>
        private float musicVolumeBeforeMute = 0;

        /// <summary>
        /// Memento that stores the sound effects volume before sound effects were muted.
        /// </summary>
        private float soundEffectVolumeBeforeMute = 0;

        /// <summary>
        /// Current music volume.
        /// </summary>
        private float musicVolume;

        /// <summary>
        /// Current sound effects volume.
        /// </summary>
        private float soundEffectVolume;

        /// <summary>
        /// Stores the previous scene name.
        /// </summary>
        private string previousSceneName;

        /// <summary>
        /// Get the current music volume.
        /// </summary>
        public virtual float MusicVolume
        {
            get
            {
                return musicVolume;
            }
        }

        /// <summary>
        /// Get the current sound effects volume.
        /// </summary>
        public virtual float SoundEffectVolume
        {
            get
            {
                return soundEffectVolume;
            }
        }

        /// <summary>
        /// Queue of music tracks to be played by the audio manager.
        /// </summary>
        private readonly Queue<AudioClip> musicQueue = new Queue<AudioClip>();

        /// <summary>
        /// Our global music player.
        /// </summary>
        private AudioSource musicPlayer;

        /// <summary>
        /// Singleton instance
        /// </summary>
        private static AudioManagerImpl instance = null;
        
        /// <summary>
        /// Store the current state of the music player.
        /// </summary>
        private bool musicPaused = false;

        /// <summary>
        /// Get the singleton instance.
        /// </summary>
        /// <returns>The AudioManager singleton instance.</returns>
        public static AudioManagerImpl GetAudioManager()
        {
            return instance;
        }

        /// <summary>
        /// Set the AudioManager singleton instance.
        /// </summary>
        /// <param name="audioManagerImpl">The AudioManager instance.</param>
        public static void SetAudioManager(AudioManagerImpl audioManagerImpl)
        {
            instance = audioManagerImpl;
        }

        void Start()
        {
            this.playerObject.AddComponent<AudioSource>();
            this.musicPlayer = this.playerObject.GetComponent<AudioSource>();
            this.musicVolume = this.defaultMusicVolume;
            this.soundEffectVolume = this.defaultSoundEffectVolume;
            this.musicPlayer.volume = this.musicVolume;
            SetAudioManager(this);
            previousSceneName = SceneManager.GetActiveScene().name;
            QueueMusic();
        }

        void Update()
        {
            if (CheckSceneChanged()) GameStateChanged();
            HandleSceneMusic();
            DeleteRemovedAudioObjects(GetRemovedAudioObjects());
        }

        /// <summary>
        /// Removed removed AudioObjects from the AudioManager's AudioObject collection.
        /// </summary>
        /// <param name="removedElements">A list of AudioObjects that were removed in the current frame.</param>
        private void DeleteRemovedAudioObjects(HashSet<AudioGameObject> removedElements)
        {
            foreach (AudioGameObject removedElement in removedElements)
            {
                soundEffectGameObjects.Remove(removedElement);
            }
        }

        /// <summary>
        /// Returns a HashSet of AudioObjects that have been removed in the current frame.
        /// Additionally calls the update method of any object that was not removed.
        /// </summary>
        /// <returns>A HashSet of removed AudiObjects.</returns>
        private HashSet<AudioGameObject> GetRemovedAudioObjects()
        {
            HashSet<AudioGameObject> removedElements = new HashSet<AudioGameObject>();
            foreach (AudioGameObject audioGameObject in soundEffectGameObjects)
            {
                if (audioGameObject.ParentObject == null || audioGameObject.EmptyQueue())
                {
                    removedElements.Add(audioGameObject);
                }
                else
                {
                    audioGameObject.Update();
                }
            }
            return removedElements;
        }

        /// <summary>
        /// Handles the music player in a loaded scene.
        /// </summary>
        private void HandleSceneMusic()
        {
            if (this.musicPlayer.isPlaying || this.musicPaused) return;
            if (this.musicPlayer.clip == null)
            {
                if (this.musicQueue.Count == 0) QueueMusic();
                this.musicPlayer.clip = musicQueue.Dequeue();
            }
            this.musicPlayer.loop = true;
            this.musicPlayer.Play();
        }

        /// <summary>
        /// Check if the loaded scene was changed.
        /// </summary>
        /// <returns>True if the scene was changed, false otherwise.</returns>
        private bool CheckSceneChanged()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            if (currentSceneName != previousSceneName)
            {
                previousSceneName = currentSceneName;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Applies the volume changes from the music/sound effects integer to actual game volume changes.
        /// </summary>
        private void TriggerVolumeChanges()
        {
            musicPlayer.volume = musicVolume;
            foreach (AudioGameObject audioGameObject in soundEffectGameObjects)
            {
                audioGameObject.ChangeVolume(soundEffectVolume);
            }
        }

        public void DecreaseMusicVolume()
        {
            if (musicVolume > 0.1f)
            {
                musicVolume -= 0.1f;
                TriggerVolumeChanges();
            }
        }

        public void DecreaseSoundEffectVolume()
        {
            if (soundEffectVolume > 0.1f)
            {
                soundEffectVolume-= 0.1f;
                TriggerVolumeChanges();

            }
           
        }

        public void GameStateChanged()
        {
            QueueMusic();
            if (!this.musicPaused) this.musicPlayer.Stop();
            this.musicPlayer.clip = musicQueue.Dequeue();
        }

        /// <summary>
        /// Queues music based on the loaded scene.
        /// </summary>
        private void QueueMusic()
        {
            this.QueueMusic(GameStateManager.GetBySceneName(this.previousSceneName));
        }

        public void IncreaseMusicVolume()
        {
            if (musicVolume <= 0.9f)
            {
                musicVolume+= 0.1f;
                TriggerVolumeChanges();
            }
            
        }

        public void IncreaseSoundEffectVolume()
        {
            if (soundEffectVolume <= 0.9f)
            {
                soundEffectVolume+= 0.1f;
                TriggerVolumeChanges();
            }
            
        }

        public void MuteMusic()
        {
            musicVolumeBeforeMute = MusicVolume;
            musicVolume = 0;
            TriggerVolumeChanges();
            PauseMusic();
        }

        public void MuteSoundEffects()
        {
            soundEffectVolumeBeforeMute = soundEffectVolume;
            soundEffectVolume = 0;
            TriggerVolumeChanges();
        }

        public void PauseMusic()
        {
            if (musicPlayer.isPlaying)
            {
                musicPlayer.Pause();
                musicPaused = true;
            }
        }

        public void QueueMusic(GameState gameState)
        {
            switch (gameState)
            {
                case GameState.LOBBY:
                    musicQueue.Enqueue(GetAudioClipFromMusicName(Music.LOBBY_MUSIC));
                    break;
                case GameState.IN_GAME:
                    musicQueue.Enqueue(GetAudioClipFromMusicName(Music.LOBBY_MUSIC));
                    break;
            }
        }

        public void QueueSoundEffect(SoundEffect soundEffect)
        {
            QueueSoundEffect(soundEffect, this.playerObject);
        }

        public void QueueSoundEffect(SoundEffect soundEffect, GameObject sourceObject)
        {
            AudioGameObject controlObject = null;
            foreach (AudioGameObject audioGameObject in soundEffectGameObjects)
            {
                if (audioGameObject.EqualsGameObject(sourceObject))
                {
                    controlObject = audioGameObject;
                    break;
                }
            }
            if (controlObject == null)
            {
                controlObject = new AudioGameObject(sourceObject, soundEffectVolume);
                soundEffectGameObjects.Add(controlObject);
            }
            controlObject.EnqueueSoundEffect(GetAudioClipFromSoundEffectName(soundEffect));
        }

        public void ResumeMusic()
        {
            if (!musicPlayer.isPlaying)
            {
                musicPlayer.Play();
                musicPaused = false;
            }
        }

        public void UnmuteMusic()
        {
            musicVolume = musicVolumeBeforeMute;
            musicVolumeBeforeMute = 0;
            TriggerVolumeChanges();
            ResumeMusic();
        }

        public void UnmuteSoundEffects()
        {
            soundEffectVolume = soundEffectVolumeBeforeMute;
            soundEffectVolumeBeforeMute = 0;
            TriggerVolumeChanges();
        }

        /// <summary>
        /// Get the AudioClip of the music that should be played.
        /// </summary>
        /// <param name="music">The enum referencing the music track that should be played.</param>
        /// <returns>An AudioSource matching the given enum music name.</returns>
        private AudioClip GetAudioClipFromMusicName(Music music)
        {
            switch (music)
            {
                case Music.LOBBY_MUSIC:
                    return lobbyMusic;
                default:
                    return lobbyMusic;

            }
        }

        /// <summary>
        /// Get the AudioClip of the sound effect that should be played.
        /// </summary>
        /// <param name="soundEffect">The enum referencing the sound effect that should be played.</param>
        /// <returns>An AudioSource matching the given enum sound effect name.</returns>
        private AudioClip GetAudioClipFromSoundEffectName(SoundEffect soundEffect)
        {
            switch (soundEffect)
            {
                case SoundEffect.CLICK_SOUND:
                    return clickSoundEffect;
                case SoundEffect.DROP_SOUND:
                    return dropSoundEffect;
                case SoundEffect.MESSAGE_POP_UP:
                    return messagePopAlert;
                case SoundEffect.PICKUP_SOUND:
                    return pickSoundEffect;
                case SoundEffect.SWITCH_SOUND:
                    return switchSoundEffekt;
                case SoundEffect.WALKING_SOUND:
                    return footstepSoundEffect;
                case SoundEffect.WARNING_SOUND:
                   return warningSoundEffect;
                default:
                    return clickSoundEffect;
            }
        }
    }
}