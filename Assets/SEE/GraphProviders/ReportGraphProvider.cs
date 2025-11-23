using SEE.DataModel.DG;
using SEE.Game.City;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using SEE.DataModel.DG.IO;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Reads metrics from a report file and applies them to a graph.
    /// </summary>
    [Serializable]
    public class ReportGraphProvider : FileBasedSingleGraphProvider
    {
        /// <summary>
        /// Configuration that specifies how the selected report must be parsed.
        /// This value must not be null when a report is provided.
        /// </summary>
        [SerializeReference, InlineProperty]
        public ParsingConfig ParsingConfig;

        /// <summary>
        /// Reads metrics from a report file and adds these to <paramref name="graph"/>.
        /// The resulting graph is returned.
        /// </summary>
        /// <param name="graph">
        /// Existing graph to which the metrics are added. Must not be null.
        /// </param>
        /// <param name="city">
        /// City that provides context for the graph. Must not be null.
        /// </param>
        /// <param name="_changePercentage">
        /// Change percentage callback. This parameter is currently ignored.
        /// </param>
        /// <param name="token">
        /// Cancellation token that can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// The input <paramref name="graph"/> with metrics added. The returned value is never null.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <see cref="FileBasedSingleGraphProvider.Path"/> is undefined or does not exist,
        /// or if <paramref name="city"/> is null.
        /// </exception>
        /// <exception cref="NotImplementedException">
        /// Thrown if <paramref name="graph"/> is null; this is currently not supported.
        /// </exception>
        public override async UniTask<Graph> ProvideAsync(
            Graph graph,
            AbstractSEECity city,
            Action<float> _changePercentage = null,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            CheckArguments(city);
            if (graph == null)
            {
                throw new NotImplementedException();
            }

            await UniTask.SwitchToThreadPool();

            IReportParser parser = ParsingConfig.CreateParser();
            MetricSchema metricSchema = await parser.ParseAsync(Path, token);
            MetricApplier.ApplyMetrics(graph, metricSchema, ParsingConfig);

            await UniTask.SwitchToMainThread();
            return graph;
        }

        /// <summary>
        /// Identifies this provider so that configuration user interfaces can list it next to other providers.
        /// </summary>
        /// <returns>
        /// The kind of this provider, which is <see cref="SingleGraphProviderKind.Report"/>.
        /// </returns>
        public override SingleGraphProviderKind GetKind()
        {
            return SingleGraphProviderKind.Report;
        }
    }
}
