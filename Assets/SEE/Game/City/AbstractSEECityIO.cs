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
        /// Label in the configuration file for <see cref="HierarchicalEdges"/>.
        /// </summary>
        private const string HierarchicalEdgesLabel = "HierarchicalEdges";
        /// <summary>
        /// Label in the configuration file for <see cref="NodeTypes"/>.
        /// </summary>
        private const string NodeTypesLabel = "NodeTypes";
        /// <summary>
        /// Label in the configuration file for <see cref="CoseGraphSettings"/>.
        /// </summary>
        private const string CoseGraphSettingsLabel = "CoseGraph";
        /// <summary>
        /// Label in the configuration file for <see cref="ErosionSettings"/>.
        /// </summary>
        private const string ErosionSettingsLabel = "ErosionIssues";
        /// <summary>
        /// Label in the configuration file for <see cref="BoardSettings"/>.
        /// </summary>
        private const string BoardSettingsLabel = "BoardSettings";
        /// <summary>
        /// Label in the configuration file for <see cref="NodeLayoutSettings"/>.
        /// </summary>
        private const string NodeLayoutSettingsLabel = "NodeLayout";
        /// <summary>
        /// Label in the configuration file for <see cref="EdgeLayoutSettings"/>.
        /// </summary>
        private const string EdgeLayoutSettingsLabel = "EdgeLayout";
        /// <summary>
        /// Label in the configuration file for <see cref="ConfigurationPath"/>.
        /// </summary>
        private const string ConfigurationPathLabel= "ConfigPath";
        /// <summary>
        /// Label in the configuration file for <see cref="SourceCodeDirectory"/>.
        /// </summary>
        private const string SourceCodeDirectoryLabel = "ProjectPath";
        /// <summary>
        /// Label in the configuration file for <see cref="SolutionPath"/>.
        /// </summary>
        private const string SolutionPathLabel = "SolutionPath";
        /// <summary>
        /// Label in the configuration file for <see cref="LODCulling"/>.
        /// </summary>
        private const string LODCullingLabel = "LODCulling";
        /// <summary>
        /// Label in the configuration file for <see cref="ZScoreScale"/>.
        /// </summary>
        private const string ZScoreScaleLabel = "ZScoreScale";
        /// <summary>
        /// Label in the configuration file for <see cref="ScaleOnlyLeafMetrics"/>.
        /// </summary>
        private const string ScaleOnlyLeafMetricsLabel = "ScaleOnlyLeafMetrics";
        /// <summary>
        /// Label in the configuration file for <see cref="EdgeSelectionSettings"/>.
        /// </summary>
        private const string EdgeSelectionSettingsLabel = "EdgeSelection";
        /// <summary>
        /// Label in the configuration file for <see cref="MetricToColor"/>.
        /// </summary>
        private const string MetricToColorLabel = "MetricToColor";
        /// <summary>
        /// Label in the configuration file for <see cref="IgnoreSelfLoopsInLifting"/>.
        /// </summary>
        private const string IgnoreSelfLoopsInLiftingLabel = "IgnoreSelfLoopsInLifting";
        /// <summary>
        /// Label in the configuration file for <see cref="MaximalAntennaSegmentHeight"/>.
        /// </summary>
        private const string MaximalAntennaSegmentHeightLabel = "MaximalAntennaSegmentHeight";
        /// <summary>
        /// Label in the configuration file for <see cref="AntennaWidth"/>.
        /// </summary>
        private const string AntennaWidthLabel = "AntennaWidth";

        /// <summary>
        /// Saves all attributes of this AbstractSEECity instance in the configuration file
        /// using the given <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">writer for the configuration file</param>
        protected virtual void Save(ConfigWriter writer)
        {
            ConfigurationPath.Save(writer, ConfigurationPathLabel);
            SourceCodeDirectory.Save(writer, SourceCodeDirectoryLabel);
            SolutionPath.Save(writer, SolutionPathLabel);
            writer.Save(LODCulling, LODCullingLabel);
            writer.Save(HierarchicalEdges.ToList(), HierarchicalEdgesLabel);
            NodeTypes.Save(writer, NodeTypesLabel);
            writer.Save(IgnoreSelfLoopsInLifting, IgnoreSelfLoopsInLiftingLabel);
            writer.Save(MaximalAntennaSegmentHeight, MaximalAntennaSegmentHeightLabel);
            writer.Save(AntennaWidth, AntennaWidthLabel);
            MetricToColor.Save(writer, MetricToColorLabel);
            writer.Save(ZScoreScale, ZScoreScaleLabel);
            writer.Save(ScaleOnlyLeafMetrics, ScaleOnlyLeafMetricsLabel);
            ErosionSettings.Save(writer, ErosionSettingsLabel);
            BoardSettings.Save(writer, BoardSettingsLabel);
            NodeLayoutSettings.Save(writer, NodeLayoutSettingsLabel);
            EdgeLayoutSettings.Save(writer, EdgeLayoutSettingsLabel);
            EdgeSelectionSettings.Save(writer, EdgeSelectionSettingsLabel);
            CoseGraphSettings.Save(writer, CoseGraphSettingsLabel);
        }

        /// <summary>
        /// Restores all attributes of this AbstractSEECity instance from the given <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">dictionary containing the attributes (key = attribute label, value = attribute value)</param>
        protected virtual void Restore(Dictionary<string, object> attributes)
        {
            ConfigurationPath.Restore(attributes, ConfigurationPathLabel);
            SourceCodeDirectory.Restore(attributes, SourceCodeDirectoryLabel);
            SolutionPath.Restore(attributes, SolutionPathLabel);
            ConfigIO.Restore(attributes, LODCullingLabel, ref LODCulling);
            ConfigIO.Restore(attributes, HierarchicalEdgesLabel, ref HierarchicalEdges);
            NodeTypes.Restore(attributes, NodeTypesLabel);
            ConfigIO.Restore(attributes, IgnoreSelfLoopsInLiftingLabel, ref IgnoreSelfLoopsInLifting);
            ConfigIO.Restore(attributes, MaximalAntennaSegmentHeightLabel, ref MaximalAntennaSegmentHeight);
            ConfigIO.Restore(attributes, AntennaWidthLabel, ref AntennaWidth);
            MetricToColor.Restore(attributes, MetricToColorLabel);
            ConfigIO.Restore(attributes, ZScoreScaleLabel, ref ZScoreScale);
            ConfigIO.Restore(attributes, ScaleOnlyLeafMetricsLabel, ref ScaleOnlyLeafMetrics);
            ErosionSettings.Restore(attributes, ErosionSettingsLabel);
            BoardSettings.Restore(attributes, BoardSettingsLabel);
            NodeLayoutSettings.Restore(attributes, NodeLayoutSettingsLabel);
            EdgeLayoutSettings.Restore(attributes, EdgeLayoutSettingsLabel);
            EdgeSelectionSettings.Restore(attributes, EdgeSelectionSettingsLabel);
            CoseGraphSettings.Restore(attributes, CoseGraphSettingsLabel);
        }
    }
}