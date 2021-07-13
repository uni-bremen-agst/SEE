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
        private const string LeafNodeAttributesCountLabel = "LeafNodeAttributesCount";
        private const string InnerNodeAttributesCountLabel = "InnerNodeAttributesCount";

        private const string LODCullingLabel = "LODCulling";
        private const string LayoutPathLabel = "LayoutPath";
        private const string CityPathLabel = "CityPath";

        private const string WidthMetricLabel = "WidthMetric";
        private const string HeightMetricLabel = "HeightMetric";
        private const string DepthMetricLabel = "DepthMetric";
        private const string LeafStyleMetricLabel = "LeafStyleMetric";

        private const string LeafLabelSettingsLabel = "LeafLabelSettings";
        private const string InnerNodeLabelSettingsLabel = "InnerNodeLabelSettings";
        private const string LeafNodeColoringKindLabel = "LeafNodeColoringKind";
        private const string InnerNodeColoringKindLabel = "InnerNodeColoringKind";
        private const string LeafNodeColorRangeLabel = "LeafNodeColorRange";
        private const string InnerNodeColorRangeLabel = "InnerNodeColorRange";
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
        private const string InnerNodeHeightMetricLabel = "InnerNodeHeightMetric";
        private const string InnerNodeStyleMetricLabel = "InnerNodeStyleMetric";

        private const string MinimalBlockLengthLabel = "MinimalBlockLength";
        private const string MaximalBlockLengthLabel = "MaximalBlockLength";

        private const string LeafObjectsLabel = "LeafObjects";
        private const string InnerNodeObjectsLabel = "InnerNodeObjects";

        private const string NodeLayoutLabel = "NodeLayout";
        private const string EdgeLayoutLabel = "EdgeLayout";

        private const string ZScoreScaleLabel = "ZScoreScale";
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

        /// <summary>
        /// Saves all attributes of this AbstractSEECity instance in the configuration file
        /// using the given <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">writer for the configuration file</param>
        protected virtual void Save(ConfigWriter writer)
        {
            writer.Save(leafNodeAttributesPerKind.Length, LeafNodeAttributesCountLabel);
            writer.Save(innerNodeAttributesPerKind.Length, InnerNodeAttributesCountLabel);

            writer.Save(globalCityAttributes.lodCulling, LODCullingLabel);
            globalCityAttributes.layoutPath.Save(writer, LayoutPathLabel);
            writer.Save(HierarchicalEdges.ToList(), HierarchicalEdgesLabel);
            writer.Save(SelectedNodeTypes, NodeTypesLabel);
            CityPath.Save(writer, CityPathLabel);
            for (int i = 0; i < leafNodeAttributesPerKind.Length; i++)
            {
                string postfix = '_' + i.ToString();
                writer.Save(leafNodeAttributesPerKind[i].kind.ToString(), LeafObjectsLabel + postfix);
                leafNodeAttributesPerKind[i].colorRange.Save(writer, LeafNodeColorRangeLabel + postfix);
                writer.Save(leafNodeAttributesPerKind[i].widthMetric, WidthMetricLabel + postfix);
                writer.Save(leafNodeAttributesPerKind[i].heightMetric, HeightMetricLabel + postfix);
                writer.Save(leafNodeAttributesPerKind[i].depthMetric, DepthMetricLabel + postfix);
                writer.Save(leafNodeAttributesPerKind[i].styleMetric, LeafStyleMetricLabel + postfix);
                leafNodeAttributesPerKind[i].labelSettings.Save(writer, LeafLabelSettingsLabel + postfix);
            }
            for (int i = 0; i < innerNodeAttributesPerKind.Length; i++)
            {
                string postfix = '_' + i.ToString();
                writer.Save(innerNodeAttributesPerKind[i].kind.ToString(), InnerNodeObjectsLabel + postfix);
                innerNodeAttributesPerKind[i].colorRange.Save(writer, InnerNodeColorRangeLabel + postfix);
                writer.Save(innerNodeAttributesPerKind[i].heightMetric, InnerNodeHeightMetricLabel + postfix);
                writer.Save(innerNodeAttributesPerKind[i].styleMetric, InnerNodeStyleMetricLabel + postfix);
                innerNodeAttributesPerKind[i].labelSettings.Save(writer, InnerNodeLabelSettingsLabel + postfix);
            }

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
            int leafNodeAttributesCount = 0;
            ConfigIO.Restore(attributes, LeafNodeAttributesCountLabel, ref leafNodeAttributesCount);
            Assert.IsNotNull(leafNodeAttributesPerKind);
            Assert.IsTrue(leafNodeAttributesPerKind.Length == leafNodeAttributesCount);

            int innerNodeAttributesCount = 0;
            ConfigIO.Restore(attributes, InnerNodeAttributesCountLabel, ref innerNodeAttributesCount);
            Assert.IsNotNull(innerNodeAttributesPerKind);
            Assert.IsTrue(innerNodeAttributesPerKind.Length == innerNodeAttributesCount);

            ConfigIO.Restore(attributes, LODCullingLabel, ref globalCityAttributes.lodCulling);
            globalCityAttributes.layoutPath.Restore(attributes, LayoutPathLabel);
            ConfigIO.Restore(attributes, HierarchicalEdgesLabel, ref HierarchicalEdges);
            ConfigIO.Restore(attributes, NodeTypesLabel, ref SelectedNodeTypes);
            CityPath.Restore(attributes, CityPathLabel);
            for (int i = 0; i < leafNodeAttributesCount; i++)
            {
                string postfix = '_' + i.ToString();
                ConfigIO.RestoreEnum(attributes, LeafObjectsLabel + postfix, ref leafNodeAttributesPerKind[i].kind);
                leafNodeAttributesPerKind[i].colorRange.Restore(attributes, LeafNodeColorRangeLabel + postfix);
                ConfigIO.Restore(attributes, WidthMetricLabel + postfix, ref leafNodeAttributesPerKind[i].widthMetric);
                ConfigIO.Restore(attributes, HeightMetricLabel + postfix, ref leafNodeAttributesPerKind[i].heightMetric);
                ConfigIO.Restore(attributes, DepthMetricLabel + postfix, ref leafNodeAttributesPerKind[i].depthMetric);
                ConfigIO.Restore(attributes, LeafStyleMetricLabel + postfix, ref leafNodeAttributesPerKind[i].styleMetric);
                leafNodeAttributesPerKind[i].labelSettings.Restore(attributes, LeafLabelSettingsLabel + postfix);
            }
            for (int i = 0; i < innerNodeAttributesCount; i++)
            {
                string postfix = '_' + i.ToString();
                ConfigIO.RestoreEnum(attributes, InnerNodeObjectsLabel + postfix, ref innerNodeAttributesPerKind[i].kind);
                innerNodeAttributesPerKind[i].colorRange.Restore(attributes, InnerNodeColorRangeLabel + postfix);
                ConfigIO.Restore(attributes, InnerNodeHeightMetricLabel + postfix, ref innerNodeAttributesPerKind[i].heightMetric);
                ConfigIO.Restore(attributes, InnerNodeStyleMetricLabel + postfix, ref innerNodeAttributesPerKind[i].styleMetric);
                innerNodeAttributesPerKind[i].labelSettings.Restore(attributes, InnerNodeLabelSettingsLabel + postfix);
            }

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