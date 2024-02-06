using System;

namespace SEE.GraphProviders
{
    /// <summary>
    /// A factory for instances of <see cref="GraphProvider"/> and its subclasses.
    /// </summary>
    static class GraphProviderFactory
    {
        internal static GraphProvider NewInstance(GraphProviderKind kind)
        {
            return kind switch
            {
                GraphProviderKind.GXL => new GXLGraphProvider(),
                GraphProviderKind.CSV => new CSVGraphProvider(),
                GraphProviderKind.Reflexion => new ReflexionGraphProvider(),
                GraphProviderKind.Pipeline => new PipelineGraphProvider(),
                GraphProviderKind.JaCoCo => new JaCoCoGraphProvider(),
                _ => throw new NotImplementedException($"Not implemented for {kind}")
            };
        }
    }
}
