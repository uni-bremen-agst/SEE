using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Utils;
using UnityEngine;

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
        private const string LeafNodeAttributesLabel = "LeafNodes";
        private const string InnerNodeAttributesLabel = "InnerNodes";
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
            SaveNodeTypes(writer, NodeTypes, NodeTypesLabel);
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
        /// Saves all <paramref name="nodeTypes"/> with label <paramref name="nodeTypesLabel"/>
        /// using <paramref name="writer"/> as a list.
        /// </summary>
        /// <param name="writer">writer for the configuration file</param>
        /// <param name="nodeTypes">node types to be saved</param>
        /// <param name="nodeTypesLabel">label for <paramref name="nodeTypes"/> in the configuration file</param>
        private static void SaveNodeTypes(ConfigWriter writer,
                                                  IDictionary<string, VisualNodeAttributes> nodeTypes,
                                                  string nodeTypesLabel)
        {
            writer.BeginList(nodeTypesLabel);
            try
            {
                if (nodeTypes != null)
                {
                    foreach (var entry in nodeTypes)
                    {
                        entry.Value.Save(writer, "");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            writer.EndList();
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
            RestoredNodeTypes(attributes, NodeTypesLabel, ref NodeTypes);
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

        /// <summary>
        /// Restores <paramref name="nodeTypes"/> from <paramref name="attributes"/> under
        /// the label <paramref name="nodeTypesLabel"/>.
        /// </summary>
        /// <param name="attributes">dictionary containing the attributes (key = attribute label, value = attribute value)</param>
        /// <param name="nodeTypesLabel">label for <paramref name="nodeTypes"/> in <paramref name="attributes"/></param>
        /// <param name="nodeTypes">dictionary in which to restore the node types (key is the node type name, value
        /// the restored <see cref="VisualNodeAttributes"/></param>
        private void RestoredNodeTypes(Dictionary<string, object> attributes,
                                              string nodeTypesLabel,
                                              ref Dictionary<string, VisualNodeAttributes> nodeTypes)
        {
            if (attributes.TryGetValue(nodeTypesLabel, out object aList))
            {
                /// The <see cref="VisualNodeAttributes"/> are stored as a list; <see cref="SaveNodeTypes"/>
                List<object> list = aList as List<object>;
                if (list == null)
                {
                    throw new Exception($"Attribute {nodeTypesLabel} is not a list.");
                }
                /// Each element in that list is a dictionary having the attributes of <see cref="VisualNodeAttributes"/>
                /// as a key-value pair.
                foreach (object entry in list)
                {
                    // The dictionary holding the attributes of LeafNodeAttributes in its key-value pairs.
                    Dictionary<string, object> value = entry as Dictionary<string, object>;
                    if (value == null)
                    {
                        throw new Exception($"Entry in attribute {nodeTypesLabel} is not a dictionary.");
                    }
                    VisualNodeAttributes nodeAttributes = new VisualNodeAttributes("");
                    nodeAttributes.Restore(value);

                    nodeTypes[nodeAttributes.NodeType] = nodeAttributes;
                }
            }
        }
    }
}