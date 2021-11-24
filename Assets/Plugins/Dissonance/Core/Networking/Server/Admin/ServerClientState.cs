using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace Dissonance.Networking.Server.Admin
{
    internal class ServerClientState<TServer, TClient, TPeer>
        : IServerClientState
        where TServer : BaseServer<TServer, TClient, TPeer>
        where TClient : BaseClient<TServer, TClient, TPeer>
        where TPeer : struct, IEquatable<TPeer>
    {
        private static readonly Log Log = new Log((int)LogCategory.Network, typeof(ServerClientState<TServer, TClient, TPeer>).Name);

        private readonly TServer _server;

        private readonly ClientInfo<TPeer> _peer;
        public ClientInfo<TPeer> Peer { get { return _peer; } }

        public string Name { get { return _peer.PlayerName; } }

        public bool IsConnected { get { return _peer.IsConnected; } }

        public event Action<IServerClientState, string> OnStartedListeningToRoom;

        public event Action<IServerClientState, string> OnStoppedListeningToRoom;

        private readonly List<string> _rooms;
        private readonly ReadOnlyCollection<string> _roomsReadonly;
        public ReadOnlyCollection<string> Rooms
        {
            get { return _roomsReadonly; }
        }

        private readonly List<RemoteChannel> _channels;
        private readonly ReadOnlyCollection<RemoteChannel> _channelsReadonly;

        public DateTime LastChannelUpdateUtc { get; private set; }
        public ReadOnlyCollection<RemoteChannel> Channels
        {
            get { return _channelsReadonly; }
        }

        public ServerClientState(TServer server, ClientInfo<TPeer> peer)
        {
            _server = server;
            _peer = peer;

            _rooms = new List<string>();
            _roomsReadonly = new ReadOnlyCollection<string>(_rooms);

            _channels = new List<RemoteChannel>();
            _channelsReadonly = new ReadOnlyCollection<RemoteChannel>(_channels);
        }

        public void RemoveFromRoom([NotNull] string roomName)
        {
            if (roomName == null)
                throw new ArgumentNullException("roomName");

            // Send a packet to the server as if this peer asked to be removed from the given room
            var p = new PacketWriter(new byte[10 + roomName.Length * 4]);
            p.WriteDeltaChannelState(_server.SessionId, false, _peer.PlayerId, roomName);
            _server.NetworkReceivedPacket(_peer.Connection, p.Written);
        }

        public void Reset()
        {
            // Send a packet to this client telling them that they're using the wrong session ID.
            // This is a lie (in fact we're using the wrong ID intentionally), but it will get the client to
            // remove itself from the room.
            var writer = new PacketWriter(new byte[7]);
            writer.WriteErrorWrongSession(unchecked(_server.SessionId + 1));
            _server.SendUnreliable(new List<TPeer> { _peer.Connection }, writer.Written);
        }

        public void InvokeOnEnteredRoom(string name)
        {
            if (!_rooms.Contains(name))
                _rooms.Add(name);

            var entered = OnStartedListeningToRoom;
            if (entered != null)
            {
                try
                {
                    entered(this, name);
                }
                catch (Exception e)
                {
                    Log.Error("Exception encountered invoking `PlayerJoined` event handler: {0}", e);
                }
            }
        }

        public void InvokeOnExitedRoom(string name)
        {
            _rooms.Remove(name);

            var exited = OnStoppedListeningToRoom;
            if (exited != null)
            {
                try
                {
                    exited(this, name);
                }
                catch (Exception e)
                {
                    Log.Error("Exception encountered invoking `PlayerJoined` event handler: {0}", e);
                }
            }
        }

        public void UpdateChannels([NotNull] List<RemoteChannel> channels)
        {
            _channels.Clear();
            _channels.AddRange(channels);
            LastChannelUpdateUtc = DateTime.UtcNow;
        }
    }
}
