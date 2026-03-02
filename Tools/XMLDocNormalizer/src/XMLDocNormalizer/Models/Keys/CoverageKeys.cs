namespace XMLDocNormalizer.Models.Keys
{
    /// <summary>
    /// Provides stable keys for derived coverage metrics.
    /// </summary>
    /// <remarks>
    /// Coverage values are ratios in the range [0..1] and should remain stable across versions so
    /// machine consumers can rely on the identifiers.
    /// </remarks>
    internal static class CoverageKeys
    {
        /// <summary>
        /// Ratio of missing &lt;param&gt; tags relative to total parameters.
        /// </summary>
        public const string ParamMissingTagRate = "ParamMissingTagRate";

        /// <summary>
        /// Ratio of empty &lt;param&gt; descriptions relative to total parameters.
        /// </summary>
        public const string ParamEmptyDescriptionRate = "ParamEmptyDescriptionRate";

        /// <summary>
        /// Ratio of missing &lt;typeparam&gt; tags relative to total type parameters.
        /// </summary>
        public const string TypeParamMissingTagRate = "TypeParamMissingTagRate";

        /// <summary>
        /// Ratio of empty &lt;typeparam&gt; descriptions relative to total type parameters.
        /// </summary>
        public const string TypeParamEmptyDescriptionRate = "TypeParamEmptyDescriptionRate";

        /// <summary>
        /// Ratio of missing &lt;returns&gt; tags relative to members that require returns documentation.
        /// </summary>
        public const string ReturnsMissingRate = "ReturnsMissingRate";

        /// <summary>
        /// Ratio of namespaces missing central namespace documentation relative to total namespace declarations.
        /// </summary>
        public const string NamespaceCentralDocMissingRate = "NamespaceCentralDocMissingRate";
    }
}