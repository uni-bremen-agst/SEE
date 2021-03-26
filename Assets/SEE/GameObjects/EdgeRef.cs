using SEE.DataModel.DG;
using System;

namespace SEE.GO
{
    /// <summary>
    /// A reference to a graph edge that can be attached to a game object as a component.
    /// </summary>
    public class EdgeRef : GraphElementRef
    {
        /// <summary>
		/// The graph edge this edge reference is referring to. It will be set either
		/// by a graph renderer while in editor mode or at runtime by way of an
		/// AbstractSEECity object.
		/// It will not be serialized to prevent duplicating and endless serialization
		/// by both Unity and Odin.
        /// </summary>
        [NonSerialized]
        public Edge edge;

        /// <summary>
        /// The unique ID of the source node of the edge referenced.
        /// </summary>
        public string SourceNodeID { get; internal set; }

        /// <summary>
        /// The unique ID of the target node of the edge referenced.
        /// </summary>
        public string TargetNodeID { get; internal set; }
    }
}
