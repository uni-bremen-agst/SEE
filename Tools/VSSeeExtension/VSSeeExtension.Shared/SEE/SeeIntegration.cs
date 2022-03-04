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
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using VSSeeExtension.Shared.Utils.Helpers;
using VSSeeExtension.Utils;
using VSSeeExtension.Utils.Helpers;
using VSSeeExtension.Utils.IPC;
using Task = System.Threading.Tasks.Task;

namespace VSSeeExtension.SEE
{
    /// <summary>
    /// Provides necessary functions to interact with the server (SEE).
    /// </summary>
    public partial class SeeIntegration : IDisposable
    {
        /// <summary>
        /// Package that owns the SeeIntegration.
        /// </summary>
        private readonly VSSeeExtensionPackage package;

        /// <summary>
        /// The current JsonRpcClient.
        /// </summary>
        private JsonRpcClient rpc;

        /// <summary>
        /// SEE methods that can be called by the client.
        /// </summary>
        public SeeCalls See { get; private set; }

        /// <summary>
        /// There is only one SeeIntegration instance.
        /// </summary>
        private static SeeIntegration instance;

        /// <summary>
        /// Private property indicating whether Visual Studio was started by SEE.
        /// </summary>
        private bool startedBySee;

        /// <summary>
        /// Indicates whether Visual Studio was started by SEE.
        /// </summary>
        public bool StartedBySee
        {
            get
            {
                if (!startedBySee) return false;
                startedBySee = false;
                return true;
            }
            private set => startedBySee = value;
        }

        /// <summary>
        /// The integration that will communicate with SEE.
        /// </summary>
        /// <param name="package">Package that owns this class</param>
        public SeeIntegration(VSSeeExtensionPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));

            if (instance != null)
            {
                _ = Logger.LogErrorAsync(this, "Only one instance is allowed!");
                throw new InvalidOperationException("Only one instance is allowed!");
            }

            instance = this;

            InitializeJsonRpcClient();
            InitializeEventHandler();
        }

        /// <summary>
        /// Checks whether the client is connected or not.
        /// </summary>
        /// <returns>Is client connected?</returns>
        public bool IsConnected()
        {
            return rpc != null && rpc.IsConnected();
        }

        /// <summary>
        /// Starts the client only if not already started. If auto connect is disabled, this
        /// method will not try to continuously connect.
        ///
        /// Note: Will always return false if auto connect is enabled.
        /// </summary>
        /// <param name="startedBySee">Whether this action was directly taken by SEE.</param>
        /// <returns>True if this started and connected the client, false otherwise.</returns>
        public async Task<bool> StartAsync(bool startedBySee = false)
        {
            StartedBySee = startedBySee;
            return rpc != null && await rpc.StartAsync());
        }

        /// <summary>
        /// Special stop method that will retry a connection attempt while in automatic mode.
        /// </summary>
        public void StopByServer()
        {
            Stop();
            InitializeJsonRpcClient();
            InitializeEventHandler();
        }

        /// <summary>
        /// Stops the integration and resets the status.
        /// </summary>
        public void Stop()
        {
            rpc?.Stop();
        }

        /// <summary>
        /// Initializes the JsonRpcClient and starts it if needed. Will use all settings from <see cref="Options"/>.
        /// </summary>
        private void InitializeJsonRpcClient()
        {
            rpc?.Dispose();

            // Due to limitations with Mono only Socket implementation works with Unity.
            rpc = new JsonRpcSocketClient(
                package.Options.AutoConnect,
                new RemoteProcedureCalls(this),
                package.Options.TcpPort);

            if (package.Options.AutoConnect)
            {
                rpc.StartAsync();
            }

            See = new SeeCalls(this);
        }

        /// <summary>
        /// Subscribes to all necessary events.
        /// </summary>
        private void InitializeEventHandler()
        {
            package.Options.SettingsChanged += (s, e) => InitializeJsonRpcClient();
            rpc.Connected += UpdateUiAsync;
            rpc.Disconnected += UpdateUiAsync;
            SolutionEvents.OnAfterBackgroundSolutionLoadComplete += (s, e) =>
            {
                if (IsConnected())
                {
                    ThreadHelper.JoinableTaskFactory.Run(async () => See.SolutionChangedAsync(
                        await SolutionHelper.GetSolutionAsync(package.DisposalToken)));
                }
            };
        }

        /// <summary>
        /// Method that will be called when the client is connected or disconnected. Will update
        /// the commands in Visual Studio's UI.
        /// </summary>
        private async Task UpdateUiAsync()
        {
            await WindowHelper.UpdateUiAsync(package.DisposalToken);
        }

        /// <summary>
        /// Disposes the JsonRpcClient.
        /// </summary>
        public void Dispose()
        {
            rpc?.Dispose();
        }
    }
}
