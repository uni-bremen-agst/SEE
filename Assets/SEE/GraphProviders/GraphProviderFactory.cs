using System;

namespace SEE.GraphProviders
{
    /// <summary>
    /// A factory for instances of <see cref="GraphProvider"/> and its subclasses.
    /// </summary>
    static class GraphProviderFactory
    {
        /// <summary>
        /// Returns a new instance of a suitable <see cref="GraphProvider"/> for the
        /// given <paramref name="kind"/>.
        ///
        /// Postcondition: NewInstance(k).GetKind() == k.
        /// </summary>
        /// <param name="kind">the requested kind of <see cref="GraphProvider"/></param>
        /// <returns>a new instance</returns>
        /// <exception cref="NotImplementedException">thrown in case the given <paramref name="kind"/>
        /// is not yet handled</exception>
        internal static SingleGraphProvider NewSingleGraphProviderInstance(GraphProviderKind kind)
        {
            return kind switch
            {
                GraphProviderKind.GXL => new GXLSingleGraphProvider(),
                GraphProviderKind.CSV => new CSVGraphProvider(),
                GraphProviderKind.Reflexion => new ReflexionGraphProvider(),
                GraphProviderKind.SinglePipeline => new SingleGraphPipelineProvider(),
                GraphProviderKind.JaCoCo => new JaCoCoSingleGraphProvider(),
                GraphProviderKind.MergeDiff => new MergeDiffGraphProvider(),
                GraphProviderKind.VCS => new VCSGraphProvider(),
                GraphProviderKind.GitAllBranches => new AllBranchGitSingleProvider(),
                _ => throw new NotImplementedException($"Not implemented for {kind}")
            };
        }

        internal static MultiGraphProvider NewMultiGraphProviderInstance(MultiGraphProviderKind kind)
        {
            return kind switch
            {
                MultiGraphProviderKind.MultiPipeline => new MultiGraphPipelineProvider(),
                MultiGraphProviderKind.GitEvolution => new GitEvolutionGraphProvider(),
                MultiGraphProviderKind.GXLEvolution => new GXLEvolutionGraphProvider(),
                _ => throw new NotImplementedException($"Not implemented for {kind}")
            };
        }
    }
}