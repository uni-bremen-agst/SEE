using Cysharp.Threading.Tasks;
using SEE.Game.City;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Provides utility methods for aggregating the results of multiple
    /// graph provider in a sequential pipeline.
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
        /// <param name="graphs">these graphs will be input to the first
        /// graph provider in <paramref name="pipeline"/></param>
        /// <param name="city">this value will be passed to each graph provider
        /// in <paramref name="pipeline"/></param>
        /// <param name="changePercentage">this callback will be called with
        /// the percentage (0–1) of completion of the pipeline</param>
        /// <param name="token">can be used to cancel the operation</param>
        /// <returns>task that can be awaited</returns>
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
    }
}