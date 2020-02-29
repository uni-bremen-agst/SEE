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
            public Packet packet;
        }

        private List<BufferedPacket> bufferedPackets = new List<BufferedPacket>();
        private int lastViewID = -1;

        public ServerPacketHandler(string packetTypePrefix) : base(packetTypePrefix)
        {
        }

        public void OnConnectionEstablished(Connection connection)
        {
            for (int i = 0; i < bufferedPackets.Count; i++)
            {
                Network.Send(connection, bufferedPackets[i].packet);
            }

            if (!Client.LocalEndPoint.Equals(connection.ConnectionInfo.RemoteEndPoint))
            {
                foreach (GameObject building in GameObject.FindGameObjectsWithTag(Tags.Building))
                {
                    Network.Send(connection, new CityBuildingPacket(building));
                }
                foreach (GameObject node in GameObject.FindGameObjectsWithTag(Tags.Node))
                {
                    Network.Send(connection, new CityNodePacket(node));
                }
                foreach (GameObject edge in GameObject.FindGameObjectsWithTag(Tags.Edge))
                {
                    Network.Send(connection, new CityEdgePacket(edge));
                }
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

        protected override bool HandleCityBuildingPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            throw new Exception("A server should never receive this type of packet!");
        }
        protected override bool HandleCityEdgePacket(PacketHeader packetHeader, Connection connection, string data)
        {
            throw new Exception("A server should never receive this type of packet!");
        }
        protected override bool HandleCityNodePacket(PacketHeader packetHeader, Connection connection, string data)
        {
            throw new Exception("A server should never receive this type of packet!");
        }
        protected override bool HandleInstantiatePacket(PacketHeader packetHeader, Connection connection, string data)
        {
            InstantiatePacket packet = InstantiatePacket.Deserialize(data);
            packet.viewID = ++lastViewID; // TODO: this could potentially overflow. server should be able to run forever without having to restart!
            BufferedPacket bufferedPacket = new BufferedPacket()
            {
                header = new PacketHeader(Client.PACKET_PREFIX + InstantiatePacket.PACKET_TYPE, packetHeader.TotalPayloadSize),
                connection = connection,
                packet = packet
            };
            bufferedPackets.Add(bufferedPacket);
            for (int i = 0; i < Server.Connections.Count; i++)
            {
                Network.Send(Server.Connections[i], packet);
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
