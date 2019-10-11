using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Abstract super class of all classes providing normalized node metrics.
    /// </summary>
    public abstract class IScale
    {
        /// <summary>
        /// Constructor defining the node metrics to be normalized.
        /// </summary>
        /// <param name="metrics">node metrics for scaling</param>
        public IScale(IList<string> metrics, float minimalLength, float maximalLength)
        {
            this.metrics = metrics;
            this.minimalLength = minimalLength;
            this.maximalLength = maximalLength;
        }

        protected readonly IList<string> metrics;

        protected readonly float minimalLength;
        protected readonly float maximalLength;

        protected float WithinLimits(float value)
        {
            return Mathf.Clamp(value, minimalLength, maximalLength);
        }

        /// <summary>
        /// Yields a normalized value of the given node metric. The type of normalization
        /// is determined by concrete subclasses.
        /// </summary>
        /// <param name="node">node for which to determine the normalized value</param>
        /// <param name="metric">name of the node metric</param>
        /// <returns>normalized value of node metric</returns>
        public abstract float GetNormalizedValue(Node node, string metric);
    }
}
