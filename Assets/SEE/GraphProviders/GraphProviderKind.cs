namespace SEE.GraphProviders
{
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
        /// For <see cref="GXLGraphProvider"/>.
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
        /// For <see cref="PipelineGraphProvider"/>.
        /// </summary>
        Pipeline,
        /// <summary>
        /// For <see cref="DashboardGraphProvider"/>.
        /// </summary>
        Dashboard,
        /// <summary>
        /// For <see cref="ReflexionGraphProvider"/>.
        /// </summary>
        Reflexion,
        /// <summary>
        /// For <see cref="MergeDiffGraphProvider"/>.
        /// </summary>
        MergeDiff,
        /// <summary>
        /// For <see cref="LSPGraphProvider"/>.
        /// </summary>
        LSP,
        /// <summary>
        /// For <see cref="GitGraphProvider"/>.
        /// </summary>
        Git
    }
}
