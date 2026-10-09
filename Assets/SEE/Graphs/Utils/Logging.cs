namespace SEE.Graphs.Utils
{
    /// <summary>
    /// The logger to emit messages. The default is <see cref="ConsoleLogger"/>,
    /// which writes to the standard output streams. It can be overridden via
    /// <see cref="Logger"/>.
    /// </summary>
    public static class Logging
    {
        /// <summary>
        /// The logger all messages of the lower layers are passed to. Never null.
        ///
        /// Setting this to null restores the default, <see cref="ConsoleLogger"/>,
        /// which writes to the standard output streams and so needs no host at all.
        /// </summary>
        public static ILogger Logger
        {
            get => logger;
            set => logger = value ?? new ConsoleLogger();
        }

        /// <summary>
        /// The backing field of <see cref="Logger"/>.
        /// </summary>
        private static ILogger logger = new ConsoleLogger();
    }
}
