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

using VSSeeExtension.Utils;
using Task = System.Threading.Tasks.Task;

namespace VSSeeExtension.Commands
{
    /// <summary>
    /// The command to manually disconnect from SEE.
    /// </summary>
    public sealed class DisconnectFromSeeCommand : Command
    {
        /// <summary>
        /// Singleton constructor.
        /// </summary>
        private DisconnectFromSeeCommand()
        {
        }

        /// <summary>
        /// Singleton instance of this class.
        /// </summary>
        public static DisconnectFromSeeCommand Instance { get; private set; }

        /// <summary>
        /// Initialize the <see cref="DisconnectFromSeeCommand"/> class.
        /// </summary>
        /// <param name="package">The Package which is calling initializing the button.</param>
        /// <returns></returns>
        public static async Task InitializeAsync(VSSeeExtensionPackage package)
        {
            Instance = new DisconnectFromSeeCommand();
            await Instance.AddCommandAsync(package, PackageInfo.VSSeePackageCommandSet,
                PackageInfo.DisconnectFromSeeCommandId);
        }

        /// <summary>
        /// Functionality of this Button.
        /// </summary>
        protected override async Task ExecuteAsync()
        {
            await base.ExecuteAsync();
            Package.Integration.Stop();
        }

        /// <summary>
        /// The visibility of this command.
        /// </summary>
        /// <returns>Visibility.</returns>
        protected override bool IsVisible()
        {
            return Package.Integration.IsConnected();
        }

        /// <summary>
        /// Is the command enabled.
        /// </summary>
        /// <returns>Is enabled?</returns>
        protected override bool IsEnabled()
        {
            return !Package.Options.AutoConnect;
        }
    }
}
