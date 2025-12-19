using Cysharp.Threading.Tasks;
using SEE.Game.City;
using SEE.GO;
using SEE.Tools.OpenTelemetry;
using SEE.UI.Notification;
using SEE.Utils;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SEE.Net
{
    /// <summary>
    /// Handles the most general parts of networking.
    /// </summary>
    [Serializable]
    public class Network
    {
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
        /// Base URL of the backend server (specified by <see cref="BackendServerAPI"/>) where the
        /// files are stored. Result does not end with a slash.
        /// </summary>
        /// <example>If <see cref="BackendServerAPI"/> equals "http://localhost:8080/api/v1/",
        /// then "http://localhost:8080" is returned.</example>
        public string BackendDomain
        {
           get
            {
                Uri uri = new(BackendServerAPI);
                return uri.GetLeftPart(UriPartial.Authority);
            }
        }

        /// <summary>
        /// The complete URL of the Client REST API consisting of the backend domain and the API path.
        /// </summary>
        [Tooltip("The complete URL of the backend server API. Must end with a /.")]
        public string BackendServerAPI = "http://localhost:8080/api/v1/";

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
        [Tooltip("The password used to enter a meeting room.")]
        public string RoomPassword = "";

        /// <summary>
        /// Used to tell the caller whether the routine has been completed.
        /// </summary>
        private CallBack callbackToMenu = null;

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
        public string GameScene = "SEENewWorld";

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
        public bool InternalLoggingEnabled
        {
            get => Util.Logger.InternalLoggingEnabled;
            set
            {
                Util.Logger.InternalLoggingEnabled = value;
            }
        }

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
        public static string RemoteServerIPAddress
            => NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.ServerListenAddress;

        /// <summary>
        /// Stores every executed Action to be synced with new connecting clients
        /// </summary>
        public static List<string> NetworkActionList = new();

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
        /// Name of the environment variable containing the backend api URL (<see cref="BackendServerAPI"/>).
        /// </summary>
        private const string backendAPIVariableName = "SEE_BACKEND_API";
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
        /// Sets up the network manager, collects environment variables and processes
        /// the command-line arguments (if not running in the editor mode).
        ///
        /// This method does not really start any server or client. Use <see cref="StartClient(CallBack)"/>,
        /// <see cref="StartServer(CallBack)"/>, or <see cref="StartHost(CallBack)/> instead.
        /// </summary>
        public void SetUp()
        {
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

            string backendServerAPI = Environment.GetEnvironmentVariable(backendAPIVariableName);
            if (!string.IsNullOrWhiteSpace(backendServerAPI))
            {
                BackendServerAPI = backendServerAPI;
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
                        BackendServerAPI = arguments[i + 1];
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
        /// Splits a string after <paramref name="fragmentSize"/> chars.
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
        /// Initializes the game.
        /// </summary>
        private void InitializeGame()
        {
            if (NetworkManager.Singleton.IsHost && NetworkManager.Singleton.IsClient)
            {
                TracingHelperService.Initialize(SEE.User.UserSettings.Instance.Player.PlayerName);
            }
            AsyncUtils.MainThreadId = Thread.CurrentThread.ManagedThreadId;

            if (HostServer)
            {
                foreach (AbstractSEECity city in UnityEngine.Object.FindObjectsByType<AbstractSEECity>(FindObjectsSortMode.None))
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
            DeleteNetworkLoggingFiles();
        }

        /// <summary>
        /// Deletes all networking logging files.
        /// </summary>
        private void DeleteNetworkLoggingFiles()
        {
            // FIXME there must be a better way to stop the logging spam!
            // FIXME Is this really still necessary?
            DirectoryInfo directoryInfo = new(Directory.GetCurrentDirectory());
            foreach (FileInfo fileInfo in directoryInfo.GetFiles())
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
            // SceneManager.sceneLoaded -= OnSceneLoaded; FIXME
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
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            NetworkManager.Singleton.SceneManager.LoadScene(GameScene, LoadSceneMode.Single);
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
                DeleteNetworkLoggingFiles();
                // Destroyer.Destroy(Instance);
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
            ShutdownNetworkAsync(InternalStartServer).Forget();

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
            ShutdownNetworkAsync(InternalStartHost).Forget();

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
            TracingHelperService.Initialize(SEE.User.UserSettings.Instance.Player.PlayerName);
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
            ShutdownNetworkAsync(InternalStartClient).Forget();

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
        /// A delegate that will be called in <see cref="ShutdownNetworkAsync(OnShutdownFinished)"/> when
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
        private async UniTask ShutdownNetworkAsync(OnShutdownFinished onShutdownFinished)
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
                await UniTask.WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
            }

            DeleteNetworkLoggingFiles();

            onShutdownFinished();
            Debug.Log("Network is shut down.\n");
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

        #region ConfigIO

        //--------------------------------
        // Configuration file input/output
        //--------------------------------

        /// <summary>
        /// Label of attribute <see cref="GameScene"/> in the configuration file.
        /// </summary>
        private const string gameSceneLabel = "gameScene";
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
        /// Label of attribute <see cref="BackendServerAPI"/> in the configuration file.
        /// </summary>
        private const string backendServerAPILabel = "backendServerAPI";

        /// <summary>
        /// Saves the settings of this network configuration using <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">the writer to be used to save the settings</param>
        /// <param name="label">the label under which to group the settings</param>
        public virtual void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(GameScene, gameSceneLabel);
            writer.Save(ServerPort, serverPortLabel);
            writer.Save(ServerIP4Address, serverIP4AddressLabel);
            writer.Save(RoomPassword, roomPasswordLabel);
            writer.Save(BackendServerAPI, backendServerAPILabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Restores the settings from <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">the attributes from which to restore the settings</param>
        /// <param name="label">the label under which to look up the settings in <paramref name="attributes"/></param>
        public virtual void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.Restore(values, gameSceneLabel, ref GameScene);
                ConfigIO.Restore(values, roomPasswordLabel, ref RoomPassword);
                {
                    int value = ServerPort;
                    ConfigIO.Restore(values, serverPortLabel, ref value);
                    ServerPort = value;
                }
                {
                    string value = ServerIP4Address;
                    ConfigIO.Restore(values, serverIP4AddressLabel, ref value);
                    ServerIP4Address = value;
                }
                ConfigIO.Restore(values, backendServerAPILabel, ref BackendServerAPI);
            }
        }

#endregion
    }
}
