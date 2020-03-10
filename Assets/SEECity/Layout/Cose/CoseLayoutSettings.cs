using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public class CoseLayoutSettings : MonoBehaviour
    {
        /// <summary>
        /// the margin of a graph
        /// </summary>
        public static int Graph_Margin = 2;

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
        /// the simple node size
        /// </summary>
        public static int Simple_Node_Size = 1;

        /// <summary>
        /// the factor by which the edge length increases with every level of the edges source/ target node
        /// </summary>
        public static double Per_Level_Ideal_Edge_Length_Factor = 0.1;

        /// <summary>
        /// Indicates whether the layout process in incrementally calculated
        /// </summary>
        public static bool Incremental = true;

        /// <summary>
        /// the maximum value of the displacement of a node when the layout prozess is incrementally calculated
        /// </summary>
        public static double Max_Node_Displacement_Incremental = 100.0;

        /// <summary>
        ///  the maximum value of the displacement of a node when the layout prozess is not incrementally calculated
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
        public static double Spring_Strength = 0.4;

        /// <summary>
        /// Indicates whether is smart repulsion range calculation is used
        /// </summary>
        public static bool Use_Smart_Repulsion_Range_Calculation = true;

        /// <summary>
        /// the check period interval for the grid calculation (multilevel scaling)
        /// </summary>
        public static int Grid_Calculation_Check_Periode = 10;

        /// <summary>
        /// the minimal distance for applying the repulsion strength
        /// </summary>
        public static double Min_Repulsion_Dist = Edge_Length / 10.0;

        /// <summary>
        /// the repulsion strength 
        /// </summary>
        public static double Repulsion_Strength = 300;

        /// <summary>
        /// the gravity range factor
        /// </summary>
        public static double Gravity_Range_Factor = 2.0;

        /// <summary>
        /// the strength of the global gravity
        /// </summary>
        public static double Gravity_Strength = 0.8;

        /// <summary>
        /// the compound gravity range factor
        /// </summary>
        public static double Compound_Gravity_Range_Factor = 1.5;

        /// <summary>
        /// the strength of the gravity in a compound node
        /// </summary>
        public static double Compound_Gravity_Strength = 1.5;

        /// <summary>
        /// Indicates whether multilevel scaling is used
        /// </summary>
        public static bool Multilevel_Scaling = true;

        /// <summary>
        /// the cooling adjuster
        /// </summary>
        public static int Cooling_Adjuster = 1;

        /// <summary>
        /// the cooling factor
        /// </summary>
        private double coolingFactor;

        /// <summary>
        /// the inital cooling factor
        /// </summary>
        private double initialCoolingFactor;

        /// <summary>
        /// the amount of cooling cycles
        /// </summary>
        private double coolingcycle;

        /// <summary>
        /// The final temperature
        /// </summary>
        private double finalTemperature;

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
        private double totalDisplacement;

        /// <summary>
        /// the old amount of the displacement done
        /// </summary>
        private double oldTotalDisplacement;

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
            coolingFactor = 1.0;
            initialCoolingFactor = 1.0;
            coolingcycle = 0;
            finalTemperature = 0;
            maxIterations = 2500;
            finalTemperature = Convergence_Check_Periode / maxIterations;
            maxCoolingCycle = maxIterations / Convergence_Check_Periode;
            displacementThresholdPerNode = (3.0 * Edge_Length) / 100;
            totalIterations = 0;
            totalDisplacement = 0.0;
            oldTotalDisplacement = 0.0;
            level = 0;
        }

        public double InitialCoolingFactor { get => initialCoolingFactor; set => initialCoolingFactor = value; }
        public double Coolingcycle { get => coolingcycle; set => coolingcycle = value; }
        public double FinalTemperature { get => finalTemperature; set => finalTemperature = value; }
        public int MaxCoolingCycle { get => maxCoolingCycle; set => maxCoolingCycle = value; }
        public int MaxIterations { get => maxIterations; set => maxIterations = value; }
        public double TotalDisplacementThreshold { get => totalDisplacementThreshold; set => totalDisplacementThreshold = value; }
        public double RepulsionRange { get => repulsionRange; set => repulsionRange = value; }
        public double DisplacementThresholdPerNode { get => displacementThresholdPerNode; set => displacementThresholdPerNode = value; }
        public double TotalIterations { get => totalIterations; set => totalIterations = value; }
        public double OldTotalDisplacement { get => oldTotalDisplacement; set => oldTotalDisplacement = value; }
        public int Level { get => level; set => level = value; }
        public double CoolingFactor { get => coolingFactor; set => coolingFactor = value; }
        public double MaxNodeDisplacement { get => maxNodeDisplacement; set => maxNodeDisplacement = value; }
        public double TotalDisplacement { get => totalDisplacement; set => totalDisplacement = value; }
        public int NoOfLevels { get => noOfLevels; set => noOfLevels = value; }
    }
}

