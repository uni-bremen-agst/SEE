using System.IO;
using UnityEngine;

/// <summary>
/// Utilities regarding Unity project data (e.g., Asset directory).
/// </summary>
public class UnityProject
{
    /// <summary>
    /// Returns the path to our Unity project folder. A Path.DirectorySeparatorChar will
    /// be appended. The chosen directory separator is the one of the platform we are
    /// currently executing on.
    /// </summary>
    /// <returns>path to our Unity project folder</returns>
    public static string GetPath()
    {
        string result = Application.dataPath;
        // Unity uses Unix directory separator; we might need Windows here.
        if (Path.DirectorySeparatorChar == '\\')
        {
            return result.Replace('/', Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        }
        else
        {
            return result + Path.DirectorySeparatorChar;
        }
    }
}