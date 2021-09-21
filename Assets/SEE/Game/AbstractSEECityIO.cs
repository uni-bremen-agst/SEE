using System.Collections.Generic;
using System.Linq;
using SEE.Utils;
using UnityEngine.Assertions;

namespace SEE.Game
{
    /// <summary>
    /// Configuration attribute input/output for AbstractSEECity.
    /// </summary>
    public partial class AbstractSEECity
    {
        /// <summary>
        /// The attribute labels for all attributes in the stored configuration file.
        /// </summary>

        private const string LODCullingLabel = "LODCulling";
        private const string LayoutPathLabel = "LayoutPath";
        private const string CityPathLabel = "CityPath";

        private const string HierarchicalEdgesLabel = "HierarchicalEdges";
        private const string NodeTypesLabel = "NodeTypes";

        private const string StyleIssueLabel = "StyleIssue";
        private const string UniversalIssueLabel = "UniversalIssue";
        private const string MetricIssueLabel = "MetricIssue";
        private const string Dead_CodeIssueLabel = "Dead_CodeIssue";
        private const string CycleIssueLabel = "CycleIssue";
        private const string CloneIssueLabel = "CloneIssue";
        private const string ArchitectureIssueLabel = "ArchitectureIssue";

        private const string StyleIssue_SUMLabel = "StyleIssue_SUM";
        private const string UniversalIssue_SUMLabel = "UniversalIssue_SUM";
        private const string MetricIssue_SUMLabel = "MetricIssue_SUM";
        private const string Dead_CodeIssue_SUMLabel = "Dead_CodeIssue_SUM";
        private const string CycleIssue_SUMLabel = "CycleIssue_SUM";
        private const string CloneIssue_SUMLabel = "CloneIssue_SUM";
        private const string ArchitectureIssue_SUMLabel = "ArchitectureIssue_SUM";

        private const string InnerDonutMetricLabel = "InnerDonutMetric";

        private const string MinimalBlockLengthLabel = "MinimalBlockLength";
        private const string MaximalBlockLengthLabel = "MaximalBlockLength";

        private const string NodeLayoutLabel = "NodeLayout";
        private const string EdgeLayoutLabel = "EdgeLayout";

        private const string ZScoreScaleLabel = "ZScoreScale";
        private const string ScaleOnlyLeafMetricsLabel = "ScaleOnlyLeafMetrics";

        private const string EdgeWidthLabel = "EdgeWidth";
        private const string ShowLeafErosionsLabel = "ShowLeafErosions";
        private const string ShowInnerErosionsLabel = "ShowInnerErosions";
        private const string ErosionScalingFactorLabel = "ErosionScalingFactor";
        private const string LoadDashboardMetricsLabel = "LoadDashboardMetrics";
        private const string IssuesFromVersionLabel = "IssuesAddedFromVersion";
        private const string OverrideMetricsLabel = "OverrideMetrics";
        private const string EdgesAboveBlocksLabel = "EdgesAboveBlocks";
        private const string TensionLabel = "Tension";
        private const string RDPLabel = "RDP";

        private const string CoseGraphSettingsLabel = "CoseGraphSettings";

        private const string LeafNodeAttributesLabel = "LeafNodeAttributes";
        private const string InnerNodeAttributesLabel = "InnerNodeAttributes";

        /// <summary>
        /// Saves all attributes of this AbstractSEECity instance in the configuration file
        /// using the given <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">writer for the configuration file</param>
        protected virtual void Save(ConfigWriter writer)
        {
            writer.Save(globalCityAttributes.lodCulling, LODCullingLabel);
            globalCityAttributes.layoutPath.Save(writer, LayoutPathLabel);
            writer.Save(HierarchicalEdges.ToList(), HierarchicalEdgesLabel);
            writer.Save(SelectedNodeTypes, NodeTypesLabel);
            CityPath.Save(writer, CityPathLabel);
            leafNodeAttributesPerKind.Save(writer, LeafNodeAttributesLabel);
            innerNodeAttributesPerKind.Save(writer, InnerNodeAttributesLabel);

            writer.Save(StyleIssue, StyleIssueLabel);
            writer.Save(UniversalIssue, UniversalIssueLabel);
            writer.Save(MetricIssue, MetricIssueLabel);
            writer.Save(Dead_CodeIssue, Dead_CodeIssueLabel);
            writer.Save(CycleIssue, CycleIssueLabel);
            writer.Save(CloneIssue, CloneIssueLabel);
            writer.Save(ArchitectureIssue, ArchitectureIssueLabel);

            writer.Save(StyleIssue_SUM, StyleIssue_SUMLabel);
            writer.Save(UniversalIssue_SUM, UniversalIssue_SUMLabel);
            writer.Save(MetricIssue_SUM, MetricIssue_SUMLabel);
            writer.Save(Dead_CodeIssue_SUM, Dead_CodeIssue_SUMLabel);
            writer.Save(CycleIssue_SUM, CycleIssue_SUMLabel);
            writer.Save(CloneIssue_SUM, CloneIssue_SUMLabel);
            writer.Save(ArchitectureIssue_SUM, ArchitectureIssue_SUMLabel);

            writer.Save(InnerDonutMetric, InnerDonutMetricLabel);

            writer.Save(MinimalBlockLength, MinimalBlockLengthLabel);
            writer.Save(MaximalBlockLength, MaximalBlockLengthLabel);

            writer.Save(nodeLayoutSettings.kind.ToString(), NodeLayoutLabel);
            writer.Save(edgeLayoutSettings.kind.ToString(), EdgeLayoutLabel);

            writer.Save(nodeLayoutSettings.zScoreScale, ZScoreScaleLabel);
            writer.Save(nodeLayoutSettings.ScaleOnlyLeafMetrics, ScaleOnlyLeafMetricsLabel);

            writer.Save(edgeLayoutSettings.edgeWidth, EdgeWidthLabel);
            writer.Save(nodeLayoutSettings.showInnerErosions, ShowInnerErosionsLabel);
            writer.Save(nodeLayoutSettings.showLeafErosions, ShowLeafErosionsLabel);
            writer.Save(nodeLayoutSettings.loadDashboardMetrics, LoadDashboardMetricsLabel);
            writer.Save(nodeLayoutSettings.overrideMetrics, OverrideMetricsLabel);
            writer.Save(nodeLayoutSettings.issuesAddedFromVersion, IssuesFromVersionLabel);
            writer.Save(nodeLayoutSettings.erosionScalingFactor, ErosionScalingFactorLabel);
            writer.Save(edgeLayoutSettings.edgesAboveBlocks, EdgesAboveBlocksLabel);
            writer.Save(edgeLayoutSettings.tension, TensionLabel);
            writer.Save(edgeLayoutSettings.rdp, RDPLabel);

            coseGraphSettings.Save(writer, CoseGraphSettingsLabel);
        }

