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
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using VSSeeExtension.Options;
using VSSeeExtension.Commands;
using VSSeeExtension.Utils;
using VSSeeExtension.SEE;
using Task = System.Threading.Tasks.Task;

namespace VSSeeExtension
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideAppCommandLine("VSSeeExtension", typeof(VSSeeExtensionPackage), Arguments = "0", DemandLoad = 0)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(VSSeeExtensionOptions), "SEE Integration", "General", 0, 0, true)]
    [Guid(PackageInfo.MainPackageString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class VSSeeExtensionPackage : AsyncPackage
    {
        /// <summary>
        /// The package options that can be changed in Visual Studio's options page.
        /// </summary>
        internal VSSeeExtensionOptions Options { get; private set; }

        /// <summary>
        /// Represents the integration of SEE.
        /// </summary>
        internal SeeIntegration Integration { get; private set; }

        /// <summary>
        /// Initializes all commands (buttons).
        /// </summary>
        private async Task InitializeCommandsAsync()
        {
            await AboutCommand.InitializeAsync(this);
            await HighlightMethodCommand.InitializeAsync(this);
            await HighlightClassCommand.InitializeAsync(this);
            await HighlightClassesCommand.InitializeAsync(this);
            await HighlightMethodReferencesCommand.InitializeAsync(this);
            await ConnectToSeeCommand.InitializeAsync(this);
            await DisconnectFromSeeCommand.InitializeAsync(this);
        }

        /// <summary>
        /// Will look for the command switch "/VSSeeExtension". If set, it will automatically
        /// start the Integration even though auto connect is deactivated.
        /// </summary>
        /// <returns>Async Task.</returns>
        private async Task CheckCommandSwitchesAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            // If command line switch was set, start integration.
            if (await GetServiceAsync(typeof(SVsAppCommandLine)) is not IVsAppCommandLine cmdService)
            {
                throw new Exception(nameof(cmdService));
            }
            if (cmdService.GetOption("VSSeeExtension", out int isPresent, out _) == VSConstants.S_OK)
            {
                if (isPresent == 1)
                {
                    // RPC can't be used during initialization and thus needs to be started separately, see also:
                    // https://docs.microsoft.com/en-us/visualstudio/extensibility/how-to-use-asyncpackage-to-load-vspackages-in-the-background
                    Task.Run(async () => await Integration.StartAsync(true));
                }
            }
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that relies on services provided by Visual Studio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // Get options defined by the Visual Studio options
            Options = (VSSeeExtensionOptions)GetDialogPage(typeof(VSSeeExtensionOptions));
            Integration = new SeeIntegration(this);

            await InitializeCommandsAsync();
            await CheckCommandSwitchesAsync();
            await Logger.LogMessageAsync(this, "Successfully initialized VSSeeExtensionPackage.");
        }

        /// <summary>
        /// Disposes all open streams, etc.
        /// </summary>
        /// <param name="disposing">true if disposing.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Integration?.Dispose();
        }
    }
}
