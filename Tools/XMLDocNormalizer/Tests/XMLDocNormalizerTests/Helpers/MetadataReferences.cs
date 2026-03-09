using System.Reflection;
using Microsoft.CodeAnalysis;

namespace XMLDocNormalizerTests.Helpers
{
    /// <summary>
    /// Provides metadata references for in-memory Roslyn compilations used in tests.
    /// </summary>
    internal static class MetadataReferences
    {
        /// <summary>
        /// Gets the default metadata references required for semantic analysis tests.
        /// </summary>
        public static IReadOnlyList<MetadataReference> Default { get; } =
        [
            MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).GetTypeInfo().Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location)
        ];
    }
}