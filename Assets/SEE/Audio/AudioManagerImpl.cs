using System.Collections.Generic;
using System.IO;
using SEE.Utils.Config;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SEE.Audio.IAudioManager;
using static SEE.Audio.SceneContext;

namespace SEE.Audio
{
    /// <summary>
    /// Implements the IAudioManager interface.
    /// </summary>
    public class AudioManagerImpl : MonoBehaviour, IAudioManager
    {
        #region Configuration File

        /// <summary>
        /// Path to the configuration file relative to StreamingAssets.
        /// </summary>
        private const string configFilePath = "audio.cfg";

        /// <summary>
        /// Label of attribute <see cref="MusicVolume"/> in the configuration file.
        /// </summary>
        private const string musicVolumeLabel = "musicVolume";

        /// <summary>
        /// Label of attribute <see cref="SoundEffectVolume"/> in the configuration file.
        /// </summary>
        private const string sfxVolumeLabel = "soundEffectVolume";

        /// <summary>
        /// Label of attribute <see cref="MusicMuted"/> in the configuration file.
        /// </summary>
        private const string musicMutedLabel = "musicMuted";

        /// <summary>
        /// Label of attribute <see cref="SoundEffectsMuted"/> in the configuration file.
        /// </summary>
        private const string soundEffectsMutedLabel = "soundEffectsMuted";

        /// <summary>
        /// Label of attribute <see cref="RemoteSoundEffectsMuted"/> in the configuration file.
        /// </summary>
        private const string remoteSoundEffectsMutedLabel = "remoteSoundEffectsMuted";

        #endregion Configuration File

        /// <summary>
        /// The music played in the lobby scene.
        /// </summary>
        public AudioClip LobbyMusic;

        /// <summary>
        /// The music played in all scenes other than the lobby scene.
        /// </summary>
        public AudioClip GameMusic;

        /// <summary>
        /// The sound effect played when clicking objects.
        /// </summary>
        public AudioClip ClickSoundEffect;

        /// <summary>
        /// The sound effect played when dropping objects.
        /// </summary>
        public AudioClip DropSoundEffect;

        /// <summary>
        /// The sound effect played when picking up objects.
        /// </summary>
        public AudioClip PickSoundEffect;

        /// <summary>
        /// The sound effect played when creating new edges.
        /// </summary>
        public AudioClip NewEdgeSoundEffect;

        /// <summary>
        /// The sound effect played when creating new nodes.
        /// </summary>
        public AudioClip NewNodeSoundEffect;

        /// <summary>
        /// The sound effect played when drawing.
        /// </summary>
        public AudioClip ScribbleSoundEffect;

        /// <summary>
        /// The sound effect played when players are walking.
        /// </summary>
        public AudioClip FootstepSoundEffect;

        /// <summary>
        /// The sound effect played when clicking the okay button.
        /// </summary>
        public AudioClip OkayButtonSoundEffect;

        /// <summary>
        /// The sound effect played when clicking the cancel button.
        /// </summary>
        public AudioClip CancelButtonSoundEffect;

        /// <summary>
        /// The sound effect played when hovering over objects.
        /// </summary>
        public AudioClip HoverSoundEffect;

        /// <summary>
        /// Contains a list of Game Objects that had an AudioSource attached to them to play a sound effect.
        /// </summary>
        private readonly ISet<AudioGameObject> soundEffectGameObjects = new HashSet<AudioGameObject>();

        /// <summary>
        /// The player's GameObject, used to play music (which is in an ambient environment, rather than being directional).
        /// </summary>
        public GameObject PlayerObject;

        /// <summary>
        /// Publicly accessible default for music volume. Can be set by Unity properties.
        /// </summary>
        [Range(0,1)]
        public float DefaultMusicVolume;

        /// <summary>
        /// Publicly accessible default for sound effect volume. Can be set by Unity properties.
        /// </summary>
        [Range(0, 1)]
        public float DefaultSoundEffectVolume;

        /// <summary>
        /// Memento that stores the music volume before the music was muted.
        /// </summary>
        private float musicVolumeBeforeMute;

        /// <summary>
        /// Memento that stores the sound effects volume before sound effects were muted.
        /// </summary>
        private float soundEffectVolumeBeforeMute;

        /// <summary>
        /// Current music volume.
        /// </summary>
        public float MusicVolume;

