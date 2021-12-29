using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dissonance.Extensions;
using Dissonance.Networking.Client;
using JetBrains.Annotations;

namespace Dissonance.Networking
{
    internal struct ClientInfo
    {
        public string PlayerName { get; private set; }
        public ushort PlayerId { get; private set; }
        public CodecSettings CodecSettings { get; private set; }

        public ClientInfo(string playerName, ushort playerId, CodecSettings codecSettings) : this()
        {
            PlayerName = playerName;
            PlayerId = playerId;
            CodecSettings = codecSettings;
        }
    }

    /// <summary>
    /// Information about a client in a network session
    /// </summary>
    public class ClientInfo<TPeer>
        : IEquatable<ClientInfo<TPeer>>
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(ClientInfo<TPeer>).Name);

        private readonly string _playerName;
        private readonly ushort _playerId;
        private readonly CodecSettings _codecSettings;

        private readonly List<string> _rooms = new List<string>();
        private readonly ReadOnlyCollection<string> _roomsReadonly;

        /// <summary>
        /// Name of this client (as specified by the DissonanceComms component for the client)
        /// </summary>
        [NotNull] public string PlayerName
        {
            get { return _playerName; }
        }

        /// <summary>
        /// Unique ID of this client
        /// </summary>
        public ushort PlayerId
        {
            get { return _playerId; }
        }

        /// <summary>
        /// The codec settings being used by the client
        /// </summary>
        public CodecSettings CodecSettings
        {
            get { return _codecSettings; }
        }

        /// <summary>
        /// Ordered list of rooms this client is listening to
        /// </summary>
        [NotNull] internal ReadOnlyCollection<string> Rooms
        {
            get { return _roomsReadonly; }
        }

        [CanBeNull] public TPeer Connection { get; internal set; }

        public bool IsConnected { get; internal set; }

        internal PeerVoiceReceiver VoiceReceiver { get; set; }
        #endregion

        public ClientInfo(string playerName, ushort playerId, CodecSettings codecSettings, [CanBeNull] TPeer connection)
        {
            _roomsReadonly = new ReadOnlyCollection<string>(_rooms);

            _playerName = playerName;
            _playerId = playerId;
            _codecSettings = codecSettings;
            Connection = connection;

            IsConnected = true;
        }

        public override string ToString()
        {
            return string.Format("Client '{0}/{1}/{2}'", PlayerName, PlayerId, Connection);
        }

        #region equality
        public bool Equals(ClientInfo<TPeer> other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(_playerName, other._playerName) && _playerId == other._playerId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((ClientInfo<TPeer>)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_playerName.GetFnvHashCode() * 397) ^ _playerId.GetHashCode();
            }
        }
        #endregion

        #region room management
        public bool AddRoom([NotNull] string roomName)
        {
            if (roomName == null) throw new ArgumentNullException("roomName");

            var index = _rooms.BinarySearch(roomName);
            if (index < 0)
            {
                _rooms.Insert(~index, roomName);
                Log.Trace("Added room {0} to client {1}", roomName, this);

                return true;
            }

            return false;
        }

        public bool RemoveRoom([NotNull] string roomName)
        {
            if (roomName == null) throw new ArgumentNullException("roomName");

            var index = _rooms.BinarySearch(roomName);
            if (index >= 0)
            {
                _rooms.RemoveAt(index);
                Log.Trace("Removed room {0} from client {1}", roomName, this);

                return true;
            }

            return false;
        }
        #endregion
    }
}
