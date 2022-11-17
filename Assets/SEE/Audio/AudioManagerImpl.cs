using System.Collections.Generic;
using UnityEngine;
using static SEE.Audio.AudioManager;

namespace SEE.Audio
{
    public class AudioManagerImpl : MonoBehaviour, AudioManager
    {
        public AudioClip lobbyMusic;
        public AudioClip clickSoundEffect;

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
        public int defaultMusicVolume;

        /// <summary>
        /// Publicly accessible default for sound effect volume. Can be set by Unity properties.
        /// </summary>
        public int defaultSoundEffectVolume;

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
        private readonly Queue<AudioClip> musicQueue = new Queue<AudioClip>();

        /// <summary>
        /// Our global music player.
        /// </summary>
        private AudioSource musicPlayer;

        private static AudioManagerImpl instance = null;

        public static AudioManagerImpl GetAudioManager()
        {
            return instance;
        }

        public static void SetAudioManager(AudioManagerImpl audioManagerImpl)
        {
            instance = audioManagerImpl;
        }

        void Start()
        {
            this.playerObject.AddComponent<AudioSource>();
            this.musicPlayer = this.playerObject.GetComponent<AudioSource>();
            SetAudioManager(this);
            GetAudioManager().QueueLobbyMusic(GameState.CONNECTING); // todo remove
        }

        void Update()
        {
            // todo should loop as long as scene doesnt change
            if (!GetAudioManager().musicPlayer.isPlaying)
            {
                GetAudioManager().musicPlayer.clip = musicQueue.Dequeue();
                GetAudioManager().musicPlayer.Play();
            }
            HashSet<AudioGameObject> removedElements = new HashSet<AudioGameObject>();
            foreach (AudioGameObject audioGameObject in soundEffectGameObjects) {
                if (audioGameObject.ParentObject == null || audioGameObject.EmptyQueue())
                {
                    removedElements.Add(audioGameObject);
                }
                else
                {
                    audioGameObject.Update();
                }
            }
            foreach (AudioGameObject removedElement in removedElements)
            {
                soundEffectGameObjects.Remove(removedElement);
            }
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
            if (musicPlayer.isPlaying)
            {
                musicPlayer.Pause();
            }
        }

        public void QueueLobbyMusic(GameState gameState)
        {
            switch (gameState)
            {
                case GameState.LOBBY:
                    musicQueue.Enqueue(GetAudioClipFromMusicName(AudioConstants.LobbyMusic));
                    break;
                case GameState.CONNECTING:
                    musicQueue.Enqueue(GetAudioClipFromMusicName(AudioConstants.ConnectingMusic));
                    break;
                case GameState.IN_GAME:
                    musicQueue.Enqueue(GetAudioClipFromMusicName(AudioConstants.InGameMusic));
                    break;
            }
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
            if(controlObject == null)
            {
                controlObject = new AudioGameObject(sourceObject);
                soundEffectGameObjects.Add(controlObject);
            }
            controlObject.EnqueueSoundEffect(GetAudioClipFromSoundEffectName(soundEffect));
        }

        public void ResumeMusic()
        {
            if (!musicPlayer.isPlaying)
            {
                musicPlayer.Play();
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

        private AudioClip GetAudioClipFromMusicName(string MusicName)
        {
            return lobbyMusic; // todo
        } 
        private AudioClip GetAudioClipFromSoundEffectName(SoundEffect SoundEffect)
        {
            return clickSoundEffect; // todo
        } 
    }
}