using System.Collections.Generic;
using System.Linq;
using System.Net;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using NetworkCommsDotNet.Connections.TCP;
using SEE.Net.Util;

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
        public const string PacketType = "Client";

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
        public static IPEndPoint LocalEndPoint => (IPEndPoint) Connection?.ConnectionInfo.LocalEndPoint;

        /// <summary>
        /// The remote end point of the server, this client is connected to.
        /// </summary>
        public static IPEndPoint RemoteEndPoint => (IPEndPoint) Connection?.ConnectionInfo.RemoteEndPoint;

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
        /// Whether the client is currently initialized.
        /// </summary>
        private static bool initialized = false;

        /// <summary>
        /// Connects to the server or switches to offline mode if not possible.
        /// </summary>
        public static void Initialize()
        {
            if (!initialized)
            {
                NetworkComms.AppendGlobalConnectionCloseHandler((Connection c) => { if (c.Equals(Connection)) { Network.SwitchToOfflineMode(); } });

                void OnIncomingPacket(PacketHeader packetHeader, Connection connection, string data) => PacketHandler.Push(packetHeader, connection, data);
                NetworkComms.AppendGlobalIncomingPacketHandler<string>(PacketType, OnIncomingPacket);

                List<IPEndPoint> endPoints = Network.HostServer
                    ? (from connectionListener in Server.ConnectionListeners select connectionListener.LocalListenEndPoint as IPEndPoint).ToList()
                    : new List<IPEndPoint> { new IPEndPoint(IPAddress.Parse(Network.RemoteServerIPAddress), Network.RemoteServerPort) };

                bool success = false;
                foreach (ConnectionInfo connectionInfo in from endPoint in endPoints select new ConnectionInfo(endPoint))
                {
                    try
                    {
                        Connection = TCPConnection.GetConnection(connectionInfo);
                        success = true;
                        Logger.Log($"Connection with server established: {Connection}");
                        break;
                    }
                    catch (ConnectionSetupException)
                    {
                        Logger.Log($"No server connection could be established using : {connectionInfo}");
                    }
                }
                if (!success)
                {
                    Logger.Log($"No server connection could be established using. You may want to check your firewall configuration.");
                    Network.SwitchToOfflineMode();
                    throw new ConnectionSetupException();
                }

                initialized = true;
            }
        }

        /// <summary>
        /// Handles pending packets.
        /// </summary>
        public static void Update()
        {
            if (initialized)
            {
                PacketHandler.HandlePendingPackets();
            }
        }

        /// <summary>
        /// Closes connection to server.
        /// </summary>
        public static void Shutdown()
        {
            if (initialized)
            {
                Logger.Log($"Client connected via {Connection} is shut down.");
                initialized = false;

                Connection?.CloseConnection(false);
                Connection = null;
            }
        }
    }
}
