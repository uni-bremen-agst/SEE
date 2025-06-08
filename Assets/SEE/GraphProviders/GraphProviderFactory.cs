using System;
using SEE.GraphProviders.Evolution;

namespace SEE.GraphProviders
{
    /// <summary>
    /// A factory for instances of <see cref="GraphProvider"/> and its subclasses.
    /// </summary>
    static class GraphProviderFactory
    {
        /// <summary>
        /// Returns a new instance of a suitable <see cref="SingleGraphProvider"/> for single graphs for the
        /// given <paramref name="kind"/>.
        ///
        /// Postcondition: NewInstance(k).GetKind() == k.
        /// </summary>
        /// <param name="kind">the requested kind of <see cref="SingleGraphProvider"/></param>
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
                SingleGraphProviderKind.JaCoCo => new JaCoCoGraphProvider(),
                SingleGraphProviderKind.MergeDiff => new MergeDiffGraphProvider(),
                SingleGraphProviderKind.VCS => new VCSGraphProvider(),
                SingleGraphProviderKind.LSP => new LSPGraphProvider(),
                SingleGraphProviderKind.GitAllBranches => new GitBranchesGraphProvider(),
                _ => throw new NotImplementedException($"Not implemented for {kind}")
            };
        }

        /// <summary>
        /// Returns a new instance of the corresponding <see cref="MultiGraphProvider"/> to the
        /// passed <see cref="MultiGraphPipelineProvider"/> <paramref name="kind"/>
        /// </summary>
        /// <param name="kind">The requested kind of <see cref="MultiGraphProvider"/></param>
        /// <returns>a new instance</returns>
        /// <exception cref="NotImplementedException">thrown in case the given <paramref name="kind"/>
        /// is not yet handled</exception>
        internal static MultiGraphProvider NewMultiGraphProviderInstance(MultiGraphProviderKind kind)
        {
            return kind switch
            {
                MultiGraphProviderKind.MultiPipeline => new MultiGraphPipelineProvider(),
                MultiGraphProviderKind.GXLEvolution => new GXLEvolutionGraphProvider(),
                MultiGraphProviderKind.GitEvolution => new GitEvolutionGraphProvider(),
                _ => throw new NotImplementedException($"Not implemented for {kind}")
            };
        }
    }
}
