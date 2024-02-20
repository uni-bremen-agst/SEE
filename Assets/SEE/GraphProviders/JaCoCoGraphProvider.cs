using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Game.City;
using System;
using System.IO;
using Cysharp.Threading.Tasks;
using SEE.Utils.Config;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Reads metrics from a JaCoCo XML report file and adds these to a graph.
    /// </summary>
    [Serializable]
    public class JaCoCoGraphProvider : FileBasedGraphProvider
    {
        /// <summary>
        /// The path prefix that should added at the beginning of each path
        /// extracted from the JaCoCo report file.  This addition may be necessary 
        /// to match paths retrieved from the JaCoCo report and the path attributes
        /// <see cref="GraphElement.Path"/> of nodes in the graph the coverage metrics are 
        /// to be added.
        /// </summary>
        [Tooltip("The path prefix that should added at the beginning of each path extracted "
            + "from the JaCoCo report file possibly needed to match paths in the graph "
            + "and the JaCoCo report.")]
        public string Prefix = string.Empty;

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
                JaCoCoImporter.Load(graph, Path.Path, Prefix);
                return UniTask.FromResult(graph);
            }
        }

        public override GraphProviderKind GetKind()
        {
            return GraphProviderKind.JaCoCo;
        }

        #region Config I/O

        /// <summary>
        /// The label for <see cref="Prefix"/> in the configuration file.
        /// </summary>
        private const string prefixLabel = "prefix";

        protected override void SaveAttributes(ConfigWriter writer)
        {
            base.SaveAttributes(writer);
            writer.Save(Prefix, prefixLabel);
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            base.RestoreAttributes(attributes);
            ConfigIO.Restore(attributes, prefixLabel, ref Prefix);
        }

        #endregion
    }
}
