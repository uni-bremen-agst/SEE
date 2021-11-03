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
using SEE.Game;
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
        public int EdgeLength = CoseLayoutSettings.Edge_Length;

        /// <summary>
        /// If true the edge length is calculated with the feature "use smart ideal edge calculation"
        /// </summary>
        public bool UseSmartIdealEdgeCalculation = CoseLayoutSettings.Use_Smart_Ideal_Edge_Calculation;

        /// <summary>
        /// If true the feature "use smart multilevel calculation" is used, the edge length adjusts for each level
        /// </summary>
        public bool UseSmartMultilevelScaling = CoseLayoutSettings.Use_Smart_Multilevel_Calculation;

        /// <summary>
        /// the factor by which the edge length of intergraph edges is enlarged
        /// </summary>
        public float PerLevelIdealEdgeLengthFactor = CoseLayoutSettings.Per_Level_Ideal_Edge_Length_Factor;

        /// <summary>
        /// if true the feature "smart repulsion range calculation" is used (Grid variant)
        /// </summary>
        public bool UseSmartRepulsionRangeCalculation = CoseLayoutSettings.Use_Smart_Repulsion_Range_Calculation;

        /// <summary>
        /// the strength of the gravity (root graph)
        /// </summary>
        public float GravityStrength = CoseLayoutSettings.Gravity_Strength;

        /// <summary>
        /// strength of the gravity in compound nodes (not root graph)
        /// </summary>
        public float CompoundGravityStrength = CoseLayoutSettings.Compound_Gravity_Strength;

        /// <summary>
        /// the repulsion strength
        /// </summary>
        public float RepulsionStrength = CoseLayoutSettings.Repulsion_Strength;

        /// <summary>
        /// if true the feature: multilevel scaling is used
        /// </summary>
        public bool MultiLevelScaling = CoseLayoutSettings.Multilevel_Scaling;

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
        public Dictionary<string, InnerNodeKinds> InnerNodeShape = new Dictionary<string, InnerNodeKinds>();

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

        private const string EdgeLengthLabel = "EdgeLength";
        private const string UseSmartIdealEdgeCalculationLabel = "UseSmartIdealEdgeCalculation";
        private const string UseSmartMultilevelScalingLabel = "UseSmartMultilevelScaling";
        private const string PerLevelIdealEdgeLengthFactorLabel = "PerLevelIdealEdgeLengthFactor";
        private const string UseSmartRepulsionRangeCalculationLabel = "UseSmartRepulsionRangeCalculation";
        private const string GravityStrengthLabel = "GravityStrength";
        private const string CompoundGravityStrengthLabel = "CompoundGravityStrength";
        private const string RepulsionStrengthLabel = "RepulsionStrength";
        private const string MultiLevelScalingLabel = "MultiLevelScaling";
        private const string ListInnerNodeToggleLabel = "ListInnerNodeToggle";
        private const string InnerNodeLayoutLabel = "InnerNodeLayout";
        private const string InnerNodeShapeLabel = "InnerNodeShape";
        private const string LoadedForNodeTypesLabel = "LoadedForNodeTypes";
        private const string UseCalculationParameterLabel = "UseCalculationParameter";
        private const string UseIterativeCalculationLabel = "UseIterativeCalculation";

        internal void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(EdgeLength, EdgeLengthLabel);
            writer.Save(UseSmartIdealEdgeCalculation, UseSmartIdealEdgeCalculationLabel);
            writer.Save(UseSmartMultilevelScaling, UseSmartMultilevelScalingLabel);
            writer.Save(PerLevelIdealEdgeLengthFactor, PerLevelIdealEdgeLengthFactorLabel);
            writer.Save(UseSmartRepulsionRangeCalculation, UseSmartRepulsionRangeCalculationLabel);
            writer.Save(GravityStrength, GravityStrengthLabel);
            writer.Save(CompoundGravityStrength, CompoundGravityStrengthLabel);
            writer.Save(RepulsionStrength, RepulsionStrengthLabel);
            writer.Save(MultiLevelScaling, MultiLevelScalingLabel);
            writer.Save(ListInnerNodeToggle, ListInnerNodeToggleLabel);
            writer.Save(InnerNodeLayout, InnerNodeLayoutLabel); // saves enums as strings
            writer.Save(InnerNodeShape, InnerNodeShapeLabel);   // saves enums as strings
            writer.Save(LoadedForNodeTypes, LoadedForNodeTypesLabel);
            writer.Save(UseCalculationParameter, UseCalculationParameterLabel);
            writer.Save(UseIterativeCalculation, UseIterativeCalculationLabel);
            writer.EndGroup();
        }

        internal void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                {
                    ConfigIO.Restore(values, EdgeLengthLabel, ref EdgeLength);
                    ConfigIO.Restore(values, UseSmartMultilevelScalingLabel, ref UseSmartMultilevelScaling);
                    ConfigIO.Restore(values, UseSmartIdealEdgeCalculationLabel, ref UseSmartIdealEdgeCalculation);
                    ConfigIO.Restore(values, PerLevelIdealEdgeLengthFactorLabel, ref PerLevelIdealEdgeLengthFactor);
                    ConfigIO.Restore(values, UseSmartRepulsionRangeCalculationLabel, ref UseSmartRepulsionRangeCalculation);
                    ConfigIO.Restore(values, GravityStrengthLabel, ref GravityStrength);
                    ConfigIO.Restore(values, CompoundGravityStrengthLabel, ref CompoundGravityStrength);
                    ConfigIO.Restore(values, RepulsionStrengthLabel, ref RepulsionStrength);
                    ConfigIO.Restore(values, MultiLevelScalingLabel, ref MultiLevelScaling);
                    ConfigIO.Restore(values, ListInnerNodeToggleLabel, ref ListInnerNodeToggle);
                    ConfigIO.RestoreEnumDict(values, InnerNodeLayoutLabel, ref InnerNodeLayout);
                    ConfigIO.RestoreEnumDict(values, InnerNodeShapeLabel, ref InnerNodeShape);
                    ConfigIO.Restore(values, LoadedForNodeTypesLabel, ref LoadedForNodeTypes);
                    ConfigIO.Restore(values, UseCalculationParameterLabel, ref UseCalculationParameter);
                    ConfigIO.Restore(values, UseIterativeCalculationLabel, ref UseIterativeCalculation);
                }
            }
        }
    }
}

