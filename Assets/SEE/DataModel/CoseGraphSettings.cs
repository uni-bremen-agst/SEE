using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.Layout;
using static SEE.Game.AbstractSEECity;

namespace SEE.DataModel
{
    public class CoseGraphSettings
    {
        public int EdgeLength = CoseLayoutSettings.Edge_Length;
        public bool UseSmartIdealEdgeCalculation = CoseLayoutSettings.Use_Smart_Ideal_Edge_Calculation;
        public float PerLevelIdealEdgeLengthFactor = CoseLayoutSettings.Per_Level_Ideal_Edge_Length_Factor;
        public bool Incremental = CoseLayoutSettings.Incremental;
        public bool UseSmartRepulsionRangeCalculation = CoseLayoutSettings.Use_Smart_Repulsion_Range_Calculation;
        public float GravityStrength = CoseLayoutSettings.Gravity_Strength;
        public float CompoundGravityStrength = CoseLayoutSettings.Compound_Gravity_Strength;
        public float RepulsionStrength = CoseLayoutSettings.Repulsion_Strength;
        public bool multiLevelScaling = CoseLayoutSettings.Multilevel_Scaling;
        public Dictionary<string, bool> ListDirToggle = new Dictionary<string, bool>();
        public int index = 0;
        public Dictionary<string, NodeLayouts> DirNodeLayout = new Dictionary<string, NodeLayouts>();
        public Dictionary<string, InnerNodeKinds> DirShape = new Dictionary<string, InnerNodeKinds>();
        public List<Node> rootDirs = new List<Node>();
        public Dictionary<string, bool> show = new Dictionary<string, bool>();
        public bool useOptAlgorithm = false;
    }
}

