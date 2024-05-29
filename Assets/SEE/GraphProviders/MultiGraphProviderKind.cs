
namespace SEE.GraphProviders
{
    /// <summary>
    /// MultiGraphProviderKind is basically the same as <see cref="SingleGraphProviderKind"/> but just for <see cref="MultiGraphProvider"/>.
    ///
    /// They should be short and seldexplanatory as well
    /// </summary>
    public enum MultiGraphProviderKind
    {
        /// <summary>
        /// For <see cref="MultiGraphPipelineProvider"/>
        /// </summary>
        MultiPipeline,
        
        /// <summary>
        /// For <see cref="GXLEvolutionGraphProvider"/>
        /// </summary>
        GXLEvolution,
    }
}
