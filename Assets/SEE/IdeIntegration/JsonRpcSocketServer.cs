using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using StreamRpc;
using UnityEngine;

namespace Assets.SEE.IdeIntegration
{
    /// <summary>
    /// Only the _port can be specified.
    /// </summary>
    public sealed class JsonRpcSocketServer : JsonRpcServer
    {
        private readonly int _port;
        private Socket _serverSocket;

        public JsonRpcSocketServer(int port)
        {
            this._port = port;
        }

        public override Task CallRemoteProcessAsync(string targetName)
        {
            throw new NotImplementedException();
        }

        protected override async Task StartServerAsync()
        {
            using var socket = GetSocket();

            if (socket != null)
            {
                using var stream = new NetworkStream(socket);
                Rpc = JsonRpc.Attach(stream, Target);
                Rpc.StartListening();
                await Rpc.Completion;
            }
        }

        private Socket GetSocket()
        {
            Socket tmp = null;
            var hostEntry = Dns.GetHostEntry("localhost").AddressList[0];

            try
            {
                if (_serverSocket == null)
                {
                    _serverSocket = new Socket(hostEntry.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    _serverSocket.Bind(new IPEndPoint(hostEntry, _port));
                    _serverSocket.Listen(10);
                }
                
                tmp = _serverSocket.Accept();
            }
            catch (Exception e)
            {
                // TODO: Change logger?
                Debug.LogError(e);
            }

            return tmp;
        }

        public override void Dispose()
        {
            base.Dispose();
            _serverSocket?.Dispose();
        }
    }
}
