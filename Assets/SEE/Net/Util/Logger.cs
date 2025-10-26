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
        /// Whether logging should be enabled.
        /// </summary>
        public static bool InternalLoggingEnabled = true;

        /// <summary>
        /// Calls Debug.Log with networking prefix and given message.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        [Conditional("UNITY_EDITOR")]
        public static void Log(string message)
        {
#if UNITY_EDITOR
            if (InternalLoggingEnabled)
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
            if (InternalLoggingEnabled)
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
            if (InternalLoggingEnabled)
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
            if (InternalLoggingEnabled)
            {
                UnityEngine.Debug.LogWarning($"<b>[SEE Net]</b> {message}\n");
            }
#endif
        }
    }
}