        /// <summary>
        /// Restores all attributes of this AbstractSEECity instance from the given <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">dictionary containing the attributes (key = attribute label, value = attribute value)</param>
        protected virtual void Restore(Dictionary<string, object> attributes)
        {
            leafNodeAttributesPerKind.Restore(attributes, LeafNodeAttributesLabel);
            innerNodeAttributesPerKind.Restore(attributes, InnerNodeAttributesLabel);

            ConfigIO.Restore(attributes, LODCullingLabel, ref globalCityAttributes.lodCulling);
            globalCityAttributes.layoutPath.Restore(attributes, LayoutPathLabel);
            ConfigIO.Restore(attributes, HierarchicalEdgesLabel, ref HierarchicalEdges);
            ConfigIO.Restore(attributes, NodeTypesLabel, ref SelectedNodeTypes);
            CityPath.Restore(attributes, CityPathLabel);

            ConfigIO.Restore(attributes, StyleIssueLabel, ref StyleIssue);
            ConfigIO.Restore(attributes, UniversalIssueLabel, ref UniversalIssue);
            ConfigIO.Restore(attributes, MetricIssueLabel, ref MetricIssue);
            ConfigIO.Restore(attributes, Dead_CodeIssueLabel, ref Dead_CodeIssue);
            ConfigIO.Restore(attributes, CycleIssueLabel, ref CycleIssue);
            ConfigIO.Restore(attributes, CloneIssueLabel, ref CloneIssue);
            ConfigIO.Restore(attributes, ArchitectureIssueLabel, ref ArchitectureIssue);

            ConfigIO.Restore(attributes, StyleIssue_SUMLabel, ref StyleIssue_SUM);
            ConfigIO.Restore(attributes, UniversalIssue_SUMLabel, ref UniversalIssue_SUM);
            ConfigIO.Restore(attributes, MetricIssue_SUMLabel, ref MetricIssue_SUM);
            ConfigIO.Restore(attributes, Dead_CodeIssue_SUMLabel, ref Dead_CodeIssue_SUM);
            ConfigIO.Restore(attributes, CycleIssue_SUMLabel, ref CycleIssue_SUM);
            ConfigIO.Restore(attributes, CloneIssue_SUMLabel, ref CloneIssue_SUM);
            ConfigIO.Restore(attributes, ArchitectureIssue_SUMLabel, ref ArchitectureIssue_SUM);

            ConfigIO.Restore(attributes, InnerDonutMetricLabel, ref InnerDonutMetric);

            ConfigIO.Restore(attributes, MinimalBlockLengthLabel, ref MinimalBlockLength);
            ConfigIO.Restore(attributes, MaximalBlockLengthLabel, ref MaximalBlockLength);

            ConfigIO.RestoreEnum(attributes, NodeLayoutLabel, ref nodeLayoutSettings.kind);
            ConfigIO.RestoreEnum(attributes, EdgeLayoutLabel, ref edgeLayoutSettings.kind);

            ConfigIO.Restore(attributes, ZScoreScaleLabel, ref nodeLayoutSettings.zScoreScale);
            ConfigIO.Restore(attributes, ScaleOnlyLeafMetricsLabel, ref nodeLayoutSettings.ScaleOnlyLeafMetrics);
            ConfigIO.Restore(attributes, EdgeWidthLabel, ref edgeLayoutSettings.edgeWidth);
            ConfigIO.Restore(attributes, ShowInnerErosionsLabel, ref nodeLayoutSettings.showInnerErosions);
            ConfigIO.Restore(attributes, ShowLeafErosionsLabel, ref nodeLayoutSettings.showLeafErosions);
            ConfigIO.Restore(attributes, LoadDashboardMetricsLabel, ref nodeLayoutSettings.loadDashboardMetrics);
            ConfigIO.Restore(attributes, OverrideMetricsLabel, ref nodeLayoutSettings.overrideMetrics);
            ConfigIO.Restore(attributes, IssuesFromVersionLabel, ref nodeLayoutSettings.issuesAddedFromVersion);
            ConfigIO.Restore(attributes, ErosionScalingFactorLabel, ref nodeLayoutSettings.erosionScalingFactor);
            ConfigIO.Restore(attributes, EdgesAboveBlocksLabel, ref edgeLayoutSettings.edgesAboveBlocks);
            ConfigIO.Restore(attributes, TensionLabel, ref edgeLayoutSettings.tension);
            ConfigIO.Restore(attributes, RDPLabel, ref edgeLayoutSettings.rdp);

            coseGraphSettings.Restore(attributes, CoseGraphSettingsLabel);
        }
    }
}