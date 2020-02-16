using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using NetworkCommsDotNet.Connections.TCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

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

            List<IPEndPoint> endPoints = null;
            if (Network.HostServer)
            {
                endPoints = (from connectionListener in Server.ConnectionListeners select connectionListener.LocalListenEndPoint as IPEndPoint).ToList();
            }
            else
            {
                endPoints = new List<IPEndPoint>() { new IPEndPoint(IPAddress.Parse(Network.ServerIPAddress), Network.ServerPort) };
            }
            bool success = false;
            foreach (ConnectionListenerBase clb in Server.ConnectionListeners)
            {
                try
                {
                    ConnectionInfo connectionInfo = new ConnectionInfo(clb.LocalListenEndPoint);
                    Connection = TCPConnection.GetConnection(connectionInfo);
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
