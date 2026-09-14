using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Creates and caches external assembly reference descriptors on demand
    /// for individual binding compilations.
    /// </summary>
    internal sealed class ExternalAssemblyReferenceCatalog
    {
        /// <summary>
        /// Stores successful and negative assembly lookups per compilation
        /// object identity.
        /// </summary>
        private readonly Dictionary<
            Compilation,
            Dictionary<IAssemblySymbol, ExternalAssemblyReferenceDescriptor?>> descriptorsByCompilation =
                new(ReferenceEqualityComparer.Instance);

        /// <summary>
        /// Tries to get or create the descriptor for an assembly symbol bound
        /// by a specific compilation.
        /// </summary>
        /// <param name="compilation">The compilation that bound the assembly.</param>
        /// <param name="assemblySymbol">The bound assembly symbol.</param>
        /// <param name="descriptor">
        /// The cached or newly created descriptor when available.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when an external PE descriptor is available;
        /// otherwise <see langword="false"/>. Negative results are cached for
        /// the immutable binding compilation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="compilation"/> or
        /// <paramref name="assemblySymbol"/> is <see langword="null"/>.
        /// </exception>
        public bool TryGetDescriptor(
            Compilation compilation,
            IAssemblySymbol assemblySymbol,
            out ExternalAssemblyReferenceDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(compilation);
            ArgumentNullException.ThrowIfNull(assemblySymbol);

            if (!descriptorsByCompilation.TryGetValue(
                    compilation,
                    out Dictionary<IAssemblySymbol, ExternalAssemblyReferenceDescriptor?>? descriptors))
            {
                descriptors = new Dictionary<IAssemblySymbol, ExternalAssemblyReferenceDescriptor?>(
                    SymbolEqualityComparer.Default);
                descriptorsByCompilation.Add(compilation, descriptors);
            }

            if (descriptors.TryGetValue(
                    assemblySymbol,
                    out ExternalAssemblyReferenceDescriptor? cachedDescriptor))
            {
                descriptor = cachedDescriptor!;
                return cachedDescriptor != null;
            }

            if (ExternalAssemblyReferenceDescriptorFactory.TryCreate(
                    compilation,
                    assemblySymbol,
                    out ExternalAssemblyReferenceDescriptor createdDescriptor))
            {
                descriptors.Add(assemblySymbol, createdDescriptor);
                descriptor = createdDescriptor;
                return true;
            }

            descriptors.Add(assemblySymbol, value: null);
            descriptor = null!;
            return false;
        }
    }
}
