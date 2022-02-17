using System;
using Dissonance.Threading;
using JetBrains.Annotations;

namespace Dissonance.Datastructures
{
    /// <summary>
    /// A fixed size pool of items which is safe to put/get concurrently (but not put/put or get/get)
    /// </summary>
    public class ConcurrentPool<T>
        : IRecycler<T>
        where T : class
    {
        #region fields and properties
        private readonly Func<T> _factory;

        private readonly TransferBuffer<T> _items;

        private readonly ReadonlyLockedValue<int> _getter = new ReadonlyLockedValue<int>(1);
        private readonly ReadonlyLockedValue<int> _putter = new ReadonlyLockedValue<int>(2);
        #endregion

        public ConcurrentPool(int maxSize, Func<T> factory)
        {
            _factory = factory;
            _items = new TransferBuffer<T>(maxSize);
        }

        /// <summary>
        /// Get an item from this pool
        /// </summary>
        /// <returns></returns>
        [NotNull] public T Get()
        {
            using (_getter.Lock())
            {
                T item;
                if (_items.Read(out item) && !ReferenceEquals(item, null))
                    return item;
                else
                    return _factory();
            }
        }

        /// <summary>
        /// Return an item to the pool
        /// </summary>
        /// <param name="item"></param>
        public void Put([NotNull] T item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            using (_putter.Lock())
            {
                _items.TryWrite(item);
            }
        }

        void IRecycler<T>.Recycle([NotNull] T item)
        {
            Put(item);
        }
    }
}
