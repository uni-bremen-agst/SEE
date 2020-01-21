using SEE;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extension for GraphSettings to simplify the use in CCAnimation.
/// </summary>
public static class GraphSettingsExtension
{
    /// <summary>
    /// Returns a GraphSettings instance with the default settings for usage in CCAnimation
    /// </summary>
    /// <returns>An initialized GraphSettings instance.</returns>
    public static GraphSettings DefaultCCAnimationSettings(string gxlFolderName)
    {
        var graphSettings = new GraphSettings();

        graphSettings.pathPrefix = Application.dataPath.Replace('/', '\\') + '\\';

        graphSettings.gxlPath = $"..\\Data\\GXL\\{gxlFolderName}\\";

        return graphSettings;
    }
}
