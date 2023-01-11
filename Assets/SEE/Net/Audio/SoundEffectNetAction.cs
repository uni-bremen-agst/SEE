using UnityEngine;
using static SEE.Audio.IAudioManager;
using SEE.Net.Actions;

namespace SEE.Audio
{
    public class SoundEffectNetAction : AbstractNetAction
    {
        /// <summary>
        /// Sound effect to play
        /// </summary>
        private SoundEffect soundEffect;

        /// <summary>
        /// GameObject Id of the Game Object the sound effect should eminate from
        /// </summary>
        private string targetGameObjectName;

        public SoundEffectNetAction(
            SoundEffect soundEffect,
            string gameObjectName
            ) : base()
        {
            
            this.soundEffect = soundEffect;
            this.targetGameObjectName = gameObjectName;
        }

        protected override void ExecuteOnClient()
        {
            if (IsRequester()) return;
            IAudioManager audioManager = AudioManagerImpl.GetAudioManager();
            GameObject targetGameObject = GameObject.Find(this.targetGameObjectName);
            if (targetGameObject == null)
            {
                audioManager.QueueSoundEffect(this.soundEffect);
                return;
            }
            audioManager.QueueSoundEffect(this.soundEffect, targetGameObject, true);
        }

        protected override void ExecuteOnServer()
        {
            // Intentionally left empty
        }
    }
}