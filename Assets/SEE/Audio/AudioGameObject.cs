using System.Collections.Generic;
using UnityEngine;

namespace SEE.Audio
{
    /// <summary>
    /// An Object that contains a SEE game object and an audio queue and makes the sounds originate from the game object in 3D space.
    /// </summary>
    public class AudioGameObject
    {
        /// <summary>
        /// Queue of sound effects that this object should play
        /// </summary>
        private Queue<AudioClip> effectsQueue = new Queue<AudioClip>();

        /// <summary>
        /// The game object that this audio management object is attached to.
        /// </summary>
        private GameObject parentObject;

        public GameObject ParentObject
        {
            get
            {
                return parentObject;
            }
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

        private void CheckMusicPlayer()
        {
            AudioSource audioSource = this.parentObject.GetComponent<AudioSource>();
            if (!audioSource.isPlaying && effectsQueue.Count == 1)
            {
                audioSource.clip = effectsQueue.Dequeue();
                audioSource.Play();
            }
        }

        public void ChangeVolume(float volume){
            this.parentObject.GetComponent<AudioSource>().volume = volume;
        }

        /// <summary>
        /// Check if sound is playing or play next sound
        /// </summary>
        public void Update()
        {
            AudioSource audioSource = this.parentObject.GetComponent<AudioSource>();
            if (audioSource.isPlaying && effectsQueue.Count <= 0) return;
            audioSource.Stop();
            audioSource.clip = effectsQueue.Dequeue();
            audioSource.Play();
        }

        public bool EqualsGameObject(GameObject gameObject)
        {
            return this.parentObject.GetInstanceID() == gameObject.GetInstanceID();
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="parentObject">The game object this audio management object is attached to.</param>
        public AudioGameObject(GameObject parentObject, float defaultVolume = 1.0f)
        {
            this.parentObject = parentObject;
            this.parentObject.AddComponent<AudioSource>();
            this.parentObject.GetComponent<AudioSource>().volume = defaultVolume;
        }

        public bool EmptyQueue()
        {
            bool emptyQueue = this.effectsQueue.Count == 0 && !this.parentObject.GetComponent<AudioSource>().isPlaying;
            if (emptyQueue)
            {
                GameObject.Destroy(this.parentObject.GetComponent<AudioSource>());
            }
            return emptyQueue;
        }
    }
}