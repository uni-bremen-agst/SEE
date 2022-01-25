using System;
using System.Collections.ObjectModel;

namespace Dissonance.Networking.Server.Admin
{
    public interface IServerAdmin
    {
        /// <summary>
        /// Event fires when a new player joins the voice session
        /// </summary>
        event Action<IServerClientState> ClientJoined;

        /// <summary>
        /// Event fires when a player leaves the voice session
        /// </summary>
        event Action<IServerClientState> ClientLeft;

        /// <summary>
        /// Invoked when a client (first parameter) attempts to send a voice packet claiming to be from another client (second parameter).
        /// Second parameter may be null if the spoof target is not a real player.
        /// </summary>
        event Action<IServerClientState, IServerClientState> VoicePacketSpoofed;

        /// <summary>
        /// All players currently in the voice session
        /// </summary>
        ReadOnlyCollection<IServerClientState> Clients { get; }

        /// <summary>
        /// Enable/Disable monitoring of channels that clients are speaking through.
        /// This requires partially decoding voice packets which increases server load.
        /// </summary>
        bool EnableChannelMonitoring { get; set; }
    }
}
