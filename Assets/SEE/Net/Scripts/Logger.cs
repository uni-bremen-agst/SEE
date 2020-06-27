using System;

namespace SEE.Net
{

    /// <summary>
    /// The native logger implementation of
    /// <see cref="NetworkCommsDotNet.Tools.ILogger"/>.
    /// </summary>
    public class Logger : NetworkCommsDotNet.Tools.ILogger
    {
        /// <summary>
        /// The available severities.
        /// </summary>
        public enum Severity
        {
            Low,
            Medium,
            High
        }



        /// <summary>
        /// The minimal logging severity.
        /// </summary>
        public Severity minSeverity;

        /// <summary>
        /// Creates a logger with given minimal severity.
        /// </summary>
        /// <param name="minSeverity">The minimal severity of the logger.</param>
        public Logger(Severity minSeverity)
        {
            this.minSeverity = minSeverity;
        }



        /// <summary>
        /// Debug logging.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Debug(string message)
        {
            if (minSeverity <= Severity.Low)
            {
                UnityEngine.Debug.Log(message);
            }
        }

        /// <summary>
        /// Error logging.
        /// </summary>
        /// <param name="message">The error message.</param>
        public void Error(string message)
        {
            if (minSeverity <= Severity.High)
            {
                UnityEngine.Debug.LogError(message);
            }
        }

        /// <summary>
        /// Fatal error logging.
        /// </summary>
        /// <param name="message">The error message.</param>
        public void Fatal(string message)
        {
            if (minSeverity <= Severity.High)
            {
                UnityEngine.Debug.LogError(message);
            }
        }

        /// <summary>
        /// Fatal error logging.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="ex">The exception.</param>
        public void Fatal(string message, Exception ex)
        {
            if (minSeverity <= Severity.High)
            {
                UnityEngine.Debug.LogError(message);
                UnityEngine.Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Info logging.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Info(string message)
        {
            if (minSeverity <= Severity.Low)
            {
                UnityEngine.Debug.Log(message);
            }
        }

        /// <summary>
        /// Shuts the logger down.
        /// </summary>
        public void Shutdown()
        {
        }

        /// <summary>
        /// Trace logging.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Trace(string message)
        {
            if (minSeverity <= Severity.Low)
            {
                UnityEngine.Debug.Log(message);
            }
        }

        /// <summary>
        /// Warn logging.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Warn(string message)
        {
            if (minSeverity <= Severity.Medium)
            {
                UnityEngine.Debug.LogWarning(message);
            }
        }
    }

}
