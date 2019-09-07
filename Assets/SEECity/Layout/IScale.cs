using UnityEngine;
using SEE.DataModel;

namespace SEE.Layout
{
    /// <summary>
    /// Abstract super class of all classes providing x, y, z lengths 
    /// of nodes based on their metrics.
    /// </summary>
    internal abstract class IScale
    {
        /// <summary>
        /// Constructor defining the node metrics to be used for determining
        /// the lengths of the nodes.
        /// </summary>
        /// <param name="widthMetric">metric for node width</param>
        /// <param name="heightMetric">metric for node height</param>
        /// <param name="breadthMetric">metric for node breadth</param>
        public IScale(string widthMetric, string heightMetric, string breadthMetric)
        {
            this.widthMetric = widthMetric;
            this.heightMetric = heightMetric;
            this.breadthMetric = breadthMetric;
        }

        protected readonly string widthMetric;
        protected readonly string heightMetric;
        protected readonly string breadthMetric;

        /// <summary>
        /// Returns the x, y, z lengths of the given node.
        /// </summary>
        /// <param name="node">node for which to determine the x, y, z lengths</param>
        /// <returns>x, y, z lengths of node</returns>
        public abstract Vector3 Lengths(Node node);
    }
}
