// Copyright © 2022 Jan-Philipp Schramm
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SEE.IDE
{
    /// <summary>
    /// Visual Studio pathfinder class. Uses Microsoft's "vswhere" tool to retrieve
    /// information about a Visual Studio instance. You can find the project
    /// of vswhere here: https://github.com/microsoft/vswhere.
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
            {
                throw new InvalidOperationException("This method is only supported on Windows!");
            }
            string requestedInstance = version switch
            {
                Version.VS2019 => VS2019,
                Version.VS2022 => VS2022,
                _ => throw new NotImplementedException(
                    $"Implementation of case {version} not found!")
            };

            string result = await ExecuteVSWhereAsync(
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
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = VSWherePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            try
            {
                using Process proc = Process.Start(start);

                string result = "";
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
