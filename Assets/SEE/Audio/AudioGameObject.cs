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
        /// Set or get the volume of the audio source.
        /// </summary>
        public float Volume
        {
            get
            {
                return audioSource.volume;
            }
            set
            {
                audioSource.volume = value;
            }
        }

        /// <summary>
        /// Set or get the mute state of the audio source.
        /// </summary>
        public bool Mute
        {
            get
            {
                return audioSource.mute;
            }
            set
            {
                audioSource.mute = value;
            }
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="attachedObject">The game object this audio management object is attached to.</param>
        /// <param name="volume">The audio volume</param>
        /// <param name="muted">Whether or not the audio should be muted.</param>
        public AudioGameObject(GameObject attachedObject, float volume = 1.0f, bool muted = false)
        {
            AttachedObject = attachedObject;
            audioSource = AttachedObject.AddOrGetComponent<AudioSource>();
            // Enable spatial (3D) audio
            audioSource.spatialBlend = 1.0f;
            audioSource.volume = volume;
            audioSource.mute = muted;
        }

        /// <summary>
        /// Adds a new sound effect to this game object's audio queue.
        /// </summary>
        /// <param name="soundEffect">The sound effect that should be played.</param>
        public void EnqueueSoundEffect(AudioClip soundEffect)
        {
            effectsQueue.Enqueue(soundEffect);
            CheckQueue();
        }

        /// <summary>
        /// Checks if there are sound effects left in the audio queue
        /// and plays the next sound effect from the queue if no sound effect
        /// is currently playing.
        /// </summary>
        private void CheckQueue()
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
        /// If no sound is playing, the next sound is played. This method is called
        /// by the global audio manager.
        /// </summary>
        public void Update()
        {
            if (!audioSource.isPlaying || effectsQueue.Count > 0)
            {
                audioSource.Stop();
                audioSource.clip = effectsQueue.Dequeue();
                audioSource.Play();
            }
        }

        /// <summary>
        /// Checks if the audio queue of an attached game object is empty.
        /// If the queue is empty, delete this audio game object.
        /// </summary>
        /// <returns>True, if the queue is empty, else false.</returns>
        public bool IsQueueEmpty()
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
