using System.Collections.Generic;
using System.Linq;
using SEE.Utils;

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

        private const string WidthMetricLabel = "WidthMetric";
        private const string HeightMetricLabel = "HeightMetric";
        private const string DepthMetricLabel = "DepthMetric";
        private const string LeafStyleMetricLabel = "LeafStyleMetric";

        private const string LeafLabelSettingsLabel = "LeafLabelSettings";
        private const string InnerNodeLabelSettingsLabel = "InnerNodeLabelSettings";
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
        private const string ShowErosionsLabel = "ShowErosions";
        private const string MaxErosionWidthLabel = "MaxErosionWidth";
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
            writer.Save(globalAttributes.lodCulling, LODCullingLabel);
            globalAttributes.layoutPath.Save(writer, LayoutPathLabel);
            writer.Save(HierarchicalEdges.ToList<string>(), HierarchicalEdgesLabel);
            writer.Save(SelectedNodeTypes, NodeTypesLabel);
            CityPath.Save(writer, CityPathLabel);
            leafNodeAttributes.colorRange.Save(writer, LeafNodeColorRangeLabel);
            innerNodeAttributes.colorRange.Save(writer, InnerNodeColorRangeLabel);
            writer.Save(leafNodeAttributes.widthMetric, WidthMetricLabel);
            writer.Save(leafNodeAttributes.heightMetric, HeightMetricLabel);
            writer.Save(leafNodeAttributes.depthMetric, DepthMetricLabel);
            writer.Save(leafNodeAttributes.styleMetric, LeafStyleMetricLabel);
            leafNodeAttributes.labelSettings.Save(writer, LeafLabelSettingsLabel);
            innerNodeAttributes.labelSettings.Save(writer, InnerNodeLabelSettingsLabel);

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
            writer.Save(innerNodeAttributes.heightMetric, InnerNodeHeightMetricLabel);
            writer.Save(innerNodeAttributes.styleMetric, InnerNodeStyleMetricLabel);

            writer.Save(MinimalBlockLength, MinimalBlockLengthLabel);
            writer.Save(MaximalBlockLength, MaximalBlockLengthLabel);

            writer.Save(nodeLayout.leafKind.ToString(), LeafObjectsLabel);
            writer.Save(nodeLayout.innerKind.ToString(), InnerNodeObjectsLabel);

            writer.Save(nodeLayout.kind.ToString(), NodeLayoutLabel);
            writer.Save(edgeLayout.kind.ToString(), EdgeLayoutLabel);

            writer.Save(nodeLayout.zScoreScale, ZScoreScaleLabel);
            writer.Save(edgeLayout.edgeWidth, EdgeWidthLabel);
            writer.Save(nodeLayout.showErosions, ShowErosionsLabel);
            writer.Save(nodeLayout.maxErosionWidth, MaxErosionWidthLabel);
            writer.Save(edgeLayout.edgesAboveBlocks, EdgesAboveBlocksLabel);
            writer.Save(edgeLayout.tension, TensionLabel);
            writer.Save(edgeLayout.rdp, RDPLabel);

            coseGraphSettings.Save(writer, CoseGraphSettingsLabel);
        }

        /// <summary>
        /// Restores all attributes of this AbstractSEECity instance from the given <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">dictionary containing the attributes (key = attribute label, value = attribute value)</param>
        protected virtual void Restore(Dictionary<string, object> attributes)
        {
            ConfigIO.Restore(attributes, LODCullingLabel, ref globalAttributes.lodCulling);
            globalAttributes.layoutPath.Restore(attributes, LayoutPathLabel);
            ConfigIO.Restore(attributes, HierarchicalEdgesLabel, ref HierarchicalEdges);
            ConfigIO.Restore(attributes, NodeTypesLabel, ref SelectedNodeTypes);
            CityPath.Restore(attributes, CityPathLabel);
            leafNodeAttributes.colorRange.Restore(attributes, LeafNodeColorRangeLabel);
            innerNodeAttributes.colorRange.Restore(attributes, InnerNodeColorRangeLabel);
            ConfigIO.Restore(attributes, WidthMetricLabel, ref leafNodeAttributes.widthMetric);
            ConfigIO.Restore(attributes, HeightMetricLabel, ref leafNodeAttributes.heightMetric);
            ConfigIO.Restore(attributes, DepthMetricLabel, ref leafNodeAttributes.depthMetric);
            ConfigIO.Restore(attributes, LeafStyleMetricLabel, ref leafNodeAttributes.styleMetric);
            leafNodeAttributes.labelSettings.Restore(attributes, LeafLabelSettingsLabel);
            innerNodeAttributes.labelSettings.Restore(attributes, InnerNodeLabelSettingsLabel);

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
            ConfigIO.Restore(attributes, InnerNodeHeightMetricLabel, ref innerNodeAttributes.heightMetric);
            ConfigIO.Restore(attributes, InnerNodeStyleMetricLabel, ref innerNodeAttributes.styleMetric);

            ConfigIO.Restore(attributes, MinimalBlockLengthLabel, ref MinimalBlockLength);
            ConfigIO.Restore(attributes, MaximalBlockLengthLabel, ref MaximalBlockLength);

            ConfigIO.RestoreEnum(attributes, LeafObjectsLabel, ref nodeLayout.leafKind);
            ConfigIO.RestoreEnum(attributes, InnerNodeObjectsLabel, ref nodeLayout.innerKind);

            ConfigIO.RestoreEnum(attributes, NodeLayoutLabel, ref nodeLayout.kind);
            ConfigIO.RestoreEnum(attributes, EdgeLayoutLabel, ref edgeLayout.kind);

            ConfigIO.Restore(attributes, ZScoreScaleLabel, ref nodeLayout.zScoreScale);
            ConfigIO.Restore(attributes, EdgeWidthLabel, ref edgeLayout.edgeWidth);
            ConfigIO.Restore(attributes, ShowErosionsLabel, ref nodeLayout.showErosions);
            ConfigIO.Restore(attributes, MaxErosionWidthLabel, ref nodeLayout.maxErosionWidth);
            ConfigIO.Restore(attributes, EdgesAboveBlocksLabel, ref edgeLayout.edgesAboveBlocks);
            ConfigIO.Restore(attributes, TensionLabel, ref edgeLayout.tension);
            ConfigIO.Restore(attributes, RDPLabel, ref edgeLayout.rdp);

            coseGraphSettings.Restore(attributes, CoseGraphSettingsLabel);
        }
    }
}