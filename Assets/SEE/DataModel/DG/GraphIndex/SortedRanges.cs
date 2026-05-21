using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.DataModel.DG.GraphIndex
{
    /// <summary>
    /// Represents sorted lists of <see cref="SourceRange"/>s.
    /// </summary>
    internal class SortedRanges : IEnumerable<SourceRange>
    {
        /// <summary>
        /// The sorted list of ranges.
        /// </summary>
        private readonly List<SourceRange> values = new();

        /// <summary>
        /// The number of ranges contained.
        /// </summary>
        internal int Count => values.Count;

        /// <summary>
        /// Adds <paramref name="range"/>. If there is any other range already contained
        /// that overlaps with <paramref name="range"/>, an <see cref="ArgumentException"/>
        /// will be thrown.
        /// </summary>
        /// <param name="range">The range to be added.</param>
        /// <exception cref="ArgumentException">Thrown if there is an overlap with an existing range.</exception>
        internal void Add(SourceRange range)
        {
            if (TryGetIndex(range.Range.StartLine, out int index))
            {
                throw new ArgumentException($"A range {values[index]} overlapping with {range} already exists.");
            }
            // index is the position at which this element would be inserted;
            // but it is not yet inserted
            if ((index < values.Count && values[index].Range.StartLine < range.Range.EndLine)
                || (index - 1 >= 0 && range.Range.StartLine < values[index - 1].Range.EndLine))
            {
                throw new ArgumentException($"A range {values[index]} overlapping with {range} already exists.");
            }
            values.Insert(index, range);
        }

        /// <summary>
        /// The range at given <paramref name="index"/>. The first index of this
        /// range is 0.
        ///
        /// Precondition: 0 &lt;= <paramref name="index"/> &lt; <see cref="Count"/>.
        /// </summary>
        /// <param name="index">Index of the requested range.</param>
        /// <returns>The range at given <paramref name="index"/>.</returns>
        internal SourceRange this[int index]
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
        /// <param name="line">The line to be searched for.</param>
        /// <param name="range">If true is returned, the range containing <paramref name="line"/>;
        /// otherwise undefined.</param>
        /// <returns>True if a range exists containing <paramref name="line"/>.</returns>
        internal bool TryGetValue(int line, out SourceRange range)
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
        /// <param name="line">The line to be searched for.</param>
        /// <param name="index">If true is returned, the index of the range containing <paramref name="line"/>;
        /// otherwise the position at which a range would be added starting with <paramref name="line"/>.</param>
        /// <returns>True if a range exists containg <paramref name="line"/>.</returns>
        /// <remarks>A binary search is conducted.</remarks>
        private bool TryGetIndex(int line, out int index)
        {
            int low = 0;
            int high = values.Count - 1;
            index = 0;
            while (low <= high)
            {
                index = (low + high) / 2;
                if (line >= values[index].Range.EndLine)
                {
                    low = index + 1;
                }
                else if (line < values[index].Range.StartLine)
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
            foreach (SourceRange range in values)
            {
                Debug.Log($"{i} => {range}\n");
                i++;
            }
        }

        /// <summary>
        /// Allows to enumerate on the sorted ranges.
        /// </summary>
        /// <returns>Enumerator for the sorted ranges.</returns>
        IEnumerator<SourceRange> IEnumerable<SourceRange>.GetEnumerator()
        {
            return values.GetEnumerator();
        }

        /// <summary>
        /// Allows to enumerate on the sorted ranges.
        /// </summary>
        /// <returns>Enumerator for the sorted ranges.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)values).GetEnumerator();
        }
    }
}
