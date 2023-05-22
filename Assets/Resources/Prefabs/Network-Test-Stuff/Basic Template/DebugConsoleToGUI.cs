using UnityEngine;

public class DebugConsoleToGUI : MonoBehaviour
{
    //#if !UNITY_EDITOR
    static string myLog = "";
    private string output;
    private string stack;

    void OnEnable()
    {
        Application.logMessageReceived += Log;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= Log;
    }

    public void Log(string logString, string stackTrace, LogType type)
    {
        output = logString;
        stack = stackTrace;
        myLog = output + "\n" + myLog;
        if (myLog.Length > 5000)
        {
            myLog = myLog.Substring(0, 4000);
        }
    }

    void OnGUI()
    {
        //if (!Application.isEditor) //Do not display in editor ( or you can use the UNITY_EDITOR macro to also disable the rest)
        {
            myLog = GUI.TextArea(new Rect((Screen.width / 2) -20 , 20, Screen.width / 2, Screen.height / 4), myLog);
        }
    }
    //#endif
}

// Original by bboysil · Mar 18, 2015 at 03:55 PM 
// https://answers.unity.com/questions/125049/is-there-any-way-to-view-the-console-in-a-build.html