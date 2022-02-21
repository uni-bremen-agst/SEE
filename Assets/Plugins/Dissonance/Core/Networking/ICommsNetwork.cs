using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dissonance.Audio.Playback;
using JetBrains.Annotations;

namespace Dissonance.Networking
{
    /// <summary>
    /// A packet of encoded voice data
    /// </summary>
    public struct VoicePacket
    {
        /// <summary>
        /// ID of the player who sent this voice packet
        /// </summary>
        public readonly string SenderPlayerId;

        //ncrunch: no coverage start (Justification: obsolete)
        /// <summary>
        /// Indicates if this packet should be played with positional playback
        /// </summary>
        [Obsolete("Use `PlaybackOptions.IsPositional` property")] //Marked obsolete in Dissonance v4.0.0 (2017-11-22)
        public bool Positional { get { return PlaybackOptions.IsPositional; } }

        /// <summary>
        /// Priority of the voice in this packet
        /// </summary>
        [Obsolete("Use `PlaybackOptions.Priority` property")] //Marked obsolete in Dissonance v4.0.0 (2017-11-22)
        public ChannelPriority Priority { get { return PlaybackOptions.Priority; } }

        /// <summary>
        /// Volume multiplier to apply to this audio
        /// </summary>
        [Obsolete("Use `PlaybackOptions.AmplitudeMultiplier` property")] //Marked obsolete in Dissonance v4.0.0 (2017-11-22)
        public float AmplitudeMultiplier { get { return PlaybackOptions.AmplitudeMultiplier; } }
        //ncrunch: no coverage end

        /// <summary>
        /// The encoded audio to pass directly to the codec
        /// </summary>
        public readonly ArraySegment<byte> EncodedAudioFrame;

        /// <summary>
        /// The (wrapping) sequence number of this packet
        /// </summary>
        public readonly uint SequenceNumber;

        /// <summary>
        /// The list of channels this voice packet is being delivered with
        /// </summary>
        [CanBeNull] public readonly List<RemoteChannel> Channels;

        private readonly PlaybackOptions _options;
        public PlaybackOptions PlaybackOptions
        {
            get { return _options; }
        }

        /// <summary>
        /// Create a new voice packet
        /// </summary>
        /// <param name="senderPlayerId"></param>
        /// <param name="priority"></param>
        /// <param name="ampMul"></param>
        /// <param name="positional"></param>
        /// <param name="encodedAudioFrame">The encoded audio data. The data will be copied out of this array as soon as it is passed to the decoder
        /// pipeline (i.e. you can re-use this array right away)</param>
        /// <param name="sequence"></param>
        /// <param name="channels">List of all channels this voice packet is being spoken on. Data will be copied out of the list as soon as it is
        /// passed to the decoder pipeline (i.e. you can re-use this array right away)</param>
        public VoicePacket(string senderPlayerId, ChannelPriority priority, float ampMul, bool positional, ArraySegment<byte> encodedAudioFrame, uint sequence, [CanBeNull] List<RemoteChannel> channels = null)
        {
            _options = new PlaybackOptions(positional, ampMul, priority);

            SenderPlayerId = senderPlayerId;
            EncodedAudioFrame = encodedAudioFrame;
            SequenceNumber = sequence;

            Channels = channels;
        }
    }
    
    /// <summary>
    /// A text message from a player
    /// </summary>
    public struct TextMessage
    {
        /// <summary>
        /// ID of the player who sent this message
        /// </summary>
        public readonly string Sender;

        /// <summary>
        /// The type of channel this message is directed to
        /// </summary>
        public readonly ChannelType RecipientType;

        /// <summary>
        /// The name of the recipient (either a room or a player, depends upon RecipientType
        /// </summary>
        public readonly string Recipient;

        /// <summary>
        /// The actual text of the message
        /// </summary>
        public readonly string Message;

        public TextMessage(string sender, ChannelType recipientType, string recipient, string message)
        {
            Sender = sender;
            RecipientType = recipientType;
            Recipient = recipient;
            Message = message;
        }
    }

    /// <summary>
    /// A player entered or exited a room
    /// </summary>
    public struct RoomEvent
    {
        /// <summary>
        /// ID of the player who sent this message
        /// </summary>
        public string PlayerName;

        /// <summary>
        /// Name of the room they entered/exited
        /// </summary>
        public string Room;

        /// <summary>
        /// Whether they joined or left the room
        /// </summary>
        public bool Joined;

        /// <summary>
        /// All the rooms this client is in
        /// </summary>
        internal ReadOnlyCollection<string> Rooms;

