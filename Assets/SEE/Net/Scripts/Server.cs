using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

namespace SEE.Net
{

    /// <summary>
    /// The server of the game. Various clients can connect to this server in case they
    /// know the IP-address.
    /// </summary>
    public static class Server
    {
        /// <summary>
        /// The identifier for packets designated to the server.
        /// </summary>
        public static readonly string PacketType = "Server";

        /// <summary>
        /// The list of all active connections.
        /// </summary>
        public static List<Connection> Connections { get; private set; } = new List<Connection>();

        /// <summary>
        /// The list of all connection listeners of the server.
        /// </summary>
        public static List<ConnectionListenerBase> ConnectionListeners { get; private set; } = new List<ConnectionListenerBase>();

        /// <summary>
        /// All buffered packets will be sent to every new connecting client.
        /// </summary>
        private static List<AbstractPacket> bufferedPackets = new List<AbstractPacket>();

        /// <summary>
        /// The packet handler processes incoming packets.
        /// </summary>
        private static PacketHandler packetHandler = new PacketHandler(true);

        /// <summary>
        /// All new connections that have yet to be processed. New connections are
        /// announced via a differend thread and are buffered and processed later in the
        /// main thread. This is necessary, as most of the Unity features only work in
        /// the main thread.
        /// </summary>
        private static Stack<Connection> pendingEstablishedConnections = new Stack<Connection>();

        /// <summary>
        /// All closed connectiong that have yet to be processed.
        /// </summary>
        private static Stack<Connection> pendingClosedConnections = new Stack<Connection>();

        /// <summary>
        /// The game state will be sent to newly connecting clients.
        /// </summary>
        public static GameState gameState = new GameState();

        /// <summary>
        /// For each connection, the next to be processed packet id is saved to ensue the
        /// correct execution order of incoming packets. The first id for every
        /// connection is always zero and is increased after each incoming packet.
        /// </summary>
        public static Dictionary<Connection, ulong> incomingPacketSequenceIDs = new Dictionary<Connection, ulong>();

        /// <summary>
        /// The next id per connecting for outgoing packets.
        /// </summary>
        public static Dictionary<Connection, ulong> outgoingPacketSequenceIDs = new Dictionary<Connection, ulong>();

        private static bool initialized = false;


        /// <summary>
        /// Initializes the server for listening for connections and receiving packets.
        /// </summary>
        public static void Initialize()
        {
            NetworkComms.AppendGlobalConnectionEstablishHandler((Connection c) => pendingEstablishedConnections.Push(c));
            NetworkComms.AppendGlobalConnectionCloseHandler((Connection c) => pendingClosedConnections.Push(c));

            void OnIncomingPacket(PacketHeader packetHeader, Connection connection, string data) => packetHandler.Push(packetHeader, connection, data);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(PacketType, OnIncomingPacket);

            try
            {
                ConnectionListeners.AddRange(Connection.StartListening(ConnectionType.TCP, new IPEndPoint(IPAddress.Any, Network.LocalServerPort), false));
                foreach (EndPoint localListenEndPoint in from connectionListenerBase in ConnectionListeners select connectionListenerBase.LocalListenEndPoint)
                {
                    Debug.Log("Listening on: '" + localListenEndPoint.ToString() + "'.\n");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            initialized = true;
        }

        /// <summary>
        /// Handles pending packets and established and closed connections.
        /// </summary>
        public static void Update()
        {
            if (initialized)
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
        }

        /// <summary>
        /// Stuts the server down. Stops listening for connections and closes all valid
        /// connections.
        /// </summary>
        public static void Shutdown()
        {
            if (initialized)
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
        }

        /// <summary>
        /// Bufferes the given packet, so it can be sent to newly connecting clients.
        /// </summary>
        /// <param name="packet">The packet to be buffered.</param>
        internal static void BufferPacket(AbstractPacket packet)
        {
            bufferedPackets.Add(packet);
        }

        /// <summary>
        /// Handles established connection. Sends game state and buffered packets to new
        /// client.
        /// </summary>
        /// <param name="connection">The established connection.</param>
        private static void OnConnectionEstablished(Connection connection)
        {
            if ((from connectionListener in ConnectionListeners select connectionListener.LocalListenEndPoint).Contains(connection.ConnectionInfo.LocalEndPoint))
            {
                if (!Connections.Contains(connection))
                {
                    Debug.LogFormat("Connection established: {0}\n", connection.ToString());
                    Connections.Add(connection);
                    incomingPacketSequenceIDs.Add(connection, 0);
                    outgoingPacketSequenceIDs.Add(connection, 0);
                    foreach (AbstractPacket bufferedPacket in bufferedPackets)
                    {
                        Network.SubmitPacket(connection, PacketSerializer.Serialize(bufferedPacket));
                    }
                    Network.SubmitPacket(connection, new GameStatePacket(gameState));
                }
                else
                {
                    connection.CloseConnection(true);
                }
            }
        }

        /// <summary>
        /// Handles closing of given connection. Destroys the player prefab of given
        /// connection.
        /// </summary>
        /// <param name="connection"></param>
        private static void OnConnectionClosed(Connection connection)
        {
            if ((from connectionListener in ConnectionListeners select connectionListener.LocalListenEndPoint).Contains(connection.ConnectionInfo.LocalEndPoint))
            {
                Debug.Log("Connection closed: " + connection.ToString());
                Connections.Remove(connection);
                incomingPacketSequenceIDs.Remove(connection);
                outgoingPacketSequenceIDs.Remove(connection);
                ViewContainer[] viewContainers = ViewContainer.GetViewContainersByOwner((IPEndPoint)connection.ConnectionInfo.RemoteEndPoint);
                foreach (ViewContainer viewContainer in viewContainers)
                {
                    new DestroyAction(viewContainer).Execute();
                }
            }
        }
    }

}
