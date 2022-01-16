using System;

namespace SEE.Net
{
    /// <summary>
    /// Common superclass of all exceptions thrown by <see cref="SEE.Net"/>
    /// in case of networking problems.
    /// </summary>
    internal abstract class NetworkException : Exception
    {
        /// <summary>
        /// Initializes a new instance of this exception class with the
        /// specified error <paramref name="message"/>.
        /// </summary>
        /// <param name="message">error message</param>
        public NetworkException(string message) : base(message)
        { }
    }

    /// <summary>
    /// Thrown if a client cannot establish a connection to a server.
    /// </summary>
    internal class NoServerConnection : NetworkException
    {
        /// <summary>
        /// Initializes a new instance of this exception class with the
        /// specified error <paramref name="message"/>.
        /// </summary>
        /// <param name="message">error message</param>
        public NoServerConnection(string message) : base(message)
        { }
    }

    /// <summary>
    /// Thrown if a server cannot be started.
    /// </summary>
    internal class CannotStartServer : NetworkException
    {
        /// <summary>
        /// Initializes a new instance of this exception class with the
        /// specified error <paramref name="message"/>.
        /// </summary>
        /// <param name="message">error message</param>
        public CannotStartServer(string message) : base(message)
        { }
    }

}
