using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

namespace SEE.Net.Internal
{

    public static class Server
    {
        public static readonly string PACKET_PREFIX = "Server.";
        public static List<Connection> Connections { get; private set; } = new List<Connection>();
        private static List<ConnectionListenerBase> connectionListeners = null;
        private static ServerPacketHandler packetHandler = new ServerPacketHandler(PACKET_PREFIX);

        public static void Initialize()
        {
            NetworkComms.AppendGlobalConnectionEstablishHandler(OnConnectionEstablished);
            NetworkComms.AppendGlobalConnectionCloseHandler(OnConnectionClosed);

            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_PREFIX + InstantiatePacketData.PACKET_NAME, OnIncomingPacket);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_PREFIX + TransformViewPositionPacketData.PACKET_NAME, OnIncomingPacket);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_PREFIX + TransformViewRotationPacketData.PACKET_NAME, OnIncomingPacket);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_PREFIX + TransformViewScalePacketData.PACKET_NAME, OnIncomingPacket);
            
            try
            {
                connectionListeners = Connection.StartListening(ConnectionType.TCP, new IPEndPoint(IPAddress.Any, Network.ServerPort), false);
                foreach (EndPoint localListenEndPoint in from connectionListenerBase in connectionListeners select connectionListenerBase.LocalListenEndPoint)
                {
                    Debug.Log("Listening on: '" + localListenEndPoint.ToString() + "'.");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        public static void Update()
        {
            packetHandler.HandlePendingPackets();
        }
        public static void Shutdown()
        {
            lock (Connections)
            {
                Connection.StopListening(connectionListeners);
                connectionListeners = null;
                for (int i = 0; i < Connections.Count; i++)
                {
                    Connections[i].CloseConnection(false);
                }
                Connections.Clear();
            }
        }
        private static void OnConnectionEstablished(Connection connection)
        {
            if (((IPEndPoint)connection.ConnectionInfo.LocalEndPoint).Port == Network.ServerPort)
            {
                Debug.Log("Connection established: " + connection.ToString());
                Connections.Add(connection);
                packetHandler.OnConnectionEstablished(connection);
            }
        }
        private static void OnConnectionClosed(Connection connection)
        {
            if (((IPEndPoint)connection.ConnectionInfo.LocalEndPoint).Port == Network.ServerPort)
            {
                Debug.Log("Connection closed: " + connection.ToString());
                Connections.Remove(connection);
                packetHandler.OnConnectionClosed(connection);
            }
        }
        private static void OnIncomingPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            packetHandler.Push(packetHeader, connection, data);
        }
    }

}
