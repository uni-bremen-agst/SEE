using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using StreamRpc;
using UnityEngine;

namespace Assets.SEE.IdeIntegration.IPC
{
    /// <summary>
    /// Only the _port can be specified.
    /// </summary>
    public sealed class JsonRpcSocketServer : JsonRpcServer
    {
        private readonly int _port;
        private TcpListener _socket;

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
            try
            {		
                _socket = new TcpListener(IPAddress.Parse("127.0.0.1"), _port);
                _socket.Start();

                while (true)
                {
                    using var tcpClient = await _socket.AcceptTcpClientAsync();
                    Rpc = JsonRpc.Attach(tcpClient.GetStream(), Target);
                    Debug.LogError("Connection to IDE establishedS.\n");
                    await Rpc.Completion;
                }
            }
            catch (SocketException)
            {
                
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _socket?.Stop();
        }
    }
}
