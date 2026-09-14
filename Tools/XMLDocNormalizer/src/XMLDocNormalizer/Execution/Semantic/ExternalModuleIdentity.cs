namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Identifies one module of an external assembly by its metadata name and
    /// module version identifier.
    /// </summary>
    /// <remarks>
    /// Module order is significant in an assembly descriptor. The first
    /// module returned by Roslyn is the manifest module.
    /// </remarks>
    internal readonly record struct ExternalModuleIdentity
    {
        /// <summary>
        /// Initializes an external module identity.
        /// </summary>
        /// <param name="name">The module name stored in metadata.</param>
        /// <param name="moduleVersionId">The module version identifier.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public ExternalModuleIdentity(string name, Guid moduleVersionId)
        {
            ArgumentNullException.ThrowIfNull(name);

            Name = name;
            ModuleVersionId = moduleVersionId;
        }

        /// <summary>
        /// Gets the module name stored in metadata.
        /// </summary>
        /// <value>The metadata module name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the module version identifier.
        /// </summary>
        /// <value>
        /// The metadata module version identifier. The value is retained even
        /// when it is <see cref="Guid.Empty"/>.
        /// </value>
        public Guid ModuleVersionId { get; }
    }
}
