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
        /// Should not be sent to newly connecting clients
        /// </summary>
        public override bool ShouldBeSentToNewClient { get => false; }

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
            SoundEffectName = soundEffect.ToString();
            TargetGameObjectName = gameObjectName;
        }

        /// <summary>
        /// Action executed on clients.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject targetGameObject = Find(TargetGameObjectName);
            SoundEffect soundEffect = (SoundEffect)System.Enum.Parse(typeof(SoundEffect), SoundEffectName);
            AudioManagerImpl.EnqueueSoundEffect(soundEffect, targetGameObject, false, true);
        }
    }
}
