using System.Collections.Generic;
using System.Linq;
using SEE.Utils;
using SEE.Layout.NodeLayouts;
using SEE.Layout.EdgeLayouts;

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
            writer.Save(LODCulling, LODCullingLabel);
            LayoutPath.Save(writer, LayoutPathLabel);
            writer.Save(HierarchicalEdges.ToList<string>(), HierarchicalEdgesLabel);
            writer.Save(SelectedNodeTypes, NodeTypesLabel);
            CityPath.Save(writer, CityPathLabel);
            LeafNodeColorRange.Save(writer, LeafNodeColorRangeLabel);
            InnerNodeColorRange.Save(writer, InnerNodeColorRangeLabel);
            writer.Save(WidthMetric, WidthMetricLabel);
            writer.Save(HeightMetric, HeightMetricLabel);
            writer.Save(DepthMetric, DepthMetricLabel);
            writer.Save(LeafStyleMetric, LeafStyleMetricLabel);
            LeafLabelSettings.Save(writer, LeafLabelSettingsLabel);
            InnerNodeLabelSettings.Save(writer, InnerNodeLabelSettingsLabel);

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
            writer.Save(InnerNodeHeightMetric, InnerNodeHeightMetricLabel);
            writer.Save(InnerNodeStyleMetric, InnerNodeStyleMetricLabel);

            writer.Save(MinimalBlockLength, MinimalBlockLengthLabel);
            writer.Save(MaximalBlockLength, MaximalBlockLengthLabel);

            writer.Save(LeafObjects.ToString(), LeafObjectsLabel);
            writer.Save(InnerNodeObjects.ToString(), InnerNodeObjectsLabel);

            writer.Save(NodeLayout.ToString(), NodeLayoutLabel);
            writer.Save(EdgeLayout.ToString(), EdgeLayoutLabel);

            writer.Save(ZScoreScale, ZScoreScaleLabel);
            writer.Save(EdgeWidth, EdgeWidthLabel);
            writer.Save(ShowErosions, ShowErosionsLabel);
            writer.Save(MaxErosionWidth, MaxErosionWidthLabel);
            writer.Save(EdgesAboveBlocks, EdgesAboveBlocksLabel);
            writer.Save(Tension, TensionLabel);
            writer.Save(RDP, RDPLabel);

            CoseGraphSettings.Save(writer, CoseGraphSettingsLabel);
        }

        /// <summary>
        /// Restores all attributes of this AbstractSEECity instance from the given <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">dictionary containing the attributes (key = attribute label, value = attribute value)</param>
        protected virtual void Restore(Dictionary<string, object> attributes)
        {
            ConfigIO.Restore<float>(attributes, LODCullingLabel, ref LODCulling);
            LayoutPath.Restore(attributes, LayoutPathLabel);
            ConfigIO.Restore(attributes, HierarchicalEdgesLabel, ref HierarchicalEdges);
            ConfigIO.Restore(attributes, NodeTypesLabel, ref SelectedNodeTypes);
            CityPath.Restore(attributes, CityPathLabel);
            LeafNodeColorRange.Restore(attributes, LeafNodeColorRangeLabel);
            InnerNodeColorRange.Restore(attributes, InnerNodeColorRangeLabel);
            ConfigIO.Restore(attributes, WidthMetricLabel, ref WidthMetric);
            ConfigIO.Restore(attributes, HeightMetricLabel, ref HeightMetric);
            ConfigIO.Restore(attributes, DepthMetricLabel, ref DepthMetric);
            ConfigIO.Restore(attributes, LeafStyleMetricLabel, ref LeafStyleMetric);
            LeafLabelSettings.Restore(attributes, LeafLabelSettingsLabel);
            InnerNodeLabelSettings.Restore(attributes, InnerNodeLabelSettingsLabel);

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
            ConfigIO.Restore(attributes, InnerNodeHeightMetricLabel, ref InnerNodeHeightMetric);
            ConfigIO.Restore(attributes, InnerNodeStyleMetricLabel, ref InnerNodeStyleMetric);

            ConfigIO.Restore(attributes, MinimalBlockLengthLabel, ref MinimalBlockLength);
            ConfigIO.Restore(attributes, MaximalBlockLengthLabel, ref MaximalBlockLength);

            ConfigIO.RestoreEnum<LeafNodeKinds>(attributes, LeafObjectsLabel, ref LeafObjects);
            ConfigIO.RestoreEnum<InnerNodeKinds>(attributes, InnerNodeObjectsLabel, ref InnerNodeObjects);

            ConfigIO.RestoreEnum<NodeLayoutKind>(attributes, NodeLayoutLabel, ref NodeLayout);
            ConfigIO.RestoreEnum<EdgeLayoutKind>(attributes, EdgeLayoutLabel, ref EdgeLayout);

            ConfigIO.Restore(attributes, ZScoreScaleLabel, ref ZScoreScale);
            ConfigIO.Restore(attributes, EdgeWidthLabel, ref EdgeWidth);
            ConfigIO.Restore(attributes, ShowErosionsLabel, ref ShowErosions);
            ConfigIO.Restore(attributes, MaxErosionWidthLabel, ref MaxErosionWidth);
            ConfigIO.Restore(attributes, EdgesAboveBlocksLabel, ref EdgesAboveBlocks);
            ConfigIO.Restore(attributes, TensionLabel, ref Tension);
            ConfigIO.Restore(attributes, RDPLabel, ref RDP);

            CoseGraphSettings.Restore(attributes, CoseGraphSettingsLabel);
        }
    }
}