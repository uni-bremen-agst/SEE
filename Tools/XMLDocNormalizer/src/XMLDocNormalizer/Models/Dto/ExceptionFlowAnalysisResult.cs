using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Models.DTO
{
    /// <summary>
    /// Represents the result of direct or transitive exception-flow
    /// analysis.
    /// </summary>
    internal sealed class ExceptionFlowAnalysisResult
    {
        /// <summary>
        /// Defines the maximum number of distinct paths retained for one
        /// exception type.
        /// </summary>
        internal const int MaximumPathsPerException = 64;

        /// <summary>
        /// Stores thrown exception types together with their distinct flow
        /// paths.
        /// </summary>
        private readonly Dictionary<
            INamedTypeSymbol,
            ExceptionPathCollection> exceptionPaths =
                new(SymbolEqualityComparer.Default);

        /// <summary>
        /// Stores the proven exception types using Roslyn symbol equality.
        /// </summary>
        private readonly HashSet<INamedTypeSymbol> thrownExceptions =
            new(SymbolEqualityComparer.Default);

        /// <summary>
        /// Gets the exception types that were proven to be thrown directly
        /// or transitively.
        /// </summary>
        /// <value>
        /// The exception types proven to be thrown directly or
        /// transitively.
        /// </value>
        public IReadOnlySet<INamedTypeSymbol> ThrownExceptions =>
            thrownExceptions;

        /// <summary>
        /// Gets the set of callable targets whose exception flow could not
        /// be decided.
        /// </summary>
        /// <value>
        /// The callable targets whose exception flow could not be decided.
        /// </value>
        public HashSet<string> UncertainTargets { get; } =
            new(StringComparer.Ordinal);

        /// <summary>
        /// Gets a value indicating whether at least one relevant transitive
        /// analysis path could not be evaluated conclusively.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if at least one relevant transitive
        /// analysis path could not be evaluated conclusively; otherwise
        /// <see langword="false"/>.
        /// </value>
        public bool HasUncertainPaths =>
            UncertainTargets.Count > 0;

        /// <summary>
        /// Adds a proven exception type without attaching a path yet.
        /// </summary>
        /// <param name="exceptionType">
        /// The proven exception type.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="exceptionType"/> is
        /// <see langword="null"/>.
        /// </exception>
        public void AddThrownException(
            INamedTypeSymbol exceptionType)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);

            thrownExceptions.Add(exceptionType);
            GetOrCreatePathCollection(exceptionType);
        }

        /// <summary>
        /// Adds one distinct flow path for a proven exception type.
        /// </summary>
        /// <param name="exceptionType">
        /// The proven exception type.
        /// </param>
        /// <param name="path">
        /// The flow path leading to the exception source.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the path was retained; otherwise
        /// <see langword="false"/> when it was a duplicate or the path
        /// limit was reached.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="exceptionType"/> or
        /// <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        public bool AddExceptionPath(
            INamedTypeSymbol exceptionType,
            ExceptionFlowPath path)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);
            ArgumentNullException.ThrowIfNull(path);

            thrownExceptions.Add(exceptionType);

            ExceptionPathCollection collection =
                GetOrCreatePathCollection(exceptionType);

            return collection.TryAdd(path);
        }

        /// <summary>
        /// Gets all retained paths for a proven exception type.
        /// </summary>
        /// <param name="exceptionType">
        /// The exception type whose paths should be returned.
        /// </param>
        /// <returns>
        /// The retained paths, or an empty list when the exception type is
        /// not present.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="exceptionType"/> is
        /// <see langword="null"/>.
        /// </exception>
        public IReadOnlyList<ExceptionFlowPath> GetExceptionPaths(
            INamedTypeSymbol exceptionType)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);

            return exceptionPaths.TryGetValue(
                exceptionType,
                out ExceptionPathCollection? collection)
                    ? collection.Paths
                    : Array.Empty<ExceptionFlowPath>();
        }

        /// <summary>
        /// Determines whether paths for the specified exception type were
        /// truncated.
        /// </summary>
        /// <param name="exceptionType">
        /// The exception type to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if additional paths were omitted;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="exceptionType"/> is
        /// <see langword="null"/>.
        /// </exception>
        public bool ArePathsTruncated(
            INamedTypeSymbol exceptionType)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);

            return exceptionPaths.TryGetValue(
                       exceptionType,
                       out ExceptionPathCollection? collection) &&
                   collection.PathsTruncated;
        }

        /// <summary>
        /// Creates finding-ready flow details for the specified exception
        /// type.
        /// </summary>
        /// <param name="exceptionType">
        /// The exception type whose details should be created.
        /// </param>
        /// <returns>
        /// The flow details, or <see langword="null"/> when no paths were
        /// collected.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="exceptionType"/> is
        /// <see langword="null"/>.
        /// </exception>
        public ExceptionFlowDetails? GetExceptionFlowDetails(
            INamedTypeSymbol exceptionType)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);

            if (!exceptionPaths.TryGetValue(
                    exceptionType,
                    out ExceptionPathCollection? collection) ||
                collection.Paths.Count == 0)
            {
                return null;
            }

            return new ExceptionFlowDetails(
                collection.Paths.ToArray(),
                collection.PathsTruncated);
        }

        /// <summary>
        /// Merges exception types, paths, truncation flags, and uncertainty
        /// from another result.
        /// </summary>
        /// <param name="source">
        /// The result to merge into this instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="source"/> is
        /// <see langword="null"/>.
        /// </exception>
        public void Merge(
            ExceptionFlowAnalysisResult source)
        {
            ArgumentNullException.ThrowIfNull(source);

            INamedTypeSymbol[] sourceExceptionTypes =
                source.ThrownExceptions.ToArray();

            foreach (INamedTypeSymbol exceptionType
                     in sourceExceptionTypes)
            {
                AddThrownException(exceptionType);

                ExceptionFlowPath[] sourcePaths =
                    source.GetExceptionPaths(exceptionType)
                        .ToArray();

                foreach (ExceptionFlowPath path in sourcePaths)
                {
                    AddExceptionPath(
                        exceptionType,
                        path);
                }

                if (source.ArePathsTruncated(exceptionType))
                {
                    MarkPathsTruncated(exceptionType);
                }
            }

            UncertainTargets.UnionWith(
                source.UncertainTargets);
        }

        /// <summary>
        /// Merges another result while prepending one call-site step to
        /// every collected path.
        /// </summary>
        /// <param name="source">
        /// The result to merge into this instance.
        /// </param>
        /// <param name="prefix">
        /// The call-site step to prepend.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="source"/> or
        /// <paramref name="prefix"/> is <see langword="null"/>.
        /// </exception>
        public void MergeWithPrefix(
            ExceptionFlowAnalysisResult source,
            ExceptionFlowPathStep prefix)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(prefix);

            INamedTypeSymbol[] sourceExceptionTypes =
                source.ThrownExceptions.ToArray();

            foreach (INamedTypeSymbol exceptionType
                     in sourceExceptionTypes)
            {
                AddThrownException(exceptionType);

                ExceptionFlowPath[] sourcePaths =
                    source.GetExceptionPaths(exceptionType)
                        .ToArray();

                foreach (ExceptionFlowPath path in sourcePaths)
                {
                    AddExceptionPath(
                        exceptionType,
                        path.Prepend(prefix));
                }

                if (source.ArePathsTruncated(exceptionType))
                {
                    MarkPathsTruncated(exceptionType);
                }
            }

            UncertainTargets.UnionWith(
                source.UncertainTargets);
        }

        /// <summary>
        /// Removes all exception types and associated paths matching a
        /// predicate.
        /// </summary>
        /// <param name="predicate">
        /// The predicate selecting exception types to remove.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="predicate"/> is
        /// <see langword="null"/>.
        /// </exception>
        public void RemoveThrownExceptions(
            Func<INamedTypeSymbol, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);

            INamedTypeSymbol[] typesToRemove =
                thrownExceptions
                    .Where(predicate)
                    .ToArray();

            foreach (INamedTypeSymbol exceptionType
                     in typesToRemove)
            {
                thrownExceptions.Remove(exceptionType);
                exceptionPaths.Remove(exceptionType);
            }
        }

        /// <summary>
        /// Removes all proven exception types and all associated paths.
        /// </summary>
        public void ClearThrownExceptions()
        {
            thrownExceptions.Clear();
            exceptionPaths.Clear();
        }

        /// <summary>
        /// Gets or creates the path collection associated with an
        /// exception type.
        /// </summary>
        /// <param name="exceptionType">
        /// The exception type.
        /// </param>
        /// <returns>The associated path collection.</returns>
        private ExceptionPathCollection GetOrCreatePathCollection(
            INamedTypeSymbol exceptionType)
        {
            if (exceptionPaths.TryGetValue(
                    exceptionType,
                    out ExceptionPathCollection? collection))
            {
                return collection;
            }

            collection = new ExceptionPathCollection();

            exceptionPaths.Add(
                exceptionType,
                collection);

            return collection;
        }

        /// <summary>
        /// Marks the path collection of an exception type as truncated.
        /// </summary>
        /// <param name="exceptionType">
        /// The exception type.
        /// </param>
        private void MarkPathsTruncated(
            INamedTypeSymbol exceptionType)
        {
            GetOrCreatePathCollection(exceptionType)
                .MarkTruncated();
        }

        /// <summary>
        /// Stores deduplicated paths for one exception type.
        /// </summary>
        private sealed class ExceptionPathCollection
        {
            /// <summary>
            /// Stores retained paths in deterministic insertion order.
            /// </summary>
            private readonly List<ExceptionFlowPath> paths =
                new();

            /// <summary>
            /// Stores keys of all retained paths for efficient
            /// deduplication.
            /// </summary>
            private readonly HashSet<string> pathKeys =
                new(StringComparer.Ordinal);

            /// <summary>
            /// Gets the retained exception-flow paths.
            /// </summary>
            /// <value>The retained paths.</value>
            public IReadOnlyList<ExceptionFlowPath> Paths =>
                paths;

            /// <summary>
            /// Gets a value indicating whether additional paths were
            /// omitted.
            /// </summary>
            /// <value>
            /// <see langword="true"/> if the path limit was exceeded;
            /// otherwise <see langword="false"/>.
            /// </value>
            public bool PathsTruncated { get; private set; }

            /// <summary>
            /// Adds a path if it is distinct and the configured limit has
            /// not been reached.
            /// </summary>
            /// <param name="path">The path to add.</param>
            /// <returns>
            /// <see langword="true"/> if the path was retained; otherwise
            /// <see langword="false"/>.
            /// </returns>
            public bool TryAdd(
                ExceptionFlowPath path)
            {
                if (pathKeys.Contains(
                        path.DeduplicationKey))
                {
                    return false;
                }

                if (paths.Count >=
                    MaximumPathsPerException)
                {
                    PathsTruncated = true;
                    return false;
                }

                pathKeys.Add(
                    path.DeduplicationKey);

                paths.Add(path);
                return true;
            }

            /// <summary>
            /// Marks the path collection as truncated.
            /// </summary>
            public void MarkTruncated()
            {
                PathsTruncated = true;
            }
        }
    }
}
