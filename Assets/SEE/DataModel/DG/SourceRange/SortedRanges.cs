using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.DataModel.DG.SourceRange
{
    /// <summary>
    /// Represents sorted lists of <see cref="Range"/>s.
    /// </summary>
    internal class SortedRanges : IEnumerable<Range>
    {
        /// <summary>
        /// The sorted list of ranges.
        /// </summary>
        private readonly List<Range> values = new();

        /// <summary>
        /// The number of ranges contained.
        /// </summary>
        internal int Count => values.Count;

        /// <summary>
        /// Adds <paramref name="range"/>. If there is any other range already contained
        /// that overlaps with <paramref name="range"/>, an <see cref="ArgumentException"/>
        /// will be thrown.
        /// </summary>
        /// <param name="range">the range to be added</param>
        /// <exception cref="ArgumentException">thrown if there is an overlap with an existing range</exception>
        internal void Add(Range range)
        {
            if (TryGetIndex(range.Start, out int index))
            {
                throw new ArgumentException($"A range {values[index]} overlapping with {range} already exists.");
            }
            // index is the position at which this element would be inserted;
            // but it is not yet inserted
            if ((index < values.Count && values[index].Start <= range.End)
                || (index - 1 >= 0 && range.Start <= values[index - 1].End))
            {
                throw new ArgumentException($"A range {values[index]} overlapping with {range} already exists.");
            }
            values.Insert(index, range);
        }

        /// <summary>
        /// The range at given <paramref name="index"/>. The first index of this
        /// range is 0.
        ///
        /// Precondition: 0 <= <paramref name="index"/> <see cref="Count"/>.
        /// </summary>
        /// <param name="index">index of the requested range</param>
        /// <returns>the range at given <paramref name="index"/></returns>
        internal Range this[int index]
        {
            get => values[index];
            set => values[index] = value;
        }

        /// <summary>
        /// Searches for a range containing the given <paramref name="line"/>.
        /// If one exists, that range will be contained in <paramref name="range"/>
        /// and true will be returned; otherwise false will be returned and <paramref name="range"/>
        /// will be undefined.
        /// </summary>
        /// <param name="line">the line to be searched for</param>
        /// <param name="range">if true is returned, the range containing <paramref name="line"/>;
        /// otherwise undefined</param>
        /// <returns>true if a range exists containg <paramref name="line"/></returns>
        internal bool TryGetValue(int line, out Range range)
        {
            if (TryGetIndex(line, out int index))
            {
                range = values[index];
                return true;
            }
            else
            {
                range = default;
                return false;
            }
        }

        /// <summary>
        /// Searches for a range containing the given <paramref name="line"/>.
        /// If one exists, the index of this range will be contained in <paramref name="index"/>
        /// and true will be returned; otherwise false will be returned and <paramref name="index"/>
        /// is the position at which a range would be added starting with <paramref name="line"/>.
        /// </summary>
        /// <param name="line">the line to be searched for</param>
        /// <param name="index">if true is returned, the index of the range containing <paramref name="line"/>;
        /// otherwise the position at which a range would be added starting with <paramref name="line"/></param>
        /// <returns>true if a range exists containg <paramref name="line"/></returns>
        /// <remarks>A binary search is conducted.</remarks>
        private bool TryGetIndex(int line, out int index)
        {
            int low = 0;
            int high = values.Count - 1;
            index = 0;
            while (low <= high)
            {
                index = (low + high) / 2;
                if (line > values[index].End)
                {
                    low = index + 1;
                }
                else if (line < values[index].Start)
                {
                    high = index - 1;
                }
                else
                {
                    // matching range found at index
                    return true;
                }
            }
            index = low;
            return false;
        }

        /// <summary>
        /// Dumps the sorted list of ranges. May be used for debugging.
        /// </summary>
        internal void Dump()
        {
            int i = 0;
            foreach (Range range in values)
            {
                Debug.Log($"{i} => {range}\n");
                i++;
            }
        }

        /// <summary>
        /// Allows to enumerate on the sorted ranges.
        /// </summary>
        /// <returns>enumerator for the sorted ranges</returns>
        IEnumerator<Range> IEnumerable<Range>.GetEnumerator()
        {
            return values.GetEnumerator();
        }

        /// <summary>
        /// Allows to enumerate on the sorted ranges.
        /// </summary>
        /// <returns>enumerator for the sorted ranges</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)values).GetEnumerator();
        }
    }
}
