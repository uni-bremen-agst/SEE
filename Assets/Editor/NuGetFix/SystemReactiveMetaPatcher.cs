#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;


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
    ///
    /// Setting validateReferences: 1 inside a .meta file enables the Validate
    /// References toggle in the Unity Inspector (corresponding to PluginImporter.GetValidateReferences
    /// in the API or the "validateReferences": true flag inside an .asmdef file).
    ///
    /// When this attribute is set to 1, it instructs Unity's assembly updater and build pipeline
    /// to strictly verify that all external assembly dependencies and strong-name version bindings
    /// of that .dll (or Assembly Definition) can be resolved before allowing script compilation and
    /// reflection loading to proceed.
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
            if (EndsWithSeparatedName(metaFile, "System.Reactive.dll.meta", StringComparison.OrdinalIgnoreCase))
            {
                string content;
                try
                {
                    content = File.ReadAllText(metaFile);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Could not read meta file at {metaFile}: {ex.Message}\n");
                    continue;
                }

                // If reference validation is enabled, disable it.
                if (content.Contains($"{validateReferences}: 1"))
                {
                    content = content.Replace($"{validateReferences}: 1", $"{validateReferences}: 0");
                    try
                    {
                        File.WriteAllText(metaFile, content);
                        Debug.Log($"[{nameof(SystemReactiveMetaPatcher)}] Set {validateReferences} to 0 in: {metaFile}\n");
                        changesMade = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Could not write meta file at {metaFile}: {ex.Message}\n");
                    }
                }
                else
                {
                    Debug.Log($"[{nameof(SystemReactiveMetaPatcher)}] No change needed for {validateReferences} in: {metaFile}\n");
                }
            }
        }

        // Only refresh the asset database if a file was actually modified.
        if (changesMade)
        {
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// Checks whether <paramref name="path"/> ends with <paramref name="expectedName"/>
    /// immediately preceded by either '/' or '\'.
    /// </summary>
    /// <param name="path">The path string to inspect.</param>
    /// <param name="expectedName">The trailing file or directory name (without leading separator).</param>
    /// <param name="comparison">String comparison mode.</param>
    /// <remarks>
    /// Assumes no trailing separators in <paramref name="expectedName"/>.
    /// </remarks>
    private static bool EndsWithSeparatedName
        (ReadOnlySpan<char> path,
         ReadOnlySpan<char> expectedName,
         StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (path.Length < expectedName.Length)
        {
            return false;
        }

        if (path.Length == expectedName.Length)
        {
            return path.Equals(expectedName, comparison);
        }

        // path is longer than expectedName, so we can safely check the preceding character.

        // Verify the trailing segment matches expectedName
        if (!path.EndsWith(expectedName, comparison))
        {
            return false;
        }

        // Verify the character immediately preceding expectedName is a Windows or Linux separator.
        char precedingChar = path[path.Length - expectedName.Length - 1];
        return precedingChar == '/' || precedingChar == '\\';
    }
}
#endif
