using System;
using System.IO;

namespace SEE.Utils
{
    /// <summary>
    /// An <see cref="ILogger"/> writing to the standard output streams.
    ///
    /// This is the default of <see cref="Logging.Logger"/>. It exists so that the
    /// layers below Unity still report something when they run without a Unity
    /// host, for instance in a command-line tool using the graph data model alone.
    /// </summary>
    internal class ConsoleLogger : ILogger
    {
        /// <summary>
        /// Writes <paramref name="message"/> to the standard output.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        public void LogDebug(string message)
        {
            Write(Console.Out, "DEBUG", message);
        }

        /// <summary>
        /// Writes <paramref name="message"/> to the standard error stream.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        public void LogError(string message)
        {
            Write(Console.Error, "ERROR", message);
        }

        /// <summary>
        /// Writes <paramref name="exception"/> to the standard error stream.
        /// </summary>
        /// <param name="exception">The exception to be logged.</param>
        public void LogException(Exception exception)
        {
            Write(Console.Error, "EXCEPTION", exception?.ToString());
        }

        /// <summary>
        /// Writes <paramref name="message"/> to the standard output.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        public void LogInfo(string message)
        {
            Write(Console.Out, "INFO", message);
        }

        /// <summary>
        /// Writes <paramref name="message"/> to the standard error stream.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        public void LogWarning(string message)
        {
            Write(Console.Error, "WARNING", message);
        }

        /// <summary>
        /// Writes <paramref name="message"/> to <paramref name="stream"/> as a single
        /// line, prefixed by <paramref name="severity"/>.
        ///
        /// Trailing white space is dropped, because many callers end their message
        /// with a line break of their own, which the Unity console absorbs but a
        /// plain stream would turn into a blank line.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="severity">The name of the severity to prefix the message with.</param>
        /// <param name="message">The message to be written; may be null.</param>
        private static void Write(TextWriter stream, string severity, string message)
        {
            stream.WriteLine($"[{severity}] {message?.TrimEnd()}");
        }
    }
}
