using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SEE.Utils
{
    /// <summary>
    /// A thread-safe hash set.
    /// </summary>
    /// <typeparam name="T">the element type of the set</typeparam>
    public class ThreadSafeHashSet<T> : IEnumerable<T>
    {
        /// <summary>
        /// Content of the set.
        /// We use a dictionary with a dummy value to simulate a set.
        /// </summary>
        private readonly ConcurrentDictionary<T, bool> content = new();

        /// <summary>
        /// Adds an <paramref name="item"/> to the set.
        /// </summary>
        /// <param name="item">Item to be added.</param>
        /// <returns>True if the item was added; false if it already existed.</returns>
        public bool Add(T item)
        {
            return content.TryAdd(item, true);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the set.
        /// </summary>
        /// <returns>Enumerator for the set.</returns>
        public IEnumerator GetEnumerator()
        {
            return content.Keys.GetEnumerator();
        }

        /// <summary>
        /// The elements of the set as an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <returns>Elements of the set.</returns>

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return content.Keys.GetEnumerator();
        }

        /// <summary>
        /// The number of elements in the set.
        /// </summary>
        public int Count => content.Count;
    }
}
