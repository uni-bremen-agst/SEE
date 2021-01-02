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

using SEE.DataModel.DG;
using System.Collections.Generic;
using static SEE.Game.AbstractSEECity;

namespace SEE.Layout.NodeLayouts.Cose
{
    /// <summary>
    /// This class holds all settings for the cose layout
    /// </summary>
    public class CoseGraphSettings
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
        /// if true the feature "multilevel scaling" is used
        /// </summary>
        public bool multiLevelScaling = CoseLayoutSettings.Multilevel_Scaling;

        /// <summary>
        /// key: dir ids, value: bool, if true the dir is layouted by a sublayout
        /// </summary>
        public Dictionary<string, bool> ListDirToggle = new Dictionary<string, bool>();

        /// <summary>
        ///  key: dir ids, value: the nodelayout
        /// </summary>
        public Dictionary<string, NodeLayoutKind> DirNodeLayout = new Dictionary<string, NodeLayoutKind>();

        /// <summary>
        /// key: dir ids, value: the inner node kind
        /// </summary>
        public Dictionary<string, InnerNodeKinds> DirShape = new Dictionary<string, InnerNodeKinds>();

        /// <summary>
        /// a list of root dirs from the current graph
        /// </summary>
        public List<Node> rootDirs = new List<Node>();

        /// <summary>
        /// key: dir ids, value: bool, if true the dir is shown in the foldout, if false the section foldout is collapsed 
        /// </summary>
        public Dictionary<string, bool> show = new Dictionary<string, bool>();

        /// <summary>
        /// if true, listing of dirs with posiible nodelayouts and inner node kinds is shown
        /// </summary>
        public bool showGraphListing = true;

        /// <summary>
        /// the nodetypes
        /// </summary>
        public Dictionary<string, bool> loadedForNodeTypes = new Dictionary<string, bool>();

        /// <summary>
        /// if true, the parameter edgeLength and repulsion strength are calculated automatically
        /// </summary>
        public bool useCalculationParameter = true;

        /// <summary>
        /// if true, the parameter edgeLength and repulsion strength are calculated automatically and are iteratively changed until a good layout is found
        /// </summary>
        public bool useIterativeCalculation = false;
    }
}

