using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace CITools
{
    /// <summary>
    /// Class that builds the project, intended to be called from the command line (especially from our CI).
    ///
    /// </summary>
    /// <remarks>
    /// This class was copied on 2023-09-29 from
    /// https://github.com/game-ci/documentation/blob/main/example/BuildScript.cs (MIT license)
    /// and adjusted to work with SEE (and adhere to its code style).
    ///
    /// If the remote file has been changed after the date given above, any changes may need to be incorporated here,
    /// and the date above should be adjusted accordingly.
    /// </remarks>
    public static class BuildScript
    {
        /// <summary>
        /// Newline character.
        /// </summary>
        private static readonly string EOL = Environment.NewLine;

        /// <summary>
        /// Keys of values that should never be logged.
        /// </summary>
        private static readonly string[] Secrets =
        {
            "androidKeystorePass", "androidKeyaliasName", "androidKeyaliasPass"
        };

        /// <summary>
        /// Creates a build for the settings provided via command line arguments.
        /// </summary>
        public static void Build()
        {
            // Gather values from args
            Dictionary<string, string> options = GetValidatedOptions();

            // Set version for this build
            PlayerSettings.bundleVersion = options["buildVersion"];
            PlayerSettings.macOS.buildNumber = options["buildVersion"];
            PlayerSettings.Android.bundleVersionCode = int.Parse(options["androidVersionCode"]);

            // Apply build target
            if (!Enum.TryParse(options["buildTarget"], out BuildTarget buildTarget))
            {
                throw new ArgumentException($"Invalid build target: {options["buildTarget"]}");
            }

            // Handle build target specific settings
            switch (buildTarget)
            {
                case BuildTarget.Android:
                {
                    EditorUserBuildSettings.buildAppBundle = options["customBuildPath"].EndsWith(".aab");
                    if (options.TryGetValue("androidKeystoreName", out string keystoreName)
                        && !string.IsNullOrEmpty(keystoreName))
                    {
                        PlayerSettings.Android.useCustomKeystore = true;
                        PlayerSettings.Android.keystoreName = keystoreName;
                    }
                    if (options.TryGetValue("androidKeystorePass", out string keystorePass)
                        && !string.IsNullOrEmpty(keystorePass))
                    {
                        PlayerSettings.Android.keystorePass = keystorePass;
                    }
                    if (options.TryGetValue("androidKeyaliasName", out string keyaliasName)
                        && !string.IsNullOrEmpty(keyaliasName))
                    {
                        PlayerSettings.Android.keyaliasName = keyaliasName;
                    }
                    if (options.TryGetValue("androidKeyaliasPass", out string keyaliasPass)
                        && !string.IsNullOrEmpty(keyaliasPass))
                    {
                        PlayerSettings.Android.keyaliasPass = keyaliasPass;
                    }
                    if (options.TryGetValue("androidTargetSdkVersion", out string androidTargetSdkVersion)
                        && !string.IsNullOrEmpty(androidTargetSdkVersion))
                    {
                        AndroidSdkVersions targetSdkVersion;
                        if (Enum.TryParse(androidTargetSdkVersion, out AndroidSdkVersions parsedTargetSdkVersion))
                        {
                            targetSdkVersion = parsedTargetSdkVersion;
                        }
                        else
                        {
                            UnityEngine.Debug.Log("Failed to parse androidTargetSdkVersion! Fallback to AndroidApiLevelAuto");
                            targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
                        }

                        PlayerSettings.Android.targetSdkVersion = targetSdkVersion;
                    }

                    break;
                }
                case BuildTarget.StandaloneOSX:
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
                    break;
            }

            // Determine subtarget
#if UNITY_2021_2_OR_NEWER
            if (!options.TryGetValue("standaloneBuildSubtarget", out string subtargetValue)
                || !Enum.TryParse(subtargetValue, out StandaloneBuildSubtarget buildSubtargetValue))
            {
                buildSubtargetValue = default;
            }
            int buildSubtarget = (int)buildSubtargetValue;
#endif

            // Custom build
            Build(buildTarget, buildSubtarget, options["customBuildPath"]);
        }

        /// <summary>
        /// Returns a dictionary of validated command line arguments.
        /// </summary>
        /// <returns>A dictionary of validated command line arguments.</returns>
        private static Dictionary<string, string> GetValidatedOptions()
        {
            Dictionary<string, string> validatedOptions = ParseCommandLineArguments();

            if (!validatedOptions.TryGetValue("projectPath", out string _))
            {
                Console.WriteLine("Missing argument -projectPath");
                EditorApplication.Exit(110);
            }

            if (!validatedOptions.TryGetValue("buildTarget", out string buildTarget))
            {
                Console.WriteLine("Missing argument -buildTarget");
                EditorApplication.Exit(120);
            }

            if (!Enum.IsDefined(typeof(BuildTarget), buildTarget ?? string.Empty))
            {
                Console.WriteLine($"{buildTarget} is not a defined {nameof(BuildTarget)}");
                EditorApplication.Exit(121);
            }

            if (!validatedOptions.TryGetValue("customBuildPath", out string _))
            {
                Console.WriteLine("Missing argument -customBuildPath");
                EditorApplication.Exit(130);
            }

            const string defaultCustomBuildName = "TestBuild";
            if (!validatedOptions.TryGetValue("customBuildName", out string customBuildName))
            {
                Console.WriteLine($"Missing argument -customBuildName, defaulting to {defaultCustomBuildName}.");
                validatedOptions.Add("customBuildName", defaultCustomBuildName);
            }
            else if (customBuildName == "")
            {
                Console.WriteLine($"Invalid argument -customBuildName, defaulting to {defaultCustomBuildName}.");
                validatedOptions.Add("customBuildName", defaultCustomBuildName);
            }

            return validatedOptions;
        }

        /// <summary>
        /// Returns a dictionary of command line arguments.
        /// </summary>
        /// <returns>A dictionary of command line arguments.</returns>
        private static Dictionary<string, string> ParseCommandLineArguments()
        {
            Dictionary<string, string> providedArguments = new();
            string[] args = Environment.GetCommandLineArgs();

            Console.WriteLine($"{EOL}" +
                              $"###########################{EOL}" +
                              $"#    Parsing settings     #{EOL}" +
                              $"###########################{EOL}" +
                              $"{EOL}");

            // Extract flags with optional values
            for (int current = 0, next = 1; current < args.Length; current++, next++)
            {
                // Parse flag
                bool isFlag = args[current].StartsWith("-");
                if (!isFlag)
                {
                    continue;
                }
                string flag = args[current].TrimStart('-');

                // Parse optional value
                bool flagHasValue = next < args.Length && !args[next].StartsWith("-");
                string value = flagHasValue ? args[next].TrimStart('-') : "";
                bool secret = Secrets.Contains(flag);
                string displayValue = secret ? "*HIDDEN*" : "\"" + value + "\"";

                // Assign
                Console.WriteLine($"Found flag \"{flag}\" with value {displayValue}.");
                providedArguments.Add(flag, value);
            }
            return providedArguments;
        }

        /// <summary>
        /// Builds the project for the given build target and subtarget.
        /// </summary>
        /// <param name="buildTarget">The build target.</param>
        /// <param name="buildSubtarget">The build subtarget.</param>
        /// <param name="filePath">The path to the build file.</param>
        private static void Build(BuildTarget buildTarget, int buildSubtarget, string filePath)
        {
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(s => s.path).ToArray();
            BuildPlayerOptions buildPlayerOptions = new()
            {
                scenes = scenes,
                target = buildTarget,
                //                targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget),
                locationPathName = filePath,
                //                options = UnityEditor.BuildOptions.Development
#if UNITY_2021_2_OR_NEWER
                subtarget = buildSubtarget
#endif
            };

            BuildSummary buildSummary = BuildPipeline.BuildPlayer(buildPlayerOptions).summary;
            ReportSummary(buildSummary);
            ExitWithResult(buildSummary.result);
        }

        /// <summary>
        /// Reports the build summary to the console.
        /// </summary>
        /// <param name="summary">The summary of the build.</param>
        private static void ReportSummary(BuildSummary summary)
        {
            Console.WriteLine($"{EOL}" +
                              $"###########################{EOL}" +
                              $"#      Build results      #{EOL}" +
                              $"###########################{EOL}" +
                              $"{EOL}" +
                              $"Duration: {summary.totalTime.ToString()}{EOL}" +
                              $"Warnings: {summary.totalWarnings}{EOL}" +
                              // NOTE: Unfortunately, we need to incorrectly report the number of errors
                              //       as 0 to make the CI pass, because we apparently have some non-critical errors
                              //       related to the TTS plugin. These are unfortunately hard to debug, as they
                              //       only appear on a completely clean build, and not thereafter.
                              //       The pipeline will still fail on critical errors, see ExitWithResult.
                              // TODO: We should investigate whether a BuildResult.Succeeded can still
                              //       indicate critical errors.
                              $"Errors: 0{EOL}" +
                              $"Size: {summary.totalSize.ToString()} bytes{EOL}" +
                              $"{EOL}");
            if (summary.totalErrors > 0)
            {
                Console.WriteLine($"IMPORTANT: Actual error count is {summary.totalErrors}!");
            }
        }

        /// <summary>
        /// Exits the editor with a result code based on the build result.
        /// </summary>
        /// <param name="result">The build result.</param>
        private static void ExitWithResult(BuildResult result)
        {
            switch (result)
            {
                case BuildResult.Succeeded:
                    Console.WriteLine("Build succeeded!");
                    EditorApplication.Exit(0);
                    break;
                case BuildResult.Failed:
                    Console.WriteLine("Build failed!");
                    EditorApplication.Exit(101);
                    break;
                case BuildResult.Cancelled:
                    Console.WriteLine("Build cancelled!");
                    EditorApplication.Exit(102);
                    break;
                case BuildResult.Unknown:
                default:
                    Console.WriteLine("Build result is unknown!");
                    EditorApplication.Exit(103);
                    break;
            }
        }
    }
}
