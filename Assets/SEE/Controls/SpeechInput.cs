namespace SEE.Controls
{
    /// <summary>
    /// Super class for speech input.
    /// </summary>
    public abstract class SpeechInput
    {
        // Useful information about this speech recognition:
        // https://lightbuzz.com/speech-recognition-unity-update/

        /// <summary>
        /// Starts the recognizer.
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// Stops the recognizer. It can be re-started again
        /// by calling <see cref="Start"/>.
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Stops and disposes the recognizer. It cannot be re-started again.
        /// </summary>
        public abstract void Dispose();
    }
}