namespace SEE.GraphProviders
{
    /// <summary>
    /// The kind of single graph providers. These values are shown to a user, e.g., in a configuration
    /// file or in the configuration of code cities at run-time. They should be short and
    /// self-explanatory.
    /// </summary>
    /// <remarks>If a concrete subclass is derived from <see cref="SingleGraphProvider"/>, a new
    /// value should be added here.</remarks>
    public enum SingleGraphProviderKind
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
        /// For <see cref="JaCoCoGraphProvider"/>.
        /// </summary>
        JaCoCo,

        /// <summary>
        /// For <see cref="SingleGraphPipelineProvider"/>.
        /// </summary>
        SinglePipeline,

        /// <summary>
        /// For <see cref="DashboardGraphProvider"/>.
        /// </summary>
        Dashboard,

        /// <summary>
        /// For <see cref="ReflexionGraphProvider"/>.
        /// </summary>
        Reflexion,

        /// <summary>
        /// For <see cref="GitBranchesGraphProvider"/>.
        /// </summary>
        GitAllBranches,

        /// <summary>
        /// For <see cref="MergeDiffGraphProvider"/>.
        /// </summary>
        MergeDiff,

        /// <summary>
        /// For <see cref="LSPGraphProvider"/>.
        /// </summary>
        LSP,

        /// <summary>
        /// For <see cref="BetweenCommitsGraphProvider"/>.
        /// </summary>
        VCS,

        /// <summary>
        /// For <see cref="ReportGraphProvider"/>.
        /// </summary>
        Report,
    }
}
