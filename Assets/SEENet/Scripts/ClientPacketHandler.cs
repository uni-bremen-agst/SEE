using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using SEE.Command;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net.Internal
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

        internal override void HandlePacket(PacketHeader packetHeader, Connection connection, ExecuteCommandPacket packet)
        {
            if (packet != null && packet.command != null)
            {
                Assert.IsNotNull(packet.command.requesterIPAddress);
                Assert.IsTrue(packet.command.requesterPort != -1);

                KeyValuePair<GameObject[], GameObject[]> result = packet.command.ExecuteOnClient();
                IPEndPoint stateOwner = new IPEndPoint(IPAddress.Parse(packet.command.requesterIPAddress), packet.command.requesterPort);
                if (packet.command.buffer)
                {
                    CommandHistory.OnExecute(stateOwner, result.Key, result.Value);
                }
            }
        }

        internal override void HandlePacket(PacketHeader packetHeader, Connection connection, RedoCommandPacket packet)
        {
            if (packet != null)
            {
                CommandHistory.RedoOnClient();
            }
        }

        internal override void HandlePacket(PacketHeader packetHeader, Connection connection, UndoCommandPacket packet)
        {
            if (packet != null)
            {
                CommandHistory.UndoOnClient();
            }
        }
    }

}
