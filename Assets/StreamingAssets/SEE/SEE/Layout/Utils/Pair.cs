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
        protected A first;

        /// <summary>
        /// The second pair element.
        /// </summary>
        protected B second;

        /// <summary>
        /// Constructor for a new pair.
        /// </summary>
        /// <param name="a">the first element</param>
        /// <param name="b">the second element</param>
        public Pair(A a, B b)
        {
            first = a;
            second = b;
        }

        /// <summary>
        /// Yields the first element of the pair.
        /// </summary>
        /// <returns>the first element of the pair</returns>
        public A getFirst()
        {
            return first;
        }

        /// <summary>
        /// Yields the second element of the pair.
        /// </summary>
        /// <returns>the second element of the pair</returns>
        public B getSecond()
        {
            return second;
        }

        /// <summary>
        /// Creates new pair of elements.
        /// </summary>
        /// <param name="a">first element</param>
        /// <param name="b">second element</param>
        /// <returns>new pair</returns>
        public static Pair<A, B> of(A a, B b)
        {
            return new Pair<A, B>(a, b);
        }
    }
}
