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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VSSeeExtension.Utils.Helpers;
using Task = System.Threading.Tasks.Task;

namespace VSSeeExtension.Shared.Utils.Helpers
{
    /// <summary>
    /// Provides functions for handling the Visual Studio window.
    /// </summary>
    public static class WindowHelper
    {
        /// <summary>
        /// Will set the focus in windows on this window.
        /// </summary>
        /// <param name="cancellationToken">To avoid the main thread transition when no longer
        /// needed.</param>
        /// <returns>Async Task.</returns>
        public static async Task FocusWindowAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            (await DteHelper.GetDteAsync(cancellationToken)).MainWindow.Activate();
        }

        /// <summary>
        /// Will asynchronously update the UI. 
        /// </summary>
        /// <param name="cancellationToken">To avoid the main thread transition when no longer
        /// needed.</param>
        /// <returns>Async Task.</returns>
        public static async Task UpdateUiAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            IVsUIShell vsShell = (IVsUIShell)Package.GetGlobalService(typeof(IVsUIShell));
            if (vsShell != null)
            {
                int result = vsShell.UpdateCommandUI(0);
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(result);
            }
        }
    }
}
