using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using SEE.Game;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        /// The remote IP-address of the server. Is empty, if this client hosts the
        /// server.
        /// </summary>
        [SerializeField] private string remoteServerIPAddress = string.Empty;

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
        /// <see cref="remoteServerIPAddress"/>
        /// </summary>
        public static string RemoteServerIPAddress { get => instance ? instance.remoteServerIPAddress : string.Empty; }
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
                    VivoxInitialize();
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
                    new InstantiateAction("SEENetViveControllerLeft").Execute();
                    new InstantiateAction("SEENetViveControllerRight").Execute();
                    new InstantiateAction("SEENetViveControllerRay").Execute();
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

        #region Vivox

        public const string VivoxIssuer = "torben9605-se19-dev";
        public const string VivoxDomain = "vdx5.vivox.com";
        public const string VivoxSecretKey = "kick271";
        public static readonly TimeSpan VivoxExpirationDuration = new TimeSpan(365, 0, 0, 0);

        public static VivoxUnity.Client VivoxClient { get; private set; } = null;
        public static VivoxUnity.AccountId VivoxAccountID { get; private set; } = null;
        public static VivoxUnity.ILoginSession VivoxLoginSession { get; private set; } = null;
        public static VivoxUnity.IChannelSession VivoxChannelSession { get; private set; } = null;

        [SerializeField] private string vivoxChannelName = string.Empty;
        public static string VivoxChannelName { get => instance ? instance.vivoxChannelName : string.Empty; }

        private static void VivoxInitialize()
        {
            VivoxUnity.VivoxConfig config = new VivoxUnity.VivoxConfig { InitialLogLevel = vx_log_level.log_debug };
            VivoxClient = new VivoxUnity.Client();
            VivoxClient.Initialize(config);

            string userName = "u-" + Client.LocalEndPoint.Address.ToString().Replace(':', '.') + '-' + Client.LocalEndPoint.Port;
            VivoxAccountID = new VivoxUnity.AccountId(VivoxIssuer, userName, VivoxDomain);
            VivoxLoginSession = VivoxClient.GetLoginSession(VivoxAccountID);
            VivoxLoginSession.PropertyChanged += VivoxOnLoginSessionPropertyChanged;
            VivoxLoginSession.BeginLogin(new Uri("https://vdx5.www.vivox.com/api2"), VivoxLoginSession.GetLoginToken(VivoxSecretKey, VivoxExpirationDuration), ar0 =>
            {
                VivoxLoginSession.EndLogin(ar0);

                string channelName = channelName = "c-" + VivoxChannelName;
                VivoxUnity.ChannelId channelID = new VivoxUnity.ChannelId(VivoxIssuer, channelName, VivoxDomain, VivoxUnity.ChannelType.NonPositional);

                // NOTE(torben): GetChannelSession() creates a new channel, if it does
                // not exist yet. Thus, a client, that is not the server could
                // potentially create the voice channel. To make sure this does not
                // happen, VivoxInitialize() must always be called AFTER the server
                // and client were initialized, because if this client tries to connect
                // to a server and can not connect, it will go to offline mode and not
                // initialize Vivox.
                VivoxChannelSession = VivoxLoginSession.GetChannelSession(channelID);
                VivoxChannelSession.PropertyChanged += VivoxOnChannelPropertyChanged;
                VivoxChannelSession.MessageLog.AfterItemAdded += VivoxOnChannelMessageReceived;
                VivoxChannelSession.BeginConnect(true, true, true, VivoxChannelSession.GetConnectToken(VivoxSecretKey, VivoxExpirationDuration), ar1 =>
                {
                    VivoxChannelSession.EndConnect(ar1);
                    if (HostServer && VivoxChannelSession.Participants.Count != 0)
                    {
                        // TODO: this channel already exists and the name is unavailable!
                        Debug.Log("Channel with given name already exists. Select differend name!\n");
                        VivoxChannelSession.Disconnect();
                        VivoxLoginSession.DeleteChannelSession(channelID);
                    }
                });
            });
        }

        private static void SendGroupMessage()
        {
            string channelName = VivoxChannelSession.Channel.Name;
            string senderName = VivoxAccountID.Name;
            string message = "Hello World!";

            VivoxChannelSession.BeginSendText(message, ar =>
            {
                try
                {
                    VivoxChannelSession.EndSendText(ar);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            });
        }

        private static void VivoxOnLoginSessionPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == "State")
            {
                switch ((sender as VivoxUnity.ILoginSession).State)
                {
                    case VivoxUnity.LoginState.LoggingIn:
                        break;

                    case VivoxUnity.LoginState.LoggedIn:
                        break;

                    case VivoxUnity.LoginState.LoggedOut:
                        break;
                    default:
                        break;
                }
            }
        }

        private static void VivoxOnChannelPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            VivoxUnity.IChannelSession channelSession = (VivoxUnity.IChannelSession)sender;

            if (propertyChangedEventArgs.PropertyName == "AudioState")
            {
                switch (channelSession.AudioState)
                {
                    case VivoxUnity.ConnectionState.Connected:    Debug.LogFormat("Audio connected in {0}\n", channelSession.Key.Name); break;
                    case VivoxUnity.ConnectionState.Disconnected: Debug.LogFormat("Audio disconnected in {0}\n", channelSession.Key.Name); break;
                }
            }
            else if (propertyChangedEventArgs.PropertyName == "TextState")
            {
                switch (channelSession.TextState)
                {
                    case VivoxUnity.ConnectionState.Connected:
                        Debug.LogFormat("Text connected in {0}\n", channelSession.Key.Name);
                        SendGroupMessage();
                        break;
                    case VivoxUnity.ConnectionState.Disconnected:
                        Debug.LogFormat("Text disconnected in {0}\n", channelSession.Key.Name);
                        break;
                }
            }
        }

        private static void VivoxOnChannelMessageReceived(object sender, VivoxUnity.QueueItemAddedEventArgs<VivoxUnity.IChannelTextMessage> queueItemAddedEventArgs)
        {
            string channelName = queueItemAddedEventArgs.Value.ChannelSession.Channel.Name;
            string senderName = queueItemAddedEventArgs.Value.Sender.Name;
            string message = queueItemAddedEventArgs.Value.Message;

            Debug.Log(channelName + ": " + senderName + ": " + message + "\n");
        }

        private void OnApplicationQuit()
        {
            if (VivoxClient != null)
            {
                VivoxClient.Uninitialize();
            }
        }

        #endregion
    }

}
