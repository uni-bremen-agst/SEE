using SEE.DataModel.DG;
using SEE.Game.City;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using SEE.DataModel.DG.IO;
using SEE.Utils.Config;
using System.Collections.Generic;

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
        /// <param name="changePercentage">
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
        public override async UniTask<Graph> ProvideAsync
            (Graph graph,
             AbstractSEECity city,
             Action<float> changePercentage = null,
             CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            CheckArguments(city);
            if (graph == null)
            {
                changePercentage?.Invoke(1.0f);
                throw new NotImplementedException($"{nameof(ReportGraphProvider)} requires an existing graph to apply metrics to. Creating a new graph from a report is not currently supported.");
            }

            if (ParsingConfig == null)
            {
                changePercentage?.Invoke(1.0f);
                throw new ArgumentNullException(nameof(ParsingConfig));
            }

            await UniTask.SwitchToThreadPool();

            IReportParser parser = ParsingConfig.CreateParser();
            MetricSchema metricSchema = await parser.ParseAsync(Path, token);
            changePercentage?.Invoke(0.5f);

            MetricApplier.ApplyMetrics(graph, metricSchema, ParsingConfig);

            await UniTask.SwitchToMainThread();
            changePercentage?.Invoke(1.0f);

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

        #region Config I/O

        /// <summary>
        /// Label of <see cref="ParsingConfig"/> in the configuration file.
        /// </summary>
        ///
        private const string parsingConfigLabel = "ParsingConfig";

        protected override void SaveAttributes(ConfigWriter writer)
        {
            base.SaveAttributes(writer);
            ParsingConfig?.Save(writer, parsingConfigLabel);
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            base.RestoreAttributes(attributes);
            ParsingConfig.Restore(attributes, parsingConfigLabel, out ParsingConfig);
        }

        #endregion
    }
}
