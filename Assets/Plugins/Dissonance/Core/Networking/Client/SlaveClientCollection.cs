using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Dissonance.Networking.Client
{
    internal class SlaveClientCollection<TPeer>
        : BaseClientCollection<TPeer?>
        where TPeer : struct
    {
        #region fields and properties
        private readonly ISendQueue<TPeer> _sender;
        private readonly ISession _session;
        private readonly EventQueue _events;
        private readonly Rooms _localRooms;
        private readonly string _playerName;
        private readonly CodecSettings _codecSettings;

        public event Action<ClientInfo<TPeer?>> OnClientIntroducedP2P;

        private readonly List<KeyValuePair<ushort, TPeer>> _pendingIntroductions = new List<KeyValuePair<ushort, TPeer>>();
        #endregion

        #region constructors
        public SlaveClientCollection([NotNull] ISendQueue<TPeer> sender, [NotNull] ISession session, [NotNull] EventQueue events, [NotNull] Rooms localRooms, [NotNull] string playerName, CodecSettings codecSettings)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (sender == null) throw new ArgumentNullException("sender");
            if (events == null) throw new ArgumentNullException("events");
            if (localRooms == null) throw new ArgumentNullException("localRooms");
            if (playerName == null) throw new ArgumentNullException("playerName");

            _session = session;
            _sender = sender;
            _events = events;
            _localRooms = localRooms;
            _playerName = playerName;
            _codecSettings = codecSettings;
        }
        #endregion

        protected override void OnAddedClient(ClientInfo<TPeer?> client)
        {
            _events.EnqueuePlayerJoined(client.PlayerName, client.CodecSettings);

            //Set connection property for peers who were introduced before we knew they had joined
            var done = false;
            for (var i = _pendingIntroductions.Count - 1; i >= 0; i--)
            {
                if (_pendingIntroductions[i].Key == client.PlayerId)
                {
                    if (!done)
                    {
                        var newlyMet = !client.Connection.HasValue;
                        client.Connection = _pendingIntroductions[i].Value;

                        if (newlyMet && OnClientIntroducedP2P != null)
                            OnClientIntroducedP2P(client);

                        Log.Debug("IntroduceP2P Eventual Success for '{0}' at '{1}'", client.PlayerId, client.Connection);
                        done = true;
                    }
                    _pendingIntroductions.RemoveAt(i);
                }
            }

            base.OnAddedClient(client);
        }

        protected override void OnRemovedClient(ClientInfo<TPeer?> client)
        {
            //Do not raise a leave event for the local player
            if (client.PlayerName != _playerName)
                _events.EnqueuePlayerLeft(client.PlayerName);

            base.OnRemovedClient(client);
        }

        protected override void OnClientEnteredRoom(ClientInfo<TPeer?> client, [NotNull] string room)
        {
            //Send leave event
            _events.EnqueuePlayerEnteredRoom(client.PlayerName, room, client.Rooms);
        }

        protected override void OnClientExitedRoom(ClientInfo<TPeer?> client, [NotNull] string room)
        {
            //Send leave event
            _events.EnqueuePlayerExitedRoom(client.PlayerName, room, client.Rooms);
        }

        #region packet receiving
        public void ProcessRemoveClient(ref PacketReader reader)
        {
            ushort id;
            reader.ReadRemoveClient(out id);

            ClientInfo<TPeer?> info;
            if (TryGetClientInfoById(id, out info))
                RemoveClient(info);
        }

        public void ReceiveHandshakeResponseBody(ref PacketReader reader)
        {
            // Allocation here is ok (and unavoidable). This doesn't happen very often (just once in normal circumstances) and
            // we're creating lists of peers - so at the very least we need to allocate the lists.
            var clientsByRooms = new Dictionary<string, List<ushort>>();
            var clients = new List<ClientInfo>();
            reader.ReadHandshakeResponseBody(clients, clientsByRooms);
            
            // Load player IDs
            PlayerIds.Load(clients);

            //Remove all player objects who are not in the handshake response
            //Normally we would only receive one handshake response, but it's still valid to receive more
            var currentClients = new List<ClientInfo<TPeer?>>();
            GetClients(currentClients);
            for (var i = 0; i < currentClients.Count; i++)
                if (PlayerIds.GetName(currentClients[i].PlayerId) != currentClients[i].PlayerName)
                    RemoveClient(currentClients[i]);

            //Create a client object for every player which does not already exist
            foreach (var client in clients)
                GetOrCreateClientInfo(client.PlayerId, client.PlayerName, client.CodecSettings, null);

            //Clear all rooms
            ClearRooms();

            //Add remote clients into rooms according to `clientsByRooms` which we deserialized from packet
            foreach (var item in clientsByRooms)
            {
                foreach (var client in item.Value)
                {
                    ClientInfo<TPeer?> info;
                    if (!TryGetClientInfoById(client, out info))
                        Log.Warn("Attempted to add an unknown client '{0}' into room '{1}'", client, item.Key);
                    else
                        JoinRoom(item.Key, info);
                }
            }

            //Send back a response with the complete current state of this client and follow up with deltas every time the state changes
            SendClientState();
        }
        #endregion

        #region packet sending
        private void SendClientState()
        {
            //Sanity check
            var clientId = _session.LocalId;
            if (Log.AssertAndLogError(clientId.HasValue, "EBC361ED-780A-4DE0-944D-3D4D983B785D", "Attempting to send local client state before assigned an ID by the server")) return;

            //Send the local state
            var writer = new PacketWriter(_sender.GetSendBuffer());
            writer.WriteClientState(_session.SessionId, _playerName, clientId.Value, _codecSettings, _localRooms);
            _sender.EnqueueReliable(writer.Written);
            Log.Debug("Sent local client state");

            //begin watching for changes in rooms
            _localRooms.JoinedRoom -= SendJoinRoom;
            _localRooms.JoinedRoom += SendJoinRoom;
            _localRooms.LeftRoom -= SendLeaveRoom;
            _localRooms.LeftRoom += SendLeaveRoom;
        }

        private void SendLeaveRoom(string room)
        {
            //Sanity check
            var id = _session.LocalId;
            if (Log.AssertAndLogError(id != null, "7F29AD74-7F03-46BA-A776-F63F25A39FC5", "Attempted to send channel state delta, but local client ID is null")) return;

            var writer = new PacketWriter(_sender.GetSendBuffer());
            writer.WriteDeltaChannelState(_session.SessionId, false, id.Value, room);

            _sender.EnqueueReliable(writer.Written);
            Log.Trace("Sent DeltaChannelState (Leave room)");
        }

        private void SendJoinRoom(string room)
        {
            //Sanity check
            var id = _session.LocalId;
            if (Log.AssertAndLogError(id != null, "73A33580-B876-4D16-9578-5FB417BA98F5", "Attempted to send channel state delta, but local client ID is null")) return;
            
            var writer = new PacketWriter(_sender.GetSendBuffer());
            writer.WriteDeltaChannelState(_session.SessionId, true, id.Value, room);

            _sender.EnqueueReliable(writer.Written);
            Log.Trace("Sent DeltaChannelState (Join room)");
        }
        #endregion

        public override void Stop()
        {
            _localRooms.JoinedRoom -= SendJoinRoom;
            _localRooms.LeftRoom -= SendLeaveRoom;

            base.Stop();
        }

        #region p2p
        public void IntroduceP2P(ushort id, TPeer connection)
        {
            if (!TryIntroduceP2P(id, connection))
            {
                Log.Debug("IntroduceP2P not yet complete for '{0}' at '{1}'", id, connection);
                _pendingIntroductions.Add(new KeyValuePair<ushort, TPeer>(id, connection));
            }
        }

        private bool TryIntroduceP2P(ushort id, TPeer connection)
        {
            ClientInfo<TPeer?> info;
            if (TryGetClientInfoById(id, out info))
            {
                Log.Debug("IntroduceP2P Success for '{0}' at '{1}'", id, connection);

                var newlyMet = !info.Connection.HasValue;
                info.Connection = connection;

                //raise event
                if (newlyMet && OnClientIntroducedP2P != null)
                    OnClientIntroducedP2P(info);

                return true;
            }

            return false;
        }
        #endregion
    }
}
