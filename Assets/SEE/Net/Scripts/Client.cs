﻿using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using NetworkCommsDotNet.Connections.TCP;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace SEE.Net
{

    /// <summary>
    /// The client of the game. Can connect to exactly one server.
    /// </summary>
    public static class Client
    {
        /// <summary>
        /// The identifier for packets designated to the client.
        /// </summary>
        public static readonly string PacketType = "Client";

        /// <summary>
        /// The connecting to the server.
        /// </summary>
        public static Connection Connection { get; private set; } = null;

        /// <summary>
        /// The packet handler of the client.
        /// </summary>
        public static PacketHandler PacketHandler { get; private set; } = new PacketHandler(false);

        /// <summary>
        /// The local end point of the client.
        /// </summary>
        public static IPEndPoint LocalEndPoint { get => Connection != null ? (IPEndPoint)Connection.ConnectionInfo.LocalEndPoint : null; }

        /// <summary>
        /// The remote end point of the server, this client is connected to.
        /// </summary>
        public static IPEndPoint RemoteEndPoint { get => Connection != null ? (IPEndPoint)Connection.ConnectionInfo.RemoteEndPoint : null; }

        /// <summary>
        /// The ID of the next incoming packet of the server. Is used to ensure the
        /// correct order of execution/handling of packets.
        /// </summary>
        public static ulong incomingPacketID = 0;

        /// <summary>
        /// The ID of the next outgoing packet to the server.
        /// </summary>
        public static ulong outgoingPacketID = 0;



        /// <summary>
        /// Connects to the server or switches to offline mode if not possible.
        /// </summary>
        public static void Initialize()
        {
            NetworkComms.AppendGlobalConnectionCloseHandler((Connection c) => Network.SwitchToOfflineMode());

            void OnIncomingPacket(PacketHeader packetHeader, Connection connection, string data) => PacketHandler.Push(packetHeader, connection, data);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PacketType, OnIncomingPacket);

            List<IPEndPoint> endPoints = Network.HostServer
                ? (from connectionListener in Server.ConnectionListeners select connectionListener.LocalListenEndPoint as IPEndPoint).ToList()
                : new List<IPEndPoint>() { new IPEndPoint(IPAddress.Parse(Network.RemoteServerIPAddress), Network.RemoteServerPort) };

            bool success = false;
            foreach (ConnectionInfo connectionInfo in from endPoint in endPoints select new ConnectionInfo(endPoint))
            {
                try
                {
                    Connection = TCPConnection.GetConnection(connectionInfo);
                    success = true;
                    break;
                }
                catch (ConnectionSetupException) { }
            }
            if (!success)
            {
                Network.SwitchToOfflineMode();
                throw new ConnectionSetupException();
            }
        }

        /// <summary>
        /// Handles pending packets.
        /// </summary>
        public static void Update()
        {
            PacketHandler.HandlePendingPackets();
        }

        /// <summary>
        /// Closes connection to server.
        /// </summary>
        public static void Shutdown()
        {
            Connection?.CloseConnection(false);
            Connection = null;
        }
    }

}
