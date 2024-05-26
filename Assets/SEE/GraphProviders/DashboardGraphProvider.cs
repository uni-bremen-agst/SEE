using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using UnityEngine;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Reads metrics from the Axivion Dashboard and adds these to a graph.
    /// </summary>
    [Serializable]
    public class DashboardGraphProvider: SingleGraphProvider
    {
        /// <summary>
        /// Whether metrics retrieved from the dashboard shall override existing metrics.
        /// </summary>
        [Tooltip("Whether metrics retrieved from the dashboard shall override existing metrics."),
            RuntimeTab(GraphProviderFoldoutGroup)]
        public bool OverrideMetrics = true;

        /// <summary>
        /// If empty, all issues will be retrieved. Otherwise, only those issues which have been added from
        /// the given version to the most recent one will be loaded.
        /// </summary>
        [Tooltip("Version for which to retrieve issues. If empty, all issues are loaded."),
            RuntimeTab(GraphProviderFoldoutGroup)]
        public string IssuesAddedFromVersion = "";

        /// <summary>
        /// Loads the metrics available at the Axivion Dashboard into the <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph into which the metrics shall be loaded</param>
        /// <param name="city">This parameter is currently ignored.</param>
        /// <param name="changePercentage">This parameter is currently ignored.</param>
        /// <param name="token">This parameter is currently ignored.</param>
        public override async UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city,
                                                          Action<float> changePercentage = null,
                                                          CancellationToken token = default)
        {
            string startVersion = string.IsNullOrEmpty(IssuesAddedFromVersion) ? null : IssuesAddedFromVersion;
            Debug.Log($"Loading metrics and added issues from the Axivion Dashboard for start version {startVersion}.\n");
            return await MetricImporter.LoadDashboardAsync(graph, OverrideMetrics, startVersion, changePercentage, token);
        }

        public override SingleGraphProviderKind GetKind()
        {
            return SingleGraphProviderKind.Dashboard;
        }

        #region Configuration file input/output

        /// <summary>
        /// Label of attribute <see cref="OverrideMetrics"/> in the configuration file.
        /// </summary>
        private const string overrideMetricsLabel = "OverrideMetrics";

        /// <summary>
        /// Label of attribute <see cref="IssuesAddedFromVersion"/> in the configuration file.
        /// </summary>
        private const string issuesAddedFromVersionLabel = "IssuesAddedFromVersion";

        protected override void SaveAttributes(ConfigWriter writer)
        {
            writer.Save(OverrideMetrics, overrideMetricsLabel);
            writer.Save(IssuesAddedFromVersion, issuesAddedFromVersionLabel);
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            ConfigIO.Restore(attributes, overrideMetricsLabel, ref OverrideMetrics);
            ConfigIO.Restore(attributes, issuesAddedFromVersionLabel, ref IssuesAddedFromVersion);
        }

        #endregion
    }
}
