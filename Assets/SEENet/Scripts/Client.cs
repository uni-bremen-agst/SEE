using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using NetworkCommsDotNet.Connections.TCP;
using System;
using System.Net;
using UnityEngine;

namespace SEE.Net.Internal
{

    public static class Client
    {
        public static readonly string PACKET_PREFIX = "Client.";
        public static Connection Connection { get; private set; } = null;
        public static ClientPacketHandler PacketHandler { get; private set; } = new ClientPacketHandler(PACKET_PREFIX);

        public static void Initialize()
        {
            // TODO: look up, if there's a general global packet handler, as handling the same for all packet types. server, too!
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_PREFIX + InstantiatePacketData.PACKET_NAME, OnIncomingInstantiatePacket);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_PREFIX + TransformViewPositionPacketData.PACKET_NAME, OnIncomingTransformViewPositionPacket);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_PREFIX + TransformViewRotationPacketData.PACKET_NAME, OnIncomingTransformViewRotationPacket);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_PREFIX + TransformViewScalePacketData.PACKET_NAME, OnIncomingTransformViewScalePacket);

            try
            {
                IPEndPoint endPoint = new IPEndPoint(Network.LookupLocalIPAddress(), Network.ServerPort);
                ConnectionInfo info = new ConnectionInfo(endPoint);
                Connection = TCPConnection.GetConnection(info);
                int clientPort = ((IPEndPoint)Connection.ConnectionInfo.LocalEndPoint).Port;
                IPEndPoint listenerEndPoint = new IPEndPoint(IPAddress.Any, clientPort);
                Connection.StartListening(ConnectionType.TCP, listenerEndPoint, false);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        public static void Update()
        {
            PacketHandler.HandlePendingPackets();
        }
        public static void Shutdown() // TODO: send message to server
        {
            Connection?.CloseConnection(false);
            Connection = null;
        }
        public static IPEndPoint GetLocalEndPoint()
        {
            if (Connection == null)
            {
                return new IPEndPoint(Network.LookupLocalIPAddress(), 0);
            }
            else
            {
                return (IPEndPoint)Connection.ConnectionInfo.LocalEndPoint;
            }
        }
        private static void OnIncomingInstantiatePacket(PacketHeader packetHeader, Connection connection, string data)
        {
            PacketHandler.Push(packetHeader, connection, data);
        }
        private static void OnIncomingTransformViewPositionPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            PacketHandler.Push(packetHeader, connection, data);
        }
        private static void OnIncomingTransformViewRotationPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            PacketHandler.Push(packetHeader, connection, data);
        }
        private static void OnIncomingTransformViewScalePacket(PacketHeader packetHeader, Connection connection, string data)
        {
            PacketHandler.Push(packetHeader, connection, data);
        }
    }

}
