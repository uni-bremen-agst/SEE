﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
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
        /// the simple node size
        /// </summary>
        public static int Simple_Node_Size = 1;

        /// <summary>
        /// the factor by which the edge length increases with every level of the edges source/ target node
        /// </summary>
        public static float Per_Level_Ideal_Edge_Length_Factor = 0.1f;

        /// <summary>
        /// Indicates whether the layout process in incrementally calculated
        /// </summary>
        public static bool Incremental = true;

        /// <summary>
        /// the maximum value of the displacement of a node when the layout prozess is incrementally calculated
        /// </summary>
        public static float Max_Node_Displacement_Incremental = 30.0f;//100.0f;

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
        public static float Spring_Strength = 0.4f;

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
        public static float Min_Repulsion_Dist = Edge_Length / 10.0f;

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
        public static bool Multilevel_Scaling = true;

        /// <summary>
        /// the cooling adjuster
        /// </summary>
        public static int Cooling_Adjuster = 1;

        /// <summary>
        /// the cooling factor
        /// </summary>
        private float coolingFactor;

        /// <summary>
        /// the inital cooling factor
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
            coolingFactor = 1.0f;
            initialCoolingFactor = 1.0f;
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

        public float InitialCoolingFactor { get => initialCoolingFactor; set => initialCoolingFactor = value; }
        public float Coolingcycle { get => coolingcycle; set => coolingcycle = value; }
        public float FinalTemperature { get => finalTemperature; set => finalTemperature = value; }
        public int MaxCoolingCycle { get => maxCoolingCycle; set => maxCoolingCycle = value; }
        public int MaxIterations { get => maxIterations; set => maxIterations = value; }
        public double TotalDisplacementThreshold { get => totalDisplacementThreshold; set => totalDisplacementThreshold = value; }
        public double RepulsionRange { get => repulsionRange; set => repulsionRange = value; }
        public double DisplacementThresholdPerNode { get => displacementThresholdPerNode; set => displacementThresholdPerNode = value; }
        public double TotalIterations { get => totalIterations; set => totalIterations = value; }
        public double OldTotalDisplacement { get => oldTotalDisplacement; set => oldTotalDisplacement = value; }
        public int Level { get => level; set => level = value; }
        public float CoolingFactor { get => coolingFactor; set => coolingFactor = value; }
        public double MaxNodeDisplacement { get => maxNodeDisplacement; set => maxNodeDisplacement = value; }
        public double TotalDisplacement { get => totalDisplacement; set => totalDisplacement = value; }
        public int NoOfLevels { get => noOfLevels; set => noOfLevels = value; }
    }
}

