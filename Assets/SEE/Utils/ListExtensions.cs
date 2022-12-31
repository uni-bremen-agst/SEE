using System.Collections.Generic;
using System.Linq;
using FuzzySharp.Utils;

namespace SEE.Utils
{
    public static class ListExtensions
    {
        public static void Resize<T>(this List<T> list, int count)
        {
            if (list.Count < count)
            {
                if (list.Capacity < count)
                {
                    list.Capacity = count;
                }

                int end = count - list.Count;
                for (int i = 0; i < end; i++)
                {
                    list.Add(default);
                }
            }
        }

        /// <summary>
        /// Returns all permutations of this <paramref name="inputList"/>.
        /// </summary>
        /// <param name="inputList">The list whose permutations shall be returned</param>
        /// <typeparam name="T">Type of the given list</typeparam>
        /// <returns>All permutations of this <paramref name="inputList"/></returns>
        /// <example>For [1,2,3] this would return {[1,2,3], [1,3,2], [2,1,3], [2,3,1], [3,1,2], [3,2,1]}.</example>
        public static ISet<IList<T>> Permutations<T>(this IList<T> inputList)
        {
            ISet<IList<T>> result = new HashSet<IList<T>>();
            if (inputList.Count <= 1)
            {
                result.Add(inputList);
            }
            else
            {
                foreach (IList<T> permutation in inputList.Skip(1).ToList().Permutations())
                {
                    result.UnionWith(Enumerable.Range(0, inputList.Count)
                                               .Select(i => permutation.Take(i).Concat(inputList.Take(1))
                                                                       .Concat(permutation.Skip(i)).ToList()));
                }
            }

            return result;
        }
    }
}