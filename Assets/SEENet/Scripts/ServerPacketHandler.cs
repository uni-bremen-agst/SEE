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
        public string packetType;
        public string packetData;
    }

    public class ServerPacketHandler : PacketHandler
    {
        private List<BufferedPacket> bufferedPackets = new List<BufferedPacket>();



        public ServerPacketHandler(string packetTypePrefix) : base(packetTypePrefix) { }



        public void OnConnectionEstablished(Connection connection)
        {
            string[] packetTypes = new string[bufferedPackets.Count];
            string[] packetDatas = new string[bufferedPackets.Count];
            for (int i = 0; i < bufferedPackets.Count; i++)
            {
                packetTypes[i] = bufferedPackets[i].packetType;
                packetDatas[i] = bufferedPackets[i].packetData;
            }
            BufferedPacketsPacket packet = new BufferedPacketsPacket(packetTypes, packetDatas);
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



        protected override bool HandleBufferedPacketsPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            Assertions.InvalidCodePath("The server only sends these types of packets but never receives them!");
            return false;
        }

        protected override bool HandleExecuteCommandPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            ExecuteCommandPacket packet = ExecuteCommandPacket.Deserialize(data);

            if (packet == null || packet.command == null)
            {
                return false;
            }

            packet.command.ExecuteOnServer();

            if (packet.command.buffer)
            {
                BufferedPacket bufferedPacket = new BufferedPacket()
                {
                    connection = connection,
                    packetType = packet.packetType,
                    packetData = packet.Serialize()
                };
                bufferedPackets.Add(bufferedPacket);
            }

            foreach (Connection co in Server.Connections)
            {
                Network.SendPacket(co, packet);
            }
            return true;
        }

        protected override bool HandleRedoCommandPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            RedoCommandPacket packet = RedoCommandPacket.Deserialize(data);

            if (packet == null)
            {
                return false;
            }

            BufferedPacket bufferedPacket = new BufferedPacket()
            {
                connection = connection,
                packetType = packet.packetType,
                packetData = packet.Serialize()
            };
            bufferedPackets.Add(bufferedPacket);

            foreach (Connection co in Server.Connections)
            {
                Network.SendPacket(co, packet);
            }
            return true;
        }
        
        protected override bool HandleUndoCommandPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            UndoCommandPacket packet = UndoCommandPacket.Deserialize(data);

            if (packet == null)
            {
                return false;
            }

            BufferedPacket bufferedPacket = new BufferedPacket()
            {
                connection = connection,
                packetType = packet.packetType,
                packetData = packet.Serialize()
            };
            bufferedPackets.Add(bufferedPacket);

            foreach (Connection co in Server.Connections)
            {
                Network.SendPacket(co, packet);
            }
            return true;
        }
    }

}
