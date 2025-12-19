using SEE.Utils.Config;
using System.Collections.Generic;
using System.Linq;

namespace SEE.Game.City
{
    /// <summary>
    /// Configuration attribute input/output for AbstractSEECity.
    /// </summary>
    public partial class AbstractSEECity
    {
        #region Labels
        /// <summary>
        /// Label in the configuration file for <see cref="HierarchicalEdges"/>.
        /// </summary>
        private const string hierarchicalEdgesLabel = "HierarchicalEdges";
        /// <summary>
        /// Label in the configuration file for <see cref="HiddenEdges"/>.
        /// </summary>
        private const string hiddenEdgesLabel = "HiddenEdges";
        /// <summary>
        /// Label in the configuration file for <see cref="NodeTypes"/>.
        /// </summary>
        private const string nodeTypesLabel = "NodeTypes";
        /// <summary>
        /// Label in the configuration file for <see cref="ErosionSettings"/>.
        /// </summary>
        private const string erosionSettingsLabel = "ErosionIssues";
        /// <summary>
        /// Label in the configuration file for <see cref="BoardSettings"/>.
        /// </summary>
        private const string boardSettingsLabel = "BoardSettings";
        /// <summary>
        /// Label in the configuration file for <see cref="NodeLayoutSettings"/>.
        /// </summary>
        private const string nodeLayoutSettingsLabel = "NodeLayout";
        /// <summary>
        /// Label in the configuration file for <see cref="EdgeLayoutSettings"/>.
        /// </summary>
        private const string edgeLayoutSettingsLabel = "EdgeLayout";
        /// <summary>
        /// Label in the configuration file for <see cref="ConfigurationPath"/>.
        /// </summary>
        private const string configurationPathLabel= "ConfigPath";
        /// <summary>
        /// Label in the configuration file for <see cref="SourceCodeDirectory"/>.
        /// </summary>
        private const string sourceCodeDirectoryLabel = "ProjectPath";
        /// <summary>
        /// Label in the configuration file for <see cref="SolutionPath"/>.
        /// </summary>
        private const string solutionPathLabel = "SolutionPath";
        /// <summary>
        /// Label in the configuration file for <see cref="LODCulling"/>.
        /// </summary>
        private const string lodCullingLabel = "LODCulling";
        /// <summary>
        /// Label in the configuration file for <see cref="ZScoreScale"/>.
        /// </summary>
        private const string zScoreScaleLabel = "ZScoreScale";
        /// <summary>
        /// Label in the configuration file for <see cref="ScaleOnlyLeafMetrics"/>.
        /// </summary>
        private const string scaleOnlyLeafMetricsLabel = "ScaleOnlyLeafMetrics";
        /// <summary>
        /// Label in the configuration file for <see cref="EdgeSelectionSettings"/>.
        /// </summary>
        private const string edgeSelectionSettingsLabel = "EdgeSelection";
        /// <summary>
        /// Label in the configuration file for <see cref="MetricToColor"/>.
        /// </summary>
        private const string metricToColorLabel = "MetricToColor";
        /// <summary>
        /// Label in the configuration file for <see cref="IgnoreSelfLoopsInLifting"/>.
        /// </summary>
        private const string ignoreSelfLoopsInLiftingLabel = "IgnoreSelfLoopsInLifting";
        /// <summary>
        /// Label in the configuration file for <see cref="MaximalAntennaSegmentHeight"/>.
        /// </summary>
        private const string maximalAntennaSegmentHeightLabel = "MaximalAntennaSegmentHeight";
        /// <summary>
        /// Label in the configuration file for <see cref="AntennaWidth"/>.
        /// </summary>
        private const string antennaWidthLabel = "AntennaWidth";
        /// <summary>
        /// Label in the configuration file for <see cref="BaseAnimationDuration"/>.
        /// </summary>
        private const string baseAnimationDurationLabel = "BaseAnimationDuration";
        /// <summary>
        /// Label in the configuration file for <see cref="MarkerAttributes"/>.
        /// </summary>
        private const string markerAttributesLabel = "Markers";
        /// <summary>
        /// Label in the configuration file for <see cref="TableWorldScale"/>.
        /// </summary>
        private const string tableWorldScaleLabel = "TableWorldScale";
        #endregion

