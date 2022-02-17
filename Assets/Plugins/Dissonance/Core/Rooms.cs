using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace Dissonance
{
    /// <summary>
    /// Collection of rooms the local client is listening to
    /// </summary>
    public sealed class Rooms
    {
        private static readonly Log Log = Logs.Create(LogCategory.Core, typeof(Rooms).Name);
        private static readonly RoomMembershipComparer Comparer = new RoomMembershipComparer();

        private readonly List<RoomMembership> _rooms;

        private readonly List<string> _roomNames;
        private readonly ReadOnlyCollection<string> _roomNamesReadonly;
        internal ReadOnlyCollection<string> Memberships { get { return _roomNamesReadonly; } }

        public event Action<string> JoinedRoom;
        public event Action<string> LeftRoom;

        internal Rooms()
        {
            _rooms = new List<RoomMembership>();

            _roomNames = new List<string>();
            _roomNamesReadonly = new ReadOnlyCollection<string>(_roomNames);
        }

        /// <summary>
        /// Number of rooms currently being listened to
        /// </summary>
        public int Count
        {
            get { return _rooms.Count; }
        }

        internal RoomMembership this[int i]
        {
            get { return _rooms[i]; }
        }

        /// <summary>
        /// Checks if the collection contains the given room
        /// </summary>
        /// <param name="roomName"></param>
        /// <returns></returns>
        public bool Contains([NotNull] string roomName)
        {
            if (roomName == null)
                throw new ArgumentNullException("roomName");

            var index = FindById(roomName.ToRoomId());
            return index.HasValue && _rooms.Count > 0;
        }

        /// <summary>
        ///     Registers the local client as interested in broadcasts directed at the specified room.
        /// </summary>
        /// <param name="roomName">The room name.</param>
        public RoomMembership Join([NotNull] string roomName)
        {
            if (roomName == null)
                throw new ArgumentNullException("roomName", "Cannot join a null room");

            var membership = new RoomMembership(roomName, 1);

            //Check to see if we already have this membership in the list
            var index = _rooms.BinarySearch(membership, Comparer);
            if (_rooms.Count == 0 || index < 0)
            {
                //Insert membership into list (making sure to keep it in order)
                var i = ~index;
                if (i == _rooms.Count)
                    _rooms.Add(membership);
                else
                    _rooms.Insert(i, membership);

                //newly joined, so invoke the event
                OnJoinedRoom(membership);
            }
            else
            {
                //We're already in this room, increment the subscriber count
                var m = _rooms[index];
                m.Count++;
                _rooms[index] = m;
            }

            return membership;
        }

        /// <summary>
        ///     Unregisters the local client from broadcasts directed at the specified room.
        /// </summary>
        /// <param name="membership">The room membership.</param>
        public bool Leave(RoomMembership membership)
        {
            var index = FindById(membership.RoomId);
            if (!index.HasValue)
                return false;

            var m = _rooms[index.Value];
            m.Count--;
            _rooms[index.Value] = m;

            if (m.Count <= 0)
            {
                //Remove membership
                _rooms.RemoveAt(index.Value);

                //Invoke event
                OnLeftRoom(membership);

                return true;
            }

            return false;
        }

        private void OnJoinedRoom(RoomMembership membership)
        {
            Log.Debug("Joined chat room '{0}'", membership.RoomName);

            //Add to name list
            var idIndex = _roomNames.BinarySearch(membership.RoomName);
            if (idIndex < 0)
                _roomNames.Insert(~idIndex, membership.RoomName);

            var handler = JoinedRoom;
            if (handler != null) handler(membership.RoomName);
        }

        private void OnLeftRoom(RoomMembership membership)
        {
            Log.Debug("Left chat room '{0}'", membership.RoomName);

            //Remove from name list
            var idIndex = _roomNames.BinarySearch(membership.RoomName);
            if (idIndex >= 0)
                _roomNames.RemoveAt(idIndex);

            var handler = LeftRoom;
            if (handler != null) handler(membership.RoomName);
        }

        /// <summary>
        /// Reverse the ID to get the name of a room which we are currently listening to
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        [CanBeNull] internal string Name(ushort roomId)
        {
            if (_rooms.Count == 0)
                return null;

            var index = FindById(roomId);

            if (!index.HasValue)
                return null;

            return _rooms[index.Value].RoomName;
        }

        private int? FindById(ushort id)
        {
            var maxIndex = _rooms.Count - 1;
            var minIndex = 0;
            while (maxIndex >= minIndex)
            {
                var mid = minIndex + ((maxIndex - minIndex) / 2);
                var c = id.CompareTo(_rooms[mid].RoomId);

                //Found it!
                if (c == 0)
                    return mid;

                if (c > 0)
                    minIndex = mid + 1;
                else
                    maxIndex = mid - 1;
            }

            return null;
        }
    }

    public struct RoomMembership
    {
        private readonly string _name;
        private readonly ushort _roomId;

        internal int Count;

        internal RoomMembership([NotNull] string name, int count)
        {
            _name = name;
            _roomId = name.ToRoomId();
            Count = count;
        }

        [NotNull] public string RoomName
        {
            get { return _name; }
        }

        public ushort RoomId
        {
            get { return _roomId; }
        }
    }

    internal class RoomMembershipComparer
        : IComparer<RoomMembership>
    {
        public int Compare(RoomMembership x, RoomMembership y)
        {
            return x.RoomId.CompareTo(y.RoomId);
        }
    }
}
