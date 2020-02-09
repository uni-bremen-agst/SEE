using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using System.Collections.Generic;
using System.IO;
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
                    "Sending buffered packet!\n" +
                    "\nType: '" + bufferedPackets[i].header.PacketType + "'" +
                    "\nConnection: '" + connection.ToString() + "'" +
                    "\nPacket data: '" + bufferedPackets[i].data + "'"
                );
                Network.Send(connection, bufferedPackets[i].header.PacketType, bufferedPackets[i].data);
            }
            string gxl = File.ReadAllText("C://Users//Torben//dev//SEE//Data//GXL//linux-clones//fs.gxl");
            Network.Send(connection, Client.PACKET_PREFIX + GXLPacketData.PACKET_NAME, new GXLPacketData(gxl).Serialize());
        }
        public void OnConnectionClosed(Connection connection)
        {
            List<Packet> bps = new List<Packet>(bufferedPackets);
            for (int i = 0; i < bps.Count; i++)
            {
                if (bps[i].connection.Equals(connection))
                {
#if UNITY_EDITOR
                    string oneMessage = "Removing buffered packet! There is " + (bufferedPackets.Count - 1) + " buffered packet left!";
                    string twoOrMoreMessage = "Removing buffered packet! There are " + (bufferedPackets.Count - 1) + " buffered packets left!";
                    Debug.Log(bufferedPackets.Count == 2 ? oneMessage : twoOrMoreMessage);
#endif
                    bufferedPackets.Remove(bps[i]);
                }
            }
        }

        protected override bool HandleGXLPacketData(PacketHeader packetHeader, Connection connection, string data)
        {
            throw new System.Exception("A server should never receive this type of packet!");
        }
        protected override bool HandleInstantiatePacketData(PacketHeader packetHeader, Connection connection, string data)
        {
#if UNITY_EDITOR
            string oneMessage = "Buffering packet of type '" + packetHeader.PacketType + "'! There is " + (bufferedPackets.Count + 1) + " buffered packet now!";
            string twoOrMoreMessage = "Buffering packet of type '" + packetHeader.PacketType + "'! There are " + (bufferedPackets.Count + 1) + " buffered packets now!";
            Debug.Log(bufferedPackets.Count == 0 ? oneMessage : twoOrMoreMessage);
#endif
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
            for (int i = 0; i < Server.Connections.Count; i++)
            {
                Network.Send(Server.Connections[i], Client.PACKET_PREFIX + TransformViewPositionPacketData.PACKET_NAME, data);
            }
            return true;
        }
        protected override bool HandleTransformViewRotationPacketData(PacketHeader packetHeader, Connection connection, string data)
        {
            for (int i = 0; i < Server.Connections.Count; i++)
            {
                Network.Send(Server.Connections[i], Client.PACKET_PREFIX + TransformViewRotationPacketData.PACKET_NAME, data);
            }
            return true;
        }
        protected override bool HandleTransformViewScalePacketData(PacketHeader packetHeader, Connection connection, string data)
        {
            for (int i = 0; i < Server.Connections.Count; i++)
            {
                Network.Send(Server.Connections[i], Client.PACKET_PREFIX + TransformViewScalePacketData.PACKET_NAME, data);
            }
            return true;
        }
    }

}
