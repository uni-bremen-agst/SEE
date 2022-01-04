namespace Dissonance.Datastructures
{
    /// <summary>
    /// Stores the N most recently added items
    /// </summary>
    internal class RingBuffer<T>
        where T : struct
    {
        #region fields and properties
        private readonly T[] _items;

        /// <summary>
        /// Indicates the number of items added to the collection and currently stored
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// The size of this ring buffer
        /// </summary>
        public int Capacity
        {
            get { return _items.Length; }
        }

        private int _end;
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="size"></param>
        public RingBuffer(uint size)
        {
            _items = new T[size];
        }

        /// <summary>
        /// Add an item to the buffer
        /// </summary>
        /// <param name="item"></param>
        /// <returns>The item removed from the buffer, or null if no item was pushed out</returns>
        public T? Add(T item)
        {
            //Save the item we are about to remove
            T? dequeued = null;
            if (Count == Capacity)
                dequeued = _items[_end];

            //Insert the new item
            _items[_end] = item;
            _end = (_end + 1) % _items.Length;

            //Update count (unless we wrapped around)
            if (Count < _items.Length)
                Count++;

            return dequeued;
        }

        /// <summary>
        /// Remove all items from the buffer
        /// </summary>
        public void Clear()
        {
            Count = 0;
            _end = 0;
        }
    }
}
