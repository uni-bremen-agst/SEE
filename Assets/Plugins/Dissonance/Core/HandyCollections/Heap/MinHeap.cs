using System;
using System.Collections.Generic;

// Justification: this file is pulled from another project, we don't want warnings here
// ReSharper disable AnnotateNotNullParameter, InheritdocConsiderUsage, MemberCanBePrivate.Global

// ReSharper disable once CheckNamespace (Justification: if we ever pull in the rest of HandyCollections we can just delete this file with no breakage)
namespace HandyCollections.Heap
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class MinHeap<T>
        : IMinHeap<T>
    {
        #region fields and properties
        private readonly List<T> _heap;
        private readonly IComparer<T> _comparer;

        /// <summary>
        /// Get the number of items in this heap
        /// </summary>
        public int Count
        {
            get { return _heap.Count; }
        }

        /// <summary>
        /// Get the minimum value in this heap
        /// </summary>
        public T Minimum
        {
            get { return _heap[0]; }
        }

        private bool _allowResize = true;
        public bool AllowHeapResize
        {
            get { return _allowResize; }
            set { _allowResize = value; }
        }
        #endregion

        #region constructors
        /// <summary>
        /// 
        /// </summary>
        public MinHeap()
            : this(64)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public MinHeap(int capacity)
            : this(capacity, Comparer<T>.Default)
        {
            //Contract.Requires(capacity >= 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="comparer"></param>
        public MinHeap(int capacity, IComparer<T> comparer)
        {
            //Contract.Requires(capacity >= 0);

            _heap = new List<T>(capacity);
            _comparer = comparer;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="capacity"></param>
        ///// <param name="comparison"></param>
        //public MinHeap(int capacity, Comparison<T> comparison)
        //    : this(capacity, Comparer<T>.Create(comparison))
        //{
        //    Contract.Requires(capacity >= 0);
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="comparer"></param>
        public MinHeap(IComparer<T> comparer)
        {
            _heap = new List<T>();
            _comparer = comparer;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="comparison"></param>
        //public MinHeap(Comparison<T> comparison)
        //    : this(Comparer<T>.Create(comparison))
        //{
        //}
        #endregion

        #region add
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            if (!_allowResize && _heap.Count == _heap.Capacity)
                throw new InvalidOperationException("Heap is full and resizing is disabled");

            _heap.Add(item);
            BubbleUp(_heap.Count - 1);

            DebugCheckHeapProperty();
        }

        /// <summary>
        /// Add a large number of items to the heap. This is more efficient that simply calling add on each item individually
        /// </summary>
        /// <param name="items"></param>
        public void Add(IEnumerable<T> items)
        {
            _heap.AddRange(items);
            Heapify();

            DebugCheckHeapProperty();
        }

        /// <summary>
        /// Establish the heap property (use this if you mutate an item already in the heap)
        /// </summary>
        public void Heapify()
        {
            // Using sorting is tempting, for it's sheer simplicity:
            //    _heap.Sort(_comparer);
            // There's also the possibility the framework implemented sorting algorithm is way better/faster than my heapify function in practical cases.
            // Benchmarking reveals sorting to *always* be slower, on both tiny heaps (10 items) and massive heaps (1000000 items)

            for (var i = _heap.Count - 1; i >= 0; i--)
                TrickleDown(i);

            DebugCheckHeapProperty();
        }

        /// <summary>
        /// Establish the heap property (use this if you mutate an item already in the heap)
        /// </summary>
        /// <param name="mutated">The index of the item which was mutated</param>
        public void Heapify(int mutated)
        {
            if (mutated < 0 || mutated >= Count)
                throw new IndexOutOfRangeException("mutated");

            //We either need to move this item up or down the heap. Try trickling down, and if that does nothing bubble up instead
            if (TrickleDown(mutated) == mutated)
                BubbleUp(mutated);
        }
        #endregion

        #region remove
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public T RemoveMin()
        {
            return RemoveAt(0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public T RemoveAt(int index)
        {
            if (index < 0 || index > _heap.Count)
                throw new ArgumentOutOfRangeException("index");

            var removed = _heap[index];

            //Move the last item into the first position
            _heap[index] = _heap[_heap.Count - 1];
            _heap.RemoveAt(_heap.Count - 1);

            if (_heap.Count > 0 && index < _heap.Count)
                Heapify(0);

            DebugCheckHeapProperty();

            return removed;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            _heap.Clear();
        }
        #endregion

        #region private helpers
        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                var parent = ParentIndex(index);
                if (IsLessThan(_heap[index], _heap[parent]))
                {
                    Swap(parent, index);
                    index = parent;
                }
                else
                    break;
            }
        }

        private int TrickleDown(int index)
        {
            // This code was automatically converted to iteration instead of tail recursion
            // WTB C# tail call keyword!
            /* if (index >= _heap.Count)
                throw new ArgumentException();
            int smallestChildIndex = SmallestChildSmallerThan(index, _heap[index]);
            if (smallestChildIndex == -1)
                return index;
            Swap(smallestChildIndex, index);
            return TrickleDown(smallestChildIndex); */

            while (true)
            {
                if (index >= _heap.Count)
                    throw new ArgumentException();

                var smallestChildIndex = SmallestChildSmallerThan(index, _heap[index]);
                if (smallestChildIndex == -1)
                    return index;

                Swap(smallestChildIndex, index);
                index = smallestChildIndex;
            }
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void DebugCheckHeapProperty()
        {
            //// ReSharper disable once InvokeAsExtensionMethod
            //if (Enumerable.Any(_heap, t => IsLessThan(t, Minimum)))
            //    throw new Exception("Heap property violated");
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsLessThan(T a, T b)
        {
            return _comparer.Compare(a, b) < 0;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ParentIndex(int i)
        {
            return (i - 1) / 2;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Swap(int a, int b)
        {
            var temp = _heap[a];
            _heap[a] = _heap[b];
            _heap[b] = temp;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LeftChild(int i)
        {
            return 2 * i + 1;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int RightChild(int i)
        {
            return 2 * i + 2;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int SmallestChildSmallerThan(int i, T item)
        {
            var leftChildIndex = LeftChild(i);
            var rightChildIndex = RightChild(i);

            var smallest = -1;
            if (leftChildIndex < _heap.Count)
                smallest = leftChildIndex;
            if (rightChildIndex < _heap.Count && IsLessThan(_heap[rightChildIndex], _heap[leftChildIndex]))
                smallest = rightChildIndex;

            if (smallest > -1 && IsLessThan(_heap[smallest], item))
                return smallest;

            return -1;
        }
        #endregion

        #region searching
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(T item)
        {
            return _heap.IndexOf(item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public int IndexOf(Predicate<T> predicate)
        {
            return _heap.FindIndex(predicate);
        }
        #endregion
    }
}