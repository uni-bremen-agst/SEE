using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using NetworkCommsDotNet.Connections.TCP;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace SEE.Net.Internal
{

    public static class Client
    {
        public static readonly string PACKET_PREFIX = "Client.";
        public static Connection Connection { get; private set; } = null;
        public static ClientPacketHandler PacketHandler { get; private set; } = new ClientPacketHandler(PACKET_PREFIX);

        public static void Initialize(string serverIPAddress, int serverPort)
        {
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_PREFIX + GXLPacketData.PACKET_NAME, OnIncomingPacket);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_PREFIX + InstantiatePacketData.PACKET_NAME, OnIncomingPacket);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_PREFIX + TransformViewPositionPacketData.PACKET_NAME, OnIncomingPacket);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_PREFIX + TransformViewRotationPacketData.PACKET_NAME, OnIncomingPacket);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_PREFIX + TransformViewScalePacketData.PACKET_NAME, OnIncomingPacket);

            List<IPAddress> ipAddresses = Network.LookupLocalIPAddresses();
            try { ipAddresses.Insert(0, IPAddress.Parse(serverIPAddress)); }
            catch (Exception) { }

            bool success = false;
            foreach (IPAddress ipAddress in ipAddresses)
            {
                try
                {
                    IPEndPoint endPoint = new IPEndPoint(ipAddress, Network.ServerPort);
                    ConnectionInfo info = new ConnectionInfo(endPoint);
                    Connection = TCPConnection.GetConnection(info);
                    int clientPort = ((IPEndPoint)Connection.ConnectionInfo.LocalEndPoint).Port;
                    IPEndPoint listenerEndPoint = new IPEndPoint(IPAddress.Any, clientPort);
                    Connection.StartListening(ConnectionType.TCP, listenerEndPoint, false);
                    success = true;
                    break;
                }
                catch (Exception) { }
            }
            if (!success)
            {
                throw new ConnectionSetupException();
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
            return Connection != null ? (IPEndPoint)Connection.ConnectionInfo.LocalEndPoint : null;
        }
        private static void OnIncomingPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            PacketHandler.Push(packetHeader, connection, data);
        }
    }

}
