using System;

namespace SEE.Utils
{
    /// <summary>
    /// Interface for all loggers in SEE.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs <paramref name="message"/> as a debugging message.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        void LogDebug(string message);

        /// <summary>
        /// Logs <paramref name="message"/> as an error.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        void LogError(string message);

        /// <summary>
        /// Logs <paramref name="exception"/>.
        /// </summary>
        /// <param name="exception">The exception to be logged.</param>
        void LogException(Exception exception);

        /// <summary>
        /// Logs <paramref name="message"/> as an informational message.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        void LogInfo(string message);

        /// <summary>
        /// Logs <paramref name="message"/> as a warning.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        void LogWarning(string message);

        /// <summary>
        /// Logs <paramref name="message"/> as a failed assertion, that is, as an
        /// assumption of the code that did not hold.
        ///
        /// Callers evaluate the assumption themselves and call this only when it is
        /// violated. Unlike <c>UnityEngine.Debug.Assert</c>, the check is therefore
        /// not removed from builds without assertions enabled.
        /// </summary>
        /// <param name="message">A message describing the assumption that was violated.</param>
        void LogAssertion(string message);
    }
}
