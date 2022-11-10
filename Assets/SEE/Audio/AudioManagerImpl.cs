using SEE.Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace See.Audio
{
    public class AudioManagerImpl : MonoBehaviour, AudioManager
    {
        /// <summary>
        /// Audio manager singleton instace.
        /// </summary>
        private static AudioManager instance = null;

        /// <summary>
        /// Contains a list of Game Objects that had an AudioSource attached to them to play a sound effect.
        /// </summary>
        private readonly HashSet<AudioGameObject> soundEffectGameObjects = new HashSet<AudioGameObject>();

        /// <summary>
        /// The player's GameObject, used to play music (which is in an ambient environment, rather than being directional).
        /// </summary>
        public readonly GameObject playerObject;

        /// <summary>
        /// Publicly accessible default for music volume. Can be set by Unity properties.
        /// </summary>
        public readonly int defaultMusicVolume;

        /// <summary>
        /// Publicly accessible default for sound effect volume. Can be set by Unity properties.
        /// </summary>
        public readonly int defaultSoundEffectVolume;

        /// <summary>
        /// Memento that stores the music volume before the music was muted.
        /// </summary>
        private int musicVolumeBeforeMute = 0;

        /// <summary>
        /// Memento that stores the sound effects volume before sound effects were muted.
        /// </summary>
        private int soundEffectVolumeBeforeMute = 0;

        /// <summary>
        /// Current music volume.
        /// </summary>
        private int musicVolume = 0;

        /// <summary>
        /// Current sound effects volume.
        /// </summary>
        private int soundEffectVolume = 0;

        /// <summary>
        /// Get the current music volume.
        /// </summary>
        public virtual int MusicVolume
        {
            get
            {
                return musicVolume;
            }
        }

        /// <summary>
        /// Get the current sound effects volume.
        /// </summary>
        public virtual int SoundEffectVolume
        {
            get
            {
                return soundEffectVolume;
            }
        }

        /// <summary>
        /// Queue of music tracks to be played by the audio manager.
        /// </summary>
        private readonly Queue musicQueue = new Queue();

        public AudioManager GetAudioManager()
        {
            if (instance == null) instance = new AudioManagerImpl();
            return instance;
        }

        /// <summary>
        /// Applies the volume changes from the music/sound effects integer to actual game volume changes.
        /// </summary>
        private void TriggerVolumeChanges()
        {
            // todo
        }

        public void DecreaseMusicVolume()
        {
            musicVolume--;
            TriggerVolumeChanges();
        }

        public void DecreaseSoundEffectVolume()
        {
            soundEffectVolume--;
            TriggerVolumeChanges();
        }

        public void GameStateChanged(AudioManager.GameState gameState)
        {
            throw new System.NotImplementedException();
        }

        public void IncreaseMusicVolume()
        {
            musicVolume++;
            TriggerVolumeChanges();
        }

        public void IncreaseSoundEffectVolume()
        {
            soundEffectVolume++;
            TriggerVolumeChanges();
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
            throw new System.NotImplementedException();
        }

        public void QueueLobbyMusic(AudioManager.GameState gameState)
        {
            throw new System.NotImplementedException();
        }

        public void QueueSoundEffect(AudioManager.SoundEffect soundEffect, GameObject sourceObject)
        {
        }

        public void ResumeMusic()
        {
            throw new System.NotImplementedException();
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
    }
}