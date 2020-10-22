using System;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// A simple logger for SEE that just logs the messages via UnityEngine.Debug.
    /// </summary>
    public class SEELogger : ILogger
    {
        public void LogDebug(string message)
        {
            Debug.Log(message);
        }

        public void LogError(string message)
        {
            Debug.LogError(message);
        }

        public void LogException(Exception exception)
        {
            Debug.LogException(exception);
        }

        public void LogInfo(string message)
        {
            Debug.Log(message);
        }

        public void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }
    }
}
