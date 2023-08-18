using System;
using System.Collections.Generic;
using SEE.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// The settings for <see cref="Layout.NodeLayouts.IncrementalTreeMapLayout"/>.
    /// </summary>
    [Serializable]
    public class IncrementalTreeMapSetting : ConfigIO.PersistentConfigItem
    {
        /// <summary>
        /// The depth of the local moves search.
        /// </summary>
        [SerializeField]
        [Range(0, 5)]
        [Tooltip("The maximal depth for local moves algorithm. Increase for higher visual quality, " +
                 "decrease for higher stability and to save runtime")]
        public int localMovesDepth = 3;

        /// <summary>
        /// The maximal branching factor of the local moves search.
        /// </summary>
        [SerializeField]
        [Range(1, 10)]
        [Tooltip("The maximal branching factor for local moves algorithm.  Increase for higher visual quality, " +
                 "decrease for higher stability and to save runtime")]
        public int localMovesBranchingLimit = 4;

        /// <summary>
        /// Defines the specific p norm used in the local moves algorithm. See here:
        /// <see cref="Layout.NodeLayouts.IncrementalTreeMap.LocalMoves.AspectRatiosPNorm"/>.
        ///
        /// Notice:
        /// The kind of p norm changes which layout is considered to have the greatest visual quality.
        /// For example with p=1 (Manhattan Norm) the algorithm would
        /// minimize the sum of aspect ratios, while with p=infinity (Chebyshev Norm)
        /// the algorithm would minimize the maximal aspect ratio over the layout nodes.
        /// The other p norms range between these extremes.
        ///
        /// that a higher p value means
        /// Needs therefor a mapping from <see cref="PNormRange"/> to a double value p, which is realized with the
        /// property <see cref="PNorm"/>.
        /// </summary>
        [SerializeField]
        [Tooltip("Norm for the visual quality of a set of nodes, " +
                 "Larger p values lead to stronger penalties for larger deviations in aspect ratio of single nodes.")]
        private PNormRange pNorm = PNormRange.P2Euclidean;

        /// <summary>
        /// The absolute padding between neighboring nodes so that they can be distinguished (in millimeter).
        /// </summary>
        [SerializeField]
        [Range(0.1f, 100f)]
        [LabelText("Padding (mm)")]
        [Tooltip("The distance between two neighbour nodes in mm")]
        public float paddingMm = 5f;

        /// <summary>
        /// The maximal error for the method
        /// <see cref="Layout.NodeLayouts.IncrementalTreeMap.CorrectAreas.GradientDecent"/> as power of 10.
        /// </summary>
        [SerializeField]
        [Range(-7, -2)]
        [LabelText("Gradient Descent Precision (10^n)")]
        [Tooltip("The maximal error for the gradient descent method as power of 10")]
        public int gradientDescentPrecisionExponent = -4;

        /// <summary>
        /// Maps <see cref="pNorm"/> to a double.
        /// </summary>
        public double PNorm
        {
            get
            {
                return pNorm switch
                {
                    (PNormRange.P1Manhattan) => 1d,
                    (PNormRange.P2Euclidean) => 2d,
                    (PNormRange.P3) => 3d,
                    (PNormRange.P4) => 4d,
                    (PNormRange.PInfinityChebyshev) => double.PositiveInfinity,
                    _ => 2d
                };
            }
        }

        public void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(localMovesDepth, LocalMovesDepthLabel);
            writer.Save(localMovesBranchingLimit, LocalMovesBranchingLimitLabel);
            writer.Save(pNorm.ToString(), PNormLabel);
            writer.Save(gradientDescentPrecisionExponent, GradientDescentPrecisionLabel);
            writer.Save(paddingMm, PaddingLabel);
            writer.EndGroup();
        }

        public bool Restore(Dictionary<string, object> attributes, string label)
        {
            if (!attributes.TryGetValue(label, out object dictionary)) return false;
            Dictionary<string, object> values = dictionary as Dictionary<string, object>;
            var result = ConfigIO.Restore(values, LocalMovesDepthLabel, ref localMovesDepth);
            result |= ConfigIO.Restore(values, LocalMovesBranchingLimitLabel, ref localMovesBranchingLimit);
            result |= ConfigIO.RestoreEnum(values, PNormLabel, ref pNorm);
            result |= ConfigIO.Restore(values, GradientDescentPrecisionLabel, ref gradientDescentPrecisionExponent);
            result |= ConfigIO.Restore(values, PaddingLabel, ref paddingMm);
            return result;
        }

        private const string LocalMovesDepthLabel = "LocalMovesDepth";
        private const string LocalMovesBranchingLimitLabel = "LocalMovesBranchingLimit";
        private const string PNormLabel = "PNorm";
        private const string GradientDescentPrecisionLabel = "GradientDescentPrecision";
        private const string PaddingLabel = "Padding";
    }

    /// <summary>
    /// Selection of possible PNorms. Used for better access in Unity Editor for the field
    /// <see cref="IncrementalTreeMapSetting.pNorm"/>.
    /// </summary>
    public enum PNormRange
    {
        P1Manhattan,
        P2Euclidean,
        P3,
        P4,
        PInfinityChebyshev,
    }
}
