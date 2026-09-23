using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Aggregates explicitly aligned P5I results into an immutable ordered
    /// C# syntax-tree sequence without reparsing or creating a compilation.
    /// </summary>
    internal static class ExternalCSharpSyntaxTreeSetFactory
    {
        /// <summary>
        /// Tries to validate and aggregate one reconstructed tree for every
        /// explicitly supplied source-document ordinal.
        /// </summary>
        /// <param name="configuration">
        /// The P5G configuration whose exact parse-options instance was used
        /// by every P5I result.
        /// </param>
        /// <param name="expectedDocuments">
        /// The caller-selected P4B source-document descriptors, already
        /// aligned to original compilation source ordinals.
        /// </param>
        /// <param name="sourceTrees">
        /// The reconstructed P5I results in the same explicit ordinal order.
        /// </param>
        /// <param name="syntaxTreeSet">
        /// The complete validated sequence for the supplied scope.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when counts match the recorded source-file
        /// count and every ordinal satisfies document, path, parse-options,
        /// and source-text identity invariants; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// The expected-document sequence is explicit because Portable PDB
        /// documents introduced by directives such as <c>#line</c> need not be
        /// independent compilation source trees. Inputs are never searched,
        /// reordered, normalized, or deduplicated. Embedded-source acquisition
        /// provenance is intentionally not part of document identity: name,
        /// hash algorithm, hash bytes, and language identify the source
        /// document used for this ordinal.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="configuration"/>,
        /// <paramref name="expectedDocuments"/>, or
        /// <paramref name="sourceTrees"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryCreate(
            ExternalCSharpCompilationConfiguration configuration,
            IReadOnlyList<ExternalSourceDocumentDescriptor> expectedDocuments,
            IReadOnlyList<ExternalCSharpSyntaxTree> sourceTrees,
            out ExternalCSharpSyntaxTreeSet syntaxTreeSet)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(expectedDocuments);
            ArgumentNullException.ThrowIfNull(sourceTrees);

            if (configuration.SourceFileCount != expectedDocuments.Count
                || expectedDocuments.Count != sourceTrees.Count)
            {
                syntaxTreeSet = null!;
                return false;
            }

            ImmutableArray<SyntaxTree>.Builder trees =
                ImmutableArray.CreateBuilder<SyntaxTree>(sourceTrees.Count);
            ImmutableArray<ValidatedExternalSourceMaterial>.Builder materials =
                ImmutableArray.CreateBuilder<ValidatedExternalSourceMaterial>(
                    sourceTrees.Count);

            for (int index = 0; index < sourceTrees.Count; index++)
            {
                ExternalSourceDocumentDescriptor? expected = expectedDocuments[index];
                ExternalCSharpSyntaxTree? sourceTree = sourceTrees[index];

                if (expected == null
                    || sourceTree == null
                    || !HasSameDocumentIdentity(expected, sourceTree.Document)
                    || !string.Equals(
                        sourceTree.Tree.FilePath,
                        sourceTree.Document.Name,
                        StringComparison.Ordinal)
                    || !ReferenceEquals(
                        sourceTree.Tree.Options,
                        configuration.ParseOptions)
                    || !ReferenceEquals(sourceTree.Tree.GetText(), sourceTree.Text))
                {
                    syntaxTreeSet = null!;
                    return false;
                }

                trees.Add(sourceTree.Tree);
                materials.Add(sourceTree.Material);
            }

            syntaxTreeSet = new ExternalCSharpSyntaxTreeSet(
                trees.MoveToImmutable(),
                materials.MoveToImmutable());
            return true;
        }

        /// <summary>
        /// Compares source identity while excluding acquisition provenance.
        /// </summary>
        /// <param name="expected">The expected source document.</param>
        /// <param name="actual">The document carried by the P5I result.</param>
        /// <returns>
        /// <see langword="true"/> when name, hash algorithm, hash bytes, and
        /// language are identical; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasSameDocumentIdentity(
            ExternalSourceDocumentDescriptor expected,
            ExternalSourceDocumentDescriptor actual)
        {
            return string.Equals(expected.Name, actual.Name, StringComparison.Ordinal)
                && expected.HashAlgorithm == actual.HashAlgorithm
                && expected.Hash.AsSpan().SequenceEqual(actual.Hash.AsSpan())
                && expected.Language == actual.Language;
        }
    }
}
