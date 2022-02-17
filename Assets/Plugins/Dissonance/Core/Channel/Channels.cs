using System;
using System.Collections.Generic;
using Dissonance.Audio.Capture;
using Dissonance.Datastructures;
using JetBrains.Annotations;

namespace Dissonance
{
    /// <summary>
    /// Base class for a collection of channels
    /// </summary>
    /// <typeparam name="T">Type of the channel</typeparam>
    /// <typeparam name="TId">Type of the unique ID which identifies this channel</typeparam>
    public abstract class Channels<T, TId>
        where T : IChannel<TId>, IEquatable<T>
        where TId : IEquatable<TId>
    {
        protected readonly Log Log;

        private readonly Dictionary<ushort, T> _openChannelsBySubId;
        private readonly Pool<ChannelProperties> _propertiesPool;

        private ushort _nextId;

        public event Action<TId, ChannelProperties> OpenedChannel;
        public event Action<TId, ChannelProperties> ClosedChannel;

        /// <summary>
        /// Number of currently open channels
        /// </summary>
        public int Count
        {
            get { return _openChannelsBySubId.Count; }
        }

        internal Channels([NotNull] IChannelPriorityProvider priorityProvider)
        {
            if (priorityProvider == null)
                throw new ArgumentNullException("priorityProvider");

            Log = Logs.Create(LogCategory.Core, GetType().Name);

            _openChannelsBySubId = new Dictionary<ushort, T>();
            _propertiesPool = new Pool<ChannelProperties>(64, () => new ChannelProperties(priorityProvider));
        }

        [NotNull] protected abstract T CreateChannel(ushort subscriptionId, TId channelId, ChannelProperties properties);

        /// <summary>
        /// Check if the given item is currently in this collection (i.e. is the channel open)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains([NotNull] T item)
        {
            return _openChannelsBySubId.ContainsKey(item.SubscriptionId);
        }

        /// <summary>
        /// Open a new channel
        /// </summary>
        /// <param name="id"></param>
        /// <param name="positional"></param>
        /// <param name="priority"></param>
        /// <param name="amplitudeMultiplier"></param>
        /// <returns></returns>
        [NotNull] public T Open([NotNull] TId id, bool positional = false, ChannelPriority priority = ChannelPriority.Default, float amplitudeMultiplier = 1)
        {
            if (EqualityComparer<TId>.Default.Equals(id, default(TId)))
                throw new ArgumentNullException("id", "Cannot open a channel with a null ID");

            //Sanity check to ensure we don't enter an infinite loop
            if (_openChannelsBySubId.Count >= ushort.MaxValue)
            {
                throw Log.CreateUserErrorException(
                    "Attempted to open 65535 channels",
                    "Opening too many speech channels without closing them",
                    "https://placeholder-software.co.uk/dissonance/docs/Tutorials/Script-Controlled-Speech.html",
                    "7564ECCA-73C2-4720-B4C0-B873E63216AD"
                );
            }

            //Generate a new ID for this channel (never zero, we use that elsewhere to indicate null channel)
            ushort subId;
            do
            {
                subId = unchecked(_nextId++);
                if (subId == 0)
                    subId++;

            } while (_openChannelsBySubId.ContainsKey(subId));

            var properties = _propertiesPool.Get();
            properties.Id = subId;
            properties.Positional = positional;
            properties.Priority = priority;
            properties.AmplitudeMultiplier = amplitudeMultiplier;

            var channel = CreateChannel(subId, id, properties);

            _openChannelsBySubId.Add(channel.SubscriptionId, channel);

            var handler = OpenedChannel;
            if (handler != null) handler(channel.TargetId, channel.Properties);

            return channel;
        }

        public bool Close([NotNull] T channel)
        {
            if (EqualityComparer<T>.Default.Equals(channel, default(T)))
                throw new ArgumentNullException("channel", "Cannot close a null channel");

            var removed = _openChannelsBySubId.Remove(channel.SubscriptionId);
            if (removed)
            {
                channel.Properties.Id = 0;
                _propertiesPool.Put(channel.Properties);

                var handler = ClosedChannel;
                if (handler != null) handler(channel.TargetId, channel.Properties);
            }

            return removed;
        }

        /// <summary>
        /// Close and immediately re-open all channels (keeping handles to open channels valid)
        /// </summary>
        internal void Refresh()
        {
            //Raise event to close channels
            using (var enumerator = _openChannelsBySubId.GetEnumerator())
                while (enumerator.MoveNext())
                    if (ClosedChannel != null)
                        ClosedChannel(enumerator.Current.Value.TargetId, enumerator.Current.Value.Properties);

            //Raise event to open channels
            using (var enumerator = _openChannelsBySubId.GetEnumerator())
                while (enumerator.MoveNext())
                    if (OpenedChannel != null)
                        OpenedChannel(enumerator.Current.Value.TargetId, enumerator.Current.Value.Properties);
        }

        public Dictionary<ushort, T>.Enumerator GetEnumerator()
        {
            return _openChannelsBySubId.GetEnumerator();
        }
    }
}
