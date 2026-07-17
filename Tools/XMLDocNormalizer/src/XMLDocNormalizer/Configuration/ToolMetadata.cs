using System.Reflection;

namespace XMLDocNormalizer.Configuration
{
    /// <summary>
    /// Provides tool metadata derived from the executing assembly.
    /// </summary>
    internal static class ToolMetadata
    {
        /// <summary>
        /// Gets the tool name as derived from the entry assembly.
        /// </summary>
        /// <value>
        /// The tool name derived from the entry assembly, or XMLDocNormalizer when no entry assembly is available.
        /// </value>
        public static string Name =>
            Assembly.GetEntryAssembly()?.GetName().Name ?? "XMLDocNormalizer";

        /// <summary>
        /// Gets the tool version as derived from the entry assembly.
        /// </summary>
        /// <value>
        /// The tool version derived from the entry assembly, or unknown when no version is available.
        /// </value>
        public static string Version =>
            Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
    }
}
