using System;
using System.IO;
using System.Linq;
using Dissonance.Editor.Windows.Welcome;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Dissonance.Editor.Windows
{
    [InitializeOnLoad]
    public class Startup
    {
        private static bool _recompiling = false;
        private static bool _startupDone = false;

        /// <summary>
        /// Subscribe to receive an update call every frame. Only runs if the Dissonance install path is correctly set!
        /// </summary>
        public static event Action SafeUpdate;

        /// <summary>
        /// This will run when the editor first starts
        /// </summary>
        static Startup()
        {
            //Doing things here often confuses the editor, presumably because it's still in startup. Instead subscribe to do things on the first update
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            Logs.WriteMultithreadedLogs();
            Metrics.WriteMultithreadedMetrics();

            //Do nothing while a recompile is pending
            if (_recompiling)
                return;

            //Change the value compiled into DissonanceRootPath.BasePath as necessary
            if (!CheckInstallLocation())
                return;

            //Now that we know the BasePath is correct launch the welcome window and the update checker
            if (!_startupDone)
            {
                WelcomeLauncher.Startup();
                _startupDone = true;
            }

            if (SafeUpdate != null)
                SafeUpdate();
        }

        /// <summary>
        /// Before launching anything else we need to check if Dissonance is installed in the correct position. If it is not then fix the value in DissonanceRootPath.cs
        /// </summary>
        /// <returns></returns>
        private static bool CheckInstallLocation()
        {
            //Check if DissonanceComms.cs is where it should be according to the `BasePath` constant. If it is then early exit.
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(Path.Combine(DissonanceRootPath.BasePath, "DissonanceComms.cs"))))
                return true;
            
            //Find the DissonanceComms.cs file
            var maybePathFile = Directory.GetFiles(Application.dataPath, "DissonanceComms.cs", SearchOption.AllDirectories).SingleOrDefault();
            if (maybePathFile == null)
            {
                Debug.LogError("Cannot Find DissonanceComms.cs! Dissonance has not been installed correctly. Please delete all Dissonance related files and import them again");
                return false;
            }

            //Convert into a full path with normalized file separators
            var fileFullPath = new FileInfo(Path.GetFullPath(maybePathFile)).FullName.Replace(Path.DirectorySeparatorChar.ToString(), "/");
            if (!fileFullPath.StartsWith(Application.dataPath.Replace(Path.DirectorySeparatorChar.ToString(), "/")))
                throw new InvalidOperationException("Dissonance install directory is not a child directory of 'Application.dataPath'!");

            //Work out the path to the install directory (as a project path)
            var installDirectoryProjPath = Application.dataPath.Split('/').Last() + "/" + fileFullPath.Substring(Application.dataPath.Length).TrimStart('/').Replace("/DissonanceComms.cs", "");

            //If the path is correct we're good to go
            if (installDirectoryProjPath == DissonanceRootPath.BasePath)
                return true;

            //Warn the user that we're about to change code and recompile the project
            Debug.LogWarning(string.Format("Dissonance has been detected at: '{0}'", installDirectoryProjPath));
            Debug.LogWarning(string.Format("DissonanceRootPath.BasePath will be adjusted from '{0}' to '{1}'", DissonanceRootPath.BasePath, installDirectoryProjPath));

            //Path is incorrect, we need to replace that hardcoded constant with the correct path
            var dissRootPathFilePath = Path.Combine(Path.GetFullPath(installDirectoryProjPath), Path.Combine("Resources", "DissonanceRootPath.cs"));

            //Read in all the lines of the file, find the magic GUID. Use the line after the GUID as a template for the next line after that
            var lines = File.ReadAllLines(dissRootPathFilePath).ToList();
            var magicGuidIndex = lines.FindIndex(l => l.Contains("6e445e37-4c7a-43ce-9f2d-068dd0a2413d"));
            lines[magicGuidIndex + 2] = lines[magicGuidIndex + 1].Replace("//", "").Replace("{{PATH}}", installDirectoryProjPath);
            File.WriteAllLines(dissRootPathFilePath, lines.ToArray());

            //Force a recompile, set a flag to suppress all further action. The compile will reset the flag back to it's default value when it completes
            Debug.LogWarning("DissonanceRootPath.BasePath has been adjusted, forcing a project recompile");
            AssetDatabase.Refresh();
            _recompiling = true;
            return false;
        }
    }
}
