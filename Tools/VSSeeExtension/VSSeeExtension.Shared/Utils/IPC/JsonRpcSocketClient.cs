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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using StreamRpc;
using Task = System.Threading.Tasks.Task;

namespace VSSeeExtension.Utils.IPC
{
    /// <summary>
    /// This JsonRpcClient is using TcpClient to communicate.
    /// </summary>
    public sealed class JsonRpcSocketClient : JsonRpcClient
    {
        /// <summary>
        /// Port on which the server is communicating.
        /// </summary>
        private readonly int port;

        /// <summary>
        /// The client, who is connected to the server.
        /// </summary>
        private TcpClient client;

        /// <summary>
        /// Constructor of the JsonRpcSocketClient.
        /// </summary>
        /// <param name="autoConnect">Should the Client try to reconnect after lost of connection.</param>
        /// <param name="target">An object that contains function that can be called remotely.</param>
        /// <param name="port">Port number of TCP connection</param>
        public JsonRpcSocketClient(bool autoConnect, object target, int port) : base(autoConnect, target)
        {
            this.port = port;
        }

        /// <summary>
        /// Will try to connect to a TCP server. 
        /// </summary>
        /// <param name="token">Token to cancel the current Task.</param>
        /// <returns>Async Task.</returns>
        protected override async Task ConnectAsync(CancellationToken token)
        {
            client = new TcpClient();
            while (true)
            {
                try
                {
                    await client.ConnectAsync(IPAddress.Parse("127.0.0.1"), port);
                }
                catch (SocketException)
                {
                    // Server not ready
                }
                catch (Exception e)
                {
                    await Console.Error.WriteLineAsync(e.ToString()).WithCancellation(token);
                    return;
                }

                if (client.Connected && await IsClientAcceptedAsync()) break;
                if (!AutoConnect) return;
                client?.Close();
                client = new TcpClient();

                // Sleep for 3 seconds
                await Task.Delay(3000, token);
            }

            Rpc = JsonRpc.Attach(client.GetStream(), Target);
        }

        /// <summary>
        /// Checks whether the server accepted this connection or not.
        /// </summary>
        /// <returns>True if accepted, false otherwise.</returns>
        private async Task<bool> IsClientAcceptedAsync()
        {
            if (client == null) return false;

            try
            {
                using StreamReader reader = new StreamReader(client.GetStream(), Encoding.UTF8,
                    true, 1024, true);

                string line;
                if ((line = await reader.ReadLineAsync()) != null)
                {
                    return line switch
                    {
                        "START" => true,
                        "MAX_CLIENT_REACHED" => false,
                        _ => false,
                    };
                }
            }
            catch (Exception e)
            {
                await Logger.LogMessageAsync(this, e.Message);
            }
            return false;
        }

        /// <summary>
        /// Dispose all open streams etc.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            client?.Dispose();
        }
    }
}
