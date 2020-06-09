using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using SEE.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{

    /// <summary>
    /// Handles the most general parts of networking.
    /// </summary>
    public class Network : MonoBehaviour
    {
        /// <summary>
        /// The default severity of the native logger of <see cref="NetworkCommsDotNet"/>.
        /// </summary>
        private const Logger.Severity DefaultSeverity = Logger.Severity.High;

        /// <summary>
        /// The instance of the network.
        /// </summary>
        private static Network instance;

        /// <summary>
        /// Whether the game is used in offline mode.
        /// </summary>
        [SerializeField] private bool useInOfflineMode = true;

        /// <summary>
        /// Whether this clients hosts the server. Is ignored in offline mode.
        /// </summary>
        [SerializeField] private bool hostServer = false;

        /// <summary>
        /// The IP-address of the server.
        /// </summary>
        [SerializeField] private string serverIPAddress = string.Empty;

        /// <summary>
        /// The port of the server. Is ignored, if this host does not host the server.
        /// </summary>
        [SerializeField] private int localServerPort = 55555;

        /// <summary>
        /// The port of the remote server. Is ignored, if this client hosts the server.
        /// </summary>
        [SerializeField] private int remoteServerPort = 0;

        /// <summary>
        /// Whether the city should be loaded on start up. Is ignored, if this client
        /// does not host the server.
        /// </summary>
        [SerializeField] private bool loadCityOnStart = false;

        /// <summary>
        /// The <see cref="GameObject"/> containing the <see cref="SEECity"/>-Script. Is
        /// ignored, if city can not be loaded on start.
        /// </summary>
        [SerializeField] private GameObject loadCityGameObject = null;

#if UNITY_EDITOR
        /// <summary>
        /// Whether native logging should be enabled.
        /// </summary>
        [SerializeField] private bool nativeLoggingEnabled = false;

        /// <summary>
        /// The minimal logged severity.
        /// </summary>
        [SerializeField] private Logger.Severity minimalSeverity = DefaultSeverity;
#endif

        /// <summary>
        /// Submitted packets, that will be sent in the next <see cref="LateUpdate"/>.
        /// </summary>
        private Dictionary<Connection, List<string>> submittedSerializedPackets = new Dictionary<Connection, List<string>>();



        /// <summary>
        /// <see cref="useInOfflineMode"/>
        /// </summary>
        public static bool UseInOfflineMode { get => instance ? instance.useInOfflineMode : true; }

        /// <summary>
        /// <see cref="hostServer"/>
        /// </summary>
        public static bool HostServer { get => instance ? instance.hostServer : false; }

        /// <summary>
        /// <see cref="serverIPAddress"/>
        /// </summary>
        public static string ServerIPAddress { get => instance ? instance.serverIPAddress : ""; }

        /// <summary>
        /// <see cref="localServerPort"/>
        /// </summary>
        public static int LocalServerPort { get => instance ? instance.localServerPort : -1; }

        /// <summary>
        /// <see cref="remoteServerPort"/>
        /// </summary>
        public static int RemoteServerPort { get => instance ? instance.remoteServerPort : -1; }

        /// <summary>
        /// Contains the main thread of the application.
        /// </summary>
        public static Thread MainThread { get; private set; } = Thread.CurrentThread;

        /// <summary>
        /// List of dead connections. Is packets can not be sent, this list is searched
        /// to reduce the frequency of warning messages.
        /// </summary>
        private static List<Connection> deadConnections = new List<Connection>();



        /// <summary>
        /// Initializes the server, client and game.
        /// </summary>
        private void Awake()
        {
            if (instance)
            {
                Debug.LogError("There must not be more than one Network-script! This script will be destroyed!");
                Destroy(this);
                return;
            }

            instance = this;

            if (!useInOfflineMode)
            {
#if UNITY_EDITOR
                if (nativeLoggingEnabled)
                {
                    NetworkComms.EnableLogging(new Logger(minimalSeverity));
                }
                else
                {
                    NetworkComms.DisableLogging();
                }
#else
                NetworkComms.DisableLogging();
#endif

                try
                {
                    if (hostServer)
                    {
                        Server.Initialize();
                    }
                    Client.Initialize();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogWarning("Some network-error happened! Continuing in offline mode...");
                    useInOfflineMode = true;
                }
            }

            InitializeGame();
        }

        /// <summary>
        /// Initializes the game.
        /// </summary>
        private void InitializeGame()
        {
            if ((useInOfflineMode || hostServer) && loadCityOnStart && loadCityGameObject != null)
            {
                AbstractSEECity seeCity = loadCityGameObject.GetComponent<AbstractSEECity>();
                if (seeCity)
                {
                    new LoadCityAction(seeCity).Execute();
                }
                else
                {
                    Debug.LogWarning("Attached GameObject does not contain an AbstractSEECity script! City will not be loaded!");
                }
            }

            // TODO: not sure if this should be the job of the networking script
            new InstantiateAction("Player").Execute();

            GameObject rig = GameObject.Find("Player Rig");
            if (rig)
            {
                // TODO(torben): this has to adapted once VR-hardware is available
#if false
                ControlMode mode = rig.GetComponent<ControlMode>();
#if UNITY_EDITOR
                if (mode.ViveController && mode.LeapMotion)
                {
                    Debug.LogError("Only one mode should be enabled!");
                }
#endif
                if (mode.ViveController)
                {
                    new InstantiateCommand("SEENetViveControllerLeft").Execute();
                    new InstantiateCommand("SEENetViveControllerRight").Execute();
                    new InstantiateCommand("SEENetViveControllerRay").Execute();
                }
                else if (mode.LeapMotion)
                {
                    throw new NotImplementedException("Multiplayer does not support Leap Motion!");
                }
#if UNITY_EDITOR
                else
                {
                    Debug.LogError("No mode selected!");
                }
#endif
#endif
            }
        }
        
        /// <summary>
        /// Sends all pending packets.
        /// </summary>
        private void LateUpdate()
        {
            if (hostServer && !useInOfflineMode)
            {
                Server.Update();
            }
            Client.Update();

            if (!useInOfflineMode)
            {
                if (submittedSerializedPackets.Count != 0)
                {
                    foreach (Connection connection in submittedSerializedPackets.Keys)
                    {
                        List<string> serializedObjects = submittedSerializedPackets[connection];

                        if (serializedObjects.Count != 0)
                        {
                            ulong id = ulong.MaxValue;
                            if (Server.Connections.Contains(connection))
                            {
                                id = Server.outgoingPacketSequenceIDs[connection]++;
                            }
                            else if (Client.Connection.Equals(connection))
                            {
                                id = Client.outgoingPacketID++;
                            }
                            Assert.IsTrue(id != ulong.MaxValue);

                            PacketSequencePacket packet = new PacketSequencePacket(id, serializedObjects.ToArray());
                            Send(connection, PacketSerializer.Serialize(packet));
                            serializedObjects.Clear();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Shuts down the server and the client.
        /// </summary>
        private void OnDestroy()
        {
            if (!useInOfflineMode)
            {
                if (hostServer)
                {
                    Server.Shutdown();
                }
                Client.Shutdown();
            }

            // TODO(torben): there must be a better way to stop the logging spam!
            string currentDirectory = Directory.GetCurrentDirectory();
            DirectoryInfo directoryInfo = new DirectoryInfo(currentDirectory);
            FileInfo[] fileInfos = directoryInfo.GetFiles();
            for (int i = 0; i < fileInfos.Length; i++)
            {
                FileInfo fileInfo = fileInfos[i];
                string fileName = fileInfo.Name;
                string[] prefixes = new string[] {
                    "CompleteIncomingItemTaskError",
                    "ConnectionKeepAlivePollError",
                    "Error",
                    "ManagedThreadPoolCallBackError",
                    "PacketHandlerErrorGlobal"
                };
                for (int j = 0; j < prefixes.Length; j++)
                {
                    if (fileName.Contains(prefixes[j]))
                    {
                        Debug.Log("Deleting file: '" + fileInfo.FullName + "'!");
                        fileInfo.Delete();
                        break;
                    }
                }
            }
        }



        /// <summary>
        /// Switches to offline mode.
        /// </summary>
        internal static void SwitchToOfflineMode()
        {
            if (instance)
            {
                foreach (ViewContainer viewContainer in FindObjectsOfType<ViewContainer>())
                {
                    if (!viewContainer.IsOwner())
                    {
                        Destroy(viewContainer.gameObject);
                    }
                }

                instance.useInOfflineMode = true;
                try
                {
                    if (instance.hostServer)
                    {
                        Server.Shutdown();
                    }
                    Client.Shutdown();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// Submits a packet for dispatch.
        /// </summary>
        /// <param name="connection">The connecting, the packet should be sent through.
        /// </param>
        /// <param name="packet">The packet to be sent.</param>
        internal static void SubmitPacket(Connection connection, AbstractPacket packet)
        {
            Assert.IsNotNull(connection);
            Assert.IsNotNull(packet);

            SubmitPacket(connection, PacketSerializer.Serialize(packet));
        }

        /// <summary>
        /// Submits a packet for dispatch.
        /// </summary>
        /// <param name="connection">The connecting, the packet should be sent through.
        /// </param>
        /// <param name="packet">The serialized packet to be sent.</param>
        internal static void SubmitPacket(Connection connection, string serializedPacket)
        {
            bool result = instance.submittedSerializedPackets.TryGetValue(connection, out List<string> serializedPackets);
            if (!result)
            {
                serializedPackets = new List<string>();
                instance.submittedSerializedPackets.Add(connection, serializedPackets);
            }
            serializedPackets.Add(serializedPacket);
        }

        /// <summary>
        /// Sends a serialized packet via given connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="serializedPacket">The serialized packet to be sent.</param>
        private void Send(Connection connection, string serializedPacket)
        {
            string packetType = Client.Connection.Equals(connection) ? Server.PacketType : Client.PacketType;

            try
            {
                connection.SendObject(packetType, serializedPacket);
            }
            catch (Exception)
            {
                lock (deadConnections)
                {
                    if (!deadConnections.Contains(connection))
                    {
                        deadConnections.Add(connection);
                        Invoker.Invoke((Connection c) => { deadConnections.Remove(c); }, 1.0f, connection);
                        Debug.LogWarning(
                            "Packet could not be sent to '" +
                            connection.ConnectionInfo.RemoteEndPoint.ToString() +
                            "'! Destination may not be listening or connection timed out. Closing connection!"
                        );
                        SwitchToOfflineMode();
                    }
                }
            }
        }

        /// <summary>
        /// Checks, whether the given IP-address is local.
        /// </summary>
        /// <param name="ipAddress">The IP-address.</param>
        /// <returns><code>true</code> if given IP-address is local, <code>false</code> otherwise.</returns>
        public static bool IsLocalIPAddress(IPAddress ipAddress)
        {
            if (ipAddress == null)
            {
                return false;
            }

            IPAddress[] localIPAddresses = LookupLocalIPAddresses();
            return localIPAddresses.Contains(ipAddress);
        }

        /// <summary>
        /// Returns an array of all local IP-Addresses.
        /// </summary>
        /// <returns>An array of all local IP-Addresses.</returns>
        public static IPAddress[] LookupLocalIPAddresses()
        {
            string hostName = Dns.GetHostName(); ;
            IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
            return hostEntry.AddressList;
        }
    }

}
