using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using SEE.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

namespace SEE.Net.Internal
{

    public class ServerPacketHandler : PacketHandler
    {
        private List<Packet> bufferedPackets = new List<Packet>();
        private int lastViewID = -1;

        public ServerPacketHandler(string packetTypePrefix) : base(packetTypePrefix)
        {
        }

        public void OnConnectionEstablished(Connection connection)
        {
            for (int i = 0; i < bufferedPackets.Count; i++)
            {
                Debug.Log(
                    "Sending buffered packet!" +
                    "\nType: '" + bufferedPackets[i].header.PacketType + "'" +
                    "\nConnection: '" + connection.ToString() + "'" +
                    "\nPacket data: '" + bufferedPackets[i].data + "'"
                );
                Network.Send(connection, bufferedPackets[i].header.PacketType, bufferedPackets[i].data);
            }

            if (!Client.LocalEndPoint.Equals(connection.ConnectionInfo.RemoteEndPoint))
            {
                GameObject[] buildings = GameObject.FindGameObjectsWithTag(Tags.Building);
                BuildingsPacketData buildingsPacketData = new BuildingsPacketData(buildings);
                Network.Send(connection, Client.PACKET_PREFIX + BuildingsPacketData.PACKET_NAME, buildingsPacketData.Serialize());
            }
        }
        public void OnConnectionClosed(Connection connection)
        {
            // TODO: remove instantiated objects
            List<Packet> bps = new List<Packet>(bufferedPackets);
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

        protected override bool HandleBuildingsPacketData(PacketHeader packetHeader, Connection connection, string data)
        {
            throw new Exception("A server should never receive this type of packet!");
        }
        protected override bool HandleGXLPacketData(PacketHeader packetHeader, Connection connection, string data)
        {
            throw new Exception("A server should never receive this type of packet!");
        }
        protected override bool HandleInstantiatePacketData(PacketHeader packetHeader, Connection connection, string data)
        {
            Debug.Log(
                "Buffering packet!" + 
                "\nType: '" + packetHeader.PacketType + "'" +
                "\nConnection: '" + connection.ToString() + "'" +
                "\nPacket data: '" + data + "'" + 
                "\nTotal buffered packet count: '" + (bufferedPackets.Count + 1) + "'"
            );
            InstantiatePacketData packetData = InstantiatePacketData.Deserialize(data);
            packetData.viewID = ++lastViewID; // TODO: this could potentially overflow. server should be able to run forever without having to restart!
            Packet packet = new Packet()
            {
                header = new PacketHeader(Client.PACKET_PREFIX + InstantiatePacketData.PACKET_NAME, packetHeader.TotalPayloadSize),
                connection = connection,
                data = packetData.Serialize()
            };
            bufferedPackets.Add(packet);
            for (int i = 0; i < Server.Connections.Count; i++)
            {
                Network.Send(Server.Connections[i], Client.PACKET_PREFIX + InstantiatePacketData.PACKET_NAME, packet.data);
            }
            return true;
        }
        protected override bool HandleTransformViewPositionPacketData(PacketHeader packetHeader, Connection connection, string data)
        {
            TransformViewPositionPacketData packetData = TransformViewPositionPacketData.Deserialize(data);
            foreach (Connection co in from c in Server.Connections where !c.ConnectionInfo.RemoteEndPoint.Equals(packetData.transformView.viewContainer.owner) select c)
            {
                Network.Send(co, Client.PACKET_PREFIX + TransformViewPositionPacketData.PACKET_NAME, data);
            }
            return true;
        }
        protected override bool HandleTransformViewRotationPacketData(PacketHeader packetHeader, Connection connection, string data)
        {
            TransformViewRotationPacketData packetData = TransformViewRotationPacketData.Deserialize(data);
            foreach (Connection co in from c in Server.Connections where !c.ConnectionInfo.RemoteEndPoint.Equals(packetData.transformView.viewContainer.owner) select c)
            {
                Network.Send(co, Client.PACKET_PREFIX + TransformViewRotationPacketData.PACKET_NAME, data);
            }
            return true;
        }
        protected override bool HandleTransformViewScalePacketData(PacketHeader packetHeader, Connection connection, string data)
        {
            TransformViewScalePacketData packetData = TransformViewScalePacketData.Deserialize(data);
            foreach (Connection co in from c in Server.Connections where !c.ConnectionInfo.RemoteEndPoint.Equals(packetData.transformView.viewContainer.owner) select c)
            {
                Network.Send(co, Client.PACKET_PREFIX + TransformViewScalePacketData.PACKET_NAME, data);
            }
            return true;
        }
    }

}
