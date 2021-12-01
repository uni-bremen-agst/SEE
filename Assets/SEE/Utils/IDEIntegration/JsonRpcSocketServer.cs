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
        private sealed class Client : JsonRpcClientConnection
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
            public Client(JsonRpcServer rpcServer, TcpClient client) :base(rpcServer)
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
                    Rpc = JsonRpc.Attach(client.GetStream(), RpcServer.Target);
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
        /// remotely.</param>
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
                    var tcpClient = await socket.AcceptTcpClientAsync().AsUniTask()
                        .AttachExternalCancellation(token);

                    if (RpcConnections.Count < maxClients)
                    {
                        var message = Encoding.ASCII.GetBytes("START\n");
                        tcpClient.GetStream().Write(message, 0, message.Length);
                        await tcpClient.GetStream().FlushAsync(token);
                        RunConnection(new Client(this, tcpClient));
                    }
                    else
                    {
                        var message = Encoding.ASCII.GetBytes("MAX_CLIENT_REACHED\n");
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
