using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.Audio;
using static SEE.Audio.AudioManager;

namespace SEE.Audio
{
    /// <summary>
    /// An Object that contains a SEE game object and an audio queue and makes the sounds originate from the game object in 3D space.
    /// </summary>
    public class AudioGameObject : MonoBehaviour
    {
        /// <summary>
        /// Queue of sound effects that this object should play
        /// </summary>
        private Queue<SoundEffect> effectsQueue = new Queue<SoundEffect>();

        /// <summary>
        /// The game object that this audio management object is attached to.
        /// </summary>
        private GameObject parentObject;

        /// <summary>
        /// Adds a new sound effect to this game object's audio queue.
        /// </summary>
        /// <param name="soundEffect">The sound effect that should be played.</param>
        public void PlaySoundEffect(SoundEffect soundEffect)
        {
            effectsQueue.Enqueue(soundEffect);
        }

        /// <summary>
        /// Check if sound is playing or play next sound
        /// </summary>
        void Update()
        {
            // todo 
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="parentObject">The game object this audio management object is attached to.</param>
        public AudioGameObject(GameObject parentObject)
        {
            this.parentObject = parentObject;
        }
    }
}