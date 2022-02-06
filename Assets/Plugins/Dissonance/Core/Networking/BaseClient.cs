using System;
using System.Collections.Generic;
using Dissonance.Datastructures;
using Dissonance.Extensions;
using Dissonance.Networking.Client;
using Dissonance.Threading;
using JetBrains.Annotations;

namespace Dissonance.Networking
{
    public abstract class BaseClient<TServer, TClient, TPeer>
        : IClient<TPeer>
        where TPeer : struct, IEquatable<TPeer>
        where TServer : BaseServer<TServer, TClient, TPeer>
        where TClient : BaseClient<TServer, TClient, TPeer>
    {
        #region fields and properties
        protected readonly Log Log;

        private bool _disconnected;
        private bool _error;

        public bool IsConnected { get { return !_error && !_disconnected && _serverNegotiator.State == ConnectionState.Connected; } }

        private readonly EventQueue _events;
        public event Action<string, CodecSettings> PlayerJoined
        {
            add { _events.PlayerJoined += value; }
            remove { _events.PlayerJoined -= value; }
        }
        public event Action<string> PlayerLeft
        {
            add { _events.PlayerLeft += value; }
            remove { _events.PlayerLeft -= value; }
        }
        public event Action<RoomEvent> PlayerEnteredRoom
        {
            add { _events.PlayerEnteredRoom += value; }
            remove { _events.PlayerEnteredRoom -= value; }
        }
        public event Action<RoomEvent> PlayerExitedRoom
        {
            add { _events.PlayerExitedRoom += value; }
            remove { _events.PlayerExitedRoom -= value; }
        }
        public event Action<VoicePacket> VoicePacketReceived
        {
            add { _events.VoicePacketReceived += value; }
            remove { _events.VoicePacketReceived -= value; }
        }
        public event Action<TextMessage> TextMessageReceived
        {
            add { _events.TextMessageReceived += value; }
            remove { _events.TextMessageReceived -= value; }
        }
        public event Action<string> PlayerStartedSpeaking
        {
            add { _events.PlayerStartedSpeaking += value; }
            remove { _events.PlayerStartedSpeaking -= value; }
        }
        public event Action<string> PlayerStoppedSpeaking
        {
            add { _events.PlayerStoppedSpeaking += value; }
            remove { _events.PlayerStoppedSpeaking -= value; }
        }

        private readonly SlaveClientCollection<TPeer> _peers;
        private readonly ConnectionNegotiator<TPeer> _serverNegotiator;
        private readonly SendQueue<TPeer> _sendQueue;
        private readonly PacketDelaySimulator _lossSimulator;

        private readonly VoiceReceiver<TPeer> _voiceReceiver;
        private readonly VoiceSender<TPeer> _voiceSender;
        private readonly TextReceiver<TPeer> _textReceiver;
        private readonly TextSender<TPeer> _textSender;

        private readonly TrafficCounter _recvRemoveClient = new TrafficCounter();
        [NotNull] internal TrafficCounter RecvRemoveClient { get { return _recvRemoveClient; } }
        private readonly TrafficCounter _recvVoiceData = new TrafficCounter();
        [NotNull] internal TrafficCounter RecvVoiceData { get { return _recvVoiceData; } }
        private readonly TrafficCounter _recvTextData = new TrafficCounter();
        [NotNull] internal TrafficCounter RecvTextData { get { return _recvTextData; } }
        private readonly TrafficCounter _recvHandshakeResponse = new TrafficCounter();
        [NotNull] internal TrafficCounter RecvHandshakeResponse { get { return _recvHandshakeResponse; } }
        private readonly TrafficCounter _recvHandshakeP2P = new TrafficCounter();
        [NotNull] internal TrafficCounter RecvHandshakeP2P { get { return _recvHandshakeP2P; } }
        private readonly TrafficCounter _recvClientState = new TrafficCounter();
        [NotNull] internal TrafficCounter RecvClientState { get { return _recvClientState; } }
        private readonly TrafficCounter _recvDeltaState = new TrafficCounter();
        [NotNull] internal TrafficCounter RecvDeltaState { get { return _recvDeltaState; } }
        private readonly TrafficCounter _sentServer = new TrafficCounter();
        [NotNull] internal TrafficCounter SentServerTraffic { get { return _sentServer; } }
        #endregion

        #region constructors
        protected BaseClient([NotNull] ICommsNetworkState network)
        {
            if (network == null) throw new ArgumentNullException("network");

            Log = Logs.Create(LogCategory.Network, GetType().Name);

            const int poolSize = 32;
            const int byteBufferSize = 1024;
            const int channelListSize = 8;
            var byteArrayPool = new ReadonlyLockedValue<Pool<byte[]>>(new Pool<byte[]>(poolSize, () => new byte[byteBufferSize]));
            var channelListPool = new ConcurrentPool<List<RemoteChannel>>(poolSize, () => new List<RemoteChannel>(channelListSize));

            _sendQueue = new SendQueue<TPeer>(this, byteArrayPool);
            _serverNegotiator = new ConnectionNegotiator<TPeer>(_sendQueue, network.PlayerName, network.CodecSettings);
            _lossSimulator = new PacketDelaySimulator();

            _events = new EventQueue(byteArrayPool, channelListPool);
            _peers = new SlaveClientCollection<TPeer>(_sendQueue, _serverNegotiator, _events, network.Rooms, network.PlayerName, network.CodecSettings);
            _peers.OnClientJoined += OnAddedClient;
            _peers.OnClientIntroducedP2P += OnMetClient;

            _voiceReceiver = new VoiceReceiver<TPeer>(_serverNegotiator, _peers, _events, network.Rooms, channelListPool);
            _voiceSender = new VoiceSender<TPeer>(_sendQueue, _serverNegotiator, _peers, _events, network.PlayerChannels, network.RoomChannels);

            _textReceiver = new TextReceiver<TPeer>(_events, network.Rooms, _peers);
            _textSender = new TextSender<TPeer>(_sendQueue, _serverNegotiator, _peers);
        }
        #endregion

        #region connect/disconnect
        /// <summary>
        /// Override this to perform any work necessary to join a voice session
        /// </summary>
        public abstract void Connect();

        /// <summary>
        /// Call this once work has been done and we are now in a voice session
        /// </summary>
        protected void Connected()
        {
            _serverNegotiator.Start();
        }

        /// <summary>
        /// Override this to perform any work necessary to leave a voice session
        /// </summary>
        public virtual void Disconnect()
        {
            if (_disconnected)
                return;
            _disconnected = true;

            _sendQueue.Stop();
            _serverNegotiator.Stop();
            _voiceReceiver.Stop();
            _voiceSender.Stop();
            _peers.Stop();

            _events.DispatchEvents();

            Log.Info("Disconnected");
        }

        /// <summary>
        /// Indicate that a fatal error has occured. Client will (attempt to) shut itself down and restart
        /// </summary>
        protected void FatalError(string reason)
        {
            Log.Error(reason);
            _error = true;
        }
        #endregion

        public virtual ClientStatus Update()
        {
            if (_disconnected)
                return ClientStatus.Error;

            //Only run update if we're not in an error branch
            if (!_error)
                _error |= RunUpdate(DateTime.UtcNow);

            //If we encountered an error then disconnect the client
            if (_error)
            {
                Disconnect();
                return ClientStatus.Error;
            }
            else
                return ClientStatus.Ok;
        }

        private bool RunUpdate(DateTime utcNow)
        {
            var error = false;

            try
            {
                //Update negotiator (joining session, setting up ID etc)
                _serverNegotiator.Update(utcNow);

                //Poll network layer for more packets
                ReadMessages();

                //Send messages (put in send queue as part of reading/receiving messages)
                _sendQueue.Update();

                //Update voice receiver (not procesing packets, just general bookkeeping e.g. closing sessions due to timeouts)
                _voiceReceiver.Update(utcNow);
            }
            catch (Exception e)
            {
                Log.Error("Caught fatal error: {0}\nStacktrace: {1}\n", e.Message, e.StackTrace);
                error = true;
            }
            finally
            {
                //Send events to event handlers
                if (_events.DispatchEvents())
                    error = true;
            }

            return error;
        }

        #region send
        /// <summary>
        /// Send a packet of voice data from this client
        /// </summary>
        /// <param name="encodedAudio"></param>
        public void SendVoiceData(ArraySegment<byte> encodedAudio)
        {
            _voiceSender.Send(encodedAudio);
        }

        public void SendTextData(string data, ChannelType type, string recipient)
        {
            _textSender.Send(data, type, recipient);
        }
        #endregion

        #region receive
        public ushort? NetworkReceivedPacket(ArraySegment<byte> data)
        {
            if (_disconnected)
            {
                Log.Warn("Received a packet with a disconnected client, dropping packet");
                return null;
            }

            if (_lossSimulator.ShouldLose(data))
                return null;

            return ProcessReceivedPacket(data);
        }

        private ushort? ProcessReceivedPacket(ArraySegment<byte> data)
        {
            var reader = new PacketReader(data);

            MessageTypes header;
            if (!reader.ReadPacketHeader(out header))
            {
                Log.Warn("Discarding packet - incorrect magic number.");
                return null;
            }

            Log.Trace("Received Packet type: {0}", header);

            switch (header)
            {
                case MessageTypes.VoiceData:
                    if (CheckSessionId(ref reader, header))
                    {
                        _voiceReceiver.ReceiveVoiceData(ref reader);
                        _recvVoiceData.Update(reader.Read.Count);
                    }
                    break;

                case MessageTypes.TextData:
                    if (CheckSessionId(ref reader, header))
                    {
                        _textReceiver.ProcessTextMessage(ref reader);
                        _recvTextData.Update(reader.Read.Count);
                    }
                    break;

                case MessageTypes.HandshakeResponse:
                    _serverNegotiator.ReceiveHandshakeResponseHeader(ref reader);
                    _peers.ReceiveHandshakeResponseBody(ref reader);
                    _recvHandshakeResponse.Update(reader.Read.Count);

                    if (_serverNegotiator.LocalId.HasValue)
                        OnServerAssignedSessionId(_serverNegotiator.SessionId, _serverNegotiator.LocalId.Value);

                    break;

                case MessageTypes.RemoveClient:
                    if (CheckSessionId(ref reader, header))
                    {
                        _peers.ProcessRemoveClient(ref reader);
                        _recvRemoveClient.Update(reader.Read.Count);
                    }
                    break;

                case MessageTypes.ClientState:
                    if (CheckSessionId(ref reader, header))
                    {
                        _peers.ProcessClientState(null, ref reader);
                        _recvClientState.Update(reader.Read.Count);
                    }
                    break;

                case MessageTypes.DeltaChannelState:
                    if (CheckSessionId(ref reader, header))
                    {
                        _peers.ProcessDeltaChannelState(ref reader);
                        _recvDeltaState.Update(reader.Read.Count);
                    }
                    break;

                case MessageTypes.ErrorWrongSession:
                    {
                        var session = reader.ReadUInt32();
                        if (_serverNegotiator.SessionId != session)
                            FatalError(string.Format("Kicked from session - wrong session ID. Mine:{0} Theirs:{1}", _serverNegotiator.SessionId, session));
                    }
                    break;

                case MessageTypes.HandshakeP2P:
                    if (CheckSessionId(ref reader, header))
                    {
                        ushort id;
                        reader.ReadhandshakeP2P(out id);

                        _recvHandshakeP2P.Update(reader.Read.Count);

                        return id;
                    }
                    break;

                case MessageTypes.ServerRelayReliable:
                case MessageTypes.ServerRelayUnreliable:
                case MessageTypes.HandshakeRequest:
                    Log.Error("Client received packet '{0}'. This should only ever be received by the server", header);
                    break;

                default:
                    Log.Debug("Ignoring a packet with an unknown header: '{0}'", header);
                    break;
            }

            return null;
        }

        private bool CheckSessionId(ref PacketReader reader, MessageTypes type)
        {
            var session = reader.ReadUInt32();

            if (_serverNegotiator.SessionId != session)
            {
                Log.Warn("Received a '{0}' packet with incorrect session ID. Expected {1}, got {2}", type, _serverNegotiator.SessionId, session);
                return false;
            }

            return true;
        }
        #endregion

        #region abstract
        /// <summary>
        /// Read messages from the network layer and call `NetworkReceivedPacket` with each packet
        /// </summary>
        protected abstract void ReadMessages();

        /// <summary>
        /// Send a reliable message to the server
        /// </summary>
        /// <param name="packet"></param>
        protected abstract void SendReliable(ArraySegment<byte> packet);

        /// <summary>
        /// send an unreliable message to the server
        /// </summary>
        /// <param name="packet"></param>
        protected abstract void SendUnreliable(ArraySegment<byte> packet);
        #endregion

        #region p2p
        /// <summary>
        /// Send a packet directly to a set of peers. Override this to implement p2p sending in network integrations. The base implementation sends the packet using server relay.
        /// </summary>
        /// <param name="destinations"></param>
        /// <param name="packet"></param>
        protected virtual void SendReliableP2P([NotNull] List<ClientInfo<TPeer?>> destinations, ArraySegment<byte> packet)
        {
            //Since we're calling the base implementation of send P2P we'll just relay this packet by the server

            // This packet may have been sent P2P, but we still want to update the sent counter
            SentServerTraffic.Update(packet.Count);

            if (destinations.Count > 0)
            {
                //Get a buffer to write the relay packet into
                var buffer = _sendQueue.GetSendBuffer();
                {
                    //Write relay packet
                    var writer = new PacketWriter(buffer);
                    writer.WriteRelay(_serverNegotiator.SessionId, destinations, packet, true);

                    //Send relay packet
                    ((IClient<TPeer>)this).SendReliable(writer.Written);
                }
                //Recycle relay buffer
                _sendQueue.RecycleSendBuffer(buffer);
            }
        }

        /// <summary>
        /// Send a packet directly to a set of peers. Override this to implement p2p sending in network integrations. The base implementation sends the packet using server relay.
        /// </summary>
        /// <param name="destinations"></param>
        /// <param name="packet"></param>
        protected virtual void SendUnreliableP2P([NotNull] List<ClientInfo<TPeer?>> destinations, ArraySegment<byte> packet)
        {
            //Since we're calling the base implementation of send P2P we'll just relay this packet by the server

            // This packet may have been sent P2P, but we still want to update the sent counter
            SentServerTraffic.Update(packet.Count);

            if (destinations.Count > 0)
            {
                //Get a buffer to write the relay packet into
                var buffer = _sendQueue.GetSendBuffer();
                {
                    //Write relay packet
                    var writer = new PacketWriter(buffer);
                    writer.WriteRelay(_serverNegotiator.SessionId, destinations, packet, false);

                    //Send relay packet
                    ((IClient<TPeer>)this).SendUnreliable(writer.Written);
                }
                //Recycle relay buffer
                _sendQueue.RecycleSendBuffer(buffer);
            }
        }

        protected virtual void OnServerAssignedSessionId(uint session, ushort id)
        {

        }

        /// <summary>
        /// Called when a new client is added into the session (we may not know how to directly to talk to them yet, but server relay is available)
        /// </summary>
        /// <param name="client"></param>
        protected virtual void OnAddedClient([NotNull] ClientInfo<TPeer?> client)
        {
        }

        /// <summary>
        /// Called when a client has had a connection assigned, meaning we can now communicate with them directly
        /// </summary>
        /// <param name="client"></param>
        protected virtual void OnMetClient([NotNull] ClientInfo<TPeer?> client)
        {
            //Sanity check we're really in a session
            if (Log.AssertAndLogError(IsConnected, "704E1AA4-1802-4FA6-B8BD-4CB780DD82F2", "Attempted to call IntroduceP2P before connected to Dissonance session")) return;
            if (Log.AssertAndLogError(_serverNegotiator.LocalId.HasValue, "9B611EAA-B2D9-4C96-A619-976B61F5A76B", "No LocalId assigned even though server negotiator is connected")) return;

            //Check that we can communicate directly with this peer
            if (client.Connection.HasValue)
            {
                //write a p2p handshake to introduce ourselves back to them
                var packet = new PacketWriter(_sendQueue.GetSendBuffer()).WriteHandshakeP2P(_serverNegotiator.SessionId, _serverNegotiator.LocalId.Value).Written;

                //Send the packet back directly
                //We allocate a list here but that's ok, this only happens once per remote client to connect.
                _sendQueue.EnqueueReliableP2P(
                    _serverNegotiator.LocalId.Value,
                    new List<ClientInfo<TPeer?>> { client },
                    packet
                );
            }
        }

        /// <summary>
        /// Call this to inform Dissonance how to directly contact a peer
        /// </summary>
        /// <param name="id"></param>
        /// <param name="connection"></param>
        protected void ReceiveHandshakeP2P(ushort id, TPeer connection)
        {
            if (!IsConnected)
            {
                Log.Error("Attempted to call IntroduceP2P before connected to Dissonance session");
                return;
            }

            _peers.IntroduceP2P(id, connection);
        }

        [NotNull] protected static byte[] WriteHandshakeP2P(uint sessionId, ushort clientId)
        {
            var segment = new PacketWriter(new byte[9])
                .WriteHandshakeP2P(sessionId, clientId)
                .Written;

            return segment.ToArray();
        }
        #endregion

        #region IClient explicit impl
        void IClient<TPeer>.SendReliable(ArraySegment<byte> packet)
        {
            SendReliable(packet);
        }

        void IClient<TPeer>.SendUnreliable(ArraySegment<byte> packet)
        {
            SendUnreliable(packet);
        }

        void IClient<TPeer>.SendReliableP2P([NotNull] List<ClientInfo<TPeer?>> destinations, ArraySegment<byte> packet)
        {
            SendReliableP2P(destinations, packet);
        }

        void IClient<TPeer>.SendUnreliableP2P([NotNull] List<ClientInfo<TPeer?>> destinations, ArraySegment<byte> packet)
        {
            SendUnreliableP2P(destinations, packet);
        }
        #endregion
    }
}
