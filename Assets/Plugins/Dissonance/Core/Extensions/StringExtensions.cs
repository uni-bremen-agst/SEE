using JetBrains.Annotations;

namespace Dissonance.Extensions
{
    internal static class StringExtensions
    {
        /// <summary>
        /// String hashing is documented as specifically not guaranteed stable (different instances of the same program may hash the same string differently).
        /// This is a stable hash, it will always return the same value for the same string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int GetFnvHashCode([CanBeNull] this string str)
        {
            if (ReferenceEquals(str, null))
                return 0;

            unchecked
            {
                //dotnet string hashing is documented as not guaranteed stable between runtimes!
                //Implement our own hash to ensure stability (FNV-1a Hash http://isthe.com/chongo/tech/comp/fnv/#FNV-1a)
                var hash = 2166136261;

                for (var i = 0; i < str.Length; i++)
                {
                    //FNV works on bytes, so split this char into 2 bytes
                    var c = str[i];
                    var b1 = (byte)(c >> 8);
                    var b2 = (byte)c;

                    hash = hash ^ b1;
                    hash = hash * 16777619;

                    hash = hash ^ b2;
                    hash = hash * 16777619;
                }

                return (int)hash;
            }
        }
    }
}
