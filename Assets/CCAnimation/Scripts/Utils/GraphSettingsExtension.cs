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
    /// Updates GraphSettings.pathprefis with the real path on the current machine.
    /// </summary>
    /// <param name="graphSettings">GraphSettings where the pathPrefix is updated.</param>
    public static void UpdateProjectPath(this GraphSettings graphSettings)
    {
        graphSettings.pathPrefix = Application.dataPath.Replace('/', '\\') + '\\';
    }

    /// <summary>
    /// Returns the path to the test data for animated graphs.
    /// They reside under "Data\\GXL\\animation-clones".
    /// </summary>
    /// <returns>Path to test data for animated graphs.</returns>
    public static string GetAnimatedPath(this GraphSettings graphSettings)
    {
        return graphSettings.pathPrefix + "..\\Data\\GXL\\animation-clones\\";
    }

    /// <summary>
    /// Returns a GraphSettings instance with the default settings for usage in CCAnimation
    /// </summary>
    /// <returns>An initialized GraphSettings instance.</returns>
    public static GraphSettings DefaultCCAnimationSettings()
    {
        var graphSettings = new GraphSettings();

        graphSettings.UpdateProjectPath();
        graphSettings.ShowDonuts = false;

        return graphSettings;
    }
}
