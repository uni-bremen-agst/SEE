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
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using VSSeeExtension.Utils;
using Task = System.Threading.Tasks.Task;

namespace VSSeeExtension.Commands
{
    /// <summary>
    /// Base class of the Visual Studio commands.
    /// </summary>
    public abstract class Command
    {
        /// <summary>
        /// Package that owns this command.
        /// </summary>
        protected VSSeeExtensionPackage Package;
        
        /// <summary>
        /// Adds the command instance to MenuCommandService.
        /// </summary>
        /// <param name="package">Package that owns this command.</param>
        /// <param name="commandSet">Unique identifier of this command group.</param>
        /// <param name="commandId">Id of this command.</param>
        /// <returns>Async task.</returns>
        protected async Task AddCommandAsync(VSSeeExtensionPackage package, Guid commandSet,
            int commandId)
        {
            Package = package ?? throw new ArgumentNullException(nameof(package));

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(Package.DisposalToken);

            if (await Package.GetServiceAsync(typeof(IMenuCommandService)) is not OleMenuCommandService commandService)
                throw new Exception(nameof(commandService));
            
            CommandID menuCommandId = new CommandID(commandSet, commandId);
            
            if (commandService.FindCommand(menuCommandId) == null)
            {
                OleMenuCommand command = new OleMenuCommand(Execute, null, BeforeQueryStatus, menuCommandId);
                commandService.AddCommand(command);
                await Logger.LogMessageAsync(this, "Successfully initialized command.");
            }
        }

        /// <summary>
        /// Functionality of this Button for the EventHandler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">EventArgs <see cref="EventArgs"/></param>
        private void Execute(object sender, EventArgs e)
        {
            _ = ExecuteAsync();
        }

        /// <summary>
        /// Update the status of this command.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">EventArgs <see cref="EventArgs"/></param>
        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand command = (OleMenuCommand) sender;
            command.Enabled = IsEnabled();
            command.Visible = IsVisible();
        }

        /// <summary>
        /// Functionality of this Button.
        /// </summary>
#pragma warning disable CS1998
        protected virtual async Task ExecuteAsync()
#pragma warning restore CS1998
        {
            // CS1998 (Async method lacks 'await' operators and will run synchronously) ignored.
            // Will be implemented in derived class.
        }

        /// <summary>
        /// The visibility of this command.
        /// </summary>
        /// <returns>Visibility.</returns>
        protected virtual bool IsVisible()
        {
            return true;
        }

        /// <summary>
        /// Is the command enabled.
        /// </summary>
        /// <returns>Is enabled?</returns>
        protected virtual bool IsEnabled()
        {
            return true;
        }
    }
}
