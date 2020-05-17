using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Net.Internal
{

    internal struct BufferedPacket
    {
        public Connection connection;
        public string packetData;
    }

    public class ServerPacketHandler : PacketHandler
    {
        private List<BufferedPacket> bufferedPackets = new List<BufferedPacket>();



        public ServerPacketHandler(string packetTypePrefix) : base(packetTypePrefix) { }



        public void OnConnectionEstablished(Connection connection)
        {
            string[] packetDatas = new string[bufferedPackets.Count];
            for (int i = 0; i < bufferedPackets.Count; i++)
            {
                packetDatas[i] = bufferedPackets[i].packetData;
            }
            BufferedPacketsPacket packet = new BufferedPacketsPacket(packetDatas);
            Network.SendPacket(connection, packet);
        }

        public void OnConnectionClosed(Connection connection)
        {
            // TODO: remove instantiated objects
            List<BufferedPacket> bps = new List<BufferedPacket>(bufferedPackets);
#if UNITY_EDITOR
            int removedCount = 0;
#endif
            for (int i = 0; i < bps.Count; i++)
            {
                if (bps[i].connection.Equals(connection))
                {
#if UNITY_EDITOR
                    removedCount++;
#endif
                    bufferedPackets.Remove(bps[i]);
                }
            }
#if UNITY_EDITOR
            Debug.Log("Removed '" + removedCount + "' buffered packet! Remaining buffered packet count: '" + (bufferedPackets.Count - 1) + "'");
#endif
        }



        internal override void HandlePacket(PacketHeader packetHeader, Connection connection, BufferedPacketsPacket packet)
        {
            Assertions.InvalidCodePath("The server only sends these types of packets but never receives them!");
        }

        internal override void HandlePacket(PacketHeader packetHeader, Connection connection, ExecuteCommandPacket packet)
        {
            if (packet != null && packet.command != null)
            {
                packet.command.ExecuteOnServer();

                if (packet.command.buffer)
                {
                    BufferedPacket bufferedPacket = new BufferedPacket()
                    {
                        connection = connection,
                        packetData = PacketSerializer.Serialize(packet)
                    };
                    bufferedPackets.Add(bufferedPacket);
                }

                foreach (Connection co in Server.Connections)
                {
                    Network.SendPacket(co, packet);
                }
            }
        }

        internal override void HandlePacket(PacketHeader packetHeader, Connection connection, RedoCommandPacket packet)
        {
            if (packet != null)
            {
                BufferedPacket bufferedPacket = new BufferedPacket()
                {
                    connection = connection,
                    packetData = PacketSerializer.Serialize(packet)
                };
                bufferedPackets.Add(bufferedPacket);

                foreach (Connection co in Server.Connections)
                {
                    Network.SendPacket(co, packet);
                }
            }
        }

        internal override void HandlePacket(PacketHeader packetHeader, Connection connection, UndoCommandPacket packet)
        {
            if (packet != null)
            {
                BufferedPacket bufferedPacket = new BufferedPacket()
                {
                    connection = connection,
                    packetData = PacketSerializer.Serialize(packet)
                };
                bufferedPackets.Add(bufferedPacket);

                foreach (Connection co in Server.Connections)
                {
                    Network.SendPacket(co, packet);
                }
            }
        }
    }

}
