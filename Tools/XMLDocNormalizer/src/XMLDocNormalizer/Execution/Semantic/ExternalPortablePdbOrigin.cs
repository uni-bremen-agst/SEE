namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>Identifies the permitted source of an exact Portable PDB.</summary>
    internal enum ExternalPortablePdbOrigin
    {
        /// <summary>The caller supplied an authoritative explicit PDB path.</summary>
        Explicit,

        /// <summary>The Portable PDB was embedded in the exact target PE.</summary>
        Embedded,

        /// <summary>The Portable PDB was a sibling of the exact target PE.</summary>
        Sibling,

        /// <summary>The Portable PDB came from an explicitly known local path.</summary>
        KnownPath,

        /// <summary>The Portable PDB came from an explicitly configured local root.</summary>
        ConfiguredRoot,

        /// <summary>The Portable PDB came from a configured NuGet package archive.</summary>
        NuGetPackage,

        /// <summary>The Portable PDB came from a configured symbol package archive.</summary>
        SymbolPackage
    }
}
