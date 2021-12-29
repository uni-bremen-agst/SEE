namespace Dissonance
{
    public enum CommActivationMode
    {
        /// <summary>
        ///     The transmitter will never transmit.
        /// </summary>
        None,

        /// <summary>
        ///     The transmisster will automatically activate when it detects the user speaking.
        /// </summary>
        VoiceActivation,

        /// <summary>
        ///     The transmitter will activate when the specified input axis is active.
        /// </summary>
        PushToTalk,

        /// <summary>
        ///     The transmitter will constantly transmit.
        /// </summary>
        Open
    }
}
