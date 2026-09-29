namespace SEE.Utils
{
    /// <summary>
    /// The logger through which the layers of SEE below Unity emit their messages.
    ///
    /// Those layers must not call <c>UnityEngine.Debug</c> directly, because that
    /// would tie them to Unity and prevent them from being shipped on their own.
    /// They use <see cref="Logger"/> instead. A Unity host replaces it at start-up
    /// by a logger forwarding to the Unity console; see <see cref="SEELogger"/>.
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
