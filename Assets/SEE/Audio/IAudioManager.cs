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
        void IncreaseMusicVolume();

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
        /// Mute sound effects received from remote players.
        /// </summary>
        void MuteRemoteSoundEffects();

        /// <summary>
        /// Unmute sound effects received from remote players.
        /// </summary>
        void UnmuteRemoteSoundEffects();

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
        /// Defines abstract names for different sound effects that can be played in-game.
        /// </summary>
        enum SoundEffect
        {
            /// <summary>
            /// A simple click sound.
            /// </summary>
            ClickSound,
            /// <summary>
            /// Sound for dropping objects.
            /// </summary>
            DropSound,
            /// <summary>
            /// Confirmation click sound.
            /// </summary>
            OkaySound,
            /// <summary>
            /// Sound for picking up objects.
            /// </summary>
            PickupSound,
            /// <summary>
            /// Sound for creating a new edge.
            /// </summary>
            NewEdgeSound,
            /// <summary>
            /// Sound for creating a new node.
            /// </summary>
            NewNodeSound,
            /// <summary>
            /// Player walking sound.
            /// </summary>
            WalkingSound,
            /// <summary>
            /// Declined click sound.
            /// </summary>
            CancelSound,
            /// <summary>
            /// Drawing sound.
            /// </summary>
            ScribbleSound,
            /// <summary>
            /// Sound for hovering over objects.
            /// </summary>
            HoverSound
        }

        /// <summary>
        /// Defines abstract names for different music tracks that can be played in-game.
        /// </summary>
        enum Music
        {
            /// <summary>
            /// The lobby music.
            /// </summary>
            LobbySound,
            /// <summary>
            /// The music played during the actual game.
            /// </summary>
            WorldSound
        }
    }
}