        internal RoomEvent([NotNull] string name, [NotNull] string room, bool joined, [NotNull] ReadOnlyCollection<string> rooms)
        {
            PlayerName = name;
            Room = room;
            Joined = joined;
            Rooms = rooms;
        }
    }

    /// <summary>
    /// The mode of the network
    /// </summary>
    public enum NetworkMode
    {
        /// <summary>
        /// No network is established
        /// </summary>
        None,

        /// <summary>
        /// Local machine is hosting the session (both a client and a server)
        /// </summary>
        Host,

        /// <summary>
        /// Local machine is purely a client
        /// </summary>
        Client,

        /// <summary>
        /// Local machine is purely a server
        /// </summary>
        DedicatedServer
    }

    /// <summary>
    /// Status of the connection to the session server
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// Not connected
        /// </summary>
        Disconnected,

        /// <summary>
        /// Connected/Connecting, but not fully capable of tranmitting voice
        /// </summary>
        Degraded,

        /// <summary>
        /// Connected to the server
        /// </summary>
        Connected
    }

    public static class NetworkModeExtensions
    {
        public static bool IsServerEnabled(this NetworkMode mode)
        {
            switch (mode)
            {
                case NetworkMode.Host:
                case NetworkMode.DedicatedServer:
                    return true;
                case NetworkMode.None:
                case NetworkMode.Client:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException("mode", mode, null);
            }
        }

        public static bool IsClientEnabled(this NetworkMode mode)
        {
            switch (mode)
            {
                case NetworkMode.Host:
                case NetworkMode.Client:
                    return true;
                case NetworkMode.None:
                case NetworkMode.DedicatedServer:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException("mode", mode, null);
            }
        }
    }

    public interface ICommsNetwork
    {
        /// <summary>
        /// Gets the network connection status.
        /// </summary>
        ConnectionStatus Status { get; }

        /// <summary>
        ///     Attempts a connection to the voice server.
        /// </summary>
        /// <param name="playerName">The name of the local player. Must be unique on the network.</param>
        /// <param name="rooms">The room membership collection the network should track.</param>
        /// <param name="playerChannels">The player channels collection the network should track.</param>
        /// <param name="roomChannels">The room channels collection the network should track.</param>
        /// <param name="codecSettings">The audio codec being used on the network.</param>
        void Initialize(string playerName, Rooms rooms, PlayerChannels playerChannels, RoomChannels roomChannels, CodecSettings codecSettings);

        /// <summary>
        /// Event which is raised when the network mode changes.
        /// </summary>
        event Action<NetworkMode> ModeChanged;

        /// <summary>
        /// Get the current network mode
        /// </summary>
        NetworkMode Mode { get; }

        /// <summary>
        /// Event which is raised when a remote player joins the Dissonance session. Passed the unique ID of the player
        /// </summary>
        event Action<string, CodecSettings> PlayerJoined;

        /// <summary>
        /// Event which is raised when a remote player leaves the Dissonance session. Passed the unique ID of the player
        /// </summary>
        event Action<string> PlayerLeft;

        /// <summary>
        /// Event which is raised when a voice packet is received
        /// </summary>
        event Action<VoicePacket> VoicePacketReceived;

        /// <summary>
        /// Event which is raised when a text packet is received
        /// </summary>
        event Action<TextMessage> TextPacketReceived;

        /// <summary>
        /// Event which is raised when a remote player begins speaking. Passed the unique ID of the player
        /// </summary>
        event Action<string> PlayerStartedSpeaking;

        /// <summary>
        /// Event which is raised when a remote player stops speaking. Passed the unique ID of the player
        /// </summary>
        event Action<string> PlayerStoppedSpeaking;

        /// <summary>
        /// Event which is raised when a remote player enters a room.
        /// </summary>
        event Action<RoomEvent> PlayerEnteredRoom;

        /// <summary>
        /// Event which is raised when a remote player exits a room.
        /// </summary>
        event Action<RoomEvent> PlayerExitedRoom;

        /// <summary>
        /// Send the given voice data to the specified recipients.
        /// </summary>
        /// <remarks>The implementation of this method MUST NOT keep a reference to the given array beyond the scope of this method (the array is recycled for other uses)</remarks>
        /// <param name="data">The encoded audio data to send.</param>
        void SendVoice(ArraySegment<byte> data);

        /// <summary>
        /// Send a text message to a destination
        /// </summary>
        /// <param name="data">The message to send</param>
        /// <param name="recipientType">Type of recipinent for this message (either to a room or to a player)</param>
        /// <param name="recipientId">ID of the recipient (either a room ID or a player ID depending upon the recipinent type parameter)</param>
        void SendText(string data, ChannelType recipientType, string recipientId);
    }
}
