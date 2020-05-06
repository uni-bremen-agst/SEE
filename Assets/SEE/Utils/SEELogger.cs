using System;
using UnityEngine;

namespace SEE
{
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
    }
}
