namespace SEE.GraphProviders
{
    /// <summary>
    /// MultiGraphProviderKind is basically the same as <see cref="SingleGraphProviderKind"/>
    /// but just for <see cref="MultiGraphProvider"/>.
    ///
    /// They should be short and self explanatory as they will be shown to the user.
    /// </summary>
    public enum MultiGraphProviderKind
    {
        /// <summary>
        /// For <see cref="MultiGraphPipelineProvider"/>
        /// </summary>
        MultiPipeline,

        /// <summary>
        /// For <see cref="GitEvolutionGraphProvider"/>.
        /// </summary>
        GitEvolution,

        /// <summary>
        /// For <see cref="GXLEvolutionGraphProvider"/>
        /// </summary>
        GXLEvolution,
    }
}
