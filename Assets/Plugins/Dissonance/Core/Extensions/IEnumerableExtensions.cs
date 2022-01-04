using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Dissonance.Extensions
{
    public static class IEnumerableExtensions
    {
        [NotNull] public static IEnumerable<T> Concat<T>([NotNull] this IEnumerable<T> enumerable, T tail)
        {
            if (enumerable == null)
                throw new ArgumentNullException("enumerable");

            return ConcatUnsafe(enumerable, tail);
        }

        [NotNull] private static IEnumerable<T> ConcatUnsafe<T>([NotNull] this IEnumerable<T> enumerable, T tail)
        {
            foreach (var item in enumerable)
                yield return item;
            yield return tail;
        }
    }
}
