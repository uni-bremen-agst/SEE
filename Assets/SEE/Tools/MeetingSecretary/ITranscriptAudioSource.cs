namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Provides the audio of the local player's microphone for the transcript.
    /// It hides which voice chat system is in use.
    /// </summary>
    /// <remarks>All members must be called on Unity's main thread.</remarks>
    public interface ITranscriptAudioSource
    {
        /// <summary>
        /// Starts collecting the samples of the local microphone.
        /// </summary>
        /// <returns>True if the microphone audio is available; false if the voice
        /// chat system is not running.</returns>
        bool Start();

        /// <summary>
        /// Stops collecting samples and discards those not yet read.
        /// </summary>
        void Stop();

        /// <summary>
        /// The sample rate (in Hz) of the samples returned by <see cref="Read(float[])"/>.
        /// It may change while the source is running, e.g., if the microphone is changed.
        /// </summary>
        int SampleRate { get; }

        /// <summary>
        /// True if the local player is muted, that is, if the other players cannot
        /// hear him or her. Audio recorded while muted must not be transcribed.
        /// </summary>
        bool IsMuted { get; }

        /// <summary>
        /// The total number of samples that were lost because they were not read in time.
        /// </summary>
        long DroppedSamples { get; }

        /// <summary>
        /// Copies the mono samples received since the last call into <paramref name="destination"/>,
        /// oldest first. If there are more samples than fit, the rest is returned by the next call.
        /// </summary>
        /// <param name="destination">Where to store the samples.</param>
        /// <returns>The number of samples stored in <paramref name="destination"/>.</returns>
        int Read(float[] destination);
    }
}
