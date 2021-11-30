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
            private TcpClient _client;

            /// <summary>
            /// Creates a new client connection using a <see cref="TcpClient"/>.
            /// </summary>
            /// <param name="rpcServer">The server of this client.</param>
            /// <param name="client">The TCP client.</param>
            public Client(JsonRpcServer rpcServer, TcpClient client) :base(rpcServer)
            {
                _client = client;
            }

            /// <summary>
            /// The Task that will handle the connection.
            /// </summary>
            /// <param name="token">The cancellation token.</param>
            /// <returns>Async UniTask.</returns>
            protected override async UniTask RunTask(CancellationToken token)
            {
                Rpc = JsonRpc.Attach(_client.GetStream(), RpcServer.Target);
                Connected?.Invoke(this);

                try
                {
                    await Rpc.Completion.AsUniTask().AttachExternalCancellation(token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    // Connection was unexpectedly interrupted.
                }

                Disconnected?.Invoke(this);
                Abort();
            }

            /// <summary>
            /// Disposes all streams.
            /// </summary>
            public override void Abort()
            {
                base.Abort();
                _client?.Close();
            }
        }

        #endregion

        /// <summary>
        /// The port used for the inter-process communication.
        /// </summary>
        private readonly int _port;

        /// <summary>
        /// TCP server.
        /// </summary>
        private TcpListener _socket;

        /// <summary>
        /// Creates a new JsonRpcNamedPipeServer instance.
        /// </summary>
        /// <param name="target">An object that contains function that can be called
        /// remotely.</param>
        /// <param name="port">The port, that will be used for communication.</param>
        public JsonRpcSocketServer(object target, int port) : base(target)
        {
            this._port = port;
        }

        /// <summary>
        /// Starts the socket server implementation.
        /// </summary>
        /// <param name="maxClients">The maximal number of clients that can connect to the server.</param>
        /// <param name="token">Token to cancel the current Task.</param>
        /// <returns>Async Task.</returns>
        protected override async UniTask StartServerAsync(int maxClients, CancellationToken token)
        {
            try
            {	
                _socket = new TcpListener(IPAddress.Parse("127.0.0.1"), _port);
                _socket.Start();

                // Listens to incoming Requests. Only one client will be connected to the server.
                while (true)
                {
                    var tcpClient = await _socket.AcceptTcpClientAsync().AsUniTask()
                        .AttachExternalCancellation(token);

                    if (RpcConnections.Count < maxClients)
                    {
                        RunConnection(new Client(this, tcpClient));
                    }
                    else
                    {
                        var errorMessage = Encoding.ASCII.GetBytes("Maximal number of clients reached!");
                        tcpClient.GetStream().Write(errorMessage, 0, errorMessage.Length);
                        tcpClient.Close();
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
            _socket?.Stop();
        }
    }
}
