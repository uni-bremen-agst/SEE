// Copyright 2020 Nina Unterberg
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO
// EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR
// THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using SEE.Game.City;
using SEE.Utils;

namespace SEE.Layout.NodeLayouts.Cose
{
    /// <summary>
    /// This class holds all attributes for the COSE layout
    /// </summary>
    public class CoseGraphAttributes
    {
        /// <summary>
        /// The ideal length of the edge
        /// </summary>
        public int EdgeLength = CoseLayoutSettings.EdgeLength;

        /// <summary>
        /// If true the edge length is calculated with the feature "use smart ideal edge calculation"
        /// </summary>
        public bool UseSmartIdealEdgeCalculation = CoseLayoutSettings.UseSmartIdealEdgeCalculation;

        /// <summary>
        /// If true the feature "use smart multilevel calculation" is used, the edge length adjusts for each level
        /// </summary>
        public bool UseSmartMultilevelScaling = CoseLayoutSettings.UseSmartMultilevelCalculation;

        /// <summary>
        /// the factor by which the edge length of intergraph edges is enlarged
        /// </summary>
        public float PerLevelIdealEdgeLengthFactor = CoseLayoutSettings.PerLevelIdealEdgeLengthFactor;

        /// <summary>
        /// if true the feature "smart repulsion range calculation" is used (Grid variant)
        /// </summary>
        public bool UseSmartRepulsionRangeCalculation = CoseLayoutSettings.UseSmartRepulsionRangeCalculation;

        /// <summary>
        /// the strength of the gravity (root graph)
        /// </summary>
        public float GravityStrength = CoseLayoutSettings.GravityStrength;

        /// <summary>
        /// strength of the gravity in compound nodes (not root graph)
        /// </summary>
        public float CompoundGravityStrength = CoseLayoutSettings.CompoundGravityStrength;

        /// <summary>
        /// the repulsion strength
        /// </summary>
        public float RepulsionStrength = CoseLayoutSettings.RepulsionStrength;

        /// <summary>
        /// if true the feature: multilevel scaling is used
        /// </summary>
        public bool MultiLevelScaling = CoseLayoutSettings.MultilevelScaling;

        /// <summary>
        /// key: inner-node ids, value: bool, if true the inner node is layouted by a sublayout
        /// </summary>
        public Dictionary<string, bool> ListInnerNodeToggle = new Dictionary<string, bool>();

        /// <summary>
        ///  key: inner-node ids, value: the nodelayout
        /// </summary>
        public Dictionary<string, NodeLayoutKind> InnerNodeLayout = new Dictionary<string, NodeLayoutKind>();

        /// <summary>
        /// key: inner-node ids, value: the inner node kind
        /// </summary>
        public Dictionary<string, NodeShapes> InnerNodeShape = new Dictionary<string, NodeShapes>();

        /// <summary>
        /// the nodetypes
        /// </summary>
        public Dictionary<string, bool> LoadedForNodeTypes = new Dictionary<string, bool>();

        /// <summary>
        /// is true the parameter edgeLength and repulsion strength are calculated automatically
        /// </summary>
        public bool UseCalculationParameter = true;

        /// <summary>
        /// is true the parameter edgeLength and repulsion strength are calculated automatically and are iteratily changed till a goog layout is found
        /// </summary>
        public bool UseIterativeCalculation = false;

        private const string edgeLengthLabel = "EdgeLength";
        private const string useSmartIdealEdgeCalculationLabel = "UseSmartIdealEdgeCalculation";
        private const string useSmartMultilevelScalingLabel = "UseSmartMultilevelScaling";
        private const string perLevelIdealEdgeLengthFactorLabel = "PerLevelIdealEdgeLengthFactor";
        private const string useSmartRepulsionRangeCalculationLabel = "UseSmartRepulsionRangeCalculation";
        private const string gravityStrengthLabel = "GravityStrength";
        private const string compoundGravityStrengthLabel = "CompoundGravityStrength";
        private const string repulsionStrengthLabel = "RepulsionStrength";
        private const string multiLevelScalingLabel = "MultiLevelScaling";
        private const string listInnerNodeToggleLabel = "ListInnerNodeToggle";
        private const string innerNodeLayoutLabel = "InnerNodeLayout";
        private const string innerNodeShapeLabel = "InnerNodeShape";
        private const string loadedForNodeTypesLabel = "LoadedForNodeTypes";
        private const string useCalculationParameterLabel = "UseCalculationParameter";
        private const string useIterativeCalculationLabel = "UseIterativeCalculation";

        internal void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(EdgeLength, edgeLengthLabel);
            writer.Save(UseSmartIdealEdgeCalculation, useSmartIdealEdgeCalculationLabel);
            writer.Save(UseSmartMultilevelScaling, useSmartMultilevelScalingLabel);
            writer.Save(PerLevelIdealEdgeLengthFactor, perLevelIdealEdgeLengthFactorLabel);
            writer.Save(UseSmartRepulsionRangeCalculation, useSmartRepulsionRangeCalculationLabel);
            writer.Save(GravityStrength, gravityStrengthLabel);
            writer.Save(CompoundGravityStrength, compoundGravityStrengthLabel);
            writer.Save(RepulsionStrength, repulsionStrengthLabel);
            writer.Save(MultiLevelScaling, multiLevelScalingLabel);
            writer.Save(ListInnerNodeToggle, listInnerNodeToggleLabel);
            writer.SaveAsStrings(InnerNodeLayout, innerNodeLayoutLabel); // saves enums as strings
            writer.SaveAsStrings(InnerNodeShape, innerNodeShapeLabel);   // saves enums as strings
            writer.Save(LoadedForNodeTypes, loadedForNodeTypesLabel);
            writer.Save(UseCalculationParameter, useCalculationParameterLabel);
            writer.Save(UseIterativeCalculation, useIterativeCalculationLabel);
            writer.EndGroup();
        }

        internal void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                {
                    ConfigIO.Restore(values, edgeLengthLabel, ref EdgeLength);
                    ConfigIO.Restore(values, useSmartMultilevelScalingLabel, ref UseSmartMultilevelScaling);
                    ConfigIO.Restore(values, useSmartIdealEdgeCalculationLabel, ref UseSmartIdealEdgeCalculation);
                    ConfigIO.Restore(values, perLevelIdealEdgeLengthFactorLabel, ref PerLevelIdealEdgeLengthFactor);
                    ConfigIO.Restore(values, useSmartRepulsionRangeCalculationLabel, ref UseSmartRepulsionRangeCalculation);
                    ConfigIO.Restore(values, gravityStrengthLabel, ref GravityStrength);
                    ConfigIO.Restore(values, compoundGravityStrengthLabel, ref CompoundGravityStrength);
                    ConfigIO.Restore(values, repulsionStrengthLabel, ref RepulsionStrength);
                    ConfigIO.Restore(values, multiLevelScalingLabel, ref MultiLevelScaling);
                    ConfigIO.Restore(values, listInnerNodeToggleLabel, ref ListInnerNodeToggle);
                    ConfigIO.RestoreEnumDict(values, innerNodeLayoutLabel, ref InnerNodeLayout);
                    ConfigIO.RestoreEnumDict(values, innerNodeShapeLabel, ref InnerNodeShape);
                    ConfigIO.Restore(values, loadedForNodeTypesLabel, ref LoadedForNodeTypes);
                    ConfigIO.Restore(values, useCalculationParameterLabel, ref UseCalculationParameter);
                    ConfigIO.Restore(values, useIterativeCalculationLabel, ref UseIterativeCalculation);
                }
            }
        }
    }
}

