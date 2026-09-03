using System.IO;
using SEE.UserSettings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// Regenerates the user settings configuration before a player build starts.
    /// This prevents stale UserSettings.cfg files from being copied into new builds.
    /// </summary>
    internal sealed class UserSettingsBuildProcessor : IPreprocessBuildWithReport
    {
        /// <summary>
        /// Determines the execution order of build preprocessors.
        /// Lower values are executed earlier.
        /// </summary>
        public int callbackOrder => 0;

        /// <summary>
        /// Called automatically by Unity before a player build starts.
        /// </summary>
        /// <param name="report">The Unity build report.</param>
        /// <remarks>
        /// SEE player builds are expected to be created from the SEEStart scene.
        /// This scene contains the <see cref="UserSettings"/> component whose
        /// configuration is exported into UserSettings.cfg before the player build
        /// is created.
        /// </remarks>
        public void OnPreprocessBuild(BuildReport report)
        {
            UserSetting userSettings = UserSetting.Instance;

            if (userSettings == null)
            {
                throw new BuildFailedException(
                    $"Cannot regenerate user settings because no {nameof(UserSetting)} component exists in the currently loaded scene." +
                    "Open the SEEStart scene before creating a player build.");
            }

            string path = userSettings.ConfigPath.Path;

            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"Deleted stale user settings file before build: {path}\n");
            }

            userSettings.Save(path);

            Debug.Log($"Regenerated user settings file before build: {path}\n");
        }
    }
}
