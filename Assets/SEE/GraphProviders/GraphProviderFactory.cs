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
        internal static SingleGraphProvider NewSingleGraphProviderInstance(SingleGraphProviderKind kind)
        {
            return kind switch
            {
                SingleGraphProviderKind.GXL => new GXLSingleGraphProvider(),
                SingleGraphProviderKind.CSV => new CSVGraphProvider(),
                SingleGraphProviderKind.Reflexion => new ReflexionGraphProvider(),
                SingleGraphProviderKind.SinglePipeline => new SingleGraphPipelineProvider(),
                SingleGraphProviderKind.JaCoCo => new JaCoCoSingleGraphProvider(),
                SingleGraphProviderKind.MergeDiff => new MergeDiffGraphProvider(),
                SingleGraphProviderKind.VCS => new VCSGraphProvider(),
                SingleGraphProviderKind.GitAllBranches => new AllGitBranchesSingleGraphProvider(),
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