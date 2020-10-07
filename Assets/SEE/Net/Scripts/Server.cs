using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using System;
using System.Collections.Generic;
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
        public const string PacketType = "Server";

        /// <summary>
        /// The list of all active connections.
        /// </summary>
        public static List<Connection> Connections { get; private set; }

        /// <summary>
        /// The list of all connection listeners of the server.
        /// </summary>
        public static List<ConnectionListenerBase> ConnectionListeners { get; private set; }

        /// <summary>
        /// The packet handler processes incoming packets.
        /// </summary>
        private static PacketHandler packetHandler;

        /// <summary>
        /// All new connections that have yet to be processed. New connections are
        /// announced via a differend thread and are buffered and processed later in the
        /// main thread. This is necessary, as most of the Unity features only work in
        /// the main thread.
        /// </summary>
        private static Stack<Connection> pendingEstablishedConnections;

        /// <summary>
        /// All closed connectiong that have yet to be processed.
        /// </summary>
        private static Stack<Connection> pendingClosedConnections;

        /// <summary>
        /// For each connection, the next to be processed packet id is saved to ensue the
        /// correct execution order of incoming packets. The first id for every
        /// connection is always zero and is increased after each incoming packet.
        /// </summary>
        public static Dictionary<Connection, ulong> incomingPacketSequenceIDs;

        /// <summary>
        /// The next id per connecting for outgoing packets.
        /// </summary>
        public static Dictionary<Connection, ulong> outgoingPacketSequenceIDs;

        /// <summary>
        /// Whether the server is currently initialized.
        /// </summary>
        private static bool initialized = false;


        /// <summary>
        /// Initializes the server for listening for connections and receiving packets.
        /// </summary>
        public static void Initialize()
        {
            if (!initialized)
            {
                Connections = new List<Connection>();
                ConnectionListeners = new List<ConnectionListenerBase>();
                packetHandler = new PacketHandler(true);
                pendingEstablishedConnections = new Stack<Connection>();
                pendingClosedConnections = new Stack<Connection>();
                incomingPacketSequenceIDs = new Dictionary<Connection, ulong>();
                outgoingPacketSequenceIDs = new Dictionary<Connection, ulong>();

                NetworkComms.AppendGlobalConnectionEstablishHandler((Connection c) => pendingEstablishedConnections.Push(c));
                NetworkComms.AppendGlobalConnectionCloseHandler((Connection c) => pendingClosedConnections.Push(c));

                void OnIncomingPacket(PacketHeader packetHeader, Connection connection, string data) => packetHandler.Push(packetHeader, connection, data);
                NetworkComms.AppendGlobalIncomingPacketHandler<string>(PacketType, OnIncomingPacket);

                try
                {
                    ConnectionListeners.AddRange(Connection.StartListening(ConnectionType.TCP, new IPEndPoint(IPAddress.Any, Network.LocalServerPort), false));
                    foreach (ConnectionListenerBase connectionListener in ConnectionListeners)
                    {
                        Debug.Log("Listening on: '" + connectionListener.LocalListenEndPoint.ToString() + "'.\n");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                initialized = true;
            }
        }

        /// <summary>
        /// Handles pending packets and established and closed connections.
        /// </summary>
        public static void Update()
        {
            if (initialized)
            {
                while (pendingEstablishedConnections.Count != 0)
                {
                    OnConnectionEstablished(pendingEstablishedConnections.Pop());
                }
                while (pendingClosedConnections.Count != 0)
                {
                    OnConnectionClosed(pendingClosedConnections.Pop());
                }
                packetHandler.HandlePendingPackets();
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
                    initialized = false;

                    outgoingPacketSequenceIDs = null;
                    incomingPacketSequenceIDs = null;
                    pendingClosedConnections = null;
                    pendingEstablishedConnections = null;
                    packetHandler = null;

                    Connection.StopListening(ConnectionListeners);
                    ConnectionListeners = null;

                    for (int i = 0; i < Connections.Count; i++)
                    {
                        Connections[i].CloseConnection(false);
                    }
                    Connections = null;
                }
            }
        }

        /// <summary>
        /// Handles established connection. Sends game state and buffered packets to new
        /// client.
        /// </summary>
        /// <param name="connection">The established connection.</param>
        private static void OnConnectionEstablished(Connection connection)
        {
            bool connectionListenerInitialized = false;
            foreach (ConnectionListenerBase connectionListener in ConnectionListeners)
            {
                if (connectionListener.LocalListenEndPoint.Equals(connection.ConnectionInfo.LocalEndPoint))
                {
                    connectionListenerInitialized = true;
                    break;
                }
            }

            if (connectionListenerInitialized)
            {
                if (!Connections.Contains(connection))
                {
                    Debug.LogFormat("Connection established: {0}\n", connection.ToString());

                    // synchronize current state with new client
                    if (!connection.ConnectionInfo.RemoteEndPoint.Equals(Client.LocalEndPoint))
                    {
                        IPEndPoint[] recipient = new IPEndPoint[1] { (IPEndPoint)connection.ConnectionInfo.RemoteEndPoint };
                        List<InstantiatePrefabAction> actions = PrefabAction.GetAllActions();
                        foreach (InstantiatePrefabAction action in actions)
                        {
                            action.Execute(recipient);
                        }
                        if (Network.LoadCityOnStart)
                        {
                            foreach (Game.AbstractSEECity city in UnityEngine.Object.FindObjectsOfType<Game.AbstractSEECity>())
                            {
                                new LoadCityAction(city).Execute(recipient);
                            }
                        }
                        foreach (Controls.NavigationAction navigationAction in UnityEngine.Object.FindObjectsOfType<Controls.NavigationAction>())
                        {
                            new SyncCitiesAction(navigationAction).Execute(recipient);
                        }
                        foreach (Controls.Outline outline in UnityEngine.Object.FindObjectsOfType<Controls.Outline>())
                        {
                            new SelectionAction(null, outline.GetComponent<Controls.HoverableObject>()).Execute(recipient);
                        }
                    }

                    // recognize client
                    Connections.Add(connection);
                    incomingPacketSequenceIDs.Add(connection, 0);
                    outgoingPacketSequenceIDs.Add(connection, 0);

                    // create new player head for every client
                    new InstantiatePrefabAction(
                        (IPEndPoint)connection.ConnectionInfo.RemoteEndPoint,
                        "PlayerHead",
                        Vector3.zero,
                        Quaternion.identity,
                        new Vector3(0.02f, 0.015f, 0.015f)
                    ).Execute();
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
            bool connectionListenerInitialized = false;
            foreach (ConnectionListenerBase connectionListener in ConnectionListeners)
            {
                if (connectionListener.LocalListenEndPoint.Equals(connection.ConnectionInfo.LocalEndPoint))
                {
                    connectionListenerInitialized = true;
                    break;
                }
            }

            if (connectionListenerInitialized)
            {
                lock (Connections)
                {
                    Connections.Remove(connection);
                    incomingPacketSequenceIDs.Remove(connection);
                    outgoingPacketSequenceIDs.Remove(connection);
                    ViewContainer[] viewContainers = ViewContainer.GetViewContainersByOwner((IPEndPoint)connection.ConnectionInfo.RemoteEndPoint);
                    foreach (ViewContainer viewContainer in viewContainers)
                    {
                        new DestroyPrefabAction(viewContainer).Execute();
                    }

                    Debug.Log("Connection closed: " + connection.ToString());
                }
            }
        }
    }

}
