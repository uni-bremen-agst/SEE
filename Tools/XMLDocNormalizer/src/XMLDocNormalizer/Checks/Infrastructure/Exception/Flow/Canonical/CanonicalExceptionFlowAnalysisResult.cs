using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical
{
    /// <summary>
    /// Identifies the evidence class of one canonical analysis-result entry.
    /// </summary>
    internal enum CanonicalExceptionFlowEvidenceKind
    {
        /// <summary>
        /// Executable analysis proved the exception flow.
        /// </summary>
        Proven,

        /// <summary>
        /// External XML documentation supports the exception flow.
        /// </summary>
        ExternalDocumentation
    }

    /// <summary>
    /// Represents paths retained for one canonical exception type.
    /// </summary>
    internal sealed class CanonicalExceptionFlowResultEntry
        : IEquatable<CanonicalExceptionFlowResultEntry>
    {
        /// <summary>         /// Stores the paths values.         /// </summary>
        private readonly CanonicalExceptionFlowPath[] paths;

        /// <summary>
        /// Initializes one canonical result entry.
        /// </summary>
        /// <param name="exceptionType">The exceptionType value.</param>
        /// <param name="evidenceKind">The evidenceKind value.</param>
        /// <param name="paths">The paths value.</param>
        /// <param name="pathsTruncated">The pathsTruncated value.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        public CanonicalExceptionFlowResultEntry(
            CanonicalTypeIdentity exceptionType,
            CanonicalExceptionFlowEvidenceKind evidenceKind,
            CanonicalExceptionFlowPath[]? paths,
            bool pathsTruncated)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);

            ExceptionType = exceptionType;
            EvidenceKind = evidenceKind;
            this.paths = paths?.ToArray() ?? Array.Empty<CanonicalExceptionFlowPath>();
            PathsTruncated = pathsTruncated;
        }

        /// <summary>
        /// Gets the exact exception type identity.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalTypeIdentity ExceptionType { get; }

        /// <summary>
        /// Gets the evidence class.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalExceptionFlowEvidenceKind EvidenceKind { get; }

        /// <summary>
        /// Gets a copy of the retained paths.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalExceptionFlowPath[] Paths => paths.ToArray();

        /// <summary>
        /// Gets whether additional paths were omitted.
        /// </summary>
        /// <value>The value described by this property.</value>
        public bool PathsTruncated { get; }

        /// <inheritdoc/>
        public bool Equals(CanonicalExceptionFlowResultEntry? other)
        {
            return other != null
                && ExceptionType.Equals(other.ExceptionType)
                && EvidenceKind == other.EvidenceKind
                && paths.SequenceEqual(other.paths)
                && PathsTruncated == other.PathsTruncated;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalExceptionFlowResultEntry);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(ExceptionType);
            hash.Add(EvidenceKind);
            hash.Add(PathsTruncated);

            foreach (CanonicalExceptionFlowPath path in paths)
            {
                hash.Add(path);
            }

            return hash.ToHashCode();
        }
    }

    /// <summary>
    /// Represents a transportable exception-flow analysis result.
    /// </summary>
    internal sealed class CanonicalExceptionFlowAnalysisResult
        : IEquatable<CanonicalExceptionFlowAnalysisResult>
    {
        /// <summary>         /// Stores the entries values.         /// </summary>
        private readonly CanonicalExceptionFlowResultEntry[] entries;
        /// <summary>         /// Stores the uncertainties values.         /// </summary>
        private readonly CanonicalExceptionFlowUncertainty[] uncertainties;

        /// <summary>
        /// Initializes a canonical analysis result.
        /// </summary>
        /// <param name="entries">The entries value.</param>
        /// <param name="uncertainties">The uncertainties value.</param>
        public CanonicalExceptionFlowAnalysisResult(
            CanonicalExceptionFlowResultEntry[]? entries,
            CanonicalExceptionFlowUncertainty[]? uncertainties)
        {
            this.entries = (entries ?? Array.Empty<CanonicalExceptionFlowResultEntry>())
                .OrderBy(static entry => entry.EvidenceKind)
                .ThenBy(
                    static entry => CanonicalIdentityKeyWriter.Write(entry.ExceptionType),
                    StringComparer.Ordinal)
                .ToArray();
            this.uncertainties = (uncertainties
                    ?? Array.Empty<CanonicalExceptionFlowUncertainty>())
                .OrderBy(static item => item.Kind)
                .ThenBy(static item => item.DisplayText, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Gets a copy of all exception entries.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalExceptionFlowResultEntry[] Entries => entries.ToArray();

        /// <summary>
        /// Gets a copy of all structured uncertainties.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalExceptionFlowUncertainty[] Uncertainties => uncertainties.ToArray();

        /// <inheritdoc/>
        public bool Equals(CanonicalExceptionFlowAnalysisResult? other)
        {
            return other != null
                && entries.SequenceEqual(other.entries)
                && uncertainties.SequenceEqual(other.uncertainties);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalExceptionFlowAnalysisResult);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = new();

            foreach (CanonicalExceptionFlowResultEntry entry in entries)
            {
                hash.Add(entry);
            }

            foreach (CanonicalExceptionFlowUncertainty uncertainty in uncertainties)
            {
                hash.Add(uncertainty);
            }

            return hash.ToHashCode();
        }
    }
}