        /// <summary>
        /// Saves all attributes of this AbstractSEECity instance in the configuration file
        /// using the given <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">writer for the configuration file</param>
        protected virtual void Save(ConfigWriter writer)
        {
            ConfigurationPath.Save(writer, configurationPathLabel);
            SourceCodeDirectory.Save(writer, sourceCodeDirectoryLabel);
            SolutionPath.Save(writer, solutionPathLabel);
            writer.Save(TableWorldScale, tableWorldScaleLabel);
            writer.Save(LODCulling, lodCullingLabel);
            writer.Save(HierarchicalEdges.ToList(), hierarchicalEdgesLabel);
            writer.Save(HiddenEdges.ToList(), hiddenEdgesLabel);
            NodeTypes.Save(writer, nodeTypesLabel);
            writer.Save(IgnoreSelfLoopsInLifting, ignoreSelfLoopsInLiftingLabel);
            writer.Save(MaximalAntennaSegmentHeight, maximalAntennaSegmentHeightLabel);
            writer.Save(AntennaWidth, antennaWidthLabel);
            writer.Save(BaseAnimationDuration, baseAnimationDurationLabel);
            MetricToColor.Save(writer, metricToColorLabel);
            writer.Save(ZScoreScale, zScoreScaleLabel);
            writer.Save(ScaleOnlyLeafMetrics, scaleOnlyLeafMetricsLabel);
            ErosionSettings.Save(writer, erosionSettingsLabel);
            BoardSettings.Save(writer, boardSettingsLabel);
            NodeLayoutSettings.Save(writer, nodeLayoutSettingsLabel);
            EdgeLayoutSettings.Save(writer, edgeLayoutSettingsLabel);
            EdgeSelectionSettings.Save(writer, edgeSelectionSettingsLabel);
            MarkerAttributes.Save(writer, markerAttributesLabel);
        }

        /// <summary>
        /// Restores all attributes of this AbstractSEECity instance from the given <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">dictionary containing the attributes (key = attribute label, value = attribute value)</param>
        protected virtual void Restore(Dictionary<string, object> attributes)
        {
            ConfigurationPath.Restore(attributes, configurationPathLabel);
            SourceCodeDirectory.Restore(attributes, sourceCodeDirectoryLabel);
            SolutionPath.Restore(attributes, solutionPathLabel);
            ConfigIO.Restore(attributes, tableWorldScaleLabel, value => TableWorldScale = value);
            ConfigIO.Restore(attributes, lodCullingLabel, ref LODCulling);
            ConfigIO.Restore(attributes, hierarchicalEdgesLabel, ref HierarchicalEdges);
            ConfigIO.Restore(attributes, hiddenEdgesLabel, ref HiddenEdges);
            NodeTypes.Restore(attributes, nodeTypesLabel);
            ConfigIO.Restore(attributes, ignoreSelfLoopsInLiftingLabel, ref IgnoreSelfLoopsInLifting);
            ConfigIO.Restore(attributes, maximalAntennaSegmentHeightLabel, ref MaximalAntennaSegmentHeight);
            ConfigIO.Restore(attributes, antennaWidthLabel, ref AntennaWidth);
            ConfigIO.Restore(attributes, baseAnimationDurationLabel, ref BaseAnimationDuration);
            MetricToColor.Restore(attributes, metricToColorLabel);
            ConfigIO.Restore(attributes, zScoreScaleLabel, ref ZScoreScale);
            ConfigIO.Restore(attributes, scaleOnlyLeafMetricsLabel, ref ScaleOnlyLeafMetrics);
            ErosionSettings.Restore(attributes, erosionSettingsLabel);
            BoardSettings.Restore(attributes, boardSettingsLabel);
            NodeLayoutSettings.Restore(attributes, nodeLayoutSettingsLabel);
            EdgeLayoutSettings.Restore(attributes, edgeLayoutSettingsLabel);
            EdgeSelectionSettings.Restore(attributes, edgeSelectionSettingsLabel);
            MarkerAttributes.Restore(attributes, markerAttributesLabel);
        }
    }
}
