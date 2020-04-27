using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using SEE.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Net.Internal
{

    public class ServerPacketHandler : PacketHandler
    {
        private struct BufferedPacket
        {
            public PacketHeader header;
            public Connection connection;
            public string packetType;
            public string packetData;
        }



        private List<BufferedPacket> bufferedPackets = new List<BufferedPacket>();



        public ServerPacketHandler(string packetTypePrefix) : base(packetTypePrefix) { }



        public void OnConnectionEstablished(Connection connection)
        {
            for (int i = 0; i < bufferedPackets.Count; i++)
            {
                Network.Send(connection, bufferedPackets[i].packetType, bufferedPackets[i].packetData);
            }
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

        

        protected override bool HandleCommandPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            CommandPacket packet = CommandPacket.Deserialize(data);

            if (packet == null || packet.command == null)
            {
                return false;
            }

            packet.command.ExecuteOnServer();

            if (packet.command.buffer)
            {
                BufferedPacket bufferedPacket = new BufferedPacket()
                {
                    header = new PacketHeader(Client.PACKET_PREFIX + CommandPacket.PACKET_TYPE, packetHeader.TotalPayloadSize),
                    connection = connection,
                    packetType = packet.packetType,
                    packetData = packet.Serialize()
                };
                bufferedPackets.Add(bufferedPacket);
            }

            foreach (Connection co in Server.Connections)
            {
                Network.Send(co, packet);
            }
            return true;
        }

        protected override bool HandleTransformViewPositionPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            TransformViewPositionPacket packet = TransformViewPositionPacket.Deserialize(data);
            foreach (Connection co in from c in Server.Connections where !c.ConnectionInfo.RemoteEndPoint.Equals(packet.transformView.viewContainer.owner) select c)
            {
                Network.Send(co, packet);
            }
            return true;
        }

        protected override bool HandleTransformViewRotationPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            TransformViewRotationPacket packet = TransformViewRotationPacket.Deserialize(data);
            foreach (Connection co in from c in Server.Connections where !c.ConnectionInfo.RemoteEndPoint.Equals(packet.transformView.viewContainer.owner) select c)
            {
                Network.Send(co, packet);
            }
            return true;
        }

        protected override bool HandleTransformViewScalePacket(PacketHeader packetHeader, Connection connection, string data)
        {
            TransformViewScalePacket packet = TransformViewScalePacket.Deserialize(data);
            foreach (Connection co in from c in Server.Connections where !c.ConnectionInfo.RemoteEndPoint.Equals(packet.transformView.viewContainer.owner) select c)
            {
                Network.Send(co, packet);
            }
            return true;
        }
    }

}
