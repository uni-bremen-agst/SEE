using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Dissonance.Datastructures
{
    /// <summary>
    /// A set of (Pow2) buffers
    /// </summary>
    internal class POTBuffer
    {
        private readonly List<float[]> _buffers;

        /// <summary>
        /// Maximum count which can be allocated
        /// </summary>
        public uint MaxCount { get; private set; }

        /// <summary>
        /// Power of two of the largest buffer. i.e. 2^Pow2 == Largest Buffer Size
        /// </summary>
        public uint Pow2
        {
            get { return (uint)_buffers.Count; }
        }

        /// <summary>
        /// Current allocated size of the buffer
        /// </summary>
        public uint Count { get; private set; }

        public POTBuffer(byte initialMaxPow)
        {
            _buffers = new List<float[]>(initialMaxPow);
            for (var i = 0; i < initialMaxPow; i++)
                _buffers.Add(new float[1 << i]);

            MaxCount = (uint)(1 << initialMaxPow) - 1;
        }

        /// <summary>
        /// Mark all buffers as unused
        /// </summary>
        public void Free()
        {
            Count = 0;
        }

        /// <summary>
        /// Set the count of accessible buffers
        /// </summary>
        /// <param name="count"></param>
        public void Alloc(uint count)
        {
            if (count > MaxCount)
                throw new ArgumentOutOfRangeException("count", "count is larger than buffer capacity");

            Count = count;
        }

        /// <summary>
        /// Expand the size of the buffer up to the next power of 2
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        public bool Expand(int limit = int.MaxValue)
        {
            if (Count != 0)
                throw new InvalidOperationException("Cannot expand buffer while it is in use");

            //Check if expanding the buffer would exceed the limit
            var newMax = (uint)(1 << (_buffers.Count + 1)) - 1;
            if (newMax > limit)
                return false;

            //Expand the buffer
            _buffers.Add(new float[1 << _buffers.Count]);
            MaxCount = newMax;
            return true;
        }

        /// <summary>
        /// Get the largest buffer which is smaller than the given count. Must have previously allocated sufficient space.
        /// </summary>
        /// <param name="count">max size of buffer to return. Updated to subtract off the size of the buffer returned</param>
        /// <param name="zeroed"></param>
        /// <returns></returns>
        [NotNull] public float[] GetBuffer(ref uint count, bool zeroed = false)
        {
            if (count > Count)
                throw new ArgumentOutOfRangeException("count", "count must be <= the total allocated size (set with Alloc(count))");
            if (count == 0)
                throw new ArgumentOutOfRangeException("count", "count must be > 0");

            //Find the largest array which fits within the requested amount
            for (var i = _buffers.Count - 1; i >= 0; i--)
            {
                var buf = _buffers[i];

                if (buf.Length <= count)
                {
                    //Subtract off the count the amount of space we've supplied
                    checked { count = (uint)(count - buf.Length); }

                    //Zero out the array as necessary and return it
                    if (zeroed)
                        Array.Clear(buf, 0, buf.Length);
                    return buf;
                }
            }

            // This can only happen if there is no buffer which is smaller than the count argument. Since we have buffers all the way down to 1 item...
            // ...it can obviously only happen is `count` is larger than the largest buffer. However we check that count is not larger than Count...
            // ...and inside Alloc we check that Count is less than MaxCount. We have buffers all the way up to that size, so this can't happen.
            //ncrunch: no coverage start
            throw new InvalidOperationException("Failed to find a correctly sized buffer to service request");
            //ncrunch: no coverage end
        }
    }
}