namespace Dissonance
{
    /// <summary>
    /// Denotes what type of channel this is (i.e. who receives audio sent to this channel)
    /// </summary>
    public enum ChannelType
    {
        /// <summary>
        ///     A room channel will be received by all players in the room.
        /// </summary>
        Room,

        /// <summary>
        ///     A player channel will be received only by a single player.
        /// </summary>
        Player
    }
}
