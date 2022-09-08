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
        private const string NodeTypesLabel = "NodeTypes";
        private const string CoseGraphSettingsLabel = "CoseGraph";
        private const string ErosionMetricsLabel = "ErosionIssues";
        private const string NodeLayoutSettingsLabel = "NodeLayout";
        private const string EdgeLayoutSettingsLabel = "EdgeLayout";
        private const string CityPathLabel = "ConfigPath";
        private const string ProjectPathLabel = "ProjectPath";
        private const string SolutionPathLabel = "SolutionPath";
        private const string LODCullingLabel = "LODCulling";
        private const string ZScoreScaleLabel = "ZScoreScale";
        private const string ScaleOnlyLeafMetricsLabel = "ScaleOnlyLeafMetrics";
        private const string EdgeSelectionLabel = "EdgeSelection";
        private const string MetricToColorLabel = "MetricToColor";

        /// <summary>
        /// Saves all attributes of this AbstractSEECity instance in the configuration file
        /// using the given <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">writer for the configuration file</param>
        protected virtual void Save(ConfigWriter writer)
        {
            ConfigurationPath.Save(writer, CityPathLabel);
            SourceCodeDirectory.Save(writer, ProjectPathLabel);
            SolutionPath.Save(writer, SolutionPathLabel);
            writer.Save(LODCulling, LODCullingLabel);
            writer.Save(HierarchicalEdges.ToList(), HierarchicalEdgesLabel);
            NodeTypes.Save(writer, NodeTypesLabel);
            MetricToColor.Save(writer, MetricToColorLabel);
            writer.Save(ZScoreScale, ZScoreScaleLabel);
            writer.Save(ScaleOnlyLeafMetrics, ScaleOnlyLeafMetricsLabel);
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
            ConfigurationPath.Restore(attributes, CityPathLabel);
            SourceCodeDirectory.Restore(attributes, ProjectPathLabel);
            SolutionPath.Restore(attributes, SolutionPathLabel);
            ConfigIO.Restore(attributes, LODCullingLabel, ref LODCulling);
            ConfigIO.Restore(attributes, HierarchicalEdgesLabel, ref HierarchicalEdges);
            NodeTypes.Restore(attributes, NodeTypesLabel);
            MetricToColor.Restore(attributes, MetricToColorLabel);
            ConfigIO.Restore(attributes, ZScoreScaleLabel, ref ZScoreScale);
            ConfigIO.Restore(attributes, ScaleOnlyLeafMetricsLabel, ref ScaleOnlyLeafMetrics);
            ErosionSettings.Restore(attributes, ErosionMetricsLabel);
            NodeLayoutSettings.Restore(attributes, NodeLayoutSettingsLabel);
            EdgeLayoutSettings.Restore(attributes, EdgeLayoutSettingsLabel);
            EdgeSelectionSettings.Restore(attributes, EdgeSelectionLabel);
            CoseGraphSettings.Restore(attributes, CoseGraphSettingsLabel);
        }
    }
}