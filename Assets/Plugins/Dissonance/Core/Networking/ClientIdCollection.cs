using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Dissonance.Networking
{
    internal sealed class ClientIdCollection
        : IReadonlyClientIdCollection
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(ClientIdCollection).Name);

        private readonly List<string> _items;
        private readonly List<ushort> _freeIds;
        private readonly IEnumerable<KeyValuePair<ushort, string>> _alive;

        [NotNull] public IEnumerable<KeyValuePair<ushort, string>> Items
        {
            get { return _alive; }
        }
        #endregion

        public ClientIdCollection()
        {
            _items = new List<string>();
            _freeIds = new List<ushort>();
            _alive = _items
                .Select((a, i) => new KeyValuePair<ushort, string>((ushort)i, a))
                .Where(x => x.Value != null);
        }

        #region free IDs
        private ushort GetFreeId()
        {
            if (_freeIds.Count == 0) throw new InvalidOperationException("Cannot get a free ID, none available");

            var id = _freeIds[_freeIds.Count - 1];
            _freeIds.RemoveAt(_freeIds.Count - 1);
            return id;
        }

        private void AddFreeId(ushort id)
        {
            //Insert into the free ID list (keep it in order by using binary search for find the correcy index to insert at)
            var idx = _freeIds.BinarySearch(id);
            Log.AssertAndThrowPossibleBug(idx < 0, "371F58AD-CBB7-44FD-8503-0828496433F5", "Attempted to add free ID, but ID is already free");
            _freeIds.Insert(~idx, id);
        }
        #endregion

        #region query
        /// <inheritdoc />
        public string GetName(ushort id)
        {
            if (id >= _items.Count)
                return null;

            return _items[id];
        }

        /// <inheritdoc />
        public ushort? GetId(string name)
        {
            for (ushort i = 0; i < _items.Count; i++)
            {
                if (_items[i] == name)
                    return i;
            }

            return null;
        }
        #endregion

        #region update
        /// <summary>
        /// Add a new name and generate an ID
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ushort Register([NotNull] string name)
        {
            var found = _items.IndexOf(name);
            if (found != -1)
                throw new InvalidOperationException(string.Format("Name is already in table with ID '{0}'", found));

            if (_freeIds.Count > 0)
            {
                var index = GetFreeId();
                _items[index] = name;
                return index;
            }
            else
            {
                _items.Add(name);
                return (ushort)(_items.Count - 1);
            }
        }

        /// <summary>
        /// Remove the given name and free up it's ID for re-use
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Unregister([NotNull] string name)
        {
            //Find the item
            var index = _items.IndexOf(name);
            if (index == -1)
                return false;

            //If it's the last item, we can just remove it
            if (index == _items.Count - 1)
            {
                //Remove the item
                _items.RemoveAt(index);

                //Remove trailing free IDs
                while (_freeIds.Contains((ushort)(_items.Count - 1)))
                {
                    var i = (ushort)(_items.Count - 1);

                    _freeIds.Remove(i);
                    _items.RemoveAt(i);
                }

                return true;
            }

            //It's not the last item, so we need to set it to null and save it as a free ID
            _items[index] = null;
            AddFreeId((ushort)index);
            return true;
        }

        /// <summary>
        /// Remove all items from the collection
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            _freeIds.Clear();
        }

        public void Load([NotNull] List<ClientInfo> clients)
        {
            if (clients == null)
                throw new ArgumentNullException("clients");

            Clear();

            //Find the highest ID being added
            var count = 0;
            for (var i = 0; i < clients.Count; i++)
                count = Math.Max(count, clients[i].PlayerId);

            //Ensure that the list has enough slots to add all the way to the highest ID
            if (_items.Capacity < count + 1)
                _items.Capacity = count + 1;
            for (var i = 0; i < count + 1; i++)
                _items.Add(null);

            //Insert the clients into the correct index
            for (var i = 0; i < clients.Count; i++)
            {
                var client = clients[i];
                _items[client.PlayerId] = client.PlayerName;
            }

            //Mark the gaps as free IDs
            for (var i = 0; i < clients.Count; i++)
            {
                if (_items[i] == null)
                    AddFreeId((ushort)i);
            }
        }
        #endregion
    }

    internal interface IReadonlyClientIdCollection
    {
        /// <summary>
        /// Get the ID associated with the given name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        ushort? GetId([NotNull] string name);

        /// <summary>
        /// Get the name associated with the given ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [CanBeNull] string GetName(ushort id);
    }
}
