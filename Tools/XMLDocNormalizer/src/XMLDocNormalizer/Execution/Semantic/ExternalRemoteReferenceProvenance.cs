namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>Records how one remote artifact candidate reached unchanged P5 validation.</summary>
    /// <param name="ExpectedReference">The authoritative expected P5 descriptor.</param>
    /// <param name="ExpectedReferenceOrdinal">The expected reference ordinal.</param>
    /// <param name="ProviderKind">The remote provider classification.</param>
    /// <param name="ProviderIdentity">The credential-free provider identity.</param>
    /// <param name="DiscoveryHint">The non-authoritative discovery evidence.</param>
    /// <param name="ArtifactIdentity">The package or artifact identifier.</param>
    /// <param name="ArtifactVersion">The exact artifact version.</param>
    /// <param name="ArtifactSha512">The verified artifact SHA-512 hash.</param>
    /// <param name="ArchiveEntry">The selected safe archive entry.</param>
    /// <param name="P5ValidationSucceeded">Whether unchanged P5 validation succeeded.</param>
    internal sealed record ExternalRemoteReferenceProvenance(
        ExternalCompilationMetadataReferenceDescriptor ExpectedReference,
        int ExpectedReferenceOrdinal,
        ExternalRemoteArtifactProviderKind ProviderKind,
        string ProviderIdentity,
        string DiscoveryHint,
        string ArtifactIdentity,
        string ArtifactVersion,
        string ArtifactSha512,
        string ArchiveEntry,
        bool P5ValidationSucceeded);
}
