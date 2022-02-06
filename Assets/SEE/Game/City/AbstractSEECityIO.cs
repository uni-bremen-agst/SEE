using System.Collections.Generic;
using System.Linq;
using SEE.Utils;

namespace SEE.Game.City
{
    /// <summary>
    /// Configuration attribute input/output for AbstractSEECity.
    /// </summary>
    public partial class AbstractSEECity
    {
        /// <summary>
        /// The attribute labels for all attributes in the stored configuration file.
        /// </summary>
        private const string HierarchicalEdgesLabel = "HierarchicalEdges";
        private const string SelectedNodeTypesLabel = "NodeTypes";
        private const string CoseGraphSettingsLabel = "CoseGraph";
        private const string LeafNodeAttributesLabel = "LeafNodes";
        private const string InnerNodeAttributesLabel = "InnerNodes";
        private const string ErosionMetricsLabel = "ErosionIssues";
        private const string NodeLayoutSettingsLabel = "NodeLayout";
        private const string EdgeLayoutSettingsLabel = "EdgeLayout";
        private const string CityPathLabel = "ConfigPath";
        private const string LODCullingLabel = "LODCulling";
        private const string ZScoreScaleLabel = "ZScoreScale";
        private const string ScaleOnlyLeafMetricsLabel = "ScaleOnlyLeafMetrics";
        private const string EdgeSelectionLabel = "EdgeSelection";

        /// <summary>
        /// Saves all attributes of this AbstractSEECity instance in the configuration file
        /// using the given <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">writer for the configuration file</param>
        protected virtual void Save(ConfigWriter writer)
        {
            CityPath.Save(writer, CityPathLabel);
            writer.Save(LODCulling, LODCullingLabel);
            writer.Save(HierarchicalEdges.ToList(), HierarchicalEdgesLabel);
            writer.Save(SelectedNodeTypes, SelectedNodeTypesLabel);
            writer.Save(ZScoreScale, ZScoreScaleLabel);
            writer.Save(ScaleOnlyLeafMetrics, ScaleOnlyLeafMetricsLabel);
            LeafNodeSettings.Save(writer, LeafNodeAttributesLabel);
            InnerNodeSettings.Save(writer, InnerNodeAttributesLabel);
            ErosionSettings.Save(writer, ErosionMetricsLabel);
            NodeLayoutSettings.Save(writer, NodeLayoutSettingsLabel);
            EdgeLayoutSettings.Save(writer, EdgeLayoutSettingsLabel);
            EdgeSelectionSettings.Save(writer, EdgeSelectionLabel);
            CoseGraphSettings.Save(writer, CoseGraphSettingsLabel);
        }

        /// <summary>
        /// Restores all attributes of this AbstractSEECity instance from the given <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">dictionary containing the attributes (key = attribute label, value = attribute value)</param>
        protected virtual void Restore(Dictionary<string, object> attributes)
        {
            CityPath.Restore(attributes, CityPathLabel);
            ConfigIO.Restore(attributes, LODCullingLabel, ref LODCulling);
            ConfigIO.Restore(attributes, HierarchicalEdgesLabel, ref HierarchicalEdges);
            ConfigIO.Restore(attributes, SelectedNodeTypesLabel, ref SelectedNodeTypes);
            ConfigIO.Restore(attributes, ZScoreScaleLabel, ref ZScoreScale);
            ConfigIO.Restore(attributes, ScaleOnlyLeafMetricsLabel, ref ScaleOnlyLeafMetrics);
            LeafNodeSettings.Restore(attributes, LeafNodeAttributesLabel);
            InnerNodeSettings.Restore(attributes, InnerNodeAttributesLabel);
            ErosionSettings.Restore(attributes, ErosionMetricsLabel);
            NodeLayoutSettings.Restore(attributes, NodeLayoutSettingsLabel);
            EdgeLayoutSettings.Restore(attributes, EdgeLayoutSettingsLabel);
            EdgeSelectionSettings.Restore(attributes, EdgeSelectionLabel);
            CoseGraphSettings.Restore(attributes, CoseGraphSettingsLabel);
        }
    }
}