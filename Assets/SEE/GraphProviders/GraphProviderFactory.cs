using System;
using JetBrains.Annotations;
using SEE.DataModel.DG;

namespace SEE.GraphProviders
{
    /// <summary>
    /// A factory for instances of <see cref="GraphProvider"/> and its subclasses.
    /// </summary>
    static class GraphProviderFactory<T>
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
        [CanBeNull]
        internal static GraphProvider<T> NewInstance(GraphProviderKind kind)
        {
            return kind switch
            {
                GraphProviderKind.GXL => new GXLGraphProvider() as GraphProvider<T>,
                GraphProviderKind.CSV => new CSVGraphProvider() as GraphProvider<T>,
                GraphProviderKind.Reflexion => new ReflexionGraphProvider() as GraphProvider<T>,
                GraphProviderKind.Pipeline => new PipelineGraphProvider<Graph>() as GraphProvider<T>,
                GraphProviderKind.JaCoCo => new JaCoCoGraphProvider() as GraphProvider<T>,
                GraphProviderKind.VCS => new VCSGraphProvider() as GraphProvider<T>,
                GraphProviderKind.GitHistory => new GitEvolutionGraphProvider() as GraphProvider<T> ,
                _ => throw new NotImplementedException($"Not implemented for {kind}")
            };
        }
    }
}