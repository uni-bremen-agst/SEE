using System;
using Dissonance.Networking;
using JetBrains.Annotations;
using Unity.Netcode;

namespace Dissonance.Integrations.Unity_NFGO
{
    public class NfgoClient
        : BaseClient<NfgoServer, NfgoClient, NfgoConn>
    {
        private readonly NfgoCommsNetwork _network;
        private NetworkManager _networkManager;

        private byte[] _receiveBuffer = new byte[1024];

        public NfgoClient([NotNull] NfgoCommsNetwork network) : base(network)
        {
            _network = network;
        }

        public override void Connect()
        {
            // Register receiving packets on the client from the server
            _networkManager = NetworkManager.Singleton;
            _networkManager.CustomMessagingManager.RegisterNamedMessageHandler("DissonanceToClient", NamedMessageHandler);
            Connected();
        }

        public override void Disconnect()
        {
            if (_networkManager != null)
            {
                _networkManager.CustomMessagingManager?.UnregisterNamedMessageHandler("DissonanceToClient");
                _networkManager = null;
            }

            base.Disconnect();
        }

        private void NamedMessageHandler(ulong sender, FastBufferReader stream)
        {
            var received = NfgoCommsNetwork.ReadPacket(ref stream, ref _receiveBuffer);
            NetworkReceivedPacket(received);
        }

        protected override void ReadMessages()
        {
            // Messages are delivered in the callback set in `RegisterNamedMessageHandler`. Nothing to do here.
        }

        protected override void SendReliable(ArraySegment<byte> packet)
        {
            _network.SendToServer(packet, true, _networkManager);
        }

        protected override void SendUnreliable(ArraySegment<byte> packet)
        {
            _network.SendToServer(packet, false, _networkManager);
        }
    }
}