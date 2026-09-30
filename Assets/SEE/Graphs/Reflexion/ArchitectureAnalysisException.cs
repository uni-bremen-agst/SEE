using System;
using SEE.Graphs;

namespace SEE.Graphs.Reflexion
{
    /// <summary>
    /// Super class for all exceptions thrown by the architecture analysis.
    /// </summary>
    public abstract class ArchitectureAnalysisException : Exception
    {
        /// <summary>
        /// Constructs a new <see cref="ArchitectureAnalysisException"/>.
        /// </summary>
        protected ArchitectureAnalysisException()
        {
        }

        /// <summary>
        /// Constructs a new <see cref="ArchitectureAnalysisException"/> with the given <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        protected ArchitectureAnalysisException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="ArchitectureAnalysisException"/> with the
        /// given <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        protected ArchitectureAnalysisException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
