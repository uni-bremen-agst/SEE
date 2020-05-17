using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using System.Collections.Generic;

namespace SEE.Net.Internal
{

    public class ServerPacketHandler : PacketHandler
    {
        private List<AbstractPacket> bufferedPackets = new List<AbstractPacket>();



        public ServerPacketHandler(string packetTypePrefix) : base(packetTypePrefix)
        {
        }



        public void OnConnectionEstablished(Connection connection)
        {
            foreach (AbstractPacket bufferedPacket in bufferedPackets)
            {
                Network.SubmitPacket(connection, PacketSerializer.Serialize(bufferedPacket));
            }
        }

        public void OnConnectionClosed(Connection connection)
        {
        }



        internal override bool TryHandlePacketSequence(PacketHeader packetHeader, Connection connection, PacketSequencePacket packetSequence)
        {
            bool result = Server.incomingPacketSequenceIDs.TryGetValue(connection, out ulong id);

            if (result && packetSequence.id == id)
            {
                Server.incomingPacketSequenceIDs[connection] = ++id;
                foreach (string serializedPacket in packetSequence.serializedPackets)
                {
                    AbstractPacket packet = PacketSerializer.Deserialize(serializedPacket);
                    HandlePacket(packetHeader, connection, packet);
                }
                return true;
            }

            return false;
        }

        internal override void HandlePacket(PacketHeader packetHeader, Connection connection, ExecuteCommandPacket packet)
        {
            if (packet != null && packet.command != null)
            {
                packet.command.ExecuteOnServerBase();

                if (packet.command.buffer)
                {
                    bufferedPackets.Add(packet);
                }

                foreach (Connection co in Server.Connections)
                {
                    Network.SubmitPacket(co, packet);
                }
            }
        }

        internal override void HandlePacket(PacketHeader packetHeader, Connection connection, RedoCommandPacket packet)
        {
            if (packet != null)
            {
                bufferedPackets.Add(packet);
                packet.command.RedoOnServerBase();

                foreach (Connection co in Server.Connections)
                {
                    Network.SubmitPacket(co, packet);
                }
            }
        }

        internal override void HandlePacket(PacketHeader packetHeader, Connection connection, UndoCommandPacket packet)
        {
            if (packet != null)
            {
                bufferedPackets.Add(packet);
                packet.command.UndoOnServerBase();

                foreach (Connection co in Server.Connections)
                {
                    Network.SubmitPacket(co, packet);
                }
            }
        }
    }

}
