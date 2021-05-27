using System;
using System.Diagnostics;

namespace SEE.Net.Util
{
    /// <summary>
    /// An internal logger for the networking.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Calls Debug.Log with networking prefix and given message.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        [Conditional("UNITY_EDITOR")]
        public static void Log(string message)
        {
#if UNITY_EDITOR
            if (Network.InternalLoggingEnabled)
            {
                UnityEngine.Debug.Log($"<b>[SEE Net]</b> {message}\n");
            }
#endif
        }

        /// <summary>
        /// Calls Debug.LogException with networking prefix and given message.
        /// </summary>
        /// <param name="exception">The exception to be logged.</param>
        /// <param name="message">The message to be logged.</param>
        [Conditional("UNITY_EDITOR")]
        public static void LogException(Exception exception, string message = null)
        {
#if UNITY_EDITOR
            if (Network.InternalLoggingEnabled)
            {
                if (message != null)
                {
                    UnityEngine.Debug.LogError($"<b>[SEE Net]</b> Exception: {exception}\n{message}\n");
                }
                else
                {
                    UnityEngine.Debug.LogError($"<b>[SEE Net]</b> Exception: {exception}\n");
                }
            }
#endif
        }

        /// <summary>
        /// Calls Debug.LogError with networking prefix and given message.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        [Conditional("UNITY_EDITOR")]
        public static void LogError(string message)
        {
#if UNITY_EDITOR
            if (Network.InternalLoggingEnabled)
            {
                UnityEngine.Debug.LogError($"<b>[SEE Net]</b> {message}\n");
            }
#endif
        }

        /// <summary>
        /// Calls Debug.LogWarning with networking prefix and given message.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        [Conditional("UNITY_EDITOR")]
        public static void LogWarning(string message)
        {
#if UNITY_EDITOR
            if (Network.InternalLoggingEnabled)
            {
                UnityEngine.Debug.LogWarning($"<b>[SEE Net]</b> {message}\n");
            }
#endif
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
        public readonly Severity minSeverity;

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
            //TODO: Why is this empty?
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
