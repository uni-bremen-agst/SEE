using UnityEngine;

namespace SEE.Audio
{
    /// <summary>
    /// Defines an interface for an audio managing framework.
    /// </summary>
    public interface IAudioManager
    {

        /// <summary>
        /// Mutes all music.
        /// </summary>
        void MuteMusic();

        /// <summary>
        /// Increases the music volume.
        /// </summary>
        abstract void IncreaseMusicVolume();

        /// <summary>
        /// Decreases the music volume.
        /// </summary>
        void DecreaseMusicVolume();

        /// <summary>
        /// Unmute the music.
        /// </summary>
        void UnmuteMusic();

        /// <summary>
        /// Mute all sound effects.
        /// </summary>
        void MuteSoundEffects();

        /// <summary>
        /// Increase sound effects volume.
        /// </summary>
        void IncreaseSoundEffectVolume();

        /// <summary>
        /// Decrease sound effects volume.
        /// </summary>
        void DecreaseSoundEffectVolume();

        /// <summary>
        /// Unmute all sound effects.
        /// </summary>
        void UnmuteSoundEffects();

        /// <summary>
        /// Pause the currently playing music.
        /// </summary>
        void PauseMusic();

        /// <summary>
        /// Resume music player if it was paused previously.
        /// </summary>
        void ResumeMusic();

        /// <summary>
        /// Change the currently playing music based on the new game state.
        /// </summary>
        void GameStateChanged();

        /// <summary>
        /// Queue a sound effect without specifying the object which the sound is originating from.
        /// The GameObject used is the player object itself (ambient sound rather than directional sound is used).
        /// </summary>
        /// <param name="soundEffect">The sound effect that should be played.</param>
        void QueueSoundEffect(SoundEffect soundEffect);

        /// <summary>
        /// Adds a sound effect to the sound effect queue.
        /// </summary>
        /// <param name="soundEffect">Name of the sound effect that should be added to the sound effect queue.</param>
        /// <param name="sourceObject">The GameObject where the sound originated from.</param>
        void QueueSoundEffect(SoundEffect soundEffect, GameObject sourceObject);

        /// <summary>
        /// Adds a sound effect to the sound effect queue while checking if the sound effect was passed from a multiplayer connected player,
        /// or from the local game instance (to prevent endless sound effect loops).
        /// </summary>
        /// <param name="soundEffect">Name of the sound effect that should be added to the sound effect queue.</param>
        /// <param name="sourceObject">The GameObject where the sound originated from.</param>
        /// <param name="networkAction">False if the sound effect originated from the local unity instance, else true.</param>
        public void QueueSoundEffect(SoundEffect soundEffect, GameObject sourceObject, bool networkAction);

        /// <summary>
        /// Defines abstract names for different sound effects that can be played in-game.
        /// </summary>
        enum SoundEffect
        {
            CLICK_SOUND, DROP_SOUND, MESSAGE_POP_UP, PICKUP_SOUND, SWITCH_SOUND, WALKING_SOUND, WARNING_SOUND, SCRIBBLE
        }

        /// <summary>
        /// Defines abstract names for different music tracks that can be played in-game.
        /// </summary>
        enum Music
        {
            LOBBY_MUSIC
        }
    }
}
