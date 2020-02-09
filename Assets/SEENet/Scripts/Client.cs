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
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_PREFIX + GXLPacketData.PACKET_NAME, OnIncomingPacket);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_PREFIX + InstantiatePacketData.PACKET_NAME, OnIncomingPacket);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_PREFIX + TransformViewPositionPacketData.PACKET_NAME, OnIncomingPacket);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_PREFIX + TransformViewRotationPacketData.PACKET_NAME, OnIncomingPacket);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_PREFIX + TransformViewScalePacketData.PACKET_NAME, OnIncomingPacket);

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
#if !UNITY_EDITOR // TODO: this?
                Application.Quit();
#endif
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
        private static void OnIncomingPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            PacketHandler.Push(packetHeader, connection, data);
        }
    }

}
