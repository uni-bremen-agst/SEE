#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.IO;


/// <summary>
/// Automatically patches the .meta files for System.Reactive DLLs and analyzers
/// to set validateReferences to 0 on editor startup, avoiding self-reference errors
/// without causing re-import loops.
/// </summary>
[InitializeOnLoad]
public class SystemReactiveMetaPatcher
{
    /// <summary>
    /// Triggers <see cref="PatchSystemReactiveMetaFiles"/>.
    /// </summary>
    static SystemReactiveMetaPatcher()
    {
        PatchSystemReactiveMetaFiles();
    }

    /// <summary>
    /// Scans Assets/Packages for System.Reactive .meta files and updates
    /// validateReferences from 1 to 0 if necessary.
    /// </summary>
    private static void PatchSystemReactiveMetaFiles()
    {
        string packagesPath = Path.Combine(Application.dataPath, "Packages");
        if (!Directory.Exists(packagesPath))
        {
            return;
        }

        // Find all .meta files inside the local NuGet packages directory
        string[] metaFiles = Directory.GetFiles(packagesPath, "*.meta", SearchOption.AllDirectories);
        bool changesMade = false;
        const string validateReferences = "validateReferences";

        foreach (string metaFile in metaFiles)
        {
            // Target System.Reactive related meta files
            if (metaFile.Contains("System.Reactive", System.StringComparison.OrdinalIgnoreCase))
            {
                string content;
                try
                {
                    content = File.ReadAllText(metaFile);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Could not read meta file at {metaFile}: {ex.Message}\n");
                    continue;
                }

                // If reference validation is enabled, disable it
                if (content.Contains($"{validateReferences}: 1"))
                {
                    content = content.Replace($"{validateReferences}: 1", $"{validateReferences}: 0");
                    try
                    {
                        File.WriteAllText(metaFile, content);
                        Debug.Log($"[{nameof(SystemReactiveMetaPatcher)}] Set {validateReferences} to 0 in: {metaFile}");
                        changesMade = true;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Could not write meta file at {metaFile}: {ex.Message}\n");
                    }
                }
            }
        }

        // Only refresh the asset database if a file was actually modified
        if (changesMade)
        {
            AssetDatabase.Refresh();
        }
    }
}
#endif
