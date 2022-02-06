namespace Dissonance.Networking
{
    /// <summary>
    /// Accessor for state from the comms network
    /// </summary>
    public interface ICommsNetworkState
    {
        /// <summary>
        /// The name of the local player
        /// </summary>
        string PlayerName { get; }

        /// <summary>
        /// The set of rooms the local player is listening to
        /// </summary>
        Rooms Rooms { get; }

        /// <summary>
        /// The set of player channels the local player is broadcasting to
        /// </summary>
        PlayerChannels PlayerChannels { get; }

        /// <summary>
        /// The set of room channels the local player is broadcasting to
        /// </summary>
        RoomChannels RoomChannels { get; }

        /// <summary>
        /// The codec being used on the network.
        /// </summary>
        CodecSettings CodecSettings { get; }
    }
}
