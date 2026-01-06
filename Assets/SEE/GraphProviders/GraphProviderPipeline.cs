using Cysharp.Threading.Tasks;
using SEE.Game.City;
using SEE.Utils.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Provides utility methods for multiple graph providers contained in a
    /// sequential pipeline.
    /// </summary>
    internal static class GraphProviderPipeline
    {
        /// <summary>
        /// Provides a graph based as a result of the serial execution of all
        /// graph providers in <paramref name="pipeline"/> from first to last.
        ///
        /// If the <paramref name="pipeline"/> is empty, <paramref name="graphs"/>
        /// will be returned.
        /// </summary>
        /// <param name="pipeline">The pipeline of graph providers whose results are to be aggregated.</param>
        /// <param name="graphs">These graphs will be input to the first
        /// graph provider in <paramref name="pipeline"/>.</param>
        /// <param name="city">This value will be passed to each graph provider
        /// in <paramref name="pipeline"/>.</param>
        /// <param name="changePercentage">This callback will be called with
        /// the percentage (0–1) of completion of the pipeline.</param>
        /// <param name="token">Can be used to cancel the operation.</param>
        /// <returns>Task that can be awaited.</returns>
        /// <typeparam name="P">graph provider contained in the pipeline</typeparam>
        /// <typeparam name="T">the input and output of the graph provider (single or list of graphs)</typeparam>
        /// <typeparam name="K">the enum representing the kind of graph provider</typeparam>
        /// <remarks>Exceptions may be thrown by each nested graph provider.</remarks>
        public static async UniTask<T> AggregateAsync<P, T, K>
            (List<P> pipeline,
            T graphs,
            AbstractSEECity city,
            Action<float> changePercentage = null,
            CancellationToken token = default)
            where P : GraphProvider<T, K> where K : Enum
        {
            UniTask<T> initial = UniTask.FromResult(graphs);
            // Counts the number of providers that have been executed so far.
            // Initially -1 because the first provider will increment it to 0.
            int count = -1;

            return await pipeline.Aggregate(initial, (current, provider) =>
                current.ContinueWith(g => provider.ProvideAsync(g, city, AggregatePercentage(), token)));

            Action<float> AggregatePercentage()
            {
                count++;
                // Each stage of the pipeline gets an equal share of the total percentage.
                return percentage => changePercentage?.Invoke((count + percentage) / pipeline.Count);
            }
        }

        #region Config I/O
        /// <summary>
        /// The label for a graph-provider pipeline in the configuration file.
        /// </summary>
        private const string pipelineLabel = "pipeline";

        /// <summary>
        /// Saves the values of the graph providers in <paramref name="pipeline"/> using <paramref name="writer"/>.
        /// </summary>
        /// <typeparam name="P">graph provider contained in the pipeline</typeparam>
        /// <typeparam name="T">the input and output of the graph provider (single or list of graphs)</typeparam>
        /// <typeparam name="K">the enum representing the kind of graph provider</typeparam>
        /// <param name="pipeline">The pipeline to be saved.</param>
        /// <param name="writer">Config writer to be used to save the values.</param>
        public static void SaveAttributes<P, T, K>(List<P> pipeline, ConfigWriter writer)
            where P : GraphProvider<T, K> where K : Enum
        {
            writer.BeginList(pipelineLabel);
            foreach (P provider in pipeline)
            {
                provider.Save(writer, "");
            }
            writer.EndList();
        }

        /// <summary>
        /// Restores the values of the graph providers in <paramref name="pipeline"/> from <paramref name="attributes"/>.
        /// </summary>
        /// <typeparam name="P">graph provider contained in the pipeline</typeparam>
        /// <typeparam name="T">the input and output of the graph provider (single or list of graphs)</typeparam>
        /// <typeparam name="K">the enum representing the kind of graph provider</typeparam>
        ///  <param name="pipeline">The pipeline to be restored.</param>
        /// <param name="attributes">The attributes from which to restore the values.</param>
        /// <param name="restoreProvider">The delegate to be called for restoring the attributes of a
        /// graph provider contained in <paramref name="pipeline"/>.</param>
        /// <exception cref="InvalidCastException">.</exception>
        public static void RestoreAttributes<P, T, K>
            (List<P> pipeline,
             Dictionary<string, object> attributes,
             Func<Dictionary<string, object>, P> restoreProvider)
            where P : GraphProvider<T, K> where K : Enum
        {
            if (attributes.TryGetValue(pipelineLabel, out object v))
            {
                try
                {
                    IList items = (IList)v;
                    pipeline.Clear();
                    foreach (object item in items)
                    {
                        Dictionary<string, object> dict = (Dictionary<string, object>)item;
                        P provider = restoreProvider(dict);
                        pipeline.Add(provider);
                    }
                }
                catch (InvalidCastException e)
                {
                    throw new InvalidCastException("Types are not assignment compatible."
                                                   + $" Expected type: IList<{typeof(P)}>. Actual type: {v.GetType()}."
                                                   + $" Original exception: {e.Message} {e.StackTrace}");
                }
            }
        }
        #endregion
    }
}
