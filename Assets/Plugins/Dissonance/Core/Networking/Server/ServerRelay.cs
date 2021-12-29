using System;
using System.Collections.Generic;
using Dissonance.Extensions;

namespace Dissonance.Networking.Server
{
    /// <summary>
    /// Relay packets from client to client (for cases where P2P connectivity is not available)
    /// </summary>
    internal class ServerRelay<TPeer>
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(ServerRelay<TPeer>).Name);

        private readonly IServer<TPeer> _server;
        private readonly BaseClientCollection<TPeer> _peers;

        private readonly List<TPeer> _tmpPeerBuffer = new List<TPeer>();
        private readonly List<ushort> _tmpIdBuffer = new List<ushort>();

        /// <summary>
        /// Invoked when a packet is being relayed.
        /// - First parameter is the payload being relayed.
        /// - Second parameter is the source
        /// </summary>
        public event Action<ArraySegment<byte>, TPeer> OnRelayingPacket;
        #endregion

        #region constructor
        public ServerRelay(IServer<TPeer> server, BaseClientCollection<TPeer> peers)
        {
            _server = server;
            _peers = peers;
        }
        #endregion

        public void ProcessPacketRelay(ref PacketReader reader, bool reliable, TPeer source)
        {
            //Read out the destination list and the slice of the packet which is the data to relay
            _tmpIdBuffer.Clear();
            ArraySegment<byte> data;
            reader.ReadRelay(_tmpIdBuffer, out data);

            //Parse header of body to check validity
            var bodyReader = new PacketReader(data);

            //Drop if the magic is wrong
            MessageTypes relayedPacketType;
            if (!bodyReader.ReadPacketHeader(out relayedPacketType))
            {
                Log.Error("Dropping relayed packet - magic number is incorrect");
                return;
            }

            //Drop if it's an explicitly p2p message
            if (relayedPacketType == MessageTypes.HandshakeP2P)
            {
                Log.Debug("Dropping relayed packet - cannot server relay HandshakeP2P messages");
                return;
            }

            //Convert IDs into connections
            _tmpPeerBuffer.Clear();
            for (var i = 0; i < _tmpIdBuffer.Count; i++)
            {
                ClientInfo<TPeer> clientInfo;
                if (!_peers.TryGetClientInfoById(_tmpIdBuffer[i], out clientInfo))
                    Log.Warn("Attempted to relay packet to unknown/disconnected peer ({0})", _tmpIdBuffer[i]);
                else
                    _tmpPeerBuffer.Add(clientInfo.Connection);
            }

            //Move the slice back to the zero position before sending
            // ReSharper disable once AssignNullToNotNullAttribute (Justification: Array segment cannot be null)
            data = data.CopyToSegment(data.Array);

            // Invoke event with relayed data payload
            var rp = OnRelayingPacket;
            if (rp != null)
                rp(data, source);

            //Send the packet on to the relayed recipients
            if (reliable)
                _server.SendReliable(_tmpPeerBuffer, data);
            else
                _server.SendUnreliable(_tmpPeerBuffer, data);

            //Clean up after ourselves
            _tmpIdBuffer.Clear();
            _tmpPeerBuffer.Clear();
        }
    }
}
