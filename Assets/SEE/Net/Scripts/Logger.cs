using System;

namespace SEE.Net
{

    /// <summary>
    /// An internal logger for the networking.
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// Calls Debug.Log with networking prefix and given message.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        public static void Log(string message)
        {
            if (Network.InternalLoggingEnabled)
            {
                UnityEngine.Debug.LogFormat("<b>[SEE Net]</b> {0}\n", message);
            }
        }

        /// <summary>
        /// Calls Debug.LogException with networking prefix and given message.
        /// </summary>
        /// <param name="exception">The exception to be logged.</param>
        /// <param name="message">The message to be logged.</param>
        public static void LogException(Exception exception, string message = null)
        {
            if (Network.InternalLoggingEnabled)
            {
                if (message != null)
                {
                    UnityEngine.Debug.LogErrorFormat("<b>[SEE Net]</b> Exception: {0}\n{0}\n", exception.ToString(), message);
                }
                else
                {
                    UnityEngine.Debug.LogErrorFormat("<b>[SEE Net]</b> Exception: {0}\n", exception.ToString());
                }
            }
        }

        /// <summary>
        /// Calls Debug.LogError with networking prefix and given message.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        public static void LogError(string message)
        {
            if (Network.InternalLoggingEnabled)
            {
                UnityEngine.Debug.LogErrorFormat("<b>[SEE Net]</b> {0}\n", message);
            }
        }

        /// <summary>
        /// Calls Debug.LogWarning with networking prefix and given message.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        public static void LogWarning(string message)
        {
            if (Network.InternalLoggingEnabled)
            {
                UnityEngine.Debug.LogWarningFormat("<b>[SEE Net]</b> {0}\n", message);
            }
        }
    }

    /// <summary>
    /// The native logger implementation of
    /// <see cref="NetworkCommsDotNet.Tools.ILogger"/>.
    /// </summary>
    internal class NetworkCommsLogger : NetworkCommsDotNet.Tools.ILogger
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
        public NetworkCommsLogger(Severity minSeverity)
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
