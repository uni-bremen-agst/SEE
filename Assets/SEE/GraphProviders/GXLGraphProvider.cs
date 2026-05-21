using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Game.City;
using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace SEE.GraphProviders
{
    /// <summary>
    /// A graph provider reading a graph from a GXL file.
    /// </summary>
    [Serializable]
    public class GXLSingleGraphProvider : FileBasedSingleGraphProvider
    {
        /// <summary>
        /// Reads and returns a graph from a GXL file with <see cref="Path"/> where
        /// the <see cref="AbstractSEECity.HierarchicalEdges"/> of <paramref name="city"/>
        /// specifies the hierarchical edges in the GXL file to be interested for nesting
        /// nodes and <see cref="AbstractSEECity.SourceCodeDirectory"/>
        /// of <paramref name="city"/> determins the base of the resulting graph.
        /// The loaded graph will have that value for its <see cref="Graph.BasePath"/>.
        /// It will be used to turn relative file-system paths into absolute ones. It should be chosen as
        /// the root directory in which the source code can be found.
        /// </summary>
        /// <param name="graph">Input graph (currently ignored).</param>
        /// <param name="city">Where the <see cref="AbstractSEECity.HierarchicalEdges"/>
        /// and <see cref="AbstractSEECity.SourceCodeDirectory"/> will be retrieved.</param>
        /// <param name="changePercentage">To report progress.</param>
        /// <param name="token">This parameter is currently ignored.</param>
        /// <returns>Loaded graph.</returns>
        /// <exception cref="ArgumentException">Thrown in case <see cref="Path"/>
        /// is undefined or does not exist or <paramref name="city"/> is null.</exception>
        /// <exception cref="NotImplementedException">Thrown if <paramref name="graph"/>
        /// has nodes; this case is currently not yet handled.</exception>
        public override async UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city,
                                                          Action<float> changePercentage = null,
                                                          CancellationToken token = default)
        {
            CheckArguments(city);
            return await GraphReader.LoadAsync(Path, city.HierarchicalEdges, city.SourceCodeDirectory.Path, changePercentage, token);
        }

        public override SingleGraphProviderKind GetKind()
        {
            return SingleGraphProviderKind.GXL;
        }
    }
}
