using System;
using System.Collections.Generic;
using Dissonance.Networking.Client;
using Dissonance.Networking.Server;
using Dissonance.Networking.Server.Admin;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Profiling;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dissonance.Networking
{
    public abstract class BaseCommsNetwork<TServer, TClient, TPeer, TClientParam, TServerParam>
        : MonoBehaviour, ICommsNetwork, ICommsNetworkState
        where TPeer: struct, IEquatable<TPeer>
        where TServer: BaseServer<TServer, TClient, TPeer>
        where TClient: BaseClient<TServer, TClient, TPeer>
    {
        #region States
        /// <summary>
        /// Represents a possible state which the comms session is in
        /// </summary>
        private interface IState
        {
            /// <summary>
            /// Status of the connection to the server
            /// </summary>
            ConnectionStatus Status { get; }

            /// <summary>
            /// Called once when this state is first entered
            /// </summary>
            void Enter();

            /// <summary>
            /// Called once a frame while this state is active
            /// </summary>
            void Update();

            /// <summary>
            /// Called once when this state is exited
            /// </summary>
            void Exit();
        }

        /// <summary>
        /// Indicates that the session is inactive
        /// </summary>
        /// ReSharper disable once InheritdocConsiderUsage
        private class Idle : IState
        {
            private readonly BaseCommsNetwork<TServer, TClient, TPeer, TClientParam, TServerParam> _net;

            public Idle(BaseCommsNetwork<TServer, TClient, TPeer, TClientParam, TServerParam> net)
            {
                _net = net;
            }

            /// <inheritdoc />
            public ConnectionStatus Status
            {
                get { return ConnectionStatus.Disconnected; }
            }

            /// <inheritdoc />
            public void Enter()
            {
                _net.Mode = NetworkMode.None;
            }

            /// <inheritdoc />
            public void Update() { }

            /// <inheritdoc />
            public void Exit() { }
        }

        /// <summary>
        /// Indicates that the session is active and connecting/connected
        /// </summary>
        /// ReSharper disable once InheritdocConsiderUsage
        private class Session : IState
        {
            [CanBeNull] private readonly TClientParam _clientParameter;
            [CanBeNull] private readonly TServerParam _serverParameter;
            private readonly NetworkMode _mode;
            private readonly BaseCommsNetwork<TServer, TClient, TPeer, TClientParam, TServerParam> _net;

            private float _reconnectionAttemptInterval;
            private DateTime _lastReconnectionAttempt;

            public Session(
                [NotNull] BaseCommsNetwork<TServer, TClient, TPeer, TClientParam, TServerParam> net,
                NetworkMode mode,
                [CanBeNull] TServerParam serverParameter,
                [CanBeNull] TClientParam clientParameter)
            {
                _net = net;
                _clientParameter = clientParameter;
                _serverParameter = serverParameter;
                _mode = mode;
            }

            /// <inheritdoc />
            public ConnectionStatus Status
            {
                get
                {
                    var serverOk = !_mode.IsServerEnabled() || _net.Server != null;
                    var clientOk = !_mode.IsClientEnabled() || (_net.Client != null && _net.Client.IsConnected);

                    if (serverOk && clientOk)
                        return ConnectionStatus.Connected;

                    return ConnectionStatus.Degraded;
                }
            }

            /// <inheritdoc />
            public void Enter()
            {
                _net.Log.Debug("Starting network session as {0}", _mode);

                // It's important that this is set first. It allow the StartX methods to query exactly what state they're starting into
                _net.Mode = _mode;

                if (_mode.IsServerEnabled())
                    StartServer();

                if (_mode.IsClientEnabled())
                    StartClient();
            }

            /// <inheritdoc />
            public void Update()
            {
                if (_mode.IsServerEnabled())
                {
                    var state = _net.Server.Update();

                    // If we encounter an error during update then the best we can do is slam the connection shut and try to open a new one. Since this is the server it...
                    // ...will cause all of the clients to error too (they will communicate with the new server, and get back an incorrect session reponse). They will...
                    // ...disconnect themselves and connect to the new server.
                    if (state == ServerState.Error)
                    {
                        _net.Log.Warn("Server update encountered an error - restarting server");

                        // The server should have already shut itself down internally when it detected an error. Call this to perform clean up at the BaseCommsNetwork level.
                        // It's safe to call Disconnect on the server multiple times so it's not a problem that it may have already been called by the server itself.
                        _net.StopServer();
                        StartServer();
                    }
                }

                if (_mode.IsClientEnabled())
                {
                    if (_net.Client != null)
                    {
                        var state = _net.Client.Update();

                        // If we encounter an error during update we instantly slam shut the connection and, after a small delay, attempt to create a new client. Call...
                        // ...StopClient to perform clean up at the BaseCommsNetwork level. This will call Disconnect on the client (which should have already been called...
                        // ...by the client itself) but that's fine, it's safe to call multiple times.
                        if (state == ClientStatus.Error)
                        {
                            _net.Log.Warn("Client update encountered an error - shutting down client");
                            _net.StopClient();
                        }
                        else
                        {
                            //While the client is running without any errors reduce the reconnection interval linearly.
                            _reconnectionAttemptInterval = Math.Max(0, _reconnectionAttemptInterval - Time.unscaledDeltaTime);
                        }

                    }

                    if (_net.Client == null && ShouldAttemptReconnect())
                    {
                        _net.Log.Info("Attempting to restart client");

                        StartClient();

                        //Every time we start a new client increase the interval until we're allowed to start another client (linear backoff)
                        _reconnectionAttemptInterval = Math.Min(3, _reconnectionAttemptInterval + 0.5f);
                    }
                }
            }

            /// <inheritdoc />
            public void Exit()
            {
                _net.Log.Debug("Closing network session");

                if (_net.Client != null)
                    _net.StopClient();

                if (_net.Server != null)
                    _net.StopServer();
            }

            private void StartServer()
            {
                _net.StartServer(_serverParameter);
            }

            private void StartClient()
            {
                _net.StartClient(_clientParameter);
                _lastReconnectionAttempt = DateTime.UtcNow;
            }

            private bool ShouldAttemptReconnect()
            {
                return (DateTime.UtcNow - _lastReconnectionAttempt).TotalSeconds >= _reconnectionAttemptInterval;
            }
        }
        #endregion

        #region fields and properties
        private readonly Queue<IState> _nextStates;
        private IState _state;
        private NetworkMode _mode;

        protected TServer Server { get; private set; }
        protected TClient Client { get; private set; }

        protected readonly Log Log;

        public string PlayerName { get; private set; }
        public Rooms Rooms { get; private set; }
        public PlayerChannels PlayerChannels { get; private set; }
        public RoomChannels RoomChannels { get; private set; }
        public CodecSettings CodecSettings { get; private set; }

        public event Action<NetworkMode> ModeChanged;
        public event Action<string, CodecSettings> PlayerJoined;
        public event Action<string> PlayerLeft;
        public event Action<VoicePacket> VoicePacketReceived;
        public event Action<TextMessage> TextPacketReceived;
        public event Action<string> PlayerStartedSpeaking;
        public event Action<string> PlayerStoppedSpeaking;
        public event Action<RoomEvent> PlayerEnteredRoom;
        public event Action<RoomEvent> PlayerExitedRoom;

        public bool IsInitialized { get; private set; }

        public ConnectionStatus Status
        {
            get { return _state.Status; }
        }

        public NetworkMode Mode
        {
            get { return _mode; }
            private set
            {
                if (_mode != value)
                {
                    _mode = value;
                    OnModeChanged(value);
                }
            }
        }

        /// <summary>
        /// Get the server moderation API. Returns null if this is not the server instance.
        /// </summary>
        [CanBeNull] public IServerAdmin ServerAdmin
        {
            get
            {
                var s = Server;
                if (s == null)
                    return null;

                return s.ServerAdmin;
            }
        }
        #endregion

        #region constructor
        protected BaseCommsNetwork()
        {
            Log = Logs.Create(LogCategory.Network, GetType().Name);

            _nextStates = new Queue<IState>();
            _mode = NetworkMode.None;
            _state = new Idle(this);
        }
        #endregion

        /// <summary>
        /// Create an instance of your server class
        /// </summary>
        /// <param name="connectionParameters"></param>
        /// <returns></returns>
        [NotNull] protected abstract TServer CreateServer([CanBeNull] TServerParam connectionParameters);

        /// <summary>
        /// Create an instance of your client class
        /// </summary>
        /// <param name="connectionParameters"></param>
        /// <returns></returns>
        [NotNull] protected abstract TClient CreateClient([CanBeNull] TClientParam connectionParameters);

        /// <summary>
        /// Opportunity to perform *one time* setup of the network
        /// </summary>
        protected virtual void Initialize() { }

        void ICommsNetwork.Initialize([NotNull] string playerName, [NotNull] Rooms rooms, [NotNull] PlayerChannels playerChannels, [NotNull] RoomChannels roomChannels, CodecSettings codecSettings)
        {
            if (playerName == null)
                throw new ArgumentNullException("playerName");
            if (rooms == null)
                throw new ArgumentNullException("rooms");
            if (playerChannels == null)
                throw new ArgumentNullException("playerChannels");
            if (roomChannels == null)
                throw new ArgumentNullException("roomChannels");

            PlayerName = playerName;
            Rooms = rooms;
            PlayerChannels = playerChannels;
            RoomChannels = roomChannels;
            CodecSettings = codecSettings;

            Profiler.BeginSample("virtual void Initialize");
            Initialize();
            Profiler.EndSample();

            IsInitialized = true;
        }
        
        protected virtual void Update()
        {
            if (!IsInitialized)
                return;

            Profiler.BeginSample("Load State");
            LoadState();
            Profiler.EndSample();

            Profiler.BeginSample("Update State");
            _state.Update();
            Profiler.EndSample();
        }

        private void LoadState()
        {
            while (_nextStates.Count > 0)
                ChangeState(_nextStates.Dequeue());
        }

        protected virtual void OnDisable()
        {
            Stop();
            LoadState();
        }

        public void Stop()
        {
            _nextStates.Enqueue(new Idle(this));
        }

        #region RunAsX
        /// <summary>
        /// Stops the network session (if there is one active) and transitions to a new session with this computer as a mixed client/server
        /// </summary>
        /// <param name="serverParameters"></param>
        /// <param name="clientParameters"></param>
        protected void RunAsHost(TServerParam serverParameters, TClientParam clientParameters)
        {
            _nextStates.Enqueue(new Session(this, NetworkMode.Host, serverParameters, clientParameters));
        }

        /// <summary>
        /// Stops the network session (if there is one active) and transitions to a new session with this computer as a client
        /// </summary>
        /// <param name="clientParameters"></param>
        protected void RunAsClient(TClientParam clientParameters)
        {
            _nextStates.Enqueue(new Session(this, NetworkMode.Client, default(TServerParam), clientParameters));
        }

        /// <summary>
        /// Stops the network session (if there is one active) and transitions to a new session with this computer as a dedicated server (no audio capture or playback)
        /// </summary>
        /// <param name="serverParameters"></param>
        protected void RunAsDedicatedServer(TServerParam serverParameters)
        {
            _nextStates.Enqueue(new Session(this, NetworkMode.DedicatedServer, serverParameters, default(TClientParam)));
        }
        #endregion

        private void ChangeState(IState newState)
        {
            _state.Exit();
            _state = newState;
            _state.Enter();
        }

        private void StartServer([CanBeNull] TServerParam connectParams)
        {
            if (Server != null)
            {
                throw Log.CreatePossibleBugException(
                    "Attempted to start the network server while the server is already running",
                    "680CB0B1-1F2C-4EB2-A249-3EDD513354B9"
                );
            }

            Server = CreateServer(connectParams);
            Server.Connect();
        }

        private void StopServer()
        {
            if (Server == null)
            {
                throw Log.CreatePossibleBugException(
                    "Attempted to stop the network server while the server is not running",
                    "BCA52BAC-DE86-4037-9C7B-508D1798E50B"
                );
            }

            try
            {
                Server.Disconnect();
            }
            catch (Exception e)
            {
                Log.Error("Encountered error shutting down server: '{0}'", e.Message);
            }

            Server = null;
        }

        private void StartClient([CanBeNull] TClientParam connectParams)
        {
            if (Client != null)
            {
                throw Log.CreatePossibleBugException(
                    "Attempted to start client while the client is already running",
                    "0AEB8FC5-025F-46F5-969A-B792D2E84626"
                );
            }

            Client = CreateClient(connectParams);

            Log.Trace("Subscribing to client events");

            Client.PlayerJoined += OnPlayerJoined;
            Client.PlayerLeft += OnPlayerLeft;
            Client.PlayerEnteredRoom += OnPlayerEnteredRoom;
            Client.PlayerExitedRoom += OnPlayerExitedRoom;
            Client.VoicePacketReceived += OnVoicePacketReceived;
            Client.TextMessageReceived += OnTextPacketReceived;
            Client.PlayerStartedSpeaking += OnPlayerStartedSpeaking;
            Client.PlayerStoppedSpeaking += OnPlayerStoppedSpeaking;

            Client.Connect();
        }

        private void StopClient()
        {
            if (Client == null)
            {
                throw Log.CreatePossibleBugException(
                    "Attempted to stop the client while the client is not running",
                    "F44A101A-6EF3-4668-9E29-2447B0137921"
                );
            }

            try
            {
                Client.Disconnect();
            }
            catch (Exception e)
            {
                Log.Error("Encountered error shutting down client: '{0}'", e.Message);
            }

            Log.Trace("Unsubscribing from client events");

            Client.PlayerJoined -= OnPlayerJoined;
            Client.PlayerLeft -= OnPlayerLeft;
            Client.VoicePacketReceived -= OnVoicePacketReceived;
            Client.TextMessageReceived -= OnTextPacketReceived;
            Client.PlayerStartedSpeaking -= OnPlayerStartedSpeaking;
            Client.PlayerStoppedSpeaking -= OnPlayerStoppedSpeaking;

            Client = null;
        }

        public void SendVoice(ArraySegment<byte> data)
        {
            if (Client != null)
                Client.SendVoiceData(data);
        }

        public void SendText(string data, ChannelType recipientType, string recipientId)
        {
            if (Client != null)
                Client.SendTextData(data, recipientType, recipientId);
        }

        #region event invokers
        private void OnPlayerJoined(string obj, CodecSettings codecSettings)
        {
            var handler = PlayerJoined;
            if (handler != null) handler(obj, codecSettings);
        }

        private void OnPlayerLeft(string obj)
        {
            var handler = PlayerLeft;
            if (handler != null) handler(obj);
        }

        private void OnPlayerEnteredRoom(RoomEvent evt)
        {
            var handler = PlayerEnteredRoom;
            if (handler != null) handler(evt);
        }

        private void OnPlayerExitedRoom(RoomEvent evt)
        {
            var handler = PlayerExitedRoom;
            if (handler != null) handler(evt);
        }

        private void OnVoicePacketReceived(VoicePacket obj)
        {
            var handler = VoicePacketReceived;
            if (handler != null) handler(obj);
        }

        private void OnTextPacketReceived(TextMessage obj)
        {
            var handler = TextPacketReceived;
            if (handler != null) handler(obj);
        }

        private void OnPlayerStartedSpeaking(string obj)
        {
            var handler = PlayerStartedSpeaking;
            if (handler != null) handler(obj);
        }

        private void OnPlayerStoppedSpeaking(string obj)
        {
            var handler = PlayerStoppedSpeaking;
            if (handler != null) handler(obj);
        }

        private void OnModeChanged(NetworkMode obj)
        {
            var handler = ModeChanged;
            if (handler != null) handler(obj);
        }
        #endregion

        /// <summary>
        /// Draw an inspector GUI for this network
        /// </summary>
        public void OnInspectorGui()
        {
#if UNITY_EDITOR
            string mode = "None";
            if (Mode == NetworkMode.Host)
                mode = "Server & Client";
            else if (Mode == NetworkMode.Client)
                mode = "Client";
            else if (Mode == NetworkMode.DedicatedServer)
                mode = "Server";

            EditorGUILayout.LabelField("Mode", mode);

            if (!Mode.IsServerEnabled() && !Mode.IsClientEnabled())
                return;
            
            EditorGUILayout.LabelField("Connection Status", Status.ToString());

            EditorGUILayout.LabelField("Received");
            EditorGUI.indentLevel++;
            try
            {
                if (Client != null)
                {
                    EditorGUILayout.LabelField("Client");
                    EditorGUI.indentLevel++;
                    try
                    {
                        EditorGUILayout.LabelField("Handshake Response", Client.RecvHandshakeResponse.ToString());
                        EditorGUILayout.LabelField("Handshake P2P", Client.RecvHandshakeP2P.ToString());
                        EditorGUILayout.LabelField("Client State", Client.RecvClientState.ToString());
                        EditorGUILayout.LabelField("Join/Leave Channel", Client.RecvDeltaState.ToString());
                        EditorGUILayout.LabelField("Remove Client", Client.RecvRemoveClient.ToString());
                        EditorGUILayout.LabelField("Voice Data", Client.RecvVoiceData.ToString());
                        EditorGUILayout.LabelField("Text Data", Client.RecvTextData.ToString());

                        uint totalPackets, totalBytes, totalBytesPerSecond;
                        TrafficCounter.Combine(
                            out totalPackets, out totalBytes, out totalBytesPerSecond,
                            Client.RecvHandshakeResponse,
                            Client.RecvHandshakeP2P,
                            Client.RecvClientState,
                            Client.RecvDeltaState,
                            Client.RecvRemoveClient,
                            Client.RecvVoiceData,
                            Client.RecvTextData
                        );
                        EditorGUILayout.LabelField("TOTAL", TrafficCounter.Format(totalPackets, totalBytes, totalBytesPerSecond));
                    }
                    finally
                    {
                        EditorGUI.indentLevel--;
                    }
                }

                if (Server != null)
                {
                    EditorGUILayout.LabelField("Server");
                    EditorGUI.indentLevel++;
                    try
                    {
                        EditorGUILayout.LabelField("Handshake", Server.RecvHandshakeRequest.ToString());
                        EditorGUILayout.LabelField("Client State", Server.RecvClientState.ToString());
                        EditorGUILayout.LabelField("P2P Relay", Server.RecvPacketRelay.ToString());
                        EditorGUILayout.LabelField("Delta Channel State", Server.RecvDeltaChannelState.ToString());

                        uint totalPackets, totalBytes, totalBytesPerSecond;
                        TrafficCounter.Combine(out totalPackets, out totalBytes, out totalBytesPerSecond, Server.RecvHandshakeRequest, Server.RecvClientState, Server.RecvPacketRelay, Server.RecvDeltaChannelState);
                        EditorGUILayout.LabelField("TOTAL", TrafficCounter.Format(totalPackets, totalBytes, totalBytesPerSecond));
                    }
                    finally
                    {
                        EditorGUI.indentLevel--;
                    }
                }
            }
            finally
            {
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.LabelField("Sent");
            EditorGUI.indentLevel++;
            try
            {
                if (Server != null)
                    EditorGUILayout.LabelField("Server To Clients", Server.SentTraffic.ToString());
                if (Client != null)
                    EditorGUILayout.LabelField("Client To Others", Client.SentServerTraffic.ToString());

            }
            finally
            {
                EditorGUI.indentLevel--;
            }
#endif
        }
    }
}
