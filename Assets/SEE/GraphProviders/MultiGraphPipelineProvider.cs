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
    /// A graph provider pipeline of multiple <see cref="MultiGraphProvider"/>
    /// </summary>
    public class MultiGraphPipelineProvider : MultiGraphProvider
    {
        /// <summary>
        /// The list of nested providers in this pipeline. These will be executed
        /// from first to last.
        /// </summary>
        [HideReferenceObjectPicker,
         ListDrawerSettings(DefaultExpandedState = true, ListElementLabelName = nameof(GetKind))]
        public List<MultiGraphProvider> Pipeline = new();

        /// <summary>
        /// Provides a graph based as a result of the serial execution of all
        /// graph providers in <see cref="Pipeline"/> from first to last.
        ///
        /// If the <see cref="Pipeline"/> is empty, <paramref name="graphs"/>
        /// will be returned.
        /// </summary>
        /// <param name="graphs">This list of graphs will be input to the first
        /// graph provider in <see cref="Pipeline"/>.</param>
        /// <param name="city">This value will be passed to each graph provider
        /// in <see cref="Pipeline"/>.</param>
        /// <param name="changePercentage">This callback will be called with
        /// the percentage (0–1) of completion of the pipeline.</param>
        /// <param name="token">Can be used to cancel the operation.</param>
        /// <returns>Task that can be awaited.</returns>
        /// <remarks>Exceptions may be thrown by each nested graph provider.</remarks>
        public override async UniTask<List<Graph>> ProvideAsync(List<Graph> graphs, AbstractSEECity city,
            Action<float> changePercentage = null,
            CancellationToken token = default)
        {
            return await GraphProviderPipeline.AggregateAsync<MultiGraphProvider, List<Graph>, MultiGraphProviderKind>
                (Pipeline, graphs, city, changePercentage, token);
        }

        /// <summary>
        /// Adds <paramref name="provider"/> at the end of the <see cref="Pipeline"/>.
        /// </summary>
        /// <param name="provider">Graph provider to be added.</param>
        internal void Add(MultiGraphPipelineProvider provider) => Pipeline.Add(provider);

        /// <summary>
        /// Returns the kind of this provider
        /// </summary>
        /// <returns>Returns <see cref="MultiGraphProviderKind.MultiPipeline"/>.</returns>
        public override MultiGraphProviderKind GetKind()
        {
            return MultiGraphProviderKind.MultiPipeline;
        }

        #region Config I/O

        /// <summary>
        /// Saves the attributes to the config writer <paramref name="writer"/>
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes to.</param>
        protected override void SaveAttributes(ConfigWriter writer)
        {
            GraphProviderPipeline.SaveAttributes<MultiGraphProvider, List<Graph>, MultiGraphProviderKind>(Pipeline, writer);
        }

        /// <summary>
        /// Resotres the attributes from the passed attributes <paramref name="attributes"/>
        /// </summary>
        /// <param name="attributes">The attributes to restore from.</param>
        /// <exception cref="InvalidCastException">If some types don't match in the config file.</exception>
        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            GraphProviderPipeline.RestoreAttributes<MultiGraphProvider, List<Graph>, MultiGraphProviderKind>
                          (Pipeline, attributes, MultiGraphProvider.RestoreProvider);

        }
        #endregion
    }
}
