using Cysharp.Threading.Tasks;
using Dissonance;
using SEE.Game.City;
using SEE.GO;
using SEE.UI.Notification;
using SEE.Utils;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using SEE.Tools.OpenTelemetry;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace SEE.Net
{
    /// <summary>
    /// Handles the most general parts of networking.
    /// </summary>
    [Serializable]
    public class Network : MonoBehaviour
    {
        /// <summary>
        /// The single unique instance of the network.
        /// </summary>
        public static Network Instance { get; private set; }

        /// <summary>
        /// The <see cref="ActionNetwork"/> instance for communication between the clients and the server.
        /// </summary>
        public static readonly Lazy<ActionNetwork> ActionNetworkInst = new(InitActionNetworkInst);

        /// <summary>
        /// The maximal port number.
        /// </summary>
        private const int maxServerPort = 65535;

        /// <summary>
        /// The ID of the server to fetch files from.
        /// </summary>
        public static string ServerId;

        /// <summary>
        /// The protocol of the backend server. Either "http://" or "https://".
        /// </summary>
        public static string Protocol = "http://";
        /// <summary>
        /// Base URL of the backend server where the files are stored
        /// </summary>
        public static string BackendDomain = "localhost:8080";
        /// <summary>
        /// REST resource path, i.e., the URL part identifying the client REST API.
        /// </summary>
        public static string ClientAPI = "/api/v1/";
        /// <summary>
        /// The complete URL of the Client REST API.
        /// </summary>
        public static string ClientRestAPI => Protocol + Network.BackendDomain + ClientAPI;

        /// <summary>
        /// The UDP port where the server listens to NetCode and Dissonance traffic.
        /// Valid range is [0, 65535].
        /// </summary>
        public int ServerPort
        {
            set
            {
                if (value < 0 || value > maxServerPort)
                {
                    throw new ArgumentOutOfRangeException($"A port must be in [0..{maxServerPort}. Received: {value}.");
                }
                UnityTransport netTransport = GetNetworkTransport();
                netTransport.ConnectionData.Port = (ushort)value;

            }
            get
            {
                UnityTransport netTransport = GetNetworkTransport();
                return netTransport.ConnectionData.Port;
            }
        }

        /// <summary>
        /// The password used to enter a meeting room.
        /// </summary>
        public string RoomPassword = "";

        /// <summary>
        /// Used to tell the caller whether the routine has been completed.
        /// </summary>
        private CallBack callbackToMenu = null;

        /// <summary>
        /// Name of the local player; used for the text chat and the avatar badge.
        /// </summary>
        [Tooltip("The name of the player."), ShowInInspector]
        public string PlayerName { get; set; } = "Me";

        /// <summary>
        /// The index of the player's avatar.
        /// </summary>
        [Tooltip("The index of the player's avatar"), ShowInInspector]
        public uint AvatarIndex { get; set; } = 0;

        /// <summary>
        /// Returns the underlying <see cref="UnityTransport"/> of the <see cref="NetworkManager"/>.
        /// This information is retrieved differently depending upon whether we are running
        /// in the editor or in game play because <see cref="NetworkManager.Singleton"/> is
        /// available only during run-time.
        /// </summary>
        /// <returns>underlying <see cref="UNetTransport"/> of the <see cref="NetworkManager"/></returns>
        private static UnityTransport GetNetworkTransport()
        {
            NetworkManager networkManager = GetNetworkManager();
            NetworkConfig networkConfig = networkManager.NetworkConfig;
            if (networkConfig == null)
            {
                Debug.LogError($"NetworkManager.Singleton has no valid {nameof(NetworkConfig)}.\n");
                return null;
            }
            return networkConfig.NetworkTransport as UnityTransport;
        }

        /// <summary>
        /// Returns the <see cref="NetworkManager"/>.
        /// </summary>
        /// <returns>the <see cref="NetworkManager"/></returns>
        /// <remarks>This method works both in the editor and at runtime. In the editor,
        /// a NetworkManager is retrieved from a game object. It that fails, an exception
        /// is thrown.</remarks>
        private static NetworkManager GetNetworkManager()
        {
#if UNITY_EDITOR
            const string metworkManagerName = "NetworkManager";
            GameObject networkManagerGO = GameObject.Find(metworkManagerName);
            if (networkManagerGO == null)
            {
                throw new Exception($"The scene currently opened in the editor does not have a game object {metworkManagerName}.");
            }
            if (networkManagerGO.TryGetComponentOrLog(out NetworkManager result))
            {
                return result;
            }
            else
            {
                throw new Exception($"The game object {metworkManagerName} in the scene currently opened in the editor does not have a component {nameof(NetworkManager)}.");
            }
#else
            // NetworkManager.Singleton is available only during run-time.
            if (NetworkManager.Singleton)
            {
                return NetworkManager.Singleton;
            }
            else
            {
                throw new Exception($"{nameof(NetworkManager)} does not exist.");
            }
#endif
        }

        /// <summary>
        /// The IP4 address of the server.
        /// </summary>
        public string ServerIP4Address
        {
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentOutOfRangeException($"Invalid server IP address: {value}.");
                }
                UnityTransport netTransport = GetNetworkTransport();
                netTransport.ConnectionData.ServerListenAddress = value;
            }

            get
            {
                UnityTransport netTransport = GetNetworkTransport();
                return netTransport.ConnectionData.ServerListenAddress;
            }
        }

        /// <summary>
        /// The name of the scene to be loaded when the game starts.
        /// </summary>
        [Tooltip("The name of the game scene.")]
        public string GameScene = "SEEWorld";

