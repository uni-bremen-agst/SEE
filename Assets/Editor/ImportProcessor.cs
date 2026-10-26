
#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;

namespace SEEEditor
{
    public class ImportProcessor : AssetPostprocessor
    {
        /// <summary>
        /// The function is called after importing of any number of assets is complete.
        ///
        /// The Nuget package Microsoft.CodeAnalysis.Analyzers.5.9.0-1.26328.17 contains a rulesets folder
        /// that causes Unity to report errors in the console of the kind:
        ///
        /// Assets/Packages/Microsoft.CodeAnalysis.Analyzers.*/rulesets
        /// already contains a ruleset file:
        /// Assets/Packages/Microsoft.CodeAnalysis.Analyzers.*/rulesets/MicrosoftCodeAnalysisLocalizationRulesEnabled.ruleset.
        /// Assets/Packages/Microsoft.CodeAnalysis.Analyzers.*/rulesets/MicrosoftCodeAnalysisPerformanceRulesDefault.ruleset
        /// will be ignored.
        ///
        /// The Microsoft.CodeAnalysis.Analyzers NuGet package includes various rule sets in the “rulesets” subfolder.
        /// When the package is extracted, the package manager (or Unity itself when importing the folders into the
        /// Assets directory) attempts to load all files.
        /// Since Unity treats all files in the Assets folder as importable assets, and the package has a structure
        /// where certain mechanisms are triggered multiple times or where files conflict during extraction
        /// (or during a package update with leftover files), Unity briefly aborts the process with this warning/message.
        /// The term “error message” sounds dramatic, but in Unity this is usually just a log warning telling you
        /// that a rule file was ignored—either because one apparently already existed or because the path was processed
        /// twice.
        /// Is that a problem? No, not at all for your game. These are just Analyzer rules for development, this
        /// does not corrupt any gameplay code or build. The catch: Since the package is now located in the
        /// Assets/Packages/ folder, Unity scans these configuration files every time the project is started or reloaded.
        /// We can safely delete the rulesets folder in the Assets/Packages/Microsoft.CodeAnalysis.Analyzers.*/rulesets folder.
        /// The IDE analysis will still work perfectly, since the actual analyzer logic is contained in the package's .dll files.
        /// The rulests are completely useless for Unity, since Unity does not use them.
        /// Whenever NuGet updates or restores the package, the rulesets folder will be recreated, and this function will
        /// delete it again.
        /// </summary>
        /// <param name="importedAssets">Paths to imported assets.</param>
        /// <param name="deletedAssets">Paths to deleted assets.</param>
        /// <param name="movedAssets">Paths to moved assets.</param>
        /// <param name="movedFromAssetPaths">Original paths for moved assets.</param>
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            string packagesPath = Path.Combine(Application.dataPath, "Packages");
            if (!Directory.Exists(packagesPath))
            {
                return;
            }

            foreach (string packageDir in Directory.GetDirectories(packagesPath, "Microsoft.CodeAnalysis.Analyzers.*"))
            {
                string rulesetsPath = Path.Combine(packageDir, "rulesets");
                if (Directory.Exists(rulesetsPath))
                {
                    try
                    {
                        Directory.Delete(rulesetsPath, true);
                        Debug.Log($"Deleted rulesets folder at {rulesetsPath} to avoid Unity import warnings.\n");
                        // Optional: Also delete the .meta file, if it exists
                        string metaPath = rulesetsPath + ".meta";
                        if (File.Exists(metaPath))
                        {
                            File.Delete(metaPath);
                        }

                        AssetDatabase.Refresh();
                    }
                    catch (System.Exception)
                    {
                        // Ignore if files are currently locked.
                        Debug.LogWarning($"Could not delete rulesets folder at {rulesetsPath}. It may be locked by another process.\n");
                    }
                }
            }
        }
    }
}

#endif
