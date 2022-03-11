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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using StreamRpc;
using Task = System.Threading.Tasks.Task;

namespace VSSeeExtension.Utils.IPC
{
    /// <summary>
    /// Base Class of the JsonRpcClient. Communication can be accomplished by streams.
    /// </summary>
    public abstract class JsonRpcClient : IDisposable
    {
        /// <summary>
        /// Connection events for this client. Since many methods for Visual Studio need to
        /// executed asynchronously, the return type needs to be a <see cref="Task"/>.
        /// </summary>
        public delegate Task ConnectionEvent();

        /// <summary>
        /// Event fired when client is connected.
        /// </summary>
        public ConnectionEvent Connected;

        /// <summary>
        /// Event fired when client is disconnected.
        /// </summary>
        public ConnectionEvent Disconnected;

        /// <summary>
        /// The JsonRpc instance for standardized communication over a stream.
        /// </summary>
        protected JsonRpc Rpc;

        /// <summary>
        /// The object with all function for the server.
        /// </summary>
        protected object Target;

        /// <summary>
        /// Should this client automatically connect?
        /// </summary>
        protected bool AutoConnect;

        /// <summary>
        /// Task to check if this class is already trying to connect.
        /// </summary>
        private Task connectTask;

        /// <summary>
        /// For canceling the current task.
        /// </summary>
        private CancellationTokenSource token;

        /// <summary>
        /// For awaiting the connection status.
        /// </summary>
        private SemaphoreSlim semaphore;

        /// <summary>
        /// Abstract class for the JsonRpcClient class.
        /// </summary>
        /// <param name="autoConnect">Should the Client try to reconnect after lost of connection.</param>
        /// <param name="target">An object that contains function that can be called remotely.</param>
        protected JsonRpcClient(bool autoConnect, object target)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));
            Target = target;
            AutoConnect = autoConnect;
            token = new CancellationTokenSource();
        }

        /// <summary>
        /// Starts the client only if not already started. If auto connect is disabled, this
        /// method will not try to continuously connect.
        ///
        /// Note: Will always return false if auto connect is enabled.
        /// </summary>
        /// <returns>True if this started and connected the client, false otherwise.</returns>
        public async Task<bool> StartAsync()
        {
            if (connectTask != null) return false;
            connectTask ??= StartServerAsync();
            semaphore = new SemaphoreSlim(0, 1);
            await semaphore.WaitAsync();
            return IsConnected();
        }

        /// <summary>
        /// Start connecting to the server asynchronously.
        /// </summary>
        /// <returns>Async Task.</returns>
        private async Task StartServerAsync()
        {
            await ConnectAsync(token.Token);
            semaphore.Release();
            if (IsConnected())
            {
                if (AutoConnect)
                {
                    Rpc.Disconnected += Reconnect;
                }
                Connected?.Invoke();
                try
                {
                    await Rpc.Completion.WithCancellation(token.Token);
                }
                catch (Exception)
                {
                    // Connection lost
                    return;
                }
                Disconnected?.Invoke();
            }

            // clean up
            Dispose();
        }

        /// <summary>
        /// Stops the client if connected.
        /// </summary>
        public void Stop()
        {
            if (!IsConnected()) return;


            if (Disconnected != null)
            {
                _ = Disconnected.Invoke();
            }
            Dispose();
        }

        /// <summary>
        /// Call a Process remotely.
        /// </summary>
        /// <param name="targetName">Method name.</param>
        /// <param name="arguments">Arguments for remotely called process.</param>
        /// <returns></returns>
        public async Task CallRemoteProcessAsync(string targetName, params object[] arguments)
        {
            if (IsConnected())
            {
                try
                {
                    await Rpc.InvokeAsync(targetName, arguments);
                }
                catch (Exception e)
                {
                    // Connection was unexpectedly interrupted
                    await Logger.LogErrorAsync(this, e.Message);
                }
            }
            else
            {
                await Logger.LogErrorAsync(this, "Couldn't call remote process. " +
                                                      "Client isn't connected to server");
            }
        }

        /// <summary>
        /// Returns the connection status of the client.
        /// </summary>
        /// <returns>Is the Client connected?</returns>
        public bool IsConnected()
        {
            return Rpc != null;
        }

        /// <summary>
        /// For EventHandler to reconnect.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">EventArgs <see cref="System.EventArgs"/></param>
        protected void Reconnect(object sender, EventArgs e)
        {
            Dispose();
            _ = StartAsync();
        }

        /// <summary>
        /// Specific connection implementation of the derived class that will set <see cref="Rpc"/>.
        /// Will try to connect to the server depending on <see cref="AutoConnect"/>.
        /// </summary>
        /// <param name="token">Token to cancel the current Task.</param>
        /// <returns>Async Task.</returns>
        protected abstract Task ConnectAsync(CancellationToken token);

        /// <summary>
        /// Dispose all open streams, connections etc. Will not call <see cref="Disconnected"/>. Call
        /// <see cref="Stop"/> instead.
        /// </summary>
        public virtual void Dispose()
        {
            // to prevent reconnecting after disposing the old JSON-RPC connection
            if (Rpc != null && AutoConnect)
            {
                Rpc.Disconnected -= Reconnect;
            }
            token.Cancel();
            Rpc?.Dispose();

            token = new CancellationTokenSource();
            connectTask = null;
            Rpc = null;
        }
    }
}
