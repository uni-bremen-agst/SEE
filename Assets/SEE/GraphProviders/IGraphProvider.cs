using SEE.DataModel.DG;
using SEE.Game.City;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Defines the interface of classes providing <see cref="Graph"/>s.
    /// </summary>
    internal interface IGraphProvider
    {
        /// <summary>
        /// Yields a new graph based on the input <paramref name="graph"/>.
        /// The input <paramref name="graph"/> may be empty.
        /// </summary>
        /// <param name="graph">input graph</param>
        /// <param name="city">settings possibly necessary to provide a graph</param>
        /// <returns>provided graph based on <paramref name="graph"/></returns>
        Graph Provide(Graph graph, AbstractSEECity city);
    }
}
