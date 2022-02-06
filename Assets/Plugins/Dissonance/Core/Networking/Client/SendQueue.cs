using System;
using System.Collections.Generic;
using Dissonance.Datastructures;
using Dissonance.Threading;
using JetBrains.Annotations;

namespace Dissonance.Networking.Client
{
    /// <inheritdoc />
    internal class SendQueue<TPeer>
        : ISendQueue<TPeer>
        where TPeer : struct
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(SendQueue<TPeer>).Name);

        private readonly IClient<TPeer> _client;

        private readonly ReadonlyLockedValue<List<ArraySegment<byte>>> _serverReliableQueue = new ReadonlyLockedValue<List<ArraySegment<byte>>>(new List<ArraySegment<byte>>());
        private readonly ReadonlyLockedValue<List<ArraySegment<byte>>> _serverUnreliableQueue = new ReadonlyLockedValue<List<ArraySegment<byte>>>(new List<ArraySegment<byte>>());
        private readonly ReadonlyLockedValue<List<KeyValuePair<List<ClientInfo<TPeer?>>, ArraySegment<byte>>>> _reliableP2PQueue = new ReadonlyLockedValue<List<KeyValuePair<List<ClientInfo<TPeer?>>, ArraySegment<byte>>>>(new List<KeyValuePair<List<ClientInfo<TPeer?>>, ArraySegment<byte>>>());
        private readonly ReadonlyLockedValue<List<KeyValuePair<List<ClientInfo<TPeer?>>, ArraySegment<byte>>>> _unreliableP2PQueue = new ReadonlyLockedValue<List<KeyValuePair<List<ClientInfo<TPeer?>>, ArraySegment<byte>>>>(new List<KeyValuePair<List<ClientInfo<TPeer?>>, ArraySegment<byte>>>());

        private readonly ReadonlyLockedValue<Pool<byte[]>> _sendBufferPool;

        private readonly ConcurrentPool<List<ClientInfo<TPeer?>>> _listPool = new ConcurrentPool<List<ClientInfo<TPeer?>>>(32, () => new List<ClientInfo<TPeer?>>());

        private readonly List<byte[]> _tmpRecycleQueue = new List<byte[]>();
        #endregion

        #region constructor
        public SendQueue([NotNull] IClient<TPeer> client, [NotNull] ReadonlyLockedValue<Pool<byte[]>> bytePool)
        {
            if (client == null) throw new ArgumentNullException("client");
            if (bytePool == null) throw new ArgumentNullException("bytePool");

            _client = client;
            _sendBufferPool = bytePool;
        }
        #endregion

        public void Update()
        {
            //Reliable traffic to server
            using (var locker = _serverReliableQueue.Lock())
            {
                var q = locker.Value;
                for (var i = 0; i < q.Count; i++)
                {
                    var item = q[i];
                    _client.SendReliable(item);
                    _tmpRecycleQueue.Add(item.Array);
                }

                q.Clear();
            }

            //Unreliable traffic to server
            using (var locker = _serverUnreliableQueue.Lock())
            {
                var q = locker.Value;
                for (var i = 0; i < q.Count; i++)
                {
                    var item = q[i];
                    _client.SendUnreliable(item);
                    _tmpRecycleQueue.Add(item.Array);
                }

                q.Clear();
            }

            //P2P reliable traffic
            using (var locker = _reliableP2PQueue.Lock())
            {
                var q = locker.Value;
                for (var i = 0; i < q.Count; i++)
                {
                    var item = q[i];

                    //Send it
                    _client.SendReliableP2P(item.Key, item.Value);

                    //Recycle
                    _tmpRecycleQueue.Add(item.Value.Array);
                    item.Key.Clear();
                    _listPool.Put(item.Key);
                }

                q.Clear();
            }

            //P2P reliable traffic
            using (var locker = _unreliableP2PQueue.Lock())
            {
                var q = locker.Value;
                for (var i = 0; i < q.Count; i++)
                {
                    var item = q[i];

                    //Send it
                    _client.SendUnreliableP2P(item.Key, item.Value);

                    //Recycle
                    _tmpRecycleQueue.Add(item.Value.Array);
                    item.Key.Clear();
                    _listPool.Put(item.Key);
                }

                q.Clear();
            }

            //Recycle all the buffers
            using (var locker = _sendBufferPool.Lock())
            {
                for (var i = 0; i < _tmpRecycleQueue.Count; i++)
                {
                    var v = _tmpRecycleQueue[i];
                    if (v != null)
                        locker.Value.Put(v);
                }
            }
            _tmpRecycleQueue.Clear();
        }

        private static int Drop<T>([NotNull] ReadonlyLockedValue<List<T>> l)
        {
            using (var ll = l.Lock())
            {
                var dropped = ll.Value.Count;
                ll.Value.Clear();
                return dropped;
            }
        }

        public void Stop()
        {
            var dropped = Drop(_serverReliableQueue)
                        + Drop(_serverUnreliableQueue)
                        + Drop(_reliableP2PQueue)
                        + Drop(_unreliableP2PQueue);

            Log.Debug("Stopped network SendQueue (dropped {0} remaining packets)", dropped);
        }

        #region Enqueue
        public void EnqueueReliable(ArraySegment<byte> packet)
        {
            if (packet.Array == null) throw new ArgumentNullException("packet");

            using (var locker = _serverReliableQueue.Lock())
                locker.Value.Add(packet);
        }

        public void EnqeueUnreliable(ArraySegment<byte> packet)
        {
            if (packet.Array == null) throw new ArgumentNullException("packet");

            using (var locker = _serverUnreliableQueue.Lock())
                locker.Value.Add(packet);
        }

        public void EnqueueReliableP2P(ushort localId, IList<ClientInfo<TPeer?>> destinations, ArraySegment<byte> packet)
        {
            if (destinations == null) throw new ArgumentNullException("destinations");
            if (packet.Array == null) throw new ArgumentNullException("packet");

            using (var locker = _reliableP2PQueue.Lock())
            {
                EnqueueP2P(
                    localId,
                    destinations,
                    locker.Value,
                    packet
                );
            }
        }

        public void EnqueueUnreliableP2P(ushort localId, IList<ClientInfo<TPeer?>> destinations, ArraySegment<byte> packet)
        {
            if (destinations == null) throw new ArgumentNullException("destinations");
            if (packet.Array == null) throw new ArgumentNullException("packet");

            using (var locker = _unreliableP2PQueue.Lock())
            {
                EnqueueP2P(
                    localId,
                    destinations,
                    locker.Value,
                    packet
                );
            }
        }

        public byte[] GetSendBuffer()
        {
            using (var locker = _sendBufferPool.Lock())
                return locker.Value.Get();
        }

        public void RecycleSendBuffer([NotNull] byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");

            using (var locker = _sendBufferPool.Lock())
                locker.Value.Put(buffer);
        }

        private void EnqueueP2P(ushort localId, [NotNull] ICollection<ClientInfo<TPeer?>> destinations, [NotNull] ICollection<KeyValuePair<List<ClientInfo<TPeer?>>, ArraySegment<byte>>> queue, ArraySegment<byte> packet)
        {
            if (packet.Array == null) throw new ArgumentNullException("packet");
            if (destinations == null) throw new ArgumentNullException("destinations");
            if (queue == null) throw new ArgumentNullException("queue");

            //early exit
            if (destinations.Count == 0)
                return;

            //Copy destinations into a new list we're allowed to mutate
            var dests = _listPool.Get();
            dests.Clear();
            dests.AddRange(destinations);

            //Make sure we don't send to ourselves
            for (var i = 0; i < dests.Count; i++)
            {
                if (dests[i].PlayerId == localId)
                {
                    dests.RemoveAt(i);
                    break;
                }
            }

            //If we were only trying to send to ourself we can early exit now
            if (dests.Count == 0)
            {
                _listPool.Put(dests);
                return;
            }

            //Add to queue to send next update
            queue.Add(new KeyValuePair<List<ClientInfo<TPeer?>>, ArraySegment<byte>>(dests, packet));
        }
        #endregion
    }

    internal interface ISendQueue<TPeer>
        where TPeer : struct
    {
        /// <summary>
        /// Send a reliable message to the server
        /// </summary>
        void EnqueueReliable(ArraySegment<byte> packet);

        /// <summary>
        /// Send an unreliable message to the server
        /// </summary>
        void EnqeueUnreliable(ArraySegment<byte> packet);

        /// <summary>
        /// Send a reliable message directly to the given list of peers (excluding the local peer)
        /// </summary>
        /// <param name="localId"></param>
        /// <param name="destinations"></param>
        /// <param name="packet"></param>
        void EnqueueReliableP2P(ushort localId, [NotNull] IList<ClientInfo<TPeer?>> destinations, ArraySegment<byte> packet);

        /// <summary>
        /// Send an unreliable message directly to the given list of peers (excluding the local peer)
        /// </summary>
        /// <param name="localId"></param>
        /// <param name="destinations"></param>
        /// <param name="packet"></param>
        void EnqueueUnreliableP2P(ushort localId, [NotNull] IList<ClientInfo<TPeer?>> destinations, ArraySegment<byte> packet);

        /// <summary>
        /// Get a buffer from the packet sending pool
        /// </summary>
        /// <returns></returns>
        byte[] GetSendBuffer();

        /// <summary>
        /// Return a buffer to the packet sending pool
        /// </summary>
        /// <param name="buffer"></param>
        void RecycleSendBuffer(byte[] buffer);
    }
}
