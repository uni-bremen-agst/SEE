using System;

namespace SEE.Net.Internal
{

    public class Logger : NetworkCommsDotNet.Tools.ILogger
    {
        public enum Severity
        {
            Low,
            Medium,
            High
        }

        public Severity minSeverity;

        public Logger(Severity minSeverity)
        {
            this.minSeverity = minSeverity;
        }

        public void Debug(string message)
        {
            if (minSeverity <= Severity.Low)
            {
                UnityEngine.Debug.Log(message);
            }
        }
        public void Error(string message)
        {
            if (minSeverity <= Severity.High)
            {
                UnityEngine.Debug.LogError(message);
            }
        }
        public void Fatal(string message)
        {
            if (minSeverity <= Severity.High)
            {
                UnityEngine.Debug.LogError(message);
            }
        }
        public void Fatal(string message, Exception ex)
        {
            if (minSeverity <= Severity.High)
            {
                UnityEngine.Debug.LogError(message);
                UnityEngine.Debug.LogException(ex);
            }
        }
        public void Info(string message)
        {
            if (minSeverity <= Severity.Low)
            {
                UnityEngine.Debug.Log(message);
            }
        }
        public void Shutdown()
        {
        }
        public void Trace(string message)
        {
            if (minSeverity <= Severity.Low)
            {
                UnityEngine.Debug.Log(message);
            }
        }
        public void Warn(string message)
        {
            if (minSeverity <= Severity.Medium)
            {
                UnityEngine.Debug.LogWarning(message);
            }
        }
    }

}
