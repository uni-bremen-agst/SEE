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
        public static readonly string PACKET_TYPE = "Server.";
        public static List<Connection> Connections { get; private set; } = new List<Connection>();
        public static List<ConnectionListenerBase> ConnectionListeners { get; private set; } = new List<ConnectionListenerBase>();
        private static ServerPacketHandler packetHandler = new ServerPacketHandler(PACKET_TYPE);
        private static Stack<Connection> pendingEstablishedConnections = new Stack<Connection>();
        private static Stack<Connection> pendingClosedConnections = new Stack<Connection>();

        public static Dictionary<Connection, ulong> incomingPacketSequenceIDs = new Dictionary<Connection, ulong>();
        public static Dictionary<Connection, ulong> outgoingPacketSequenceIDs = new Dictionary<Connection, ulong>();



        public static void Initialize()
        {
            NetworkComms.AppendGlobalConnectionEstablishHandler((Connection c) => pendingEstablishedConnections.Push(c));
            NetworkComms.AppendGlobalConnectionCloseHandler((Connection c) => pendingClosedConnections.Push(c));

            void OnIncomingPacket(PacketHeader packetHeader, Connection connection, string data) => packetHandler.Push(packetHeader, connection, data);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PACKET_TYPE, OnIncomingPacket);
            
            try
            {
                ConnectionListeners.AddRange(Connection.StartListening(ConnectionType.TCP, new IPEndPoint(IPAddress.Any, Network.LocalServerPort), false));
                foreach (EndPoint localListenEndPoint in from connectionListenerBase in ConnectionListeners select connectionListenerBase.LocalListenEndPoint)
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
            while (pendingEstablishedConnections.Count != 0)
            {
                OnConnectionEstablished(pendingEstablishedConnections.Pop());
            }
            while (pendingClosedConnections.Count != 0)
            {
                OnConnectionClosed(pendingClosedConnections.Pop());
            }
        }

        public static void Shutdown()
        {
            lock (Connections)
            {
                Connection.StopListening(ConnectionListeners);
                ConnectionListeners.Clear();
                for (int i = 0; i < Connections.Count; i++)
                {
                    Connections[i].CloseConnection(false);
                }
                Connections.Clear();
            }
        }

        private static void OnConnectionEstablished(Connection connection)
        {
            if ((from connectionListener in ConnectionListeners select connectionListener.LocalListenEndPoint).Contains(connection.ConnectionInfo.LocalEndPoint))
            {
                if (!Connections.Contains(connection))
                {
                    Debug.Log("Connection established: " + connection.ToString());
                    Connections.Add(connection);
                    incomingPacketSequenceIDs.Add(connection, 0);
                    outgoingPacketSequenceIDs.Add(connection, 0);
                    packetHandler.OnConnectionEstablished(connection);
                }
                else
                {
                    connection.CloseConnection(true);
                }
            }
        }

        private static void OnConnectionClosed(Connection connection)
        {
            if ((from connectionListener in ConnectionListeners select connectionListener.LocalListenEndPoint).Contains(connection.ConnectionInfo.LocalEndPoint))
            {
                Debug.Log("Connection closed: " + connection.ToString());
                Connections.Remove(connection);
                packetHandler.OnConnectionClosed(connection);
                incomingPacketSequenceIDs.Remove(connection);
                outgoingPacketSequenceIDs.Remove(connection);
            }
        }
    }

}
