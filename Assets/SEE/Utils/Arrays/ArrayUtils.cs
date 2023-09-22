using System;
using System.Linq;

namespace SEE.Utils
{
    public static class ArrayUtils
    {
        /// <summary>
        /// Creates new array of length <paramref name="length"/> and each slot with the result of
        /// <paramref name="objCreatorFunc"/>, respectively.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="length">The lenght of the array.</param>
        /// <param name="objCreatorFunc">The function, that creates the object for each given index.</param>
        /// <returns>The created array.</returns>
        public static T[] New<T>(int length, Func<int, T> objCreatorFunc)
        {
            return Enumerable.Range(0, length).Select(objCreatorFunc).ToArray();
        }
    }
}
