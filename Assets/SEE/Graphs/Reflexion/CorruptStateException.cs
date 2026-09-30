using System;

namespace SEE.Graphs.Reflexion
{
    /// <summary>
    ///  Thrown if the analysis is in an invalid state.
    /// </summary>
    public class CorruptStateException : ArchitectureAnalysisException
    {
        /// <summary>
        /// Constructs a new <see cref="CorruptStateException"/>.
        /// </summary>
        public CorruptStateException()
        {
        }

        /// <summary>
        /// Constructs a new <see cref="CorruptStateException"/> with the given <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CorruptStateException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="CorruptStateException"/> with the given <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public CorruptStateException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
