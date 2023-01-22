using UnityEngine;
using static SEE.Audio.IAudioManager;
using SEE.Net.Actions;

namespace SEE.Audio
{
    /// <summary>
    /// Propagates sound effects across network to connected clients.
    /// </summary>
    public class SoundEffectNetAction : AbstractNetAction
    {
        /// <summary>
        /// Sound effect to play.
        /// </summary>
        public string SoundEffectName;

        /// <summary>
        /// GameObject Id of the Game Object the sound effect should eminate from.
        /// </summary>
        public string TargetGameObjectName;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="soundEffect">The sound effect to play.</param>
        /// <param name="gameObjectName">The name of the game object that the sound should eminate from.</param>
        public SoundEffectNetAction(SoundEffect soundEffect, string gameObjectName) 
        {
            this.SoundEffectName = soundEffect.ToString();
            this.TargetGameObjectName = gameObjectName;
        }

        /// <summary>
        /// Action executed on clients.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject targetGameObject = Find(this.TargetGameObjectName);
                SoundEffect soundEffect = (SoundEffect)System.Enum.Parse(typeof(SoundEffect), this.SoundEffectName);
                AudioManagerImpl.EnqueueSoundEffect(soundEffect, targetGameObject, true);
            }
        }

        /// <summary>
        /// Action executed on server.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left empty
        }
    }
}