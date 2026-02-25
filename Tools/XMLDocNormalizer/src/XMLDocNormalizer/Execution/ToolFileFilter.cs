using XMLDocNormalizer.Cli;

namespace XMLDocNormalizer.Execution
{
    /// <summary>
    /// Provides helper methods for deciding whether a source file should be excluded
    /// from analysis and metrics.
    /// </summary>
    internal static class ToolFileFilter
    {
        /// <summary>
        /// Determines whether a file should be excluded based on the given tool options.
        /// </summary>
        /// <param name="filePath">
        /// The file path to evaluate.
        /// </param>
        /// <param name="options">
        /// The tool options controlling include/exclude behavior.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the file should be excluded; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public static bool ShouldExclude(string? filePath, ToolOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            if (!options.IncludeGenerated && IsGeneratedFile(filePath))
            {
                return true;
            }

            if (!options.IncludeTests && IsTestFile(filePath))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the given path matches common generated code patterns.
        /// </summary>
        /// <param name="filePath">
        /// The file path to evaluate.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the file is considered generated; otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsGeneratedFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            string lower = filePath.ToLowerInvariant();

            if (IsInBuildOutputDirectory(lower))
            {
                return true;
            }

            if (HasGeneratedFileSuffix(lower))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the given path matches common test source patterns.
        /// </summary>
        /// <param name="filePath">
        /// The file path to evaluate.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the file is considered a test file; otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsTestFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            string lower = filePath.ToLowerInvariant();

            if (ContainsPathSegment(lower, "test") ||
                ContainsPathSegment(lower, "tests") ||
                ContainsPathSegment(lower, "unittests") ||
                ContainsPathSegment(lower, "integrationtests"))
            {
                return true;
            }

            if (lower.Contains(".tests", StringComparison.Ordinal) ||
                lower.Contains(".test", StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the given lower-case path is located in build output directories.
        /// </summary>
        /// <param name="lowerFilePath">
        /// The file path in lower-case form.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the path indicates a build output directory; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsInBuildOutputDirectory(string lowerFilePath)
        {
            if (lowerFilePath.Contains(@"\obj\") || lowerFilePath.Contains(@"/obj/"))
            {
                return true;
            }

            if (lowerFilePath.Contains(@"\bin\") || lowerFilePath.Contains(@"/bin/"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the given lower-case path ends with a known generated-file suffix.
        /// </summary>
        /// <param name="lowerFilePath">
        /// The file path in lower-case form.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the path ends with a generated-file suffix; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasGeneratedFileSuffix(string lowerFilePath)
        {
            if (lowerFilePath.EndsWith(".g.cs", StringComparison.Ordinal))
            {
                return true;
            }

            if (lowerFilePath.EndsWith(".g.i.cs", StringComparison.Ordinal))
            {
                return true;
            }

            if (lowerFilePath.EndsWith(".designer.cs", StringComparison.Ordinal))
            {
                return true;
            }

            if (lowerFilePath.EndsWith(".generated.cs", StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the given lower-case path contains a segment that matches the provided segment name.
        /// </summary>
        /// <param name="lowerFilePath">
        /// The file path in lower-case form.
        /// </param>
        /// <param name="segment">
        /// The segment name to match (lower-case).
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the segment is present as a path segment; otherwise <see langword="false"/>.
        /// </returns>
        private static bool ContainsPathSegment(string lowerFilePath, string segment)
        {
            if (lowerFilePath.Contains(@"\", StringComparison.Ordinal))
            {
                string windowsNeedle = @"\" + segment + @"\";
                if (lowerFilePath.Contains(windowsNeedle, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            if (lowerFilePath.Contains("/", StringComparison.Ordinal))
            {
                string unixNeedle = "/" + segment + "/";
                if (lowerFilePath.Contains(unixNeedle, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}