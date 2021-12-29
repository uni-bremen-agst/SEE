using System;
using Dissonance.Networking;
using Unity.Netcode;

namespace Dissonance.Integrations.Unity_NFGO
{
    public class NfgoServer
        : BaseServer<NfgoServer, NfgoClient, NfgoConn>
    {
        private readonly NfgoCommsNetwork _network;
        private byte[] _receiveBuffer = new byte[1024];

        private NetworkManager _networkManager;

        public NfgoServer(NfgoCommsNetwork network)
        {
            _network = network;
        }

        public override void Connect()
        {
            _networkManager = NetworkManager.Singleton;
            _networkManager.OnClientDisconnectCallback += Disconnected;
            _networkManager.CustomMessagingManager.RegisterNamedMessageHandler("DissonanceToServer", NamedMessageHandler);

            base.Connect();
        }

        public override void Disconnect()
        {
            if (_networkManager != null)
            {
                _networkManager.OnClientDisconnectCallback -= Disconnected;
                _networkManager.CustomMessagingManager?.UnregisterNamedMessageHandler("DissonanceToServer");
                _networkManager = null;
            }

            base.Disconnect();
        }

        private void Disconnected(ulong client)
        {
            ClientDisconnected(new NfgoConn(client));
        }

        private void NamedMessageHandler(ulong sender, FastBufferReader stream)
        {
            var length = stream.Length;

            if (_receiveBuffer.Length < length)
                Array.Resize(ref _receiveBuffer, length);

            var received = NfgoCommsNetwork.ReadPacket(ref stream, ref _receiveBuffer);
            NetworkReceivedPacket(new NfgoConn(sender), received);
        }

        protected override void ReadMessages()
        {
            // Messages are delivered in the callback set in `RegisterNamedMessageHandler`. Nothing to do here.
        }

        protected override void SendReliable(NfgoConn destination, ArraySegment<byte> packet)
        {
            if (_networkManager == null)
                return;

            _network.SendToClient(packet, destination, true, _networkManager);
        }

        protected override void SendUnreliable(NfgoConn destination, ArraySegment<byte> packet)
        {
            if (_networkManager == null)
                return;

            _network.SendToClient(packet, destination, false, _networkManager);
        }
    }
}