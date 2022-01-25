using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace (Justification: if we ever pull in the rest of HandyCollections we can just delete this file with no breakage)
namespace HandyCollections.Heap
{
    /// <summary>
    /// A binary heap which allows efficient querying and removal of the minimum item in the heap
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IMinHeap<T>
    {
        /// <summary>
        /// Number of items in the heap
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Peek the minimum value on the heap
        /// </summary>
        T Minimum { get; }

        /// <summary>
        /// Add a new item to the heap
        /// </summary>
        /// <param name="item"></param>
        void Add(T item);

        /// <summary>
        /// Add a lot of items to the heap (more efficient than calling Add(item) lots)
        /// </summary>
        /// <param name="items"></param>
        void Add(IEnumerable<T> items);

        /// <summary>
        /// Remove the minimum item from the heap
        /// </summary>
        /// <returns></returns>
        T RemoveMin();

        /// <summary>
        /// Remove the item at the given index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        T RemoveAt(int index);

        /// <summary>
        /// Get the index of a given item (or -1 if cannot be found)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        int IndexOf(T item);

        /// <summary>
        /// Get the index of the first item which matches the given predicate (or -1 if cannot be found)
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        int IndexOf(Predicate<T> predicate);

        /// <summary>
        /// Remove all items from the heap
        /// </summary>
        void Clear();
    }
}
