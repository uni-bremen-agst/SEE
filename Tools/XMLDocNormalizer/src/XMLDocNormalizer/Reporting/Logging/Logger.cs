namespace XMLDocNormalizer.Reporting.Logging
{
    /// <summary>
    /// A simple logger that can be used to log messages to the console. The logging can be enabled or disabled by setting the <see cref="VerboseEnabled"/> property.
    /// </summary>
    internal static class Logger
    {
        /// <summary>
        /// Gets or sets a value indicating whether verbose logging is enabled. 
        /// If set to true, the logger will output messages to the console. 
        /// If set to false, the logger will not output any messages.
        /// </summary>
        public static bool VerboseEnabled { get; set; }

        /// <summary>
        /// Logs a message to the console if verbose logging is enabled.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Info(string message)
        {
            if (VerboseEnabled)
            {
                System.Console.WriteLine(message);
            }
        }

        /// <summary>
        /// Logs a warning message to the console. 
        /// This method will always log the message regardless of the <see cref="VerboseEnabled"/> setting.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        public static void Warn(string message)
        {
            System.Console.WriteLine($"WARNING: {message}");
        }
    }

}