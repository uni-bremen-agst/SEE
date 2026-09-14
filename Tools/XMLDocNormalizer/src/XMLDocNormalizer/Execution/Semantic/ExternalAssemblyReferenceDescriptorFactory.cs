using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Creates external assembly descriptors from the exact metadata
    /// references bound by Roslyn compilations.
    /// </summary>
    internal static class ExternalAssemblyReferenceDescriptorFactory
    {
        /// <summary>
        /// The metadata name of the framework reference-assembly marker.
        /// </summary>
        private const string ReferenceAssemblyAttributeMetadataName =
            "System.Runtime.CompilerServices.ReferenceAssemblyAttribute";

        /// <summary>
        /// Tries to describe the external PE assembly reference bound to an
        /// assembly symbol in a compilation.
        /// </summary>
        /// <param name="compilation">The compilation that bound the assembly symbol.</param>
        /// <param name="assemblySymbol">The bound external assembly symbol.</param>
        /// <param name="descriptor">
        /// The resulting descriptor when the symbol maps to an assembly PE
        /// reference.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the bound reference is a portable
        /// executable containing assembly metadata; otherwise
        /// <see langword="false"/>. Compilation references and module-only
        /// metadata references return <see langword="false"/>. Invalid,
        /// unreadable, or disposed PE metadata also fails closed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="compilation"/> or
        /// <paramref name="assemblySymbol"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryCreate(
            Compilation compilation,
            IAssemblySymbol assemblySymbol,
            out ExternalAssemblyReferenceDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(compilation);
            ArgumentNullException.ThrowIfNull(assemblySymbol);

            MetadataReference? metadataReference =
                compilation.GetMetadataReference(assemblySymbol);

            if (metadataReference is not PortableExecutableReference portableReference
                || portableReference.Properties.Kind != MetadataImageKind.Assembly)
            {
                descriptor = null!;
                return false;
            }

            try
            {
                if (portableReference.GetMetadata() is not AssemblyMetadata assemblyMetadata)
                {
                    descriptor = null!;
                    return false;
                }

                ImmutableArray<ModuleMetadata> moduleMetadata = assemblyMetadata.GetModules();
                ImmutableArray<ExternalModuleIdentity>.Builder modules =
                    ImmutableArray.CreateBuilder<ExternalModuleIdentity>(moduleMetadata.Length);

                foreach (ModuleMetadata module in moduleMetadata)
                {
                    modules.Add(
                        new ExternalModuleIdentity(
                            module.Name,
                            module.GetModuleVersionId()));
                }

                descriptor = new ExternalAssemblyReferenceDescriptor(
                    assemblySymbol.Identity,
                    modules.MoveToImmutable(),
                    portableReference.FilePath,
                    HasReferenceAssemblyMarker(compilation, assemblySymbol));
                return true;
            }
            catch (BadImageFormatException)
            {
                descriptor = null!;
                return false;
            }
            catch (IOException)
            {
                descriptor = null!;
                return false;
            }
            catch (ObjectDisposedException)
            {
                descriptor = null!;
                return false;
            }
        }

        /// <summary>
        /// Determines whether an assembly carries the exact framework
        /// reference-assembly marker type bound by the same compilation.
        /// </summary>
        /// <param name="compilation">The compilation that bound the symbols.</param>
        /// <param name="assemblySymbol">The external assembly to inspect.</param>
        /// <returns>
        /// <see langword="true"/> when the exact framework marker is present;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasReferenceAssemblyMarker(
            Compilation compilation,
            IAssemblySymbol assemblySymbol)
        {
            INamedTypeSymbol? markerType =
                compilation.GetTypeByMetadataName(ReferenceAssemblyAttributeMetadataName);

            if (markerType == null)
            {
                return false;
            }

            return assemblySymbol.GetAttributes().Any(
                attribute => SymbolEqualityComparer.Default.Equals(
                    attribute.AttributeClass,
                    markerType));
        }
    }
}
