using UnityEngine;

class Logger : ILogger
{
    public void LogInfo(string message)
    {
        Debug.Log(message);
    }

    public void LogError(string message)
    {
        Debug.LogError(message);
    }

    public void LogDebug(string message)
    {
        Debug.Log(message);
    }
}

