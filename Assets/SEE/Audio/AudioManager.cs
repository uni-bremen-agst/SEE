using UnityEngine;

namespace SEE.Audio
{
    /// <summary>
    /// Defines an interface for an audio managing framework.
    /// </summary>
    public interface AudioManager
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
        /// Adds a sound effect to the sound effect queue.
        /// </summary>
        /// <param name="soundEffect">Name of the sound effect that should be added to the sound effect queue.</param>
        /// <param name="sourceObject">The GameObject where the sound originated from.</param>
        void QueueSoundEffect(SoundEffect soundEffect, GameObject sourceObject);

        /// <summary>
        /// Defines abstract names for different sound effects that can be played ingame.
        /// </summary>
        enum SoundEffect
        {
            CLICK_SOUND, DROP_SOUND, MESSAGE_POP_UP, PICKUP_SOUND, SWITCH_SOUND, WALKING_SOUND, WARNING_SOUND
        }

        enum Music
        {
            LOBBY_MUSIC
        }
    }
}
