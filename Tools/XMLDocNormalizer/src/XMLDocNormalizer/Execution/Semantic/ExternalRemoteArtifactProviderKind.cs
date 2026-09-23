namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>Identifies an implemented remote reference artifact source.</summary>
    internal enum ExternalRemoteArtifactProviderKind
    {
        /// <summary>A package obtained from a configured NuGet V3 feed.</summary>
        NuGetPackage,

        /// <summary>An official .NET reference or targeting pack distributed as a package.</summary>
        DotNetReferencePack
    }
}
