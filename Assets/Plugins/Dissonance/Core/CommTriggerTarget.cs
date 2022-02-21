namespace Dissonance
{
    public enum CommTriggerTarget
    {
        /// <summary>
        ///     Broadcast to a chat room.
        /// </summary>
        Room,

        /// <summary>
        ///     Broadcast to a specific player.
        /// </summary>
        Player,

        /// <summary>
        ///     Broadcast to the player represented by the entity the trigger is attached to.
        /// </summary>
        Self
    }
}