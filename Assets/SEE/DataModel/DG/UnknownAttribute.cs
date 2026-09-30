using System;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// An exception thrown in case a graph, node, or edge attribute is unknown.
    /// </summary>
    [Serializable]
    public class UnknownAttribute : Exception
    {
        /// <summary>
        /// For an unknown attribute of an <see cref="Attributable"/>, this constructor
        /// creates an exception with a message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public UnknownAttribute(string message)
            : base(message)
        {
        }

        /// <summary>
        /// For an unknown attribute of an <see cref="Attributable"/>, this constructor
        /// creates an exception with a message and an inner exception that is the cause
        /// of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The inner exception that is the cause of this exception.</param>
        public UnknownAttribute(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
