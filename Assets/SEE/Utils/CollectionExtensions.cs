using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace SEE.Utils
{
    /// <summary>
    /// Contains utility extension methods for collections and enumerables.
    /// </summary>
    public static class CollectionExtensions
    {
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
        /// will be evaluated and its result added to the dictionary and returned.
        /// </summary>
        /// <param name="dict">The dictionary from which the value shall be retrieved.</param>
        /// <param name="key">The key for which the value shall be retrieved.</param>
        /// <param name="defaultValue">A lambda returning the default value which shall be added to the dictionary if the key is not present.</param>
        /// <typeparam name="K">The type of the keys in the dictionary.</typeparam>
        /// <typeparam name="V">The type of the values in the dictionary.</typeparam>
        /// <returns>The value for the given <paramref name="key"/> from the given <paramref name="dict"/>.</returns>
        public static V GetOrAdd<K,V>(this IDictionary<K, V> dict, K key, Func<V> defaultValue)
        {
            if (dict.TryGetValue(key, out V value))
            {
                return value;
            }
            else
            {
                return dict[key] = defaultValue();
            }
        }

        /// <summary>
        /// Gets the value for the given <paramref name="key"/> from the given <paramref name="dict"/>.
        /// If the key is not present in the dictionary, the given <paramref name="defaultValue"/>
        /// will be returned instead.
        /// </summary>
        /// <param name="dict">The dictionary from which the value shall be retrieved.</param>
        /// <param name="key">The key for which the value shall be retrieved.</param>
        /// <param name="defaultValue">The default value which shall be returned if the key is not present.</param>
        /// <typeparam name="K">The type of the keys in the dictionary.</typeparam>
        /// <typeparam name="V">The type of the values in the dictionary.</typeparam>
        /// <returns>The value for the given <paramref name="key"/> from the given <paramref name="dict"/>.</returns>
        public static V GetValueOrDefault<K, V>(this IDictionary<K, V> dict, K key, V defaultValue = default)
        {
            return dict.TryGetValue(key, out V value) ? value : defaultValue;
        }

        /// <summary>
        /// Returns true if, from the given <paramref name="booleanOr"/>,
        /// its boolean is true or its value is not null.
        /// </summary>
        /// <param name="booleanOr">The boolean or value to check.</param>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <returns>True if, from the given <paramref name="booleanOr"/>,
        /// its boolean is true or its value is not null.</returns>
        public static bool TrueOrValue<T>(this BooleanOr<T> booleanOr) where T : class
        {
            return booleanOr != null && (booleanOr.Bool || booleanOr.Value != null);
        }

        /// <summary>
        /// Removes all elements from the given <paramref name="list"/> that are contained in the given <paramref name="elements"/>.
        /// </summary>
        /// <param name="list">The list from which the elements shall be removed.</param>
        /// <param name="elements">The elements that shall be removed from the list.</param>
        /// <typeparam name="T">The type of the elements in the list.</typeparam>
        /// <returns>The number of elements that have been removed from the list.</returns>
        public static int RemoveAll<T>(this IList<T> list, IEnumerable<T> elements)
        {
            return elements.Count(list.Remove);
        }

        /// <summary>
        /// Removes all elements from the given <paramref name="list"/> that match the given <paramref name="predicate"/>.
        /// </summary>
        /// <param name="list">The list from which the elements shall be removed.</param>
        /// <param name="predicate">The predicate that the elements must match to be removed.</param>
        /// <typeparam name="T">The type of the elements in the list.</typeparam>
        /// <returns>The number of elements that have been removed from the list.</returns>
        public static int RemoveWhere<T>(this IList<T> list, Predicate<T> predicate)
        {
            return list.RemoveAll(list.Where(x => predicate(x)).ToList());
        }
    }
}
