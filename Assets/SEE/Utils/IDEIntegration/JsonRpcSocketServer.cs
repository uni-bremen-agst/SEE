using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using StreamRpc;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Only the _port can be specified.
    /// </summary>
    public sealed class JsonRpcSocketServer : JsonRpcServer
    {
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
        /// <param name="token">Token to cancel the current Task.</param>
        /// <returns>Async Task.</returns>
        protected override async Task StartServerAsync(CancellationToken token)
        {
            try
            {		
                _socket = new TcpListener(IPAddress.Parse("127.0.0.1"), _port);
                _socket.Start();

                // Listens to incoming Requests. Only one client will be connected to the server.
                while (true)
                {
                    // TODO: For some reason cancel token can't be applied here!
                    using var tcpClient = await _socket.AcceptTcpClientAsync();
                    Connected?.Invoke();
                    Rpc = JsonRpc.Attach(tcpClient.GetStream(), Target);
                    await Rpc.Completion;
                    Disconnected?.Invoke();
                }
            }
            catch (SocketException e)
            {
#if UNITY_EDITOR
                Debug.LogError($"{e.Message}\n");
#endif
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
