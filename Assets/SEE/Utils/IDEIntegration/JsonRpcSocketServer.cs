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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using StreamRpc;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// The implementation of JsonRpcServer that will use <see cref="TcpListener"/>.
    /// </summary>
    public sealed class JsonRpcSocketServer : JsonRpcServer
    {
        #region Client

        /// <summary>
        /// A class that will represent the client.
        /// </summary>
        private sealed class Client : JsonRpcConnection
        {
            /// <summary>
            /// TCP connection to the client.
            /// </summary>
            private readonly TcpClient client;

            /// <summary>
            /// Creates a new client connection using a <see cref="TcpClient"/>.
            /// </summary>
            /// <param name="rpcServer">The server of this client.</param>
            /// <param name="client">The TCP client.</param>
            public Client(TcpClient client)
            {
                this.client = client;
            }

            /// <summary>
            /// Will initiate the JsonRpc connection.
            /// </summary>
            protected override bool InitiateJsonRpc()
            {
                try
                {
                    Rpc = JsonRpc.Attach(client.GetStream());
                }
                catch (Exception)
                {
                    return false;
                }

                return true;
            }

            /// <summary>
            /// Disposes all streams.
            /// </summary>
            public override void Abort()
            {
                base.Abort();
                client?.Close();
            }
        }

        #endregion

        /// <summary>
        /// The port used for the inter-process communication.
        /// </summary>
        private readonly int port;

        /// <summary>
        /// TCP server.
        /// </summary>
        private TcpListener socket;

        /// <summary>
        /// Creates a new JsonRpcNamedPipeServer instance.
        /// </summary>
        /// <param name="target">An object that contains function that can be called
        /// remotely. If null no target will be added by default.</param>
        /// <param name="port">The port, that will be used for communication.</param>
        public JsonRpcSocketServer(object target, int port) : base(target)
        {
            this.port = port;
        }

        /// <summary>
        /// Starts the socket server implementation.
        /// </summary>
        /// <param name="maxClients">The maximal number of clients that can connect to the server.</param>
        /// <param name="token">Token to cancel the current Task.</param>
        /// <returns>Async Task.</returns>
        protected override async UniTask StartServerAsync(uint maxClients, CancellationToken token)
        {
            try
            {	
                socket = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
                socket.Start();

                // Listens to incoming Requests. Only one client will be connected to the server.
                while (true)
                {
                    TcpClient tcpClient = await socket.AcceptTcpClientAsync().AsUniTask()
                        .AttachExternalCancellation(token);

                    if (RpcConnections.Count < maxClients)
                    {
                        byte[] message = Encoding.ASCII.GetBytes("START\n");
                        tcpClient.GetStream().Write(message, 0, message.Length);
                        await tcpClient.GetStream().FlushAsync(token);
                        RunConnection(new Client(tcpClient));
                    }
                    else
                    {
                        byte[] message = Encoding.ASCII.GetBytes("MAX_CLIENT_REACHED\n");
                        tcpClient.GetStream().Write(message, 0, message.Length);
                        await tcpClient.GetStream().FlushAsync(token);
                    }
                }
            }
            catch (SocketException e)
            {
#if UNITY_EDITOR
                Debug.LogError($"{e.Message}\n");
#endif
                throw new JsonRpcServerCreationFailedException("Couldn't initiate the Server instance.");
            }
        }

        /// <summary>
        /// Dispose all open streams etc.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            socket?.Stop();
        }
    }
}