        /// <summary>
        /// Current sound effects volume.
        /// </summary>
        public float SoundEffectVolume;

        /// <summary>
        /// Is the music muted?
        /// </summary>
        public bool MusicMuted = false;

        /// <summary>
        /// Are sound local sound effects muted?
        /// </summary>
        public bool SoundEffectsMuted = false;

        /// <summary>
        /// Are sound effects from remote players muted?
        /// </summary>
        public bool RemoteSoundEffectsMuted = false;

        /// <summary>
        /// Stores the current scene name.
        /// </summary>
        private SceneType currentScene;

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
        /// Stores the current state of the music player.
        /// </summary>
        private bool musicPaused = false;

        /// <summary>
        /// Audio source used for playing sound effects.
        /// </summary>
        private AudioSource soundEffectPlayer;

        /// <summary>
        /// Get the singleton instance.
        /// </summary>
        /// <returns>The AudioManager singleton instance.</returns>
        public static AudioManagerImpl Instance()
        {
            return instance;
        }

        /// <summary>
        /// Attaches an audio player for music to the player's game object.
        /// </summary>
        private void AttachAudioPlayer()
        {
            PlayerObject.AddComponent<AudioSource>();
            musicPlayer = PlayerObject.GetComponent<AudioSource>();
            MusicVolume = DefaultMusicVolume;
            SoundEffectVolume = DefaultSoundEffectVolume;
            musicPlayer.volume = MusicVolume;
        }

        /// <summary>
        /// Default MonoBehaviour method, executed when the object is created.
        /// Initializes a global audio player and starts the lobby music.
        /// </summary>
        private void Start()
        {
            instance = this; // required since a mono behaviour object cannot be instantiated.
            RestoreConfiguration();
            // TODO Make sure that volumes are applied:
            // - Convert attributes to properties and apply on set.
            // - Throw away manual setters.
            // TODO Can we drop ...BeforeMute and keep the last volume in the original attribute?
            // - There are ...Muted flags now to remember mute state.
            // - Add mute toggles for SFX/music to the settings menu.
            AttachAudioPlayer();
            InitializeSoundEffectPlayer();
            currentScene = SceneContext.GetSceneType(SceneManager.GetActiveScene());
            QueueMusic();
        }

        /// <summary>
        /// Attaches an audio player for sound effects to the player's game object.
        /// </summary>
        private void InitializeSoundEffectPlayer()
        {
            soundEffectPlayer = PlayerObject.AddComponent<AudioSource>();
            soundEffectPlayer.volume = SoundEffectVolume;
        }

        /// <summary>
        /// Default MonoBehaviour method, called every frame,
        /// Checks if the current scene has changed since the last frame, and
        /// plays music according to the loaded scene.
        /// Also ensures that sound effects that should be played are played.
        /// </summary>
        private void Update()
        {
            if (CheckSceneChanged())
            {
                GameStateChanged();
            }
            HandleSceneMusic();
            DeleteRemovedAudioObjects(GetRemovedAudioObjects());
        }

        /// <summary>
        /// Removes removed <see cref="AudioGameObject">s from the AudioManager's AudioObject collection.
        /// </summary>
        /// <param name="removedElements">A list of <see cref="AudioGameObject">s that were removed in the current frame.</param>
        private void DeleteRemovedAudioObjects(ISet<AudioGameObject> removedElements)
        {
            soundEffectGameObjects.ExceptWith(removedElements);
        }

        /// <summary>
        /// Returns a HashSet of AudioObjects that have been removed in the current frame.
        /// Additionally calls the update method of any object that was not removed.
        /// </summary>
        /// <returns>A HashSet of removed AudiObjects.</returns>
        private ISet<AudioGameObject> GetRemovedAudioObjects()
        {
            ISet<AudioGameObject> removedElements = new HashSet<AudioGameObject>();
            foreach (AudioGameObject audioGameObject in soundEffectGameObjects)
            {
                if (audioGameObject.AttachedObject == null || audioGameObject.EmptyQueue())
                {
                    removedElements.Add(audioGameObject);
                }
                else
                {
                    try
                    {
                        audioGameObject.Update();
                    }
                    catch
                    {
                        // Block executed when referenced element no longer exists in scene.
                        removedElements.Add(audioGameObject);
                    }
                }
            }
            return removedElements;
        }

