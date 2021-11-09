using System;

namespace SEE.Utils
{
    /// <summary>
    /// Interface for all loggers in SEE.
    /// </summary>
    public interface ILogger
    {
        void LogDebug(string message);
        void LogError(string message);
        void LogException(Exception exception);
        void LogInfo(string message);
        void LogWarning(string message);
    }
}

