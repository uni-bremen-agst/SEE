namespace SEE.Utils
{
    /// <summary>
    /// Compares two arrays for equivalence.
    /// </summary>
    public static class ArrayComparer
    {
        /// <summary>
        /// Compares two arrays for equivalence.
        /// </summary>
        /// <typeparam name="T">Type of the arrays.</typeparam>
        /// <param name="a1">First array.</param>
        /// <param name="a2">Second array.</param>
        /// <returns><code>true</code> if arrays are equivalent, <code>false</code> otherwise.
        /// </returns>
        public static bool Equal<T>(T[] a1, T[] a2)
        {
            if (a1 == a2)
            {
                return true;
            }

            if (a1 == null || a2 == null)
            {
                return false;
            }

            if (a1.Length != a2.Length)
            {
                return false;
            }

            for (int i = 0; i < a1.Length; i++)
            {
                if (!a1[i].Equals(a2[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
