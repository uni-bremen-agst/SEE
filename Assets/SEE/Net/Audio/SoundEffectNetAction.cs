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
        public string SoundEffectName;

        /// <summary>
        /// GameObject Id of the Game Object the sound effect should eminate from
        /// </summary>
        public string TargetGameObjectName;

        public SoundEffectNetAction(SoundEffect soundEffect, string gameObjectName) 
        {
            this.SoundEffectName = soundEffect.ToString();
            this.TargetGameObjectName = gameObjectName;
        }

        protected override void ExecuteOnClient()
        {
            if (IsRequester()) return;
            IAudioManager audioManager = AudioManagerImpl.GetAudioManager();
            GameObject targetGameObject = GameObject.Find(this.TargetGameObjectName);
            SoundEffect soundEffect = (SoundEffect) System.Enum.Parse(typeof(SoundEffect), this.SoundEffectName);
            if (targetGameObject == null)
            {
                audioManager.QueueSoundEffect(soundEffect);
                return;
            }
            audioManager.QueueSoundEffect(soundEffect, targetGameObject, true);
        }

        protected override void ExecuteOnServer()
        {
            // Intentionally left empty
        }
    }
}