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
                string content = File.ReadAllText(metaFile);

                // If reference validation is enabled, disable it
                if (content.Contains($"{validateReferences}: 1"))
                {
                    content = content.Replace($"{validateReferences}: 1", $"{validateReferences}: 0");
                    File.WriteAllText(metaFile, content);
                    Debug.Log($"[{nameof(SystemReactiveMetaPatcher)}] Set {validateReferences} to 0 in: {metaFile}");
                    changesMade = true;
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

#if false

The following code does not work as it runs into an endless loop due to the reimport.

/// <summary>
/// Automatically disables assembly reference validation for System.Reactive DLLs and
/// analyzers to prevent self-reference compilation errors in Unity.
/// </summary>
[InitializeOnLoad]
public class SystemReactivePostprocessor : AssetPostprocessor
{
    /// <summary>
    /// Name of the property validateReferences in a meta file.
    /// </summary>
    /// <remarks>This is the internal name of the property. In the meta file,
    /// the attribute is called 'validateReferences'.</remarks>
    private const string validateReferencesProperty = "m_ValidateReferences";

    /// <summary>
    /// Static constructor that registers a delayed callback on editor startup to
    /// check existing System.Reactive assets on disk.
    /// </summary>
    static SystemReactivePostprocessor()
    {
        EditorApplication.delayCall += EnsureSystemReactiveValid;
    }

    /// <summary>
    /// Intercepts the import process for System.Reactive assets and disables
    /// reference validation in-memory without triggering re-import loops.
    /// </summary>
    private void OnPreprocessAsset()
    {
	    Debug.Log($"{nameof(SystemReactivePostprocessor)}.{nameof(OnPreprocessAsset)} is called.\n");
        if (assetPath.Contains("System.Reactive") && assetPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            if (assetImporter is PluginImporter pluginImporter)
            {
                DisableValidation(pluginImporter);
            }
        }
    }

    /// <summary>
    /// Scans the project for existing System.Reactive DLLs on startup and
    /// corrects their reference validation settings if they were extracted
    /// before this script compiled.
    /// </summary>
    private static void EnsureSystemReactiveValid()
    {
        string[] guids = AssetDatabase.FindAssets("System.Reactive");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Assets/Packages/System.Reactive", StringComparison.OrdinalIgnoreCase) &&
                path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                if (AssetImporter.GetAtPath(path) is PluginImporter pluginImporter)
                {
                    using SerializedObject serializedObject = new(pluginImporter);
                    SerializedProperty validateRefProp = serializedObject.FindProperty(validateReferencesProperty);
                    if (validateRefProp != null && validateRefProp.boolValue)
                    {
                        Debug.Log($"[{nameof(SystemReactivePostprocessor)}] Fixing {validateReferencesProperty} for: {pluginImporter.assetPath}");
                        validateRefProp.boolValue = false;
                        serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        pluginImporter.SaveAndReimport();
                    }
                    else
                    {
                        Debug.LogError($"{path} does not have a {validateReferencesProperty} property.\n");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Helper method that uses <see cref="SerializedObject"/> to set <c>m_ValidateReferences</c>
    /// to <c>false</c> on the specified plugin importer.
    /// </summary>
    /// <param name="importer">The plugin importer for the target asset.</param>
    private static void DisableValidation(PluginImporter importer)
    {
        using SerializedObject serializedObject = new SerializedObject(importer);
        SerializedProperty validateRefProp = serializedObject.FindProperty(validateReferencesProperty);
        if (validateRefProp != null && validateRefProp.boolValue)
        {
            validateRefProp.boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            // NOTE: Do NOT call pluginImporter.SaveAndReimport() here inside OnPreprocessAsset to avoid infinite loops!
        }
        else
        {
            Debug.LogError($"{importer.assetPath} does not have a {validateReferencesProperty} property.\n");
        }
    }
}
#endif
#endif
