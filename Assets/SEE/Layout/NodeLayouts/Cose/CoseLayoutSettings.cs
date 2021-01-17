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

namespace SEE.Layout.NodeLayouts.Cose
{
    public class CoseLayoutSettings
    {
        /// <summary>
        /// the margin of a graph
        /// </summary>
        public static float Graph_Margin = 0.5f;

        /// <summary>
        /// the margin of a compound node
        /// </summary>
        public static int Compound_Node_Margin = 1;

        /// <summary>
        /// the empty compound node size
        /// </summary>
        public static int Empty_Compound_Size = 5;

        /// <summary>
        /// the ideal edge length
        /// </summary>
        public static int Edge_Length = 20;

        /// <summary>
        /// indicates whether to use the smart edge length calculation during the layout process
        /// </summary>
        public static bool Use_Smart_Ideal_Edge_Calculation = false;

        /// <summary>
        /// true if smart multilevel calculation is used 
        /// </summary>
        public static bool Use_Smart_Multilevel_Calculation = false;

        /// <summary>
        /// the simple node size
        /// </summary>
        public static int Simple_Node_Size = 1;

        /// <summary>
        /// the factor by which the edge length increases with every level of the edges source/target node
        /// </summary>
        public static float Per_Level_Ideal_Edge_Length_Factor = 0.1f;

        /// <summary>
        /// Indicates whether the layout process is incrementally calculated
        /// </summary>
        public static bool Incremental = true;

        /// <summary>
        /// the maximum value of the displacement of a node when the layout process is incrementally calculated
        /// </summary>
        public static float Max_Node_Displacement_Incremental = 50.0f;

        /// <summary>
        ///  the maximum value of the displacement of a node when the layout process is not incrementally calculated
        /// </summary>
        public static double Max_Node_Displacement = 100.0 * 3;

        /// <summary>
        /// the check period
        /// </summary>
        public static int Convergence_Check_Periode = 100;

        /// <summary>
        /// Indicates whether all node have the same size 
        /// </summary>
        public static bool Uniform_Leaf_Node_Size = false;

        /// <summary>
        /// the strength of a spring
        /// </summary>
        public static float Spring_Strength = 0.45f;

        /// <summary>
        /// Indicates whether smart repulsion range calculation is used
        /// </summary>
        public static bool Use_Smart_Repulsion_Range_Calculation = false;

        /// <summary>
        /// the check period interval for the grid calculation (multilevel scaling)
        /// </summary>
        public static int Grid_Calculation_Check_Periode = 10;

        /// <summary>
        /// the minimal distance for applying the repulsion strength
        /// </summary>
        public static float Min_Repulsion_Dist = Edge_Length / 10;

        /// <summary>
        /// the repulsion strength 
        /// </summary>
        public static float Repulsion_Strength = 50;

        /// <summary>
        /// the gravity range factor
        /// </summary>
        public static float Gravity_Range_Factor = 2.0f;

        /// <summary>
        /// the strength of the global gravity
        /// </summary>
        public static float Gravity_Strength = 0.8f;

        /// <summary>
        /// the compound gravity range factor
        /// </summary>
        public static float Compound_Gravity_Range_Factor = 1.5f;

        /// <summary>
        /// the strength of the gravity in a compound node
        /// </summary>
        public static float Compound_Gravity_Strength = 1.5f;

        /// <summary>
        /// Indicates whether multilevel scaling is used
        /// </summary>
        public static bool Multilevel_Scaling = false;

        /// <summary>
        /// the cooling adjuster
        /// </summary>
        public static int Cooling_Adjuster = 1;

        /// <summary>
        /// true if the parameter edgeLength/repulsionStrength should be calculated automatically 
        /// </summary>
        public static bool Automatic_Parameter_Calculation = true;

        /// <summary>
        /// true if the layout process continues until a "good" layout is found
        /// </summary>
        public static bool Iterativ_Parameter_Calculation = false;

        /// <summary>
        /// the cooling factor
        /// </summary>
        private float coolingFactor;

        /// <summary>
        /// the initial cooling factor
        /// </summary>
        private float initialCoolingFactor;

        /// <summary>
        /// the amount of cooling cycles
        /// </summary>
        private float coolingcycle;

        /// <summary>
        /// The final temperature
        /// </summary>
        private float finalTemperature;

        /// <summary>
        /// the maximum number of cooling cycles
        /// </summary>
        private int maxCoolingCycle;

        /// <summary>
        /// the maximal displacement for a node
        /// </summary>
        private double maxNodeDisplacement;

        /// <summary>
        /// maximum number of iterations 
        /// </summary>
        private int maxIterations;

        /// <summary>
        /// the total displacement threshold
        /// </summary>
        private double totalDisplacementThreshold;

        /// <summary>
        /// the repulsion range
        /// </summary>
        private double repulsionRange;

        /// <summary>
        /// the displacement threshold per node
        /// </summary>
        private double displacementThresholdPerNode;

        /// <summary>
        /// the amount of iterations done
        /// </summary>
        private double totalIterations;

        /// <summary>
        /// the amount of the displacement done
        /// </summary>
        private decimal totalDisplacement;

        /// <summary>
        /// the old amount of the displacement done
        /// </summary>
        private decimal oldTotalDisplacement;

        /// <summary>
        /// current level (multilevel scaling)
        /// </summary>
        private int level;

        /// <summary>
        /// number of levels
        /// </summary>
        private int noOfLevels;

        public CoseLayoutSettings()
        {
            coolingFactor = 1.0f;
            initialCoolingFactor = 1.0f;
            coolingcycle = 0;
            finalTemperature = 0;
            maxIterations = 4000;
            finalTemperature = Convergence_Check_Periode / maxIterations;
            maxCoolingCycle = maxIterations / Convergence_Check_Periode;
            displacementThresholdPerNode = (3.0 * Edge_Length) / 100;
            totalIterations = 0;
            totalDisplacement = 0.0m;
            oldTotalDisplacement = 0.0m;
            level = 0;
        }

        public float InitialCoolingFactor { get => initialCoolingFactor; set => initialCoolingFactor = value; }
        public float Coolingcycle { get => coolingcycle; set => coolingcycle = value; }
        public float FinalTemperature { get => finalTemperature; set => finalTemperature = value; }
        public int MaxCoolingCycle { get => maxCoolingCycle; set => maxCoolingCycle = value; }
        public int MaxIterations { get => maxIterations; set => maxIterations = value; }
        public double TotalDisplacementThreshold { get => totalDisplacementThreshold; set => totalDisplacementThreshold = value; }
        public double RepulsionRange { get => repulsionRange; set => repulsionRange = value; }
        public double DisplacementThresholdPerNode { get => displacementThresholdPerNode; set => displacementThresholdPerNode = value; }
        public double TotalIterations { get => totalIterations; set => totalIterations = value; }
        public decimal OldTotalDisplacement { get => oldTotalDisplacement; set => oldTotalDisplacement = value; }
        public int Level { get => level; set => level = value; }
        public float CoolingFactor { get => coolingFactor; set => coolingFactor = value; }
        public double MaxNodeDisplacement { get => maxNodeDisplacement; set => maxNodeDisplacement = value; }
        public decimal TotalDisplacement { get => totalDisplacement; set => totalDisplacement = value; }
        public int NoOfLevels { get => noOfLevels; set => noOfLevels = value; }
    }
}

