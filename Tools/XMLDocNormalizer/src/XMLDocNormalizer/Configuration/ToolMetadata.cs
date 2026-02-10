using System.Reflection;

namespace XMLDocNormalizer.Configuration
{
    /// <summary>
    /// Provides tool metadata (name, version) derived from the executing assembly.
    /// </summary>
    internal static class ToolMetadata
    {
        /// <summary>
        /// Gets the tool name as derived from the entry assembly.
        /// </summary>
        public static string Name =>
            Assembly.GetEntryAssembly()?.GetName().Name ?? "XMLDocNormalizer";

        /// <summary>
        /// Gets the tool version as derived from the entry assembly.
        /// </summary>
        public static string Version =>
            Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
    }
}
