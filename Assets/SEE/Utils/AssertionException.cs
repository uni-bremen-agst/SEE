using System;

namespace SEE.Utils
{
    /// <summary>
    /// Thrown when an assumption checked by one of <see cref="Assertion"/>'s
    /// throwing methods does not hold.
    ///
    /// This takes the place of <c>UnityEngine.Assertions.AssertionException</c>
    /// for the layers of SEE that must not depend on Unity.
    /// </summary>
    public class AssertionException : Exception
    {
        /// <summary>
        /// A violated assumption without further description.
        /// </summary>
        public AssertionException()
        {
        }

        /// <summary>
        /// A violated assumption described by <paramref name="message"/>.
        /// </summary>
        /// <param name="message">A message describing the assumption.</param>
        public AssertionException(string message) : base(message)
        {
        }

        /// <summary>
        /// A violated assumption described by <paramref name="message"/> and caused
        /// by <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">A message describing the assumption.</param>
        /// <param name="innerException">The exception that led to this one.</param>
        public AssertionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
