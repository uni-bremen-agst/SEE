namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Identifies the local source that supplied a P5-validated reference candidate.
    /// </summary>
    internal enum ExternalReferenceArtifactSourceKind
    {
        /// <summary>The candidate came from an explicitly configured P7A root.</summary>
        ExplicitRoot,

        /// <summary>The candidate was already loaded as a Roslyn metadata reference.</summary>
        LoadedReference,

        /// <summary>The candidate came from a local .NET reference pack.</summary>
        DotNetReferencePack,

        /// <summary>The candidate came from a local .NET shared framework.</summary>
        DotNetSharedFramework,

        /// <summary>The candidate came from the local NuGet global-packages folder.</summary>
        NuGetGlobalPackages,

        /// <summary>The candidate came from a P5-validated remote NuGet package.</summary>
        RemoteNuGetPackage,

        /// <summary>The candidate came from a P5-validated remote .NET reference pack.</summary>
        RemoteDotNetReferencePack
    }
}
