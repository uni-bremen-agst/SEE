using System;

namespace SEE.Game.City
{
    /// <summary>
    /// To what kind of concept a property refers to.
    /// </summary>
    [Serializable]
    public enum PropertyKind
    {
        /// <summary>
        /// Refers to a node type.
        /// </summary>
        Type,
        /// <summary>
        /// Refers to a node metric.
        /// </summary>
        Metric
    }
}
