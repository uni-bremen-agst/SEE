using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Dissonance.Networking
{
    internal class BaseClientCollection<TPeer>
        : IClientCollection<TPeer>
    {
        #region fields and properties
        protected readonly Log Log;

        protected readonly ClientIdCollection PlayerIds = new ClientIdCollection();

        protected readonly RoomClientsCollection<TPeer> ClientsInRooms = new RoomClientsCollection<TPeer>();

        private readonly Dictionary<ushort, ClientInfo<TPeer>> _clientsByPlayerId = new Dictionary<ushort, ClientInfo<TPeer>>();
        private readonly Dictionary<string, ClientInfo<TPeer>> _clientsByName = new Dictionary<string, ClientInfo<TPeer>>();

        private readonly List<string> _tmpRoomList = new List<string>();

        public event Action<ClientInfo<TPeer>> OnClientJoined;
        public event Action<ClientInfo<TPeer>> OnClientLeft;
        public event Action<ClientInfo<TPeer>, string> OnClientEnteredRoomEvent;
        public event Action<ClientInfo<TPeer>, string> OnClientExitedRoomEvent;
        #endregion

        protected BaseClientCollection()
        {
            Log = Logs.Create(LogCategory.Network, GetType().Name);
        }

        public virtual void Stop()
        {
            //Explicitly remove all clients one by one (this will raise the approprate events for anyone who cares)
            var clients = new List<ClientInfo<TPeer>>();
            GetClients(clients);
            foreach (var client in clients)
                RemoveClient(client);

            //Sanity check that removing all clients cleared the collection...
            Log.AssertAndLogError(!PlayerIds.Items.Any(), "E8313B54-97FE-43F6-BC8D-7E0D52D01C7A", "{0} player(s) were not properly removed from the session", PlayerIds.Items.Count());
            PlayerIds.Clear();

            var count = ClientsInRooms.ClientCount();
            Log.AssertAndLogError(count == 0, "441F07AE-A25F-4968-B028-DABA51794B45", "{0} player(s) were not properly removed from the session", count);
            ClientsInRooms.Clear();

            Log.AssertAndLogError(_clientsByPlayerId.Count == 0, "17F67420-9874-4A2E-ABDF-3EF0C4037378", "{0} player(s) were not properly removed from the session", _clientsByPlayerId.Count);
            _clientsByPlayerId.Clear();
        }

        #region add/remove clients
        protected virtual void OnAddedClient([NotNull] ClientInfo<TPeer> client)
        {
            var evt = OnClientJoined;
            if (evt != null)
                evt(client);
        }

        protected virtual void OnRemovedClient([NotNull] ClientInfo<TPeer> client)
        {
            var evt = OnClientLeft;
            if (evt != null)
                evt(client);
        }

        [NotNull] protected ClientInfo<TPeer> GetOrCreateClientInfo(ushort id, [NotNull] string name, CodecSettings codecSettings, [CanBeNull] TPeer connection)
        {
            if (name == null) throw new ArgumentNullException("name");

            ClientInfo<TPeer> info;
            if (TryGetClientInfoById(id, out info))
                return info;

            info = new ClientInfo<TPeer>(name, id, codecSettings, connection);
            _clientsByPlayerId[id] = info;
            _clientsByName[name] = info;

            OnAddedClient(info);

            return info;
        }

        protected void RemoveClient([NotNull] ClientInfo<TPeer> client)
        {
            Log.Debug("Removing client '{0}'", client.PlayerName);

            //Set the flag to indicate that this client is gone
            client.IsConnected = false;

            //Remove from player ID collection
            PlayerIds.Unregister(client.PlayerName);

            //Remove from client sets
            _clientsByPlayerId.Remove(client.PlayerId);
            _clientsByName.Remove(client.PlayerName);

            //Remove from room membership lists
            for (var i = client.Rooms.Count - 1; i >= 0; i--)
                LeaveRoom(client.Rooms[i], client);

            //Raise the removal event
            OnRemovedClient(client);
        }
        #endregion

        #region query
        [ContractAnnotation("=> true, info:notnull; => false, info:null")]
        public bool TryGetClientInfoById(ushort player, out ClientInfo<TPeer> info)
        {
            return _clientsByPlayerId.TryGetValue(player, out info);
        }

        [ContractAnnotation("=> true, info:notnull; => false, info:null")]
        public bool TryGetClientInfoByName([CanBeNull] string name, out ClientInfo<TPeer> info)
        {
            if (name == null)
            {
                // ReSharper disable once AssignNullToNotNullAttribute (Justification: this is within the method contract)
                info = null;
                return false;
            }

            return _clientsByName.TryGetValue(name, out info);
        }

        public bool TryGetClientsInRoom(string room, out List<ClientInfo<TPeer>> clients)
        {
            return ClientsInRooms.TryGetClientsInRoom(room, out clients);
        }

        public bool TryGetClientsInRoom(ushort roomId, out List<ClientInfo<TPeer>> clients)
        {
            return ClientsInRooms.TryGetClientsInRoom(roomId, out clients);
        }

        protected void GetClients(List<ClientInfo<TPeer>> output)
        {
            using (var enumerator = _clientsByPlayerId.Values.GetEnumerator())
            {
                while (enumerator.MoveNext())
                    output.Add(enumerator.Current);
            }
        }

        [ContractAnnotation("=> true, info:notnull; => false, info:null")]
        protected bool TryFindClientByConnection(TPeer connection, [CanBeNull] out ClientInfo<TPeer> info)
        {
            using (var enumerator = _clientsByPlayerId.Values.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var item = enumerator.Current;
                    if (item != null && connection.Equals(item.Connection))
                    {
                        info = item;
                        return true;
                    }
                }
            }

            info = null;
            return false;
        }
        #endregion

        #region room management
        protected void ClearRooms()
        {
            ClientsInRooms.Clear();
        }

        protected virtual void OnClientEnteredRoom([NotNull] ClientInfo<TPeer> client, string room)
        {
            var evt = OnClientEnteredRoomEvent;
            if (evt != null)
                evt(client, room);
        }

        protected virtual void OnClientExitedRoom([NotNull] ClientInfo<TPeer> client, string room)
        {
            var evt = OnClientExitedRoomEvent;
            if (evt != null)
                evt(client, room);
        }

        protected void JoinRoom([NotNull] string room, [NotNull] ClientInfo<TPeer> client)
        {
            if (room == null) throw new ArgumentNullException("room");
            if (client == null) throw new ArgumentNullException("client");

            //Add this client to the list of clients in the room
            ClientsInRooms.Add(room, client);

            //Add this room to the list of rooms for this client
            if (client.AddRoom(room))
                OnClientEnteredRoom(client, room);
        }

        private void LeaveRoom([NotNull] string room, [NotNull] ClientInfo<TPeer> client)
        {
            if (room == null) throw new ArgumentNullException("room");
            if (client == null) throw new ArgumentNullException("client");

            //Remove client from the list of clients in this room
            ClientsInRooms.Remove(room, client);

            //Remove room from the list of rooms for this client
            if (client.RemoveRoom(room))
                OnClientExitedRoom(client, room);
        }
        #endregion

        #region packet processing
        public virtual void ProcessClientState([CanBeNull] TPeer source, ref PacketReader reader)
        {
            //Read header to identity which client this is
            var client = reader.ReadClientStateHeader();

            //Get or create the info object for this client
            var info = GetOrCreateClientInfo(client.PlayerId, client.PlayerName, client.CodecSettings, source);

            //Remove this client from all rooms
            while (info.Rooms.Count > 0)
                LeaveRoom(info.Rooms[info.Rooms.Count - 1], info);

            //Read the rooms this client is in
            _tmpRoomList.Clear();
            reader.ReadClientStateRooms(_tmpRoomList);

            //Add client to rooms as necessary
            for (var i = 0; i < _tmpRoomList.Count; i++)
                JoinRoom(_tmpRoomList[i], info);

            //Clean up after deserialisation
            _tmpRoomList.Clear();
        }

        public virtual void ProcessDeltaChannelState(ref PacketReader reader)
        {
            bool joined;
            ushort peer;
            string room;
            reader.ReadDeltaChannelState(out joined, out peer, out room);

            ClientInfo<TPeer> info;
            if (!TryGetClientInfoById(peer, out info))
            {
                Log.Warn("Received a DeltaChannelState for an unknown peer");
                return;
            }

            if (joined)
                JoinRoom(room, info);
            else
                LeaveRoom(room, info);
        }
        #endregion
    }

    internal interface IClientCollection<TPeer>
    {
        [ContractAnnotation("=> true, info:notnull; => false, info:null")]
        bool TryGetClientInfoById(ushort clientId, out ClientInfo<TPeer> info);

        [ContractAnnotation("=> true, info:notnull; => false, info:null")]
        bool TryGetClientInfoByName([NotNull] string clientName, out ClientInfo<TPeer> info);

        [ContractAnnotation("=> true, clients:notnull; => false, clients:null")]
        bool TryGetClientsInRoom([NotNull] string room, out List<ClientInfo<TPeer>> clients);

        [ContractAnnotation("=> true, clients:notnull; => false, clients:null")]
        bool TryGetClientsInRoom(ushort roomId, out List<ClientInfo<TPeer>> clients);
    }
}