#if UNITY_EDITOR

        /// <summary>
        /// Name of the Inspector foldout group for the logging setttings.
        /// </summary>
        private const string loggingFoldoutGroup = "Logging";

        /// <summary>
        /// Whether the internal logging should be enabled.
        /// </summary>
        [SerializeField, FoldoutGroup(loggingFoldoutGroup)]
        [PropertyTooltip("Whether the network logging should be enabled.")]
        private bool internalLoggingEnabled = true;

        /// <summary>
        /// <see cref="internalLoggingEnabled"/>
        /// </summary>
        public static bool InternalLoggingEnabled => Instance && Instance.internalLoggingEnabled;

#endif
        /// <summary>
        /// True if we are running a host or server.
        /// </summary>
        public static bool HostServer => NetworkManager.Singleton != null
            && (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer);

        /// <summary>
        /// The IP address of the host or server, respectively; the empty string
        /// if none is set.
        /// </summary>
        public static string RemoteServerIPAddress => NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.ServerListenAddress;

        /// <summary>
        /// The Unity main thread. Note that we cannot initialize its value here
        /// because the elaboration code initializing static attributes may be
        /// executed by a thread different from Unity's main thread. This attribute
        /// will be initialized in <see cref="Awake"/> for this reason.
        /// </summary>
        private static Thread mainThread = null;
        /// <summary>
        /// Contains the Unity main thread of the application.
        /// </summary>
        public static Thread MainThread
        {
            get
            {
                Assert.IsNotNull(mainThread, "The main Unity thread must not have been determined as of now!");
                return mainThread;
            }
            private set
            {
                Assert.IsNotNull(value, "The main Unity thread must not be null!");
                if (mainThread != value)
                {
                    Assert.IsNull(mainThread, "The main Unity thread has already been determined!");
                    mainThread = value;
                }
            }
        }

        /// <summary>
        /// Stores every executed Action to be synced with new connecting clients
        /// </summary>
        public static List<string> NetworkActionList = new();

        private void Awake()
        {
            /// The field <see cref="MainThread"/> is supposed to denote Unity's main thread.
            /// The <see cref="Awake"/> function is guaranteed to be executed by Unity's main
            /// thread, that is, <see cref="Thread.CurrentThread"/> represents Unity's
            /// main thread here.
            MainThread = Thread.CurrentThread;

            Load();
        }

        /// <summary>
        /// Name of the command-line argument containing the room password (<see cref="RoomPassword"/>).
        /// </summary>
        private const string passwordArgumentName = "--password";
        /// <summary>
        /// Name of the environment variable containing the room password (<see cref="RoomPassword"/>).
        /// </summary>
        private const string passwordVariableName = "SEE_SERVER_PASSWORD";
        /// <summary>
        /// Name of the command-line argument containing the UDP port (<see cref="ServerPort"/>).
        /// </summary>
        private const string portArgumentName = "--port";
        /// <summary>
        /// Name of the environment variable containing the UDP port (<see cref="ServerPort"/>).
        /// </summary>
        private const string portVariableName = "SEE_SERVER_PORT";
        /// <summary>
        /// Name of the command-line argument containing the backend domain URL (<see cref="BackendDomain"/>).
        /// </summary>
        private const string domainArgumentName = "--host";
        /// <summary>
        /// Name of the environment variable containing the backend domain URL (<see cref="BackendDomain"/>).
        /// </summary>
        private const string domainVariableName = "SEE_BACKEND_DOMAIN";
        /// <summary>
        /// Name of the command-line argument containing the the server id (<see cref="ServerId"/>).
        /// </summary>
        private const string serverIdArgumentName = "--id";
        /// <summary>
        /// Name of the environment variable containing the the server id (<see cref="ServerId"/>).
        /// </summary>
        private const string serverIdVariableName = "SEE_SERVER_ID";
        /// <summary>
        /// Name of the command-line argument for launching this Unity instance
        /// as a dedicated server.
        /// </summary>
        private const string launchAsServerArgumentName = "--launch-as-server";

        /// <summary>
        /// Makes sure that we have only one <see cref="Instance"/> and checks
        /// command-line arguments.
        /// </summary>
        private void Start()
        {
            if (Instance)
            {
                if (Instance != this)
                {
                    Util.Logger.LogWarning("There must not be more than one Network component! "
                        + $"The component {typeof(Network)} in {Instance.gameObject.FullName()} will be destroyed!\n");
                }
            }
            Instance = this;

            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            CollectEnvironmentVariables();
#if UNITY_EDITOR
            Debug.Log("Skipping parsing command-line parameters in Editor mode.\n");
#else
            ProcessCommandLineArguments();
#endif
        }

        /// <summary>
        /// Processes and determines the values of the environment variables
        /// <see cref="ServerPort"/>, <see cref="RoomPassword"/>, <see cref="BackendDomain"/>,
        /// <see cref="ServerId"/>.
        /// </summary>
        private void CollectEnvironmentVariables()
        {
            string serverPort = Environment.GetEnvironmentVariable(portVariableName);
            if (!string.IsNullOrWhiteSpace(serverPort))
            {
                try
                {
                    ServerPort = Int32.Parse(serverPort);
                }
                catch (FormatException _)
                {
                    Debug.LogWarning($"Server port passed via {portVariableName} environment variable is not a valid number.");
                }
            }

            string roomPassword = Environment.GetEnvironmentVariable(passwordVariableName);
            if (!string.IsNullOrWhiteSpace(roomPassword))
            {
                RoomPassword = roomPassword;
            }

            string backendDomain = Environment.GetEnvironmentVariable(domainVariableName);
            if (!string.IsNullOrWhiteSpace(backendDomain))
            {
                BackendDomain = backendDomain;
            }

            string serverId = Environment.GetEnvironmentVariable(serverIdVariableName);
            if (!string.IsNullOrWhiteSpace(serverId))
            {
                ServerId = serverId;
            }
        }

        /// <summary>
        /// Processes and determines the values of the command-line arguments
        /// <see cref="ServerPort"/>, <see cref="RoomPassword"/>, <see cref="BackendDomain"/>,
        /// <see cref="ServerId"/>, and starts the server if the command-line argument
        /// <see cref="launchAsServerArgumentName"/> is present.
        /// </summary>
        /// <exception cref="ArgumentException">thrown if an option requiring a value does
        /// not have one</exception>
        private void ProcessCommandLineArguments()
        {
            string[] arguments = Environment.GetCommandLineArgs();

            bool launchAsServer = false;

            // Commented out because it logs the plaintext password!
            Debug.Log($"Parsing {arguments.Length} command-line parameters.\n"); //:\n{string.Join("; ", arguments)}");

            // Check command line arguments
            // The first element in the array contains the file name of the executing program.
            // If the file name is not available, the first element is equal to String.Empty.
            for (int i = 1; i < arguments.Length; i++)
            {
                switch (arguments[i])
                {
                    case portArgumentName:
                        Debug.Log($"Found {portArgumentName} as parameter {i}.\n");
                        CheckArgumentValue(arguments, i, portArgumentName);
                        ServerPort = Int32.Parse(arguments[i + 1]);
                        i++; // skip one parameter
                        break;
                    case passwordArgumentName:
                        Debug.Log($"Found {passwordArgumentName} as parameter {i}.\n");
                        CheckArgumentValue(arguments, i, passwordArgumentName);
                        RoomPassword = arguments[i + 1];
                        i++; // skip one parameter
                        break;
                    case domainArgumentName:
                        Debug.Log($"Found {domainArgumentName} as parameter {i}.\n");
                        CheckArgumentValue(arguments, i, domainArgumentName);
                        BackendDomain = arguments[i + 1];
                        i++; // skip one parameter
                        break;
                    case serverIdArgumentName:
                        Debug.Log($"Found {serverIdArgumentName} as parameter {i}.\n");
                        CheckArgumentValue(arguments, i, serverIdArgumentName);
                        ServerId = arguments[i + 1];
                        i++; // skip one parameter
                        break;
                    case launchAsServerArgumentName:
                        Debug.Log($"Found {launchAsServerArgumentName} as parameter {i}.\n");
                        // This argument does not have a value. It works as a flag.
                        launchAsServer = true;
                        break;
                    default:
                        Debug.LogWarning($"Unknown command-line parameter {i} will be ignored: {arguments[i]}.\n");
                        break;
                }
            }

            if (launchAsServer)
            {
                CallBack serverCallback = (success, message) =>
                {
                    if (success)
                    {
                        Debug.Log($"Server started successfully: {message}.\n");
                    }
                    else
                    {
                        Debug.LogError($"Starting server failed: {message}.\n");
                    }
                };
                Debug.LogWarning("Starting server...\n");
                StartServer(serverCallback);
            }

            return;

            static void CheckArgumentValue(string[] arguments, int i, string argument)
            {
                if (i + 1 >= arguments.Length)
                {
                    throw new ArgumentException($"Argument value for {argument} is missing.");
                }
                if (string.IsNullOrWhiteSpace(arguments[i + 1]))
                {
                    throw new ArgumentException($"Argument value for {argument} is missing.");
                }
            }
        }

        /// <summary>
        /// Yields the <see cref="ActionNetwork"/> component attached to the Server game object.
        /// </summary>
        private static ActionNetwork InitActionNetworkInst()
        {
            const string serverName = "Server";
            GameObject server = GameObject.Find(serverName);
            if (server != null)
            {
                server.TryGetComponentOrLog(out ActionNetwork serverNetwork);
                return serverNetwork;
            }
            else
            {
                Debug.LogError($"There is no game object named {serverName} in the scene.\n");
                return null;
            }
        }

        /// <summary>
        /// Broadcasts a serialized action.
        /// </summary>
        /// <param name="serializedAction">Serialized action to be broadcast</param>
        /// <param name="recipients">List of recipients to broadcast to. Will broadcast to all clients if this is <c>null</c> or omitted.</param>
        public static void BroadcastAction(String serializedAction, ulong[] recipients = null)
        {
            /// TODO(#754): Replace with the exact value.
            int maxPacketSize = 32000;
            if (serializedAction.Length < maxPacketSize)
            {
                ActionNetworkInst.Value?.BroadcastActionServerRpc(serializedAction, recipients);
            }
            else
            {
                List<string> fragmentData = SplitString(serializedAction, maxPacketSize);
                string id = Guid.NewGuid().ToString();
                for (int i = 0; i < fragmentData.Count; i++)
                {
                    ActionNetworkInst.Value?.BroadcastActionServerRpc(id, fragmentData.Count, i, fragmentData[i], recipients);
                }
            }
        }

        /// <summary>
        /// Splitts a string after <paramref name="fragmentSize"/> chars.
        /// </summary>
        /// <param name="str">The string to be split</param>
        /// <param name="fragmentSize">The size for the sub strings.</param>
        /// <returns>A list with the split strings.</returns>
        private static List<string> SplitString(string str, int fragmentSize)
        {
            List<string> fragments = new();

            for (int i = 0; i < str.Length; i += fragmentSize)
            {
                if (i + fragmentSize > str.Length)
                {
                    fragments.Add(str.Substring(i));
                }
                else
                {
                    fragments.Add(str.Substring(i, fragmentSize));
                }
            }

            return fragments;
        }

        /// <summary>
        /// Starts the selected voice chat system according to <see cref="VoiceChat"/>.
        /// </summary>
        private void StartVoiceChat()
        {
            switch (VoiceChat)
            {
                case VoiceChatSystems.Dissonance:
                    EnableDissonance(true);
                    break;
                case VoiceChatSystems.None:
                    EnableDissonance(false);
                    break;
                default:
                    EnableDissonance(false);
                    throw new NotImplementedException($"Unhanded voice chat option {VoiceChat}.");
            }
        }

        /// <summary>
        /// Enables/disables Dissonance as the voice chat system.
        /// </summary>
        /// <param name="enable">whether to enable Dissonance</param>
        private static void EnableDissonance(bool enable)
        {
            // The DissonanceComms is initially active and the local player is not muted and not deafened.
            DissonanceComms dissonanceComms = FindObjectOfType<DissonanceComms>(includeInactive: true);
            if (dissonanceComms != null)
            {
                dissonanceComms.IsMuted = !enable;
                dissonanceComms.IsDeafened = !enable;
                dissonanceComms.enabled = enable;
            }
            else
            {
                Debug.LogError($"There is no {typeof(DissonanceComms)} in the current scene.\n");
            }
        }

        /// <summary>
        /// Initializes the game.
        /// </summary>
        private void InitializeGame()
        {
            if (NetworkManager.Singleton.IsHost && NetworkManager.Singleton.IsClient)
            {
                TracingHelperService.Initialize(PlayerName);
            }
            AsyncUtils.MainThreadId = Thread.CurrentThread.ManagedThreadId;

            if (HostServer)
            {
                foreach (AbstractSEECity city in FindObjectsOfType<AbstractSEECity>())
                {
                    if (city is SEECity seeCity)
                    {
                        seeCity.LoadAndDrawGraphAsync().Forget();
                    }
                    else
                    {
                        Util.Logger.LogError("Unsupported city type!");
                    }
                }
            }
        }

        /// <summary>
        /// Shuts down the server and the clients.
        /// This method is called only when this component is destroyed, which
        /// may be at the very end of the game.
        /// </summary>
        private void OnDestroy()
        {
            ShutdownNetwork();
        }

        /// <summary>
        /// Shuts down the network (server and clients).
        /// </summary>
        private void ShutdownNetwork()
        {
            // FIXME there must be a better way to stop the logging spam!
            string currentDirectory = Directory.GetCurrentDirectory();
            DirectoryInfo directoryInfo = new(currentDirectory);
            FileInfo[] fileInfos = directoryInfo.GetFiles();
            foreach (FileInfo fileInfo in fileInfos)
            {
                string fileName = fileInfo.Name;
                string[] prefixes = {
                    "CompleteIncomingItemTaskError",
                    "ConnectionKeepAlivePollError",
                    "Error",
                    "ManagedThreadPoolCallBackError",
                    "PacketHandlerErrorGlobal"
                };
                if (prefixes.Any(t => fileName.Contains(t)))
                {
                    try
                    {
                        fileInfo.Delete();
                    }
                    catch (IOException)
                    {
                    }
                }
            }
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Debug.Log("Network is shut down.\n");
        }

        /// <summary>
        /// Returns an array of all local IP-Addresses.
        /// </summary>
        /// <returns>An array of all local IP-Addresses.</returns>
        public static IPAddress[] LookupLocalIPAddresses()
        {
            string hostName = Dns.GetHostName();
            IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
            return hostEntry.AddressList;
        }

        /// <summary>
        /// Loads the <see cref="GameScene"/>. Will be called when the server was started.
        /// Registers <see cref="OnSceneLoaded(Scene, LoadSceneMode)"/> to be called when
        /// the scene is fully loaded.
        /// </summary>
        private void OnServerStarted()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            NetworkManager.Singleton.SceneManager.LoadScene(GameScene, LoadSceneMode.Single);
        }

        /// <summary>
        /// Starts the voice-chat system selected. Unregisters itself from
        /// <see cref="SceneManager.sceneLoaded"/>.
        /// Note: This method is assumed to be called when the new scene is fully loaded.
        /// </summary>
        /// <param name="scene">scene that was loaded</param>
        /// <param name="mode">the mode in which the scene was loaded</param>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Now we have loaded the scene that is supposed to contain settings for the voice chat
            // system. We can now turn on the voice chat system.
            Debug.Log($"Loaded scene {scene.name} in mode {mode}.\n");
            SceneManager.sceneLoaded -= OnSceneLoaded;
            StartVoiceChat();
        }

        /// <summary>
        /// Unregisters itself from <see cref="SceneManager.sceneLoaded"/>.
        /// Note: This method is assumed to be called when the new scene is fully unloaded.
        /// </summary>
        /// <param name="scene">scene that was loaded</param>
        private void OnSceneUnloaded(Scene scene)
        {
            Debug.Log($"Unloaded scene {scene.name}.\n");
            if (scene.name == GameScene)
            {
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
                ShutdownNetwork();
                Destroyer.Destroy(Instance);
            }
        }

        /// <summary>
        /// Callback delegate used by <see cref="StartHost(CallBack)"/>, <see cref="StartClient(CallBack)"/>,
        /// and <see cref="StartServer(CallBack)"/> after they have been finished (they are using
        /// co-routines).
        /// </summary>
        /// <param name="success">if true, the operation was successful</param>
        /// <param name="message">a description of what happened</param>
        public delegate void CallBack(bool success, string message);

        /// <summary>
        /// Starts a server process.
        ///
        /// Note: This method starts a co-routine and then returns to the caller immediately.
        /// The <paramref name="callBack"/> tells the caller that the co-routine has come to
        /// an end.
        /// </summary>
        /// <param name="callBack">a callback to be called when done; its parameter will be true
        /// in case of success or otherwise false</param>
        public void StartServer(CallBack callBack)
        {
            StartCoroutine(ShutdownNetwork(InternalStartServer));

            void InternalStartServer()
            {
                // Using an IP address of 0.0.0.0 for the server listen address will make a
                // server or host listen on all IP addresses assigned to the local system.
                // This can be particularly helpful if you are testing a client instance
                // on the same system as well as one or more client instances connecting
                // from other systems on your local area network. Another scenario is while
                // developing and debugging you might sometimes test local client instances
                // on the same system and sometimes test client instances running on external
                // systems.
                ServerIP4Address = "0.0.0.0";
                Debug.Log($"Server is starting to listen at {ServerAddress}...\n");
                try
                {
                    NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
                    if (NetworkManager.Singleton.StartServer())
                    {
                        InitializeGame();
                        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallbackForServer;
                        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallbackForServer;
                    }
                    else
                    {
                        throw new CannotStartServer($"Could not start server {ServerAddress}.");
                    }
                }
                catch (Exception exception)
                {
                    callBack(false, exception.Message);
                    throw;
                }
                callBack(true, $"Server started at {ServerAddress}.");
            }
        }

        /// <summary>
        /// Callback called when a client has connected to the server.
        /// Emits a user message.
        /// </summary>
        /// <param name="client">the ID of the client</param>
        private void OnClientConnectedCallbackForServer(ulong client)
        {
            ShowNotification.Info("Connection", $"Client {client} has connected.");
        }

        /// <summary>
        /// Callback called when a client has disconnected from the server.
        /// Emits a user message.
        /// </summary>
        /// <param name="client">the ID of the client</param>
        private void OnClientDisconnectCallbackForServer(ulong client)
        {
            ShowNotification.Info("Connection", $"Client {client} has disconnected.");
        }

        /// The IP4 address, port, and protocol.
        /// </summary>
        private string ServerAddress => $"{ServerIP4Address}:{ServerPort} (UDP)";

        /// <summary>
        /// Starts a host process, i.e., a server and a local client.
        ///
        /// Note: This method starts a co-routine and then returns to the caller immediately.
        /// The <paramref name="callBack"/> tells the caller that the co-routine has come to
        /// an end.
        /// </summary>
        /// <param name="callBack">a callback to be called when done; its parameter will be true
        /// in case of success or otherwise false</param>
        public void StartHost(CallBack callBack)
        {
            StartCoroutine(ShutdownNetwork(InternalStartHost));

            void InternalStartHost()
            {
                Debug.Log($"Host is starting to listen at {ServerAddress}...\n");
                Debug.Log($"Local client is trying to connect to server {ServerAddress}...\n");
                try
                {
                    NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
                    if (NetworkManager.Singleton.StartHost())
                    {
                        InitializeGame();
                        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallbackForServer;
                        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallbackForServer;
                    }
                    else
                    {
                        throw new CannotStartServer($"Could not start host {ServerAddress}");
                    }
                }
                catch (Exception exception)
                {
                    callBack(false, exception.Message);
                    throw;
                }
                callBack(true, $"Host started at {ServerAddress}.");
            }
        }

        /// <summary>
        /// Checks whether a specific client is authorized to establish a connection to the server.
        /// The client sends the server a request with a password to join the room,
        /// the server sends a corresponding response depending on whether the sent password matches
        /// the set password.
        /// <param name="request">contains the password</param>
        /// <param name="response">contains the answer of the server</param>
        /// </summary>
        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
                                   NetworkManager.ConnectionApprovalResponse response)
        {
            if (RoomPassword == Encoding.ASCII.GetString(request.Payload))
            {
                Debug.Log($"Client {request.ClientNetworkId} has sent correct room password.\n");
                response.Approved = true;

            }
            else
            {
                response.Approved = false;
                response.Reason = "Invalid password";
                Debug.LogWarning($"Client {request.ClientNetworkId} has sent incorrect room password.\n");
            }
        }

        /// <summary>
        /// Removes the reference to the callback used to send the client back to the main menu
        /// because the connection was successfully established.
        /// The <paramref name="owner"/> is not used.
        /// </summary>
        /// <param name="owner">ID of the owner (ignored)</param>
        private void OnClientConnectedCallback(ulong owner)
        {
            callbackToMenu?.Invoke(true, $"You are connected to {ServerAddress}.");
            callbackToMenu = null;
            TracingHelperService.Initialize(PlayerName);
        }

        /// <summary>
        /// Sends the client back to the main menu because the connection could not
        /// be established.
        /// The <paramref name="owner"/> is not used.
        /// </summary>
        /// <param name="owner">ID of the owner (ignored)</param>
        private void OnClientDisconnectCallback(ulong owner)
        {
            callbackToMenu?.Invoke(false,
                $"The server {ServerAddress} has refused the connection due to the following reason: "
                + NetworkManager.Singleton.DisconnectReason);
            callbackToMenu = null;
            if (!HostServer)
            {
                TracingHelperService.Shutdown(false);
            }
        }

        /// <summary>
        /// Starts a client.
        ///
        /// Note: This method starts a co-routine and then returns to the caller immediately.
        /// The <paramref name="callBack"/> tells the caller that the co-routine has come to
        /// an end.
        /// </summary>
        /// <param name="callBack">a callback to be called when done; its parameter will be true
        /// in case of success or otherwise false</param>
        public void StartClient(CallBack callBack)
        {
            callbackToMenu = callBack;
            // Set the address of the server to connect to.
            UnityTransport netTransport = GetNetworkTransport();
            netTransport.ConnectionData.Address = ServerIP4Address;
            StartCoroutine(ShutdownNetwork(InternalStartClient));

            void InternalStartClient()
            {
                Debug.Log($"Client is trying to connect to server {ServerAddress}...\n");

                NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(RoomPassword);
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;

                if (NetworkManager.Singleton.StartClient())
                {
                    InitializeGame();
                }
            }
        }

        /// <summary>
        /// The maximal waiting time in seconds a client is willing to wait until a connection
        /// can be established.
        /// </summary>
        private const float maxWaitingTimeInSeconds = 5 * 60;

        /// <summary>
        /// A delegate that will be called in <see cref="ShutdownNetwork(OnShutdownFinished)"/> when
        /// the network has been shut down (if needed at all), for instance, to (re-)start the network.
        /// </summary>
        delegate void OnShutdownFinished();

        /// <summary>
        /// If the network is already running, it will be shut down. Finally, <paramref name="onShutdownFinished"/>
        /// will be called.
        ///
        /// This method is used as a co-routine started in <see cref="StartHost(CallBack)"/>,
        /// <see cref="StartServer(CallBack)"/>, and <see cref="StartClient(CallBack)"/>.
        /// </summary>
        /// <param name="onShutdownFinished">function to be called at the end of the shutdown</param>
        /// <returns>whether to continue this co-routine</returns>
        private IEnumerator ShutdownNetwork(OnShutdownFinished onShutdownFinished)
        {
            // In case we are connected, we will first disconnect.
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            {
                Debug.Log("Network is shutting down...\n");

                // NetworkManager.Singleton.Shutdown() can be called by clients, server, and host.
                // It will take of the necessary shutdown actions required for these respective
                // roles.
                NetworkManager.Singleton.Shutdown();

                // The shutdown is not immediate. We will need to wait until this process has
                // finished, i.e., until NetworkManager.Singleton.ShutdownInProgress becomes false.
                while (NetworkManager.Singleton.ShutdownInProgress)
                {
                    yield return null;
                }
            }

            ShutdownNetwork();

            onShutdownFinished();
        }

        /// <summary>
        /// The kinds of voice-chats system we support. None means no voice
        /// chat whatsoever.
        /// </summary>
        public enum VoiceChatSystems
        {
            None = 0,       // no voice chat
            Dissonance = 1, // Dissonance voice chat
        }

        /// <summary>
        /// Name of the Inspector foldout group for the logging setttings.
        /// </summary>
        private const string voiceChatFoldoutGroup = "Voice Chat";

        /// <summary>
        /// The voice chat system as selected by the user. Note: This attribute
        /// can be changed in the editor via <see cref="NetworkEditor"/> as well
        /// as at the start up in the <see cref="OpeningDialog"/>.
        /// </summary>
        [Tooltip("The voice chat system to be used. 'None' for no voice chat."), FoldoutGroup(voiceChatFoldoutGroup)]
        public VoiceChatSystems VoiceChat = VoiceChatSystems.None;

        /// <summary>
        /// Shuts down the voice-chat system and opentelemetry.
        /// </summary>
        private void OnApplicationQuit()
        {
            TracingHelperService.Shutdown(true);
            switch (VoiceChat)
            {
                case VoiceChatSystems.None:
                    // nothing to be done
                    break;
                case VoiceChatSystems.Dissonance:
                    // nothing to be done
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Represents an IP address along with its IP address family.
        /// This is used only for informational purposes in <see cref="AddressesInfo"/>.
        /// </summary>
        [Serializable]
        private readonly struct AddressInfo
        {
            /// <summary>
            /// The address family of the TCP/IP protocol, e.g., InterNetworkV6
            /// or InterNetwork.
            /// </summary>
            public readonly string AddressFamily;
            /// <summary>
            /// The IP address.
            /// </summary>
            public readonly string IPAddress;
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="addressFamiliy">address family of the TCP/IP protocol</param>
            /// <param name="ipAddress">IP address</param>
            public AddressInfo(string addressFamiliy, string ipAddress)
            {
                AddressFamily = addressFamiliy;
                IPAddress = ipAddress;
            }
        }

        /// <summary>
        /// The IP addresses and the address families they belong to for the local machine.
        /// This will be used in the Inspector for informational purposes only.
        /// </summary>
        [ShowInInspector]
        [PropertyTooltip("IP addresses of this machine.")]
        [TableList(IsReadOnly = true)]
        private IList<AddressInfo> AddressesInfo
        {
            get
            {
                return LookupLocalIPAddresses()
                              .Select(ip => new AddressInfo(ip.AddressFamily.ToString(), ip.ToString()))
                              .ToList();
            }
        }

        /// <summary>
        /// The name of the group for the fold-out group of the configuration file.
        /// </summary>
        private const string configurationFoldoutGroup = "Configuration File";

        /// <summary>
        /// The name of the group for the Inspector buttons loading and saving the configuration file.
        /// </summary>
        private const string configurationButtonsGroup = "ConfigurationButtonsGroup";

        /// <summary>
        /// Default path of the configuration file (path and filename).
        /// </summary>
        [PropertyTooltip("Path of the file containing the network configuration.")]
        [OdinSerialize, HideReferenceObjectPicker, FoldoutGroup(configurationFoldoutGroup)]
        public DataPath ConfigPath = new();

        /// <summary>
        /// Saves the settings of this network configuration to <see cref="ConfigPath()"/>.
        /// If the configuration file exists already, it will be overridden.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [PropertyTooltip("Saves the network settings in a configuration file.")]
        [ButtonGroup(configurationButtonsGroup)]
        public void Save()
        {
            Save(ConfigPath.Path);
        }

        /// <summary>
        /// Loads the settings of this network configuration from <see cref="ConfigPath()"/>
        /// if it exists. If it does not exist, nothing happens.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [PropertyTooltip("Loads the network configuration file.")]
        [ButtonGroup(configurationButtonsGroup)]
        public void Load()
        {
            Load(ConfigPath.Path);
        }

        #region ConfigIO

        //--------------------------------
        // Configuration file input/output
        //--------------------------------

        /// <summary>
        /// Label of attribute <see cref="GameScene"/> in the configuration file.
        /// </summary>
        private const string gameSceneLabel = "gameScene";
        /// <summary>
        /// Label of attribute <see cref="VoiceChat"/> in the configuration file.
        /// </summary>
        private const string voiceChatLabel = "voiceChat";
        /// <summary>
        /// Label of attribute <see cref="ServerPort"/> in the configuration file.
        /// </summary>
        private const string serverPortLabel = "serverPort";
        /// <summary>
        /// Label of attribute <see cref="RoomPassword"/> in the configuration file.
        /// </summary>
        private const string roomPasswordLabel = "roomPassword";
        /// <summary>
        /// Label of attribute <see cref="ServerIP4Address"/> in the configuration file.
        /// </summary>
        private const string serverIP4AddressLabel = "serverIP4Address";
        /// <summary>
        /// Label of attribute <see cref="PlayerName"/> in the configuration file.
        /// </summary>
        private const string playernameLabel = "playername";
        /// <summary>
        /// Label of attribute <see cref="AvatarIndex"/> in the configuration file.
        /// </summary>
        private const string avatarIndexLabel = "avatarIndex";

        /// <summary>
        /// Saves the settings of this network configuration to <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">name of the file in which the settings are stored</param>
        public void Save(string filename)
        {
            using ConfigWriter writer = new(filename);
            Save(writer);
        }

        /// <summary>
        /// Reads the settings of this network configuration from <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">name of the file from which the settings are restored</param>
        public void Load(string filename)
        {
            if (File.Exists(filename))
            {
                Debug.Log($"Loading network configuration file from {filename}.\n");
                using ConfigReader stream = new(filename);
                Restore(stream.Read());
            }
            else
            {
                Debug.LogError($"Network configuration file {filename} does not exist.\n");
            }
        }

        /// <summary>
        /// Saves the settings of this network configuration using <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">the writer to be used to save the settings</param>
        protected virtual void Save(ConfigWriter writer)
        {
            writer.Save(GameScene, gameSceneLabel);
            writer.Save(VoiceChat.ToString(), voiceChatLabel);
            writer.Save(ServerPort, serverPortLabel);
            writer.Save(ServerIP4Address, serverIP4AddressLabel);
            writer.Save(RoomPassword, roomPasswordLabel);
            writer.Save(PlayerName, playernameLabel);
            // The following cast from uint to int is necessary because otherwise the value
            // would be saved as a float.
            writer.Save((int)AvatarIndex, avatarIndexLabel);
        }

        /// <summary>
        /// Restores the settings from <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">the attributes from which to restore the settings</param>
        protected virtual void Restore(Dictionary<string, object> attributes)
        {
            ConfigIO.Restore(attributes, gameSceneLabel, ref GameScene);
            ConfigIO.RestoreEnum(attributes, voiceChatLabel, ref VoiceChat);
            ConfigIO.Restore(attributes, roomPasswordLabel, ref RoomPassword);
            {
                int value = ServerPort;
                ConfigIO.Restore(attributes, serverPortLabel, ref value);
                ServerPort = value;
            }
            {
                string value = ServerIP4Address;
                ConfigIO.Restore(attributes, serverIP4AddressLabel, ref value);
                ServerIP4Address = value;
            }
            {
                string value = PlayerName;
                ConfigIO.Restore(attributes, playernameLabel, ref value);
                PlayerName = value;
            }
            {
                int value = (int)AvatarIndex;
                if (ConfigIO.Restore(attributes, avatarIndexLabel, ref value))
                {
                    AvatarIndex = (uint)value;
                }
            }

        }

#endregion
    }
}
