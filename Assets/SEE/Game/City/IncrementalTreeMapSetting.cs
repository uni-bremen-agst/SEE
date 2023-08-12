using System;
using System.Collections.Generic;
using SEE.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.City
{
    [Serializable]
    [HideReferenceObjectPicker]
    public class IncrementalTreeMapSetting : ConfigIO.PersistentConfigItem
    {
        [SerializeField]
        public int NumberOfLocalMoves = 3;

        [SerializeField]
        public int BranchingLimit = 4;

        [SerializeField]
        public float PNorm = 3f;

        [SerializeField]
        public float GradientPrecision = 0.00001f;

        [SerializeField]
        public float Padding = 0.005f;
        
        public void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(NumberOfLocalMoves, NumberOfLocalMovesLabel);
            writer.Save(BranchingLimit, BranchingLimitLabel);
            writer.Save(PNorm, PNormLabel);
            writer.Save(GradientPrecision, GradientPrecisionLabel);
            writer.Save(Padding, PaddingLabel);
            writer.EndGroup();
        }
        
        public bool Restore(Dictionary<string, object> attributes, string label)
        {
            if (!attributes.TryGetValue(label, out object dictionary)) return false;
            Dictionary<string, object> values = dictionary as Dictionary<string, object>;
            var result = ConfigIO.Restore(values, NumberOfLocalMovesLabel, ref NumberOfLocalMoves);
            result |= ConfigIO.Restore(values, BranchingLimitLabel, ref BranchingLimit);
            result |= ConfigIO.Restore(values, PNormLabel, ref PNorm);
            result |= ConfigIO.Restore(values, GradientPrecisionLabel, ref GradientPrecision);
            result |= ConfigIO.Restore(values, PaddingLabel, ref Padding);
            return result;
        }

        private const string NumberOfLocalMovesLabel = "NumberOfLocalMoves";
        private const string BranchingLimitLabel = "BranchingLimit";
        private const string PNormLabel = "PNorm";
        private const string GradientPrecisionLabel = "GradientPrecision";
        private const string PaddingLabel = "Padding";
    }
}
