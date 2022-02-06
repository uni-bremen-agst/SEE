using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Dissonance.Networking.Server
{
    /// <summary>
    /// A client collection which assigns peer IDs and broadcasts all state changes
    /// </summary>
    /// <typeparam name="TPeer"></typeparam>
    internal class BroadcastingClientCollection<TPeer>
        : BaseClientCollection<TPeer>
    {
        #region fields and properties
        private readonly IServer<TPeer> _server;

        private readonly byte[] _tmpSendBuffer = new byte[1024];
        private readonly List<TPeer> _tmpConnectionBuffer = new List<TPeer>();
        private readonly List<ClientInfo<TPeer>> _tmpClientBuffer = new List<ClientInfo<TPeer>>();
        private readonly List<ClientInfo<TPeer>> _tmpClientBufferHandshake = new List<ClientInfo<TPeer>>();
        private readonly Dictionary<string, List<ClientInfo<TPeer>>> _tmpClientByRoomsBufferHandshake = new Dictionary<string, List<ClientInfo<TPeer>>>();
        #endregion

        #region constructor
        public BroadcastingClientCollection(IServer<TPeer> server)
        {
            _server = server;
        }
        #endregion

        protected override void OnRemovedClient(ClientInfo<TPeer> client)
        {
            base.OnRemovedClient(client);

            //Write the removal message
            var writer = new PacketWriter(_tmpSendBuffer);
            writer.WriteRemoveClient(_server.SessionId, client.PlayerId);

            //Broadcast to all peers
            Broadcast(writer.Written);
        }

        protected override void OnAddedClient(ClientInfo<TPeer> client)
        {
            base.OnAddedClient(client);

            _server.AddClient(client);
        }

        #region packet processing
        public void ProcessHandshakeRequest(TPeer source, ref PacketReader reader)
        {
            //Parse packet
            string name;
            CodecSettings codecSettings;
            reader.ReadHandshakeRequest(out name, out codecSettings);

            // Validate that we have a player name, and ignore if not
            if (name == null)
            {
                Log.Warn("Ignoring a handshake with a null player name");
                return;
            }

            // It's possible that the client is already in the session. Get the information for this client in two different ways: by name and by connection.
            // - If they are the same then this client has sent another handshake request even though it is already in the session. Re-Initialise it.
            // - If they are different then this client lost connection to the network session and has reconnected. Re-Initialise it.
            ClientInfo<TPeer> currentInfoByName;
            ClientInfo<TPeer> currentInfoByConn;
            if (TryGetClientInfoByName(name, out currentInfoByName) | TryFindClientByConnection(source, out currentInfoByConn))
            {
                if (EqualityComparer<ClientInfo<TPeer>>.Default.Equals(currentInfoByName, currentInfoByConn))
                {
                    // ClientInfo is the same! Client resent it's handshake request without losing connection.
                    Log.Debug("Client '{0}' handshake received but client is already connected! Disconnecting client '{1}', connecting '{2}'",
                        name,
                        currentInfoByConn,
                        source
                    );

                    // Remove the client from the collection so that it will be re-initialised.
                    if (currentInfoByConn != null && currentInfoByConn.IsConnected)
                        RemoveClient(currentInfoByConn);
                }
                else
                {
                    // ClientInfo is different! Client lost connection and reconnected.
                    Log.Debug("Client '{0}' handshake received but client is already connected with a different connection! Disconnecting client '{1}' & '{2}', connecting '{3}'",
                        name,
                        currentInfoByConn,
                        currentInfoByName,
                        source
                    );

                    // Remove both clients from the collection so that it will be re-initialised.
                    if (currentInfoByConn != null && currentInfoByConn.IsConnected)
                        RemoveClient(currentInfoByConn);
                    if (currentInfoByName != null && currentInfoByName.IsConnected)
                        RemoveClient(currentInfoByName);
                }
            }

            //Get or register the ID for this client
            var id = PlayerIds.GetId(name) ?? PlayerIds.Register(name);
            var info = GetOrCreateClientInfo(id, name, codecSettings, source);

            // Send the handshake response - but with _no_ clients in the list, as if the session has no one in it. The handshake packet previously listed everyone in the session and what rooms they're in. However,
            // this could cause the packet to become very large and eventually it would overflow a buffers. Rather than expand all the buffers (which could break network integrations) we're not going to send any of
            // that data in the handshake (as if it's an empty session) and then we'll immediately send all the client data in separate packets.
            var writer = new PacketWriter(_tmpSendBuffer);
            _tmpClientBufferHandshake.Clear();
            writer.WriteHandshakeResponse(_server.SessionId, info.PlayerId, _tmpClientBufferHandshake, _tmpClientByRoomsBufferHandshake);
            _server.SendReliable(source, writer.Written);

            //Send individual client state messages for all clients in the session
            GetClients(_tmpClientBufferHandshake);
            for (var i = 0; i < _tmpClientBufferHandshake.Count; i++)
                SendFakeClientState(source, _tmpClientBufferHandshake[i]);
        }

        private void SendFakeClientState(TPeer destination, [NotNull] ClientInfo<TPeer> clientInfo)
        {
            var writer = new PacketWriter(_tmpSendBuffer);
            writer.WriteClientState(_server.SessionId, clientInfo.PlayerName, clientInfo.PlayerId, clientInfo.CodecSettings, clientInfo.Rooms);
            _server.SendReliable(destination, writer.Written);
        }

        public override void ProcessClientState(TPeer source, ref PacketReader reader)
        {
            //Rebroadcast packet to all peers so they can update their state
            Broadcast(reader.All);

            base.ProcessClientState(source, ref reader);
        }

        public override void ProcessDeltaChannelState(ref PacketReader reader)
        {
            //Rebroadcast packet to all peers so they can update their state
            Broadcast(reader.All);

            base.ProcessDeltaChannelState(ref reader);
        }
        #endregion

        private void Broadcast(ArraySegment<byte> packet)
        {
            _tmpConnectionBuffer.Clear();
            _tmpClientBuffer.Clear();

            //Get all client infos
            GetClients(_tmpClientBuffer);

            //Now get all connections (except the one excluded one, if applicable)
            for (var i = 0; i < _tmpClientBuffer.Count; i++)
            {
                var c = _tmpClientBuffer[i];
                _tmpConnectionBuffer.Add(c.Connection);
            }

            //Broadcast to all those connections
            _server.SendReliable(_tmpConnectionBuffer, packet);

            _tmpConnectionBuffer.Clear();
            _tmpClientBuffer.Clear();
        }

        public void RemoveClient(TPeer connection)
        {
            ClientInfo<TPeer> info;
            if (TryFindClientByConnection(connection, out info))
                RemoveClient(info);
        }
    }
}
