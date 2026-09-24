namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical
{
    /// <summary>
    /// Represents an immutable, Roslyn-independent module identity.
    /// </summary>
    internal sealed class CanonicalModuleIdentity
        : IEquatable<CanonicalModuleIdentity>
    {
        /// <summary>
        /// Initializes a canonical module identity.
        /// </summary>
        /// <param name="assembly">The containing assembly identity.</param>
        /// <param name="name">The metadata module name.</param>
        /// <param name="ordinal">The module position in the assembly.</param>
        /// <param name="moduleVersionId">
        /// The metadata MVID, or <see langword="null"/> when no emitted MVID exists.
        /// </param>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        public CanonicalModuleIdentity(
            CanonicalAssemblyIdentity assembly,
            string name,
            int ordinal,
            Guid? moduleVersionId)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Assembly = assembly;
            Name = name;
            Ordinal = ordinal;
            ModuleVersionId = moduleVersionId;
        }

        /// <summary>
        /// Gets the containing assembly identity.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalAssemblyIdentity Assembly { get; }

        /// <summary>
        /// Gets the metadata module name.
        /// </summary>
        /// <value>The value described by this property.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the module position in the containing assembly.
        /// </summary>
        /// <value>The value described by this property.</value>
        public int Ordinal { get; }

        /// <summary>
        /// Gets the emitted metadata MVID when one is available.
        /// </summary>
        /// <value>The value described by this property.</value>
        public Guid? ModuleVersionId { get; }

        /// <inheritdoc/>
        public bool Equals(CanonicalModuleIdentity? other)
        {
            return other != null
                && Assembly.Equals(other.Assembly)
                && StringComparer.Ordinal.Equals(Name, other.Name)
                && Ordinal == other.Ordinal
                && ModuleVersionId == other.ModuleVersionId;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalModuleIdentity);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                Assembly,
                StringComparer.Ordinal.GetHashCode(Name),
                Ordinal,
                ModuleVersionId);
        }
    }
}
