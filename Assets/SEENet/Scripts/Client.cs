using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using NetworkCommsDotNet.Connections.TCP;
using System;
using System.Collections.Generic;
using System.Net;

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

            IPAddress[] ipAddresses = null;
            if (serverIPAddress != null && serverIPAddress.Trim(new char[] { ' ' }).Length != 0)
            {
                try
                {
                    ipAddresses = new IPAddress[]
                    {
                        IPAddress.Parse(serverIPAddress)
                    };
                }
                catch (Exception e)
                {
#if UNITY_EDITOR
                    UnityEngine.Debug.LogException(e);
                    UnityEngine.Debug.LogWarning("Until proper handling, the game will simply stop upon entering an invalid address and port");
                    UnityEditor.EditorApplication.isPlaying = false; // TODO: proper handling!
                    throw e;
#endif
                }
            }
            else
            {
                ipAddresses = Network.LookupLocalIPAddresses();
            }

            bool success = false;
            ConnectionInfo connectionInfo = null;
            foreach (IPAddress ipAddress in ipAddresses)
            {
                try
                {
                    connectionInfo = new ConnectionInfo(new IPEndPoint(ipAddress, serverPort));
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
