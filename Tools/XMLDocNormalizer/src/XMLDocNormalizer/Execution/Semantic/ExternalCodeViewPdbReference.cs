using System.Reflection.Metadata;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Describes one CodeView PDB reference recorded in a PE debug directory.
    /// </summary>
    internal readonly record struct ExternalCodeViewPdbReference
    {
        /// <summary>
        /// Initializes a CodeView PDB reference.
        /// </summary>
        /// <param name="path">The PDB path recorded in the PE.</param>
        /// <param name="guid">The CodeView PDB identifier GUID.</param>
        /// <param name="age">The CodeView PDB age.</param>
        /// <param name="stamp">The debug-directory stamp.</param>
        /// <param name="isPortable">
        /// Whether the entry identifies a Portable PDB.
        /// </param>
        /// <param name="portablePdbId">
        /// The Portable PDB content identifier, when applicable.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        public ExternalCodeViewPdbReference(
            string path,
            Guid guid,
            int age,
            uint stamp,
            bool isPortable,
            BlobContentId? portablePdbId)
        {
            ArgumentNullException.ThrowIfNull(path);

            Path = path;
            Guid = guid;
            Age = age;
            Stamp = stamp;
            IsPortable = isPortable;
            PortablePdbId = portablePdbId;
        }

        /// <summary>
        /// Gets the PDB path recorded in the PE.
        /// </summary>
        /// <value>
        /// The unmodified provenance path. It is not a trusted file location.
        /// </value>
        public string Path { get; }

        /// <summary>
        /// Gets the CodeView PDB identifier GUID.
        /// </summary>
        /// <value>The CodeView GUID.</value>
        public Guid Guid { get; }

        /// <summary>
        /// Gets the CodeView PDB age.
        /// </summary>
        /// <value>The CodeView age.</value>
        public int Age { get; }

        /// <summary>
        /// Gets the debug-directory stamp.
        /// </summary>
        /// <value>
        /// The stamp used with <see cref="Guid"/> to identify a Portable PDB.
        /// </value>
        public uint Stamp { get; }

        /// <summary>
        /// Gets whether the CodeView entry identifies a Portable PDB.
        /// </summary>
        /// <value>
        /// <see langword="true"/> for Portable PDB CodeView data; otherwise
        /// <see langword="false"/>.
        /// </value>
        public bool IsPortable { get; }

        /// <summary>
        /// Gets the expected Portable PDB content identifier.
        /// </summary>
        /// <value>
        /// The identifier formed from <see cref="Guid"/> and <see cref="Stamp"/>,
        /// or <see langword="null"/> for non-portable CodeView data.
        /// </value>
        public BlobContentId? PortablePdbId { get; }
    }
}
