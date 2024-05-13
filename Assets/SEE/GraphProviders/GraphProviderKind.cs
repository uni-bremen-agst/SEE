namespace SEE.GraphProviders
{
    public enum MultiGraphProviderKind
    {
        MultiPipeline,
        GitEvolution,

        /// <summary>
        /// For <see cref="GXLEvolutionGraphProvider"/>
        /// </summary>
        GXLEvolution,
    }

    /// <summary>
    /// The kind of graph providers. These values are shown to a user, e.g., in a configuration
    /// file or in the configuration of code cities at run-time. They should be short and
    /// self-explanatory.
    /// </summary>
    /// <remarks>If a concrete subclass is derived from <see cref="GraphProvider"/>, a new
    /// value should be added here.</remarks>
    public enum GraphProviderKind
    {
        /// <summary>
        /// For <see cref="GXLSingleGraphProvider"/>.
        /// </summary>
        GXL,

        /// <summary>
        /// For <see cref="CSVGraphProvider"/>.
        /// </summary>
        CSV,

        /// <summary>
        /// For <see cref="JaCoCoSingleGraphProvider"/>.
        /// </summary>
        JaCoCo,

        /// <summary>
        /// For <see cref="PipelineGraphProvider"/>.
        /// </summary>
        SinglePipeline,

        MultiPipeline,

        /// <summary>
        /// For <see cref="DashboardGraphProvider"/>.
        /// </summary>
        Dashboard,

        /// <summary>
        /// For <see cref="VCSGraphProvider"/>.
        /// </summary>
        VCS,

        /// <summary>
        /// For <see cref="ReflexionGraphProvider"/>.
        /// </summary>
        Reflexion,

        /// <summary>
        /// For <see cref="GitEvolutionGraphSingleProvider"/>
        /// </summary>
        GitHistory,

        /// <summary>
        /// For <see cref="AllBranchGitSingleProvider"/>
        /// </summary>
        GitAllBranches,

        /// <summary>
        /// For <see cref="MergeDiffGraphProvider"/>.
        /// </summary>
        MergeDiff,

        /// <summary>
        /// For <see cref="LSPGraphProvider"/>.
        /// </summary>
        LSP
    }
}