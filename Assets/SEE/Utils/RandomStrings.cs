using System;
using System.Linq;

namespace SEE.Utils
{
    /// <summary>
    /// A random generator for strings.
    /// </summary>
    internal class RandomStrings
    {
        private static Random random = new Random();

        /// <summary>
        /// Returns a random string with given <paramref name="length"/>.
        /// </summary>
        /// <param name="length">requested length of the random string</param>
        /// <returns>random string</returns>
        public static string Get(int length = 20)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}