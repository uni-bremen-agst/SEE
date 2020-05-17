using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace SEE.Net.Internal
{

    public class ServerPacketHandler : PacketHandler
    {
        private List<string> bufferedSerializedPackets = new List<string>();



        public ServerPacketHandler(string packetTypePrefix) : base(packetTypePrefix)
        {
        }



        public void OnConnectionEstablished(Connection connection)
        {
            foreach (string bufferedSerializedPacket in bufferedSerializedPackets)
            {
                Network.SubmitPacket(connection, bufferedSerializedPacket);
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
                packet.command.ExecuteOnServer();

                if (packet.command.buffer)
                {
                    bufferedSerializedPackets.Add(PacketSerializer.Serialize(packet));
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
                bufferedSerializedPackets.Add(PacketSerializer.Serialize(packet));

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
                bufferedSerializedPackets.Add(PacketSerializer.Serialize(packet));

                foreach (Connection co in Server.Connections)
                {
                    Network.SubmitPacket(co, packet);
                }
            }
        }
    }

}
