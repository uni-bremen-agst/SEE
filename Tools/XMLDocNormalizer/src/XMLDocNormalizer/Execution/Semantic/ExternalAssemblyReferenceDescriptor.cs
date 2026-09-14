using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Describes the binary identity, location, and reference-assembly
    /// classification of one external assembly bound by Roslyn.
    /// </summary>
    /// <remarks>
    /// Value equality represents binary identity and classification. It uses
    /// the full assembly identity, the ordered module identities, and
    /// <see cref="IsReferenceAssembly"/>. <see cref="FilePath"/> records only
    /// provenance and does not participate in equality.
    /// </remarks>
    internal sealed class ExternalAssemblyReferenceDescriptor :
        IEquatable<ExternalAssemblyReferenceDescriptor>
    {
        /// <summary>
        /// Initializes an external assembly reference descriptor.
        /// </summary>
        /// <param name="assemblyIdentity">The complete Roslyn assembly identity.</param>
        /// <param name="modules">
        /// The module identities in assembly metadata order, beginning with
        /// the manifest module.
        /// </param>
        /// <param name="filePath">
        /// The reference file path, or <see langword="null"/> for an in-memory
        /// reference.
        /// </param>
        /// <param name="isReferenceAssembly">
        /// Whether the bound assembly has the framework
        /// <c>ReferenceAssemblyAttribute</c> marker.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="assemblyIdentity"/> is
        /// <see langword="null"/>.
        /// </exception>
        public ExternalAssemblyReferenceDescriptor(
            AssemblyIdentity assemblyIdentity,
            ImmutableArray<ExternalModuleIdentity> modules,
            string? filePath,
            bool isReferenceAssembly)
        {
            ArgumentNullException.ThrowIfNull(assemblyIdentity);

            AssemblyIdentity = assemblyIdentity;
            Modules = modules.IsDefault
                ? ImmutableArray<ExternalModuleIdentity>.Empty
                : modules;
            FilePath = filePath;
            IsReferenceAssembly = isReferenceAssembly;
        }

        /// <summary>
        /// Gets the complete Roslyn assembly identity.
        /// </summary>
        /// <value>The assembly identity bound by the compilation.</value>
        public AssemblyIdentity AssemblyIdentity { get; }

        /// <summary>
        /// Gets the ordered identities of all modules in the assembly.
        /// </summary>
        /// <value>
        /// The module identities in metadata order. The first element is the
        /// manifest module.
        /// </value>
        public ImmutableArray<ExternalModuleIdentity> Modules { get; }

        /// <summary>
        /// Gets the path carried by the bound metadata reference.
        /// </summary>
        /// <value>
        /// The reference file path, or <see langword="null"/> for an in-memory
        /// reference. The path is provenance and not binary identity.
        /// </value>
        public string? FilePath { get; }

        /// <summary>
        /// Gets whether the bound assembly is marked as a reference assembly.
        /// </summary>
        /// <value>
        /// <see langword="true"/> when the framework
        /// <c>ReferenceAssemblyAttribute</c> marker was found; otherwise
        /// <see langword="false"/>.
        /// </value>
        public bool IsReferenceAssembly { get; }

        /// <summary>
        /// Determines whether another descriptor represents the same binary
        /// identity and reference-assembly classification.
        /// </summary>
        /// <param name="other">The descriptor to compare.</param>
        /// <returns>
        /// <see langword="true"/> when the assembly identity, ordered module
        /// identities, and classification are equal; otherwise
        /// <see langword="false"/>. File paths are ignored.
        /// </returns>
        public bool Equals(ExternalAssemblyReferenceDescriptor? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null)
            {
                return false;
            }

            return AssemblyIdentity.Equals(other.AssemblyIdentity)
                && Modules.SequenceEqual(other.Modules)
                && IsReferenceAssembly == other.IsReferenceAssembly;
        }

        /// <summary>
        /// Determines whether another object represents the same binary
        /// identity and reference-assembly classification.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>
        /// <see langword="true"/> when the object is an equal descriptor;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public override bool Equals(object? obj)
        {
            return Equals(obj as ExternalAssemblyReferenceDescriptor);
        }

        /// <summary>
        /// Gets a hash code for the binary identity and reference-assembly
        /// classification.
        /// </summary>
        /// <returns>The value-based hash code, excluding the file path.</returns>
        public override int GetHashCode()
        {
            HashCode hashCode = new();
            hashCode.Add(AssemblyIdentity);

            foreach (ExternalModuleIdentity module in Modules)
            {
                hashCode.Add(module);
            }

            hashCode.Add(IsReferenceAssembly);
            return hashCode.ToHashCode();
        }
    }
}
