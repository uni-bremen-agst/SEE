using System.Collections.Generic;
using System.Linq;

namespace SEE.Utils
{
    /// <summary>
    /// Contains utility extension methods for collections and enumerables.
    /// </summary>
    public static class CollectionExtensions
    {
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

        /// <summary>
        /// Toggles the given <paramref name="element"/> in the given <paramref name="set"/>,
        /// that is, if the set contains the element, it will be removed, otherwise it will be added.
        /// </summary>
        /// <param name="set">The set in which the element shall be toggled.</param>
        /// <param name="element">The element which shall be toggled.</param>
        /// <typeparam name="T">The type of the elements in the set.</typeparam>
        public static void Toggle<T>(this ISet<T> set, T element)
        {
            if (!set.Add(element))
            {
                set.Remove(element);
            }
        }

        /// <summary>
        /// Gets the value for the given <paramref name="key"/> from the given <paramref name="dict"/>.
        /// If the key is not present in the dictionary, the given <paramref name="defaultValue"/>
        /// will be added to the dictionary and returned.
        /// </summary>
        /// <param name="dict">The dictionary from which the value shall be retrieved.</param>
        /// <param name="key">The key for which the value shall be retrieved.</param>
        /// <param name="defaultValue">The default value which shall be added to the dictionary if the key is not present.</param>
        /// <typeparam name="K">The type of the keys in the dictionary.</typeparam>
        /// <typeparam name="V">The type of the values in the dictionary.</typeparam>
        /// <returns>The value for the given <paramref name="key"/> from the given <paramref name="dict"/>.</returns>
        public static V GetOrAdd<K,V>(this IDictionary<K, V> dict, K key, V defaultValue)
        {
            if (dict.TryGetValue(key, out V value))
            {
                return value;
            }
            else
            {
                return dict[key] = defaultValue;
            }
        }
    }
}
