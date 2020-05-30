using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using UnityEngine.Assertions;

namespace SEE.Net
{

    public class ClientPacketHandler : PacketHandler
    {
        public ClientPacketHandler(string packetTypePrefix) : base(packetTypePrefix)
        {
        }



        internal override bool TryHandlePacketSequence(PacketHeader packetHeader, Connection connection, PacketSequencePacket packetSequence)
        {
            Assert.IsNotNull(connection);
            Assert.IsNotNull(packetSequence);
            Assert.IsTrue(Client.Connection == connection);

            if (packetSequence.id == Client.incomingPacketID)
            {
                Client.incomingPacketID++;
                foreach (string serializedPacket in packetSequence.serializedPackets)
                {
                    AbstractPacket packet = PacketSerializer.Deserialize(serializedPacket);
                    HandlePacket(packetHeader, connection, packet);
                }
                return true;
            }
            return false;
        }

        internal override void HandlePacket(PacketHeader packetHeader, Connection connection, ExecuteActionPacket packet)
        {
            if (packet != null && packet.action != null)
            {
                Assert.IsNotNull(packet.action.requesterIPAddress);
                Assert.IsTrue(packet.action.requesterPort != -1);

                packet.action.ExecuteOnClientBase();
            }
        }

        internal override void HandlePacket(PacketHeader packetHeader, Connection connection, RedoActionPacket packet)
        {
            if (packet != null)
            {
                packet.action.RedoOnClientBase();
            }
        }

        internal override void HandlePacket(PacketHeader packetHeader, Connection connection, UndoActionPacket packet)
        {
            if (packet != null)
            {
                packet.action.UndoOnClientBase();
            }
        }
    }

}
