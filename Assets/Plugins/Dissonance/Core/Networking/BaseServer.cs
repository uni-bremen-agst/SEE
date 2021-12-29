using System;
using System.Collections.Generic;
using Dissonance.Networking.Server;
using Dissonance.Networking.Server.Admin;
using JetBrains.Annotations;

namespace Dissonance.Networking
{
    public abstract class BaseServer<TServer, TClient, TPeer>
        : IServer<TPeer>
        where TPeer : struct, IEquatable<TPeer>
        where TServer : BaseServer<TServer, TClient, TPeer>
        where TClient : BaseClient<TServer, TClient, TPeer>
    {
        #region fields and properties
        protected readonly Log Log;

        private bool _disconnected;
        private bool _error;

        internal TrafficCounter RecvHandshakeRequest { get; private set; }
        internal TrafficCounter RecvClientState { get; private set; }
        internal TrafficCounter RecvPacketRelay { get; private set; }
        internal TrafficCounter RecvDeltaChannelState { get; private set; }
        internal TrafficCounter SentTraffic { get; private set; }

        private readonly ServerRelay<TPeer> _relay;
        private readonly BroadcastingClientCollection<TPeer> _clients;

        private readonly uint _sessionId;
        public uint SessionId { get { return _sessionId; } }

#if !DARK_RIFT_SERVER
        private readonly ServerAdmin<TServer, TClient, TPeer> serverAdmin;
        [NotNull] public IServerAdmin ServerAdmin
        {
            get { return serverAdmin; }
        }
#endif
        #endregion

        #region constructors
        protected BaseServer()
        {
            Log = Logs.Create(LogCategory.Network, GetType().Name);

            RecvClientState = new TrafficCounter();
            RecvHandshakeRequest = new TrafficCounter();
            RecvPacketRelay = new TrafficCounter();
            SentTraffic = new TrafficCounter();
            RecvDeltaChannelState = new TrafficCounter();

            var rand = new Random();
            while (_sessionId == 0)
                _sessionId = unchecked((uint)rand.Next());

            _clients = new BroadcastingClientCollection<TPeer>(this);
            _relay = new ServerRelay<TPeer>(this, _clients);

#if !DARK_RIFT_SERVER
            serverAdmin = new ServerAdmin<TServer, TClient, TPeer>((TServer)this);
            _clients.OnClientJoined += serverAdmin.InvokeOnClientJoined;
            _clients.OnClientLeft += serverAdmin.InvokeOnClientLeft;
            _clients.OnClientEnteredRoomEvent += serverAdmin.InvokeOnClientEnteredRoom;
            _clients.OnClientExitedRoomEvent += serverAdmin.InvokeOnClientExitedRoom;
            _relay.OnRelayingPacket += serverAdmin.InvokeOnRelayingPacket;
#endif

            Log.Info("Created server with SessionId:{0}", _sessionId);
        }
        #endregion

        /// <summary>
        /// Perform any initial work required to connect
        /// </summary>
        public virtual void Connect()
        {
            Log.Info("Connected");
        }

        /// <summary>
        /// Perform any teardown work required to disconnect
        /// </summary>
        public virtual void Disconnect()
        {
            if (_disconnected) return;
            _disconnected = true;

            _clients.Stop();

            Log.Info("Disconnected");
        }

        /// <summary>
        /// Indicate that a fatal error has occured. Server will (attempt to) shut itself down and restart
        /// </summary>
        protected void FatalError([NotNull] string reason)
        {
            Log.Error(reason);
            _error = true;
        }

        /// <summary>
        /// This must be called by the extending network integration implementation when a client disconnects from the session
        /// </summary>
        /// <param name="connection"></param>
        protected void ClientDisconnected(TPeer connection)
        {
            Log.Debug("Received disconnection event for peer '{0}'", connection);

            _clients.RemoveClient(connection);
        }

        public virtual ServerState Update()
        {
            if (_disconnected)
                return ServerState.Error;

            //Only run update if we're not in an error branch
            _error |= RunUpdate();

            //If we encountered an error then disconnect the client
            if (_error)
            {
                Disconnect();
                return ServerState.Error;
            }
            else
                return ServerState.Ok;
        }

        private bool RunUpdate()
        {
            try
            {
                ReadMessages();
                return false;
            }
            catch (Exception e)
            {
                Log.Error("Caught fatal error: {0}\nStacktrace: {1}\n", e.Message, e.StackTrace);
                return true;
            }
        }

        #region sending
        /// <summary>
        /// Send a control packet (reliable, in-order) to the given destination
        /// </summary>
        /// <param name="connection">Destination</param>
        /// <param name="packet">Packet to send</param>
        protected abstract void SendReliable(TPeer connection, ArraySegment<byte> packet);

        /// <summary>
        /// Send an unreliable packet (unreliable, unordered) to the given destination
        /// </summary>
        /// <param name="connection">Destination</param>
        /// <param name="packet">Packet to send</param>
        protected abstract void SendUnreliable(TPeer connection, ArraySegment<byte> packet);

        /// <summary>
        /// Send an unreliable packet (unreliable, unordered) to the given set of destinations.
        /// Remove peers from the connections list if you send to them and then call base with the rest.
        /// </summary>
        /// <param name="connections"></param>
        /// <param name="packet"></param>
        public virtual void SendUnreliable([NotNull] List<TPeer> connections, ArraySegment<byte> packet)
        {
            if (connections == null) throw new ArgumentNullException("connections");

            SentTraffic.Update(packet.Count * connections.Count);

            for (var i = 0; i < connections.Count; i++)
                SendUnreliable(connections[i], packet);
        }

        /// <summary>
        /// Send a control packet (reliable, in-order) to the given set of destinations.
        /// Remove peers from the connections list if you send to them and then call base with the rest.
        /// </summary>
        /// <param name="connections"></param>
        /// <param name="packet"></param>
        public virtual void SendReliable([NotNull] List<TPeer> connections, ArraySegment<byte> packet)
        {
            if (connections == null) throw new ArgumentNullException("connections");

            SentTraffic.Update(packet.Count * connections.Count);

            for (var i = 0; i < connections.Count; i++)
                SendReliable(connections[i], packet);
        }
        #endregion

        #region IServer explicit impl
        void IServer<TPeer>.SendReliable(TPeer connection, ArraySegment<byte> packet)
        {
            SentTraffic.Update(packet.Count);
            SendReliable(connection, packet);
        }

        void IServer<TPeer>.SendUnreliable(List<TPeer> connections, ArraySegment<byte> packet)
        {
            SendUnreliable(connections, packet);
        }

        void IServer<TPeer>.SendReliable(List<TPeer> connections, ArraySegment<byte> packet)
        {
            SendReliable(connections, packet);
        }
        #endregion

        #region packet processing
        /// <summary>
        /// Read messages (call NetworkReceivedPacket with all messages)
        /// </summary>
        protected abstract void ReadMessages();

        /// <summary>
        /// Receive a packet from the network for dissonance
        /// </summary>
        /// <param name="source">A value identifying where this packet came from (same ID will be used for sending)</param>
        /// <param name="data">Packet received</param>
        public void NetworkReceivedPacket(TPeer source, ArraySegment<byte> data)
        {
            if (_disconnected)
            {
                Log.Warn("Received a packet with a disconnected server, dropping packet");
                return;
            }

            var reader = new PacketReader(data);

            MessageTypes header;
            if (!reader.ReadPacketHeader(out header))
            {
                Log.Warn("Discarding packet - incorrect magic number.");
                return;
            }

            switch (header)
            {
                case MessageTypes.HandshakeRequest:
                    RecvHandshakeRequest.Update(data.Count);
                    _clients.ProcessHandshakeRequest(source, ref reader);
                    break;

                case MessageTypes.ClientState:
                    if (CheckSessionId(ref reader, source))
                    {
                        RecvClientState.Update(data.Count);
                        _clients.ProcessClientState(source, ref reader);
                    }
                    break;

                case MessageTypes.ServerRelayReliable:
                case MessageTypes.ServerRelayUnreliable:
                    if (CheckSessionId(ref reader, source))
                    {
                        RecvPacketRelay.Update(data.Count);
                        _relay.ProcessPacketRelay(ref reader, header == MessageTypes.ServerRelayReliable, source);
                    }
                    break;

                case MessageTypes.DeltaChannelState:
                    if (CheckSessionId(ref reader, source))
                    {
                        RecvDeltaChannelState.Update(data.Count);
                        _clients.ProcessDeltaChannelState(ref reader);
                    }
                    break;

                case MessageTypes.HandshakeP2P:
                case MessageTypes.RemoveClient:
                case MessageTypes.VoiceData:
                case MessageTypes.TextData:
                case MessageTypes.HandshakeResponse:
                case MessageTypes.ErrorWrongSession:
                    Log.Error("Server received packet '{0}'. This should only ever be received by the client", header);
                    break;

                default:
                    Log.Error("Ignoring a packet with an unknown header: '{0}'", header);
                    break;
            }
        }

        private bool CheckSessionId(ref PacketReader reader, TPeer source)
        {
            var session = reader.ReadUInt32();
            if (session != _sessionId)
            {
                Log.Warn("Received a packet with incorrect session ID. Expected {0}, got {1}. Resetting client.", _sessionId, session);

                //Send back a packet forcing this client to disconnect. The client may reconnect, in which case it will re-run the entire handshake and acquire the correct session ID.
                var writer = new PacketWriter(new byte[7]);
                writer.WriteErrorWrongSession(_sessionId);
                SendUnreliable(source, writer.Written);

                return false;
            }

            return true;
        }
        #endregion

        /// <summary>
        /// Called whenever a new client joins the session. Override to perform some work on this event
        /// </summary>
        /// <param name="client"></param>
        protected virtual void AddClient(ClientInfo<TPeer> client)
        {
        }

        void IServer<TPeer>.AddClient(ClientInfo<TPeer> client)
        {
            AddClient(client);
        }
    }
}
