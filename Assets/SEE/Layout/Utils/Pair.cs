namespace SEE.Layout.Utils
{
    /// <summary>
    /// Provides ordered pairs of arbitrary element types.
    /// </summary>
    /// <typeparam name="A">type of first element</typeparam>
    /// <typeparam name="B">type of second element</typeparam>
    internal class Pair<A, B>
    {
        /// <summary>
        /// The first pair element
        /// </summary>
        protected A First;

        /// <summary>
        /// The second pair element.
        /// </summary>
        protected B Second;

        /// <summary>
        /// Constructor for a new pair.
        /// </summary>
        /// <param name="a">The first element.</param>
        /// <param name="b">The second element.</param>
        public Pair(A a, B b)
        {
            First = a;
            Second = b;
        }

        /// <summary>
        /// Yields the first element of the pair.
        /// </summary>
        /// <returns>The first element of the pair.</returns>
        public A GetFirst()
        {
            return First;
        }

        /// <summary>
        /// Yields the second element of the pair.
        /// </summary>
        /// <returns>The second element of the pair.</returns>
        public B GetSecond()
        {
            return Second;
        }

        /// <summary>
        /// Creates new pair of elements.
        /// </summary>
        /// <param name="a">First element.</param>
        /// <param name="b">Second element.</param>
        /// <returns>New pair.</returns>
        public static Pair<A, B> Of(A a, B b)
        {
            return new Pair<A, B>(a, b);
        }
    }
}
