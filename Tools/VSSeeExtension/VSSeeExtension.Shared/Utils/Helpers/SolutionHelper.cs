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

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace VSSeeExtension.Utils.Helpers
{
    /// <summary>
    /// Provides functions for handling the solution.
    /// </summary>
    public static class SolutionHelper
    {
        /// <summary>
        /// Opens a given solution.
        /// </summary>
        /// <param name="cancellationToken">To avoid the main thread transition when no longer
        /// needed.</param>
        /// <param name="sln">The absolute path of the .sln file.</param>
        /// <returns>True if successfully opened.</returns>
        public static async Task<bool> OpenSolutionAsync(CancellationToken cancellationToken,
            string sln)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            // To open a solution file
            IVsSolution solutionService = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));
            int result = solutionService.OpenSolutionFile((int)__VSSLNOPENOPTIONS.SLNOPENOPT_Silent, sln);

            if (result == VSConstants.S_OK)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the currently opened solution.
        /// </summary>
        /// <param name="cancellationToken">To avoid the main thread transition when no longer
        /// needed.</param>
        /// <returns>Returns the path of the opened solution.</returns>
        public static async Task<string> GetSolutionAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return (await DteHelper.GetDteAsync(cancellationToken)).Solution.FullName;
        }
    }
}
