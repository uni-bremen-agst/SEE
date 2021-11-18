using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SEE.Utils
{
    /// <summary>
    /// Visual Studio pathfinder class. Uses Microsoft's "vswhere" project to receive
    /// information about a Visual Studio Instance. You can find the project
    /// here: https://github.com/microsoft/vswhere.
    ///
    /// Note: This only works on Windows.
    /// </summary>
    public static class VSPathFinder
    {
        /// <summary>
        /// All version that can be looked up.
        /// </summary>
        public enum Version
        {
            VS2019,
            VS2022
        }

        /// <summary>
        /// Path to external program to receive all information about Visual Studio.
        /// </summary>
        private static readonly string VSWherePath = Application.streamingAssetsPath + "\\vswhere\\vswhere.exe";

        /// <summary>
        /// Version range for Visual Studio 2019
        /// </summary>
        private static readonly string VS2019 = "[16.0,17.0)";

        /// <summary>
        /// Version range for Visual Studio 2022
        /// </summary>
        private static readonly string VS2022 = "[17.0,18.0)";

        /// <summary>
        /// Gets the requested path with executable. If executed by other operating than windows
        /// will throw <exception cref="InvalidOperationException">Only allowed on Windows</exception>.
        /// </summary>
        /// <param name="version">Which version of Visual Studio is wanted.</param>
        /// <returns>The path of the "devenv.exe".</returns>
        public static async UniTask<string> GetVisualStudioExecutableAsync(Version version)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new InvalidOperationException("This method is only supported on Windows!");
            var requestedInstance = version switch
            {
                Version.VS2019 => VS2019,
                Version.VS2022 => VS2022,
                _ => throw new NotImplementedException(
                    $"Implementation of case {version} not found!")
            };

            var result = await ExecuteVSWhereAsync(
                $"-version {requestedInstance} -format value -property productPath");

            return await UniTask.FromResult(result);
        }

        /// <summary>
        /// Executes "vswhere.exe".
        /// </summary>
        /// <param name="arguments">Arguments for "vswhere.exe".</param>
        /// <returns>Returns the output of the process</returns>
        private static async UniTask<string> ExecuteVSWhereAsync(string arguments)
        {
            var start = new ProcessStartInfo
            {
                FileName = VSWherePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            try
            {
                using var proc = Process.Start(start);

                var result = "";
                while (proc != null && !proc.StandardOutput.EndOfStream)
                {
                    result += await proc.StandardOutput.ReadLineAsync();
                }

                return await UniTask.FromResult(result);
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError(e);
#endif
                throw;
            }
        }
    }
}
