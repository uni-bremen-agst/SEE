using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Assembles a semantic C# compilation from previously validated external
    /// target, source, reference, and compilation-option provenance.
    /// </summary>
    /// <remarks>
    /// The result is a structural semantic reconstruction, not a bit-identical
    /// original compilation or reproducible build. Success does not guarantee
    /// zero diagnostics, successful emit, identical warning or signing
    /// configuration, or analyzer and generator parity.
    /// </remarks>
    internal static class ExternalCSharpCompilationFactory
    {
        /// <summary>
        /// Tries to compose one assembly compilation while preserving every
        /// previously reconstructed Roslyn input instance and ordinal.
        /// </summary>
        /// <param name="targetAssembly">
        /// The P3-validated identity of the original target assembly.
        /// </param>
        /// <param name="compilationProvenance">
        /// The P5A provenance linked through P4A to the target manifest module
        /// and containing the original metadata-reference ordinals.
        /// </param>
        /// <param name="configuration">
        /// The P5G parse and compilation configuration.
        /// </param>
        /// <param name="syntaxTreeSet">
        /// The complete ordered P5J source-tree sequence.
        /// </param>
        /// <param name="referenceSet">
        /// The complete ordered P5E metadata-reference sequence.
        /// </param>
        /// <param name="compilation">
        /// The assembled semantic compilation when successful.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when target, source, reference, and options
        /// provenance agree and Roslyn preserves all structural composition
        /// invariants; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Netmodule targets are unsupported because the current P3 provenance
        /// models assembly targets and P5G intentionally does not reconstruct
        /// an original module name. Metadata references are checked in memory;
        /// no candidate file or Portable PDB is opened. Diagnostics are not an
        /// acceptance condition.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any input is <see langword="null"/>.
        /// </exception>
        public static bool TryCreate(
            ExternalAssemblyReferenceDescriptor targetAssembly,
            ExternalCompilationProvenanceDescriptor compilationProvenance,
            ExternalCSharpCompilationConfiguration configuration,
            ExternalCSharpSyntaxTreeSet syntaxTreeSet,
            ExternalMetadataReferenceSet referenceSet,
            out CSharpCompilation compilation)
        {
            ArgumentNullException.ThrowIfNull(targetAssembly);
            ArgumentNullException.ThrowIfNull(compilationProvenance);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(syntaxTreeSet);
            ArgumentNullException.ThrowIfNull(referenceSet);

            if (configuration.CompilationOptions.OutputKind == OutputKind.NetModule
                || targetAssembly.Modules.IsDefaultOrEmpty
                || targetAssembly.Modules[0]
                    != compilationProvenance.DebugDirectory.ManifestModule
                || syntaxTreeSet.Trees.Length != configuration.SourceFileCount
                || !HaveExpectedTrees(syntaxTreeSet.Trees, configuration.ParseOptions)
                || !HaveExpectedReferences(
                    compilationProvenance.MetadataReferences,
                    referenceSet.References))
            {
                compilation = null!;
                return false;
            }

            try
            {
                compilation = CSharpCompilation.Create(
                    targetAssembly.AssemblyIdentity.Name,
                    syntaxTrees: syntaxTreeSet.Trees,
                    references: referenceSet.References,
                    options: configuration.CompilationOptions);
            }
            catch (ArgumentException)
            {
                compilation = null!;
                return false;
            }

            if (!ReferenceEquals(compilation.Options, configuration.CompilationOptions)
                || !string.Equals(
                    compilation.AssemblyName,
                    targetAssembly.AssemblyIdentity.Name,
                    StringComparison.Ordinal)
                || !compilation.Assembly.Identity.Equals(targetAssembly.AssemblyIdentity)
                || !string.Equals(
                    compilation.SourceModule.Name,
                    targetAssembly.Modules[0].Name,
                    StringComparison.Ordinal)
                || !HaveSameTreeInstances(compilation.SyntaxTrees, syntaxTreeSet.Trees)
                || !HaveSameReferenceInstances(
                    compilation.References,
                    referenceSet.References))
            {
                compilation = null!;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks the inexpensive P5J count and parse-options invariants.
        /// </summary>
        /// <param name="trees">The ordered P5J syntax trees.</param>
        /// <param name="parseOptions">The exact P5G parse-options instance.</param>
        /// <returns>
        /// <see langword="true"/> when every tree is non-null and uses the
        /// expected options instance; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HaveExpectedTrees(
            ImmutableArray<SyntaxTree> trees,
            CSharpParseOptions parseOptions)
        {
            foreach (SyntaxTree? tree in trees)
            {
                if (tree == null || !ReferenceEquals(tree.Options, parseOptions))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validates finished references against P5A properties and manifest
        /// module identities without reopening their candidate images.
        /// </summary>
        /// <param name="metadataReferences">The P5A reference provenance.</param>
        /// <param name="references">The ordered P5E Roslyn references.</param>
        /// <returns>
        /// <see langword="true"/> when count, properties, and every MVID match
        /// at the same ordinal; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HaveExpectedReferences(
            ExternalCompilationMetadataReferencesDescriptor? metadataReferences,
            ImmutableArray<PortableExecutableReference> references)
        {
            if (metadataReferences == null
                || metadataReferences.References.Length != references.Length)
            {
                return false;
            }

            for (int index = 0; index < references.Length; index++)
            {
                PortableExecutableReference? reference = references[index];
                ExternalCompilationMetadataReferenceDescriptor? expected =
                    metadataReferences.References[index];

                if (reference == null
                    || expected == null
                    || reference.Properties.Kind != expected.Kind
                    || reference.Properties.EmbedInteropTypes != expected.EmbedInteropTypes
                    || !AliasesEqual(reference.Properties.Aliases, expected.Aliases)
                    || !TryGetManifestModuleVersionId(
                        reference,
                        expected.Kind,
                        out Guid moduleVersionId)
                    || moduleVersionId != expected.ModuleVersionId)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Reads the already materialized reference metadata's manifest MVID.
        /// </summary>
        /// <param name="reference">The reconstructed in-memory reference.</param>
        /// <param name="expectedKind">The expected metadata image kind.</param>
        /// <param name="moduleVersionId">The manifest MVID when available.</param>
        /// <returns>
        /// <see langword="true"/> when metadata kind and MVID are readable;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetManifestModuleVersionId(
            PortableExecutableReference reference,
            MetadataImageKind expectedKind,
            out Guid moduleVersionId)
        {
            try
            {
                Metadata metadata = reference.GetMetadata();
                ModuleMetadata? module = expectedKind switch
                {
                    MetadataImageKind.Assembly when metadata is AssemblyMetadata assembly =>
                        assembly.GetModules().FirstOrDefault(),
                    MetadataImageKind.Module when metadata is ModuleMetadata standalone =>
                        standalone,
                    _ => null,
                };

                if (module == null)
                {
                    moduleVersionId = default;
                    return false;
                }

                moduleVersionId = module.GetModuleVersionId();
                return true;
            }
            catch (BadImageFormatException)
            {
                moduleVersionId = default;
                return false;
            }
            catch (IOException)
            {
                moduleVersionId = default;
                return false;
            }
            catch (ObjectDisposedException)
            {
                moduleVersionId = default;
                return false;
            }
        }

        /// <summary>
        /// Compares ordered aliases without normalization.
        /// </summary>
        /// <param name="actual">The aliases on the Roslyn reference.</param>
        /// <param name="expected">The aliases recorded by P5A.</param>
        /// <returns>
        /// <see langword="true"/> when both sequences match ordinally;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool AliasesEqual(
            ImmutableArray<string> actual,
            ImmutableArray<string> expected)
        {
            if (actual.Length != expected.Length)
            {
                return false;
            }

            for (int index = 0; index < actual.Length; index++)
            {
                if (!string.Equals(actual[index], expected[index], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks exact syntax-tree instance preservation by ordinal.
        /// </summary>
        /// <param name="actual">The compilation syntax trees.</param>
        /// <param name="expected">The P5J syntax trees.</param>
        /// <returns>
        /// <see langword="true"/> when counts and instances match;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool HaveSameTreeInstances(
            IEnumerable<SyntaxTree> actual,
            ImmutableArray<SyntaxTree> expected)
        {
            return HaveSameInstances(actual, expected);
        }

        /// <summary>
        /// Checks exact metadata-reference instance preservation by ordinal.
        /// </summary>
        /// <param name="actual">The compilation references.</param>
        /// <param name="expected">The P5E references.</param>
        /// <returns>
        /// <see langword="true"/> when counts and instances match;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool HaveSameReferenceInstances(
            IEnumerable<MetadataReference> actual,
            ImmutableArray<PortableExecutableReference> expected)
        {
            return HaveSameInstances<MetadataReference>(actual, expected);
        }

        /// <summary>
        /// Compares two sequences using reference identity at every ordinal.
        /// </summary>
        /// <typeparam name="T">The shared reference type.</typeparam>
        /// <param name="actual">The actual sequence.</param>
        /// <param name="expected">The expected sequence.</param>
        /// <returns>
        /// <see langword="true"/> when both sequences have identical object
        /// references in identical order; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HaveSameInstances<T>(
            IEnumerable<T> actual,
            IEnumerable<T> expected)
            where T : class
        {
            using IEnumerator<T> actualEnumerator = actual.GetEnumerator();
            using IEnumerator<T> expectedEnumerator = expected.GetEnumerator();

            while (expectedEnumerator.MoveNext())
            {
                if (!actualEnumerator.MoveNext()
                    || !ReferenceEquals(actualEnumerator.Current, expectedEnumerator.Current))
                {
                    return false;
                }
            }

            return !actualEnumerator.MoveNext();
        }
    }
}
