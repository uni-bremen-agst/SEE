using System.Collections.Generic;

namespace SEE.Utils
{
    /// <summary>
    /// Allows to permute a list of values.
    /// </summary>
    /// <typeparam name="T">the type of the values</typeparam>
    internal static class Permuter<T>
    {
        /// <summary>
        /// Returns all permutations of <paramref name="values"/>.
        /// </summary>
        /// <param name="values">the list of values to be permuted</param>
        /// <returns>all permutations of <paramref name="values"/></returns>
        /// <remarks>Obviously this is an expensive operation in the order of O(values.Count!)</remarks>
        internal static IList<IList<T>> Permute(T[] values)
        {
            return PermuteRange(values, 0, values.Length - 1, new List<IList<T>>());
        }

        /// <summary>
        /// Permutes the <paramref name="values"/> in the range from <paramref name="start"/>
        /// to <paramref name="end"/>. The permutations will be added to <paramref name="list"/>.
        /// </summary>
        /// <param name="values">the list of values to be permuted</param>
        /// <param name="start">begin of the range in <paramref name="values"/> to be permuted</param>
        /// <param name="end">end of the range in <paramref name="values"/> to be permuted</param>
        /// <param name="list">the list of permutations to extended</param>
        /// <returns>resulting permutations</returns>
        private static IList<IList<T>> PermuteRange(T[] values, int start, int end, IList<IList<T>> list)
        {
            if (start == end)
            {
                // We have one of our possible n! solutions,
                // add it to the list.
                list.Add(new List<T>(values));
            }
            else
            {
                for (var i = start; i <= end; i++)
                {
                    Swap(ref values[start], ref values[i]);
                    PermuteRange(values, start + 1, end, list);
                    Swap(ref values[start], ref values[i]);
                }
            }
            return list;
        }

        /// <summary>
        /// Swaps the values of <paramref name="left"/> and <paramref name="right"/>.
        /// </summary>
        /// <param name="left">left value to be swapped</param>
        /// <param name="right">right value to be swapped</param>
        private static void Swap(ref T left, ref T right)
        {
            (right, left) = (left, right);
        }
    }
}
