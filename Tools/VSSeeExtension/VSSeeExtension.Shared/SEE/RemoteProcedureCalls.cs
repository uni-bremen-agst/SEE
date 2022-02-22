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

using Microsoft.VisualStudio.Shell;
using VSSeeExtension.Utils;
using VSSeeExtension.Utils.Helpers;
using WindowHelper = VSSeeExtension.Shared.Utils.Helpers.WindowHelper;

namespace VSSeeExtension.SEE
{
    public partial class SeeIntegration
    {
        /// <summary>
        /// All commands that get called by SEE.
        /// </summary>
        private class RemoteProcedureCalls
        {
            /// <summary>
            /// Parent class.
            /// </summary>
            private readonly SeeIntegration integration;

            /// <summary>
            /// Initializes this class. Brings functionality for See.
            /// </summary>
            /// <param name="integration">Parent class.</param>
            public RemoteProcedureCalls(SeeIntegration integration)
            {
                this.integration = integration;
            }

            /// <summary>
            /// Opens a File and jumps to the given line.
            /// </summary>
            /// <param name="path">The absolute path of the file</param>
            /// <param name="line">The line number. If 0 it will go to the previous spot.</param>
            /// <returns>Was successful.</returns>
            public void OpenFile(string path, int? line = null)
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    if (await DocumentHelper.OpenFileAsync(integration.package.DisposalToken, path, line))
                    {
                        await DocumentHelper.HighlightLineAsync(integration.package.DisposalToken);
                    }
                });
            }

            /// <summary>
            /// Gets the absolute solution path.
            /// </summary>
            /// <returns>The solution path.</returns>
            public string GetProject()
            { 
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                    await SolutionHelper.GetSolutionAsync(integration.package.DisposalToken));
            }

            /// <summary>
            /// Returns the version specified in <see cref="PackageInfo.IdeVersion"/>.
            /// </summary>
            /// <returns>The version.</returns>
            public string GetIdeVersion()
            {
                return PackageInfo.IdeVersion;
            }

            /// <summary>
            /// Was this client instance started by SEE directly through a command switch.
            /// </summary>
            /// <returns>True if SEE started this client.</returns>
            public bool WasStartedBySee()
            {
                return integration.StartedBySee;
            }

            /// <summary>
            /// Will focus this IDE instance.
            /// </summary>
            public void SetFocus()
            {
                _ = WindowHelper.FocusWindowAsync(integration.package.DisposalToken);
            }

            /// <summary>
            /// Calling this method will change the loaded solution of this IDE.
            /// </summary>
            /// <param name="path">The absolute solution path.</param>
            public void ChangeSolution(string path)
            {
                _ = SolutionHelper.OpenSolutionAsync(integration.package.DisposalToken, path);
            }

            /// <summary>
            /// Declines this instance of Visual Studio and stops auto connecting attempts. Should be
            /// called by the server when solution doesn't match the current solution.
            /// </summary>
            public void Decline()
            {
                integration.StopByServer();
            }
        }
    }
}
