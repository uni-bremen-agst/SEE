using System;

namespace SEE.UI.Window
{
    /// Exception that is thrown whenever a value of a particular type was expected,
    /// but a value of an unexpected type was supplied instead.
    public class UnsupportedTypeException : Exception
    {
        /// <summary>
        /// Creates an exception that indicates a value of type <paramref name="expectedType"/> was expected,
        /// but a value of type <paramref name="actualType"/> was supplied instead.
        /// </summary>
        /// <param name="expectedType">The expected type of the value.</param>
        /// <param name="actualType">The actual type of the value.</param>
        public UnsupportedTypeException(Type expectedType, Type actualType)
            : base($"Expected a value of type {expectedType}, but got {actualType}!")
        {
        }
    }
}
