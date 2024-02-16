using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;

namespace SEE.GraphProviders
{
    /// <summary>
    /// A graph provider that merges the difference between two graphs.
    /// </summary>
    internal class MergeDiffGraphProvider : GraphProvider
    {
        /// <summary>
        /// The list of providers that should be merged into the input graph
        /// of <see cref="ProvideAsync(Graph, AbstractSEECity)"/>.
        /// These will be merged successively from first to last.
        /// </summary>
        [HideReferenceObjectPicker]
        public GraphProvider OldGraph = new PipelineGraphProvider();

        public override GraphProviderKind GetKind()
        {
            return GraphProviderKind.MergeDiff;
        }

        /// <summary>
        /// Merges the graph provided by <see cref="OldGraph"/> into <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">graph in which to merge</param>
        /// <param name="city"></param>
        /// <returns>the resulting graph where changes between <paramref name="graph"/>
        /// and <see cref="OldGraph"/> have been merged into</returns>
        /// <exception cref="NotImplementedException"></exception>
        public override async UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city)
        {
            graph ??= new Graph("");
            if (OldGraph == null)
            {
                return graph;
            }

            Graph oldGraph = await OldGraph.ProvideAsync(new Graph(graph.BasePath), city);
            graph.MergeDiff(oldGraph);
            return graph;
        }

        #region Config I/O

        /// <summary>
        /// The label for <see cref="OldGraph"/> in the configuration file.
        /// </summary>
        private const string oldGraphLabel = "oldGraph";

        protected override void SaveAttributes(ConfigWriter writer)
        {
            OldGraph.Save(writer, oldGraphLabel);
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            OldGraph = Restore(attributes, oldGraphLabel);
        }

        #endregion
    }
}