        /// <summary>
        /// Checks if there is still music playing, if not queues another music track.
        /// </summary>
        private void HandleSceneMusic()
        {
            if (musicPlayer == null || soundEffectPlayer == null)
            {
                AttachAudioPlayer();
            }
            if (!musicPlayer.isPlaying && !musicPaused)
            {
                if (musicPlayer.clip == null)
                {
                    if (musicQueue.Count == 0)
                    {
                        QueueMusic();
                    }
                    musicPlayer.clip = musicQueue.Dequeue();
                }
                musicPlayer.loop = true;
                musicPlayer.Play();
            }
        }

        /// <summary>
        /// Check if the loaded scene was changed, by comparing the current scene's name
        /// with the scene name from the last frame.
        /// </summary>
        /// <returns>True if the scene was changed, false otherwise.</returns>
        private bool CheckSceneChanged()
        {
            SceneType newScene = SceneContext.GetSceneType(SceneManager.GetActiveScene());
            if (newScene != currentScene)
            {
                currentScene = newScene;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Called when the object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            PersistConfiguration();
        }

        /// <summary>
        /// Applies the volume changes.
        /// </summary>
        private void TriggerVolumeChanges()
        {
            musicPlayer.volume = MusicVolume;
            soundEffectPlayer.volume = SoundEffectVolume;
            foreach (AudioGameObject audioGameObject in soundEffectGameObjects)
            {
                audioGameObject.ChangeVolume(SoundEffectVolume);
            }
        }

        #region IAudioManager Members

        /// <summary>
        /// Decreases music volume by 10%.
        /// </summary>
        public void DecreaseMusicVolume()
        {
            if (MusicVolume > 0.1f)
            {
                MusicVolume -= 0.1f;
                TriggerVolumeChanges();
            }
        }

        /// <summary>
        /// Decreases sound effect volume by 10%.
        /// </summary>
        public void DecreaseSoundEffectVolume()
        {
            if (SoundEffectVolume > 0.1f)
            {
                SoundEffectVolume -= 0.1f;
                TriggerVolumeChanges();
            }
        }

        /// <summary>
        /// Changes playing music when switching scenes.
        /// </summary>
        public void GameStateChanged()
        {
            soundEffectGameObjects.Clear();
            QueueMusic();
            if (!musicPaused)
            {
                musicPlayer.Stop();
            }
            musicPlayer.clip = musicQueue.Dequeue();
        }

        /// <summary>
        /// Increase music volume by 10%.
        /// </summary>
        public void IncreaseMusicVolume()
        {
            if (MusicVolume <= 0.9f)
            {
                MusicVolume += 0.1f;
                TriggerVolumeChanges();
            }
        }

        /// <summary>
        /// Increase sound effect volume by 10%.
        /// </summary>
        public void IncreaseSoundEffectVolume()
        {
            if (SoundEffectVolume <= 0.9f)
            {
                SoundEffectVolume += 0.1f;
                TriggerVolumeChanges();
            }
        }

        /// <inheritdoc/>
        public void MuteMusic()
        {
            if (MusicMuted)
            {
                return;
            }
            musicVolumeBeforeMute = MusicVolume;
            MusicVolume = 0;
            TriggerVolumeChanges();
            PauseMusic();
            MusicMuted = true;
        }

        /// <inheritdoc/>
        public void MuteSoundEffects()
        {
            if (SoundEffectsMuted)
            {
                return;
            }
            soundEffectVolumeBeforeMute = SoundEffectVolume;
            SoundEffectVolume = 0;
            TriggerVolumeChanges();
            SoundEffectsMuted = true;
        }

        /// <inheritdoc/>
        public void MuteRemoteSoundEffects()
        {
            RemoteSoundEffectsMuted = true;
        }

        /// <summary>
        /// Pauses the music player.
        /// </summary>
        public void PauseMusic()
        {
            if (musicPlayer.isPlaying)
            {
                musicPlayer.Pause();
                musicPaused = true;
            }
        }

        /// <inheritdoc />
        public void ResumeMusic()
        {
            if (!musicPlayer.isPlaying)
            {
                musicPlayer.Play();
                musicPaused = false;
            }
        }

        /// <inheritdoc />
        public void UnmuteMusic()
        {
            if (!MusicMuted)
            {
                return;
            }
            MusicVolume = musicVolumeBeforeMute;
            musicVolumeBeforeMute = 0;
            TriggerVolumeChanges();
            ResumeMusic();
            MusicMuted = false;
        }

        /// <inheritdoc />
        public void UnmuteSoundEffects()
        {
            if (!SoundEffectsMuted)
            {
                return;
            }
            SoundEffectVolume = soundEffectVolumeBeforeMute;
            soundEffectVolumeBeforeMute = 0;
            TriggerVolumeChanges();
            SoundEffectsMuted = false;
        }

        /// <inheritdoc />
        public void UnmuteRemoteSoundEffects()
        {
            RemoteSoundEffectsMuted = false;
        }

        #endregion IAudioManager Members

        #region Public Methods

        /// <summary>
        /// Public API method for playing sound effects.
        /// </summary>
        /// <param name="soundEffect">The sound effect to play.</param>
        /// <param name="sentToClients">Whether the sound effect should be propagated to other players.</param>
        public static void EnqueueSoundEffect(SoundEffect soundEffect, bool sentToClients = false)
        {
            if (instance != null)
            {
                AudioManagerImpl.Instance().PlaySoundEffect(soundEffect);
            }
        }

        /// <summary>
        /// Public API method for playing sound effects.
        /// </summary>
        /// <param name="soundEffect">The sound effect to play.</param>
        /// <param name="sourceObject">The object the sound should eminate from.</param>
        /// <param name="sendToPeers">Whether the sound effect should be propagated to other players.</param>
        /// <param name="receivedFromPeer">Whether the sound effect was received from another player.</param>
        public static void EnqueueSoundEffect(SoundEffect soundEffect, GameObject sourceObject, bool sendToPeers = false, bool receivedFromPeer = false)
        {
            if (instance != null)
            {
                if (sourceObject == null)
                {
                    EnqueueSoundEffect(soundEffect);
                    return;
                }
                AudioManagerImpl.Instance().QueueSoundEffect(soundEffect, sourceObject, sendToPeers, receivedFromPeer);
            }
        }

        #endregion Public Methods

        /// <summary>
        /// Plays a sound effect without specifying the object which the sound is originating from.
        /// <para>
        /// The GameObject used is the player object itself (ambient sound rather than directional sound is used).
        /// </para><para>
        /// Any currently playing sound effect is interrupted.
        /// </para>
        /// </summary>
        /// <param name="soundEffect">The sound effect that should be played.</param>
        private void PlaySoundEffect(SoundEffect soundEffect)
        {
            soundEffectPlayer.Stop();
            soundEffectPlayer.clip = GetAudioClipFromSoundEffectName(soundEffect);
            soundEffectPlayer.Play();
        }

        /// <summary>
        /// Adds a sound effect to the sound effect queue while checking,
        /// if the sound effect was passed from a multiplayer connected player,
        /// or from the local game instance (to prevent endless sound effect loops).
        /// </summary>
        /// <param name="soundEffect">The sound effect that should be added to the sound effect queue.</param>
        /// <param name="sourceObject">The GameObject where the sound originated from.</param>
        /// <param name="sendToPeers">Whether the sound effect should be propagated to other players.</param>
        /// <param name="receivedFromPeer">Whether the sound effect was received from another player.</param>
        private void QueueSoundEffect(SoundEffect soundEffect, GameObject sourceObject, bool sendToPeers = false, bool receivedFromPeer = false)
        {
            if (receivedFromPeer && RemoteSoundEffectsMuted)
            {
                return;
            }
            AudioGameObject controlObject = null;
            foreach (AudioGameObject audioGameObject in soundEffectGameObjects)
            {
                if (audioGameObject.HasAudioListenerAttached(sourceObject))
                {
                    controlObject = audioGameObject;
                    break;
                }
            }
            if (controlObject == null)
            {
                controlObject = new AudioGameObject(sourceObject, SoundEffectVolume);
                soundEffectGameObjects.Add(controlObject);
            }
            controlObject.EnqueueSoundEffect(GetAudioClipFromSoundEffectName(soundEffect));

            if (sendToPeers && !receivedFromPeer && currentScene != SceneType.Lobby)
            {
                new SoundEffectNetAction(soundEffect, sourceObject.name).Execute();
            }
        }

        /// <summary>
        /// Queues music based on the loaded scene.
        /// </summary>
        private void QueueMusic()
        {
            QueueMusic(currentScene);
        }

        /// <summary>
        /// Adds a new music track to the music queue.
        /// </summary>
        /// <param name="sceneType">The current type of scene.</param>
        private void QueueMusic(SceneType sceneType)
        {
            musicQueue.Enqueue(GetAudioClipFromMusicName(sceneType == SceneType.Lobby ? Music.LobbySound : Music.WorldSound));
        }

        /// <summary>
        /// Get the AudioClip of the music that should be played.
        /// </summary>
        /// <param name="music">The music track that should be played.</param>
        /// <returns>An AudioSource matching the given enum music name.</returns>
        private AudioClip GetAudioClipFromMusicName(Music music) => music switch
        {
            Music.LobbySound => LobbyMusic,
            Music.WorldSound => GameMusic,
            _ => throw new System.Exception($"Unhandled {music}."),
        };

        /// <summary>
        /// Get the AudioClip of the sound effect that should be played.
        /// </summary>
        /// <param name="soundEffect">The sound effect that should be played.</param>
        /// <returns>An AudioSource matching the given enum sound effect name.</returns>
        private AudioClip GetAudioClipFromSoundEffectName(SoundEffect soundEffect) => soundEffect switch
        {
            SoundEffect.ClickSound => ClickSoundEffect,
            SoundEffect.DropSound => DropSoundEffect,
            SoundEffect.OkaySound => OkayButtonSoundEffect,
            SoundEffect.PickupSound => PickSoundEffect,
            SoundEffect.NewEdgeSound => NewEdgeSoundEffect,
            SoundEffect.NewNodeSound => NewNodeSoundEffect,
            SoundEffect.WalkingSound => FootstepSoundEffect,
            SoundEffect.CancelSound => CancelButtonSoundEffect,
            SoundEffect.ScribbleSound => ScribbleSoundEffect,
            SoundEffect.HoverSound => HoverSoundEffect,
            _ => ClickSoundEffect,
        };

        // /// <summary>
        // /// Saves the audio configuration to <paramref name="filename"/>.
        // /// </summary>
        // /// <param name="filename">name of the file in which the settings are stored</param>
        // public void Save(string filename)
        // {
        //     using ConfigWriter writer = new(filename);
        //     Save(writer);
        // }

        /// <summary>
        /// Persists the sound configuration to the file specified by <see cref="configFilePath"/>.
        /// </summary>
        private void PersistConfiguration()
        {
            using ConfigWriter writer = new(Application.streamingAssetsPath + "/" + configFilePath);

            float sfxVolume = SoundEffectsMuted ? soundEffectVolumeBeforeMute : SoundEffectVolume;
            float musicVolume = MusicMuted ? musicVolumeBeforeMute : MusicVolume;

            writer.Save(musicVolume, musicVolumeLabel);
            writer.Save(sfxVolume, sfxVolumeLabel);
            writer.Save(MusicMuted, musicMutedLabel);
            writer.Save(SoundEffectsMuted, soundEffectsMutedLabel);
            writer.Save(RemoteSoundEffectsMuted, remoteSoundEffectsMutedLabel);
        }

        /// <summary>
        /// Restores the sound configuration from the file specified by the <see cref="configFilePath"/> property.
        /// </summary>
        private void RestoreConfiguration()
        {
            string filePath = Application.streamingAssetsPath + "/" + configFilePath;
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Audio configuration file does not exist: {filePath} \n");
                return;
            }

            Debug.Log($"Loading audio configuration from file: {filePath}\n");
            using ConfigReader stream = new(filePath);
            Dictionary<string, object> attributes = stream.Read();

            ConfigIO.Restore(attributes, musicVolumeLabel, ref MusicVolume);
            ConfigIO.Restore(attributes, sfxVolumeLabel, ref SoundEffectVolume);
            ConfigIO.Restore(attributes, musicMutedLabel, ref MusicMuted);
            ConfigIO.Restore(attributes, soundEffectsMutedLabel, ref SoundEffectsMuted);
            ConfigIO.Restore(attributes, remoteSoundEffectsMutedLabel, ref RemoteSoundEffectsMuted);
        }
    }
}
