using Dissonance.Networking;
using UnityEngine;

namespace Dissonance.Audio.Playback
{
    /// <summary>
    /// **Do not** use this interface in your scripts - use `IVoicePlayback` instead!
    /// This is intended for Dissonance internals to control and configure the audio pipeline.
    /// </summary>
    public interface IVoicePlaybackInternal
        : IRemoteChannelProvider, IVoicePlayback
    {
        /// <summary>
        /// Get or set if speech from this instance is muted
        /// </summary>
        bool IsMuted { get; set; }

        /// <summary>
        /// Set the name of the player this playback instance is associated with
        /// </summary>
        new string PlayerName { get; set; }

        /// <summary>
        /// Reset this player. Clearing all buffered audio data.
        /// </summary>
        void Reset();

        /// <summary>
        /// Inform the playback instance that a new voice session has should start
        /// </summary>
        void StartPlayback();

        /// <summary>
        /// Inform the playback instance that the current voice session should end
        /// </summary>
        void StopPlayback();

        /// <summary>
        /// Get or set a value indicating if positional playback may be used
        /// </summary>
        bool AllowPositionalPlayback { get; set; }

        /// <summary>
        /// Get/set the codec settings which this pipeline is using
        /// </summary>
        CodecSettings CodecSettings { get; set; }

        /// <summary>
        /// Set the transform to use for positional playback
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        void SetTransform(Vector3 position, Quaternion rotation);

        /// <summary>
        /// Get or set the attenuation applied to voices played through this instance
        /// </summary>
        float PlaybackVolume { get; set; }

        /// <summary>
        /// Add a packet to the playback queue for this instance
        /// </summary>
        /// <param name="packet"></param>
        void ReceiveAudioPacket(VoicePacket packet);

        /// <summary>
        /// Force resetof playback pipeline, immediately discarding all open/pending voice sessions
        /// </summary>
        void ForceReset();

        /// <summary>
        /// Perform first time setup
        /// </summary>
        void Setup(IPriorityManager priority, IVolumeProvider volume);
    }

    public interface IVoicePlayback
    {
        /// <summary>
        /// Get the name of the player this playback instance is associated with
        /// </summary>
        string PlayerName { get; }

        /// <summary>
        /// Get if this playback instance is currently in use by a player
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Get if this playback instance is currently playing back voice
        /// </summary>
        bool IsSpeaking { get; }

        /// <summary>
        /// Get a live reading of the amplitude of this playback instance
        /// </summary>
        float Amplitude { get; }

        /// <summary>
        /// Get an estimate of packets lost
        /// </summary>
        float? PacketLoss { get; }

        /// <summary>
        /// Get the standard deviation of latency of packets delivered to be played back through this instance
        /// </summary>
        float Jitter { get; }

        /// <summary>
        /// Get the current priority of audio being played through this instance
        /// </summary>
        ChannelPriority Priority { get; }
    }
}
