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
    /// Reads metrics from a JaCoCo XML report file and adds these to a graph.
    /// </summary>
    [Serializable]
    public class JaCoCoGraphProvider : FileBasedSingleGraphProvider
    {
        /// <summary>
        /// Reads metrics from a JaCoCo XML report file and adds these to <paramref name="graph"/>.
        /// The resulting graph is returned.
        /// </summary>
        /// <param name="graph">an existing graph where to add the metrics</param>
        /// <param name="city">this value is currently ignored</param>
        /// <param name="changePercentage">this parameter is currently ignored</param>
        /// <param name="token">this parameter is currently ignored</param>
        /// <returns>the input <paramref name="graph"/> with metrics added</returns>
        /// <exception cref="ArgumentException">thrown in case <see cref="Path"/>
        /// is undefined or does not exist or <paramref name="city"/> is null</exception>
        /// <exception cref="NotImplementedException">thrown in case <paramref name="graph"/> is
        /// null; this is currently not supported.</exception>
        public override async UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city,
                                                          Action<float> changePercentage = null,
                                                          CancellationToken token = default)
        {
            CheckArguments(city);
            if (graph == null)
            {
                throw new NotImplementedException();
            }
            else
            {
                await UniTask.SwitchToThreadPool();
                await JaCoCoImporter.LoadAsync(graph, Path);
                await UniTask.SwitchToMainThread();
                return graph;
            }
        }

        public override SingleGraphProviderKind GetKind()
        {
            return SingleGraphProviderKind.JaCoCo;
        }
    }
}
