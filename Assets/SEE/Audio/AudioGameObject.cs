using SEE.GO;
using System.Collections.Generic;
using SEE.Utils;
using UnityEngine;

namespace SEE.Audio
{
    /// <summary>
    /// An Object that contains a SEE game object
    /// and an audio queue and makes the sounds originate
    /// from the game object in 3D space.
    /// </summary>
    public class AudioGameObject
    {
        /// <summary>
        /// Queue of sound effects that this object should play.
        /// </summary>
        private readonly Queue<AudioClip> effectsQueue = new();

        /// <summary>
        /// Audio source of the game object that this object is attached to.
        /// </summary>
        private AudioSource audioSource;

        /// <summary>
        /// The game object that this audio management object is attached to
        /// and plays music for.
        /// </summary>
        public GameObject AttachedObject
        {
            get;
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="attachedObject">The game object this audio management object is attached to.</param>
        /// <param name="defaultVolume">The default audio volume</param>
        public AudioGameObject(GameObject attachedObject, float defaultVolume = 1.0f)
        {
            AttachedObject = attachedObject;
            AttachedObject.AddOrGetComponent<AudioSource>().volume = defaultVolume;
        }

        /// <summary>
        /// Adds a new sound effect to this game object's audio queue.
        /// </summary>
        /// <param name="soundEffect">The sound effect that should be played.</param>
        public void EnqueueSoundEffect(AudioClip soundEffect)
        {
            effectsQueue.Enqueue(soundEffect);
            CheckMusicPlayer();
        }

        /// <summary>
        /// Checks if there are sound effects left in the audio queue
        /// and plays the next sound effect from the queue if no sound effect
        /// is currently playing.
        /// </summary>
        private void CheckMusicPlayer()
        {
            if (AttachedObject.TryGetComponent(out audioSource))
            {
                if (!audioSource.isPlaying && effectsQueue.Count == 1)
                {
                    audioSource.clip = effectsQueue.Dequeue();
                    audioSource.Play();
                }
            }
        }

        /// <summary>
        /// Changes the audio player's volume to the supplied volume.
        /// </summary>
        /// <param name="volume">The volume the audio player should be set to.</param>
        public void ChangeVolume(float volume)
        {
            AttachedObject.GetComponent<AudioSource>().volume = volume;
        }

        /// <summary>
        /// If no sound is playing, the next sound is played. This method is called
        /// by the global audio manager.
        /// </summary>
        public void Update()
        {
            if (audioSource == null)
            {
                audioSource = AttachedObject.GetComponent<AudioSource>();
            }
            if (!audioSource.isPlaying || effectsQueue.Count > 0)
            {
                audioSource.Stop();
                audioSource.clip = effectsQueue.Dequeue();
                audioSource.Play();
            }
        }

        /// <summary>
        /// Checks if a game object has an audio game object (and thus
        /// an audio listener) attached.
        /// </summary>
        /// <param name="gameObject">The game object to check.</param>
        /// <returns>Whether the object given object has an audio game object.</returns>
        public bool HasAudioListenerAttached(GameObject gameObject)
        {
            return AttachedObject.GetInstanceID() == gameObject.GetInstanceID();
        }

        /// <summary>
        /// Checks if the audio queue of an attached game object is empty.
        /// If the queue is empty, delete this audio game object.
        /// </summary>
        /// <returns>True, if the queue is empty, else false.</returns>
        public bool EmptyQueue()
        {
            if (!AttachedObject.TryGetComponent(out AudioSource audioSource))
            {
                return true;
            }
            else
            {
                bool emptyQueue = effectsQueue.Count == 0 && !audioSource.isPlaying;
                if (emptyQueue)
                {
                    Destroyer.Destroy(audioSource);
                }
                return emptyQueue;
            }
        }
    }
}
