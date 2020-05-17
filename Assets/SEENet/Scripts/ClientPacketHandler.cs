using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using SEE.Command;
using System;
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



        internal override void HandlePacket(PacketHeader packetHeader, Connection connection, BufferedPacketsPacket packet)
        {
            if (packet != null && packet.packetDatas != null)
            {
                foreach (string serializedPacket in packet.packetDatas)
                {
                    AbstractPacket p = PacketSerializer.Deserialize(serializedPacket);
                    HandlePacket(packetHeader, connection, p);
                }
            }
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
