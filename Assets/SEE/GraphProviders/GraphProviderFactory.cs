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
        internal static GraphProvider NewInstance(GraphProviderKind kind)
        {
            return kind switch
            {
                GraphProviderKind.GXL => new GXLGraphProvider(),
                GraphProviderKind.CSV => new CSVGraphProvider(),
                GraphProviderKind.Reflexion => new ReflexionGraphProvider(),
                GraphProviderKind.Pipeline => new PipelineGraphProvider(),
                GraphProviderKind.JaCoCo => new JaCoCoGraphProvider(),
                GraphProviderKind.MergeDiff => new MergeDiffGraphProvider(),
                GraphProviderKind.VCS => new VCSGraphProvider(),
				GraphProviderKind.LSP => new LSPGraphProvider(),
                _ => throw new NotImplementedException($"Not implemented for {kind}")
            };
        }
    }
}
