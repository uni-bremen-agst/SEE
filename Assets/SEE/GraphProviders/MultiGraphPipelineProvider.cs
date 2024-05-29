using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        /// The label for <see cref="Pipeline"/> in the configuration file.
        /// </summary>
        protected const string pipelineLabel = "pipeline";


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
        /// <returns></returns>
        /// <remarks>Exceptions may be thrown by each nested graph provider.</remarks>
        public override async UniTask<List<Graph>> ProvideAsync(List<Graph> graph, AbstractSEECity city,
            Action<float> changePercentage = null,
            CancellationToken token = default)
        {
            UniTask<List<Graph>> initial = UniTask.FromResult(graph);
            int count = -1; // -1 because the first provider will increment it to 0.
            return await Pipeline.Aggregate(initial, (current, provider) =>
                current.ContinueWith(g => provider.ProvideAsync(g, city, AggregatePercentage(), token)));

            Action<float> AggregatePercentage()
            {
                // Counts the number of providers that have been executed so far.
                count++;
                // Each stage of the pipeline gets an equal share of the total percentage.
                return percentage => changePercentage?.Invoke((count + percentage) / Pipeline.Count);
            }
        }

        /// <summary>
        /// Adds <paramref name="provider"/> at the end of the <see cref="Pipeline"/>.
        /// </summary>
        /// <param name="provider">graph provider to be added</param>
        internal void Add(MultiGraphPipelineProvider provider) => Pipeline.Add(provider);

        /// <summary>
        /// Returns the kind of this provider
        /// </summary>
        /// <returns>returns <see cref="MultiGraphProviderKind.MultiPipeline"/></returns>
        public override MultiGraphProviderKind GetKind()
        {
            return MultiGraphProviderKind.MultiPipeline;
        }

        /// <summary>
        /// Saves the attributes to the config writer <paramref name="writer"/>
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes to</param>
        protected override void SaveAttributes(ConfigWriter writer)
        {
            writer.BeginList(pipelineLabel);
            foreach (MultiGraphProvider provider in Pipeline)
            {
                provider.Save(writer, "");
            }

            writer.EndList();
        }

        /// <summary>
        /// Resotres the attributes from the passed attributes <paramref name="attributes"/>
        /// </summary>
        /// <param name="attributes">The attributes to restore from</param>
        /// <exception cref="InvalidCastException">If some types don't match in the config file</exception>
        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            if (attributes.TryGetValue(pipelineLabel, out object v))
            {
                try
                {
                    IList items = (IList)v;
                    Pipeline.Clear();
                    foreach (object item in items)
                    {
                        Dictionary<string, object> dict = (Dictionary<string, object>)item;
                        MultiGraphProvider provider = MultiGraphProvider.RestoreProvider(dict);
                        Pipeline.Add(provider);
                    }
                }
                catch (InvalidCastException e)
                {
                    throw new InvalidCastException("Types are not assignment compatible."
                                                   + $" Expected type: IList<{typeof(MultiGraphProvider)}>. Actual type: {v.GetType()}."
                                                   + $" Original exception: {e.Message} {e.StackTrace}");
                }
            }
        }
    }
}
