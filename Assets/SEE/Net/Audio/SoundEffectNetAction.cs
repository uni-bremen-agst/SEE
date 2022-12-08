using UnityEngine;
using static SEE.Audio.IAudioManager;
using SEE.Net.Actions;

namespace SEE.Audio
{
    public class SoundEffectNetAction : AbstractNetAction
    {
        /// <summary>
        /// Global AudioManager singleton instance
        /// </summary>
        readonly IAudioManager audioManager;

        /// <summary>
        /// Sound effect to play
        /// </summary>
        SoundEffect soundEffect;

        /// <summary>
        /// GameObject the sound effect should be played from
        /// </summary>
        GameObject targetGameObject;

        public SoundEffectNetAction(
            SoundEffect soundEffect,
            string gameObjectName
            ) : base()
        {
            this.audioManager = AudioManagerImpl.GetAudioManager();
            this.soundEffect = soundEffect;
            this.targetGameObject = GameObject.Find(gameObjectName);
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                this.audioManager.QueueSoundEffect(this.soundEffect, this.targetGameObject, true);
            }
        }

        protected override void ExecuteOnServer()
        {
            // Intentionally left empty
        }
    }
}