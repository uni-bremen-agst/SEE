namespace Dissonance.VAD
{
    /// <summary>
    /// Listens for events from the voice detector
    /// </summary>
    public interface IVoiceActivationListener
    {
        /// <summary>
        /// Indicates that voice activation has begun detecting voice
        /// </summary>
        void VoiceActivationStart();

        /// <summary>
        /// Indicates that voice activation has stopped detecting voice
        /// </summary>
        void VoiceActivationStop();
    }
}