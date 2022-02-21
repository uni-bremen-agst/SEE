using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dissonance.Audio.Playback;
using Dissonance.Networking.Client;
using JetBrains.Annotations;

namespace Dissonance.Networking.Server.Admin
{
    internal class ServerAdmin<TServer, TClient, TPeer>
        : IServerAdmin
        where TServer : BaseServer<TServer, TClient, TPeer>
        where TClient : BaseClient<TServer, TClient, TPeer>
        where TPeer : struct, IEquatable<TPeer>
    {
        private static readonly Log Log = new Log((int)LogCategory.Network, typeof(ServerAdmin<TServer, TClient, TPeer>).Name);

        private readonly TServer _server;

        private readonly Dictionary<ushort, string> _knownRoomNames = new Dictionary<ushort, string>();

        public event Action<IServerClientState> ClientJoined;
        public event Action<IServerClientState> ClientLeft;
        public event Action<IServerClientState, IServerClientState> VoicePacketSpoofed;

        private readonly List<IServerClientState> _clients;
        private readonly ReadOnlyCollection<IServerClientState> _readonlyClients;
        public ReadOnlyCollection<IServerClientState> Clients
        {
            get { return _readonlyClients; }
        }

        private readonly List<RemoteChannel> _channelsTmp = new List<RemoteChannel>();
        public bool EnableChannelMonitoring { get; set; }

        public ServerAdmin(TServer server)
        {
            _server = server;
            _clients = new List<IServerClientState>();
            _readonlyClients = new ReadOnlyCollection<IServerClientState>(_clients);
        }

        [CanBeNull] private ServerClientState<TServer, TClient, TPeer> FindPlayer([NotNull] ClientInfo<TPeer> peer)
        {
            for (var i = 0; i < _clients.Count; i++)
            {
                var item = (ServerClientState<TServer, TClient, TPeer>)_clients[i];
                if (item.Peer.Equals(peer))
                    return item;
            }
            
            return null;
        }

        [CanBeNull] private ServerClientState<TServer, TClient, TPeer> FindPlayer(ushort id)
        {
            for (var i = 0; i < _clients.Count; i++)
            {
                var item = (ServerClientState<TServer, TClient, TPeer>)_clients[i];
                if (item.Peer.PlayerId.Equals(id))
                    return item;
            }
            
            return null;
        }

        [CanBeNull] private ServerClientState<TServer, TClient, TPeer> FindPlayer(TPeer peer)
        {
            for (var i = 0; i < _clients.Count; i++)
            {
                var item = (ServerClientState<TServer, TClient, TPeer>)_clients[i];
                if (item.Peer.Connection.Equals(peer))
                    return item;
            }
            
            return null;
        }

        public void InvokeOnClientEnteredRoom([NotNull] ClientInfo<TPeer> peer, string name)
        {
            var player = FindPlayer(peer);
            if (player == null)
            {
                Log.Error("Failed to find player to add to room: {0}", peer.PlayerName);
                return;
            }

            player.InvokeOnEnteredRoom(name);

            // Cache this room name along with it's ID so that rooms can be identified later.
            // If there is a collision is IDs this will save the latest one.
            _knownRoomNames[name.ToRoomId()] = name;
        }

        public void InvokeOnClientExitedRoom([NotNull] ClientInfo<TPeer> peer, string name)
        {
            var player = FindPlayer(peer);
            if (player == null)
            {
                Log.Error("Failed to find player to remove from room: {0}", peer.PlayerName);
                return;
            }

            player.InvokeOnExitedRoom(name);
        }

        public void InvokeOnClientJoined([NotNull] ClientInfo<TPeer> peer)
        {
            // Construct a player object
            var player = new ServerClientState<TServer, TClient, TPeer>(_server, peer);

            // Add player to list of all players
            _clients.Add(player);

            // Invoke event handlers
            var pj = ClientJoined;
            if (pj != null)
            {
                try
                {
                    pj(player);
                }
                catch (Exception e)
                {
                    Log.Error("Exception encountered invoking `PlayerJoined` event handler: {0}", e);
                }
            }
        }

        public void InvokeOnClientLeft([NotNull] ClientInfo<TPeer> peer)
        {
            var player = FindPlayer(peer);
            if (player == null)
            {
                Log.Error("Failed to find player to remove: {0}", peer.PlayerName);
                return;
            }

            // Remove from list
            _clients.Remove(player);

            // Invoke event handlers
            var pl = ClientLeft;
            if (pl != null)
            {
                try
                {
                    pl(player);
                }
                catch (Exception e)
                {
                    Log.Error("Exception encountered invoking `PlayerLeft` event handler: {0}", e);
                }
            }
        }

        public void InvokeOnRelayingPacket(ArraySegment<byte> payload, TPeer source)
        {
            if (!EnableChannelMonitoring)
                return;

            var reader = new PacketReader(payload);

            // Read header and reject packets with invalid magic.
            // This should never happen because the relay system should have already rejected packets with invalid magic.
            MessageTypes type;
            if (!reader.ReadPacketHeader(out type))
            {
                Log.Error("Ignoring relayed packet - magic number is incorrect");
                return;
            }

            // We only care about voice packets for channel monitoring, skip others.
            if (type != MessageTypes.VoiceData)
                return;

            // Sanity check the session ID
            var session = reader.ReadUInt32();
            if (session != _server.SessionId)
                return;

            // Find out who sent this packet. Skip if we don't have state for them yet.
            var clientState = FindPlayer(source);
            if (clientState == null)
                return;

            // Read first part of header
            ushort senderId;
            reader.ReadVoicePacketHeader1(out senderId);

            // Sanity check that the voice packet is coming from where it _says_ it is coming from
            if (senderId != clientState.Peer.PlayerId)
            {
                var clientStateSpoof = FindPlayer(senderId);
                InvokeOnVoicePacketSpoof(clientState, clientStateSpoof);
                return;
            }

            // Read second part of the header from the packet
            VoicePacketOptions metadata;
            ushort sequenceNumber, numChannels;
            reader.ReadVoicePacketHeader2(out metadata, out sequenceNumber, out numChannels);

            // Read out all of the channels
            _channelsTmp.Clear();
            for (var i = 0; i < numChannels; i++)
            {
                ChannelBitField channel;
                ushort channelRecipient;
                reader.ReadVoicePacketChannel(out channel, out channelRecipient);

                // `IsClosing` indicates that this is the last packet which contains this channel. We'll skip it so that the channels list
                // does not include it. This means it's removed one packet early but that doesn't really matter for moderation purposes.
                if (channel.IsClosing)
                    continue;

                // Discover the name of the target of this channel (room or player).
                // - Player can simply be discovered from ID. Skip channels to unknown players.
                // - Rooms have been cached in a hashmap if any player has ever listened to that room. Skip unknown rooms.
                string name;
                if (channel.Type == ChannelType.Player)
                {
                    var tgt = FindPlayer(channelRecipient);
                    if (tgt == null)
                        continue;
                    name = tgt.Name;
                }
                else if (channel.Type == ChannelType.Room)
                {
                    if (!_knownRoomNames.TryGetValue(channelRecipient, out name))
                        continue;
                }
                else
                    continue;

                _channelsTmp.Add(new RemoteChannel(name, channel.Type, new PlaybackOptions(channel.IsPositional, channel.AmplitudeMultiplier, channel.Priority)));
            }

            // Update the state with the channel list
            clientState.UpdateChannels(_channelsTmp);
        }

        private void InvokeOnVoicePacketSpoof([NotNull] IServerClientState spoofer, [CanBeNull] IServerClientState spoofee)
        {
            var spoof = VoicePacketSpoofed;
            if (spoof != null)
                spoof(spoofer, spoofee);
        }
    }
}
