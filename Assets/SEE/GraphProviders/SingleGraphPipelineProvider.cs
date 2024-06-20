using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Utils.Config;
using Sirenix.OdinInspector;

namespace SEE.GraphProviders
{
    /// <summary>
    /// A graph provider pipeline of multiple <see cref="SingleGraphProvider"/>
    /// </summary>
    public class SingleGraphPipelineProvider : SingleGraphProvider
    {
        /// <summary>
        /// The list of nested providers in this pipeline. These will be executed
        /// from first to last.
        /// </summary>
        [HideReferenceObjectPicker,
         ListDrawerSettings(DefaultExpandedState = true, ListElementLabelName = nameof(GetKind))]
        public List<SingleGraphProvider> Pipeline = new();

        /// <summary>
        /// Provides a graph based as a result of the serial execution of all
        /// graph providers in <see cref="Pipeline"/> from first to last.
        ///
        /// If the <see cref="Pipeline"/> is empty, <paramref name="graph"/>
        /// will be returned.
        /// </summary>
        /// <param name="graph">this graph will be input to the first
        /// graph provider in <see cref="Pipeline"/></param>
        /// <param name="city">this value will be passed to each graph provider
        /// in <see cref="Pipeline"/></param>
        /// <param name="changePercentage">this callback will be called with
        /// the percentage (0–1) of completion of the pipeline</param>
        /// <param name="token">can be used to cancel the operation</param>
        /// <returns>task that can be awaited</returns>
        /// <remarks>Exceptions may be thrown by each nested graph provider.</remarks>
        public override async UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city,
                                                          Action<float> changePercentage = null,
                                                          CancellationToken token = default)
        {
            return await GraphProviderPipeline.AggregateAsync<SingleGraphProvider, Graph, SingleGraphProviderKind>
                              (Pipeline, graph, city, changePercentage, token);
        }

        /// <summary>
        /// Adds <paramref name="provider"/> at the end of the <see cref="Pipeline"/>.
        /// </summary>
        /// <param name="provider">graph provider to be added</param>
        internal void Add(SingleGraphProvider provider) => Pipeline.Add(provider);

        public override SingleGraphProviderKind GetKind()
        {
            return SingleGraphProviderKind.SinglePipeline;
        }

        #region Config I/O
        protected override void SaveAttributes(ConfigWriter writer)
        {
            GraphProviderPipeline.SaveAttributes<SingleGraphProvider, Graph, SingleGraphProviderKind>(Pipeline, writer);
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            GraphProviderPipeline.RestoreAttributes<SingleGraphProvider, Graph, SingleGraphProviderKind>
                                      (Pipeline, attributes, SingleGraphProvider.RestoreProvider);
        }

        #endregion
    }
}
