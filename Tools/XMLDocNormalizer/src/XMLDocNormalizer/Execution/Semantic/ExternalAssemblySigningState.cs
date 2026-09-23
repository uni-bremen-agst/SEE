namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>Classifies the signing shape observed in one exact target PE.</summary>
    internal enum ExternalAssemblySigningState
    {
        /// <summary>No public key, signature directory, or signed CLI flag is present.</summary>
        Unsigned,

        /// <summary>A public key, signed CLI flag, and nonempty signature are present.</summary>
        FullySigned,

        /// <summary>A public key and reserved zero signature exist without the signed CLI flag.</summary>
        DelaySigned,

        /// <summary>A public key and signed CLI flag exist with only a zero signature.</summary>
        PublicSigned,

        /// <summary>The PE signing fields form no supported internally consistent shape.</summary>
        Unsupported
    }
}
