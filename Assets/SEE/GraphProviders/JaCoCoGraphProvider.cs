using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Game.City;
using System;
using System.IO;
using Cysharp.Threading.Tasks;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Reads metrics from a JaCoCo XML report file and adds these to a graph.
    /// </summary>
    [Serializable]
    internal class JaCoCoGraphProvider : FileBasedGraphProvider
    {
        /// <summary>
        /// Reads metrics from a JaCoCo XML report file and adds these to <paramref name="graph"/>.
        /// The resulting graph is returned.
        /// </summary>
        /// <param name="graph">an existing graph where to add the metrics</param>
        /// <param name="city">this value is currently ignored</param>
        /// <returns>the input <paramref name="graph"/> with metrics added</returns>
        /// <exception cref="ArgumentException">thrown in case <see cref="Path"/>
        /// is undefined or does not exist or <paramref name="city"/> is null</exception>
        /// <exception cref="NotImplementedException">thrown in case <paramref name="graph"/> is
        /// null; this is currently not supported.</exception>
        public override UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city)
        {
            CheckArguments(city);
            if (graph == null)
            {
                throw new NotImplementedException();
            }
            else
            {
                JaCoCoImporter.Load(graph, Path.Path);
                return UniTask.FromResult(graph);
            }
        }
    }
}
