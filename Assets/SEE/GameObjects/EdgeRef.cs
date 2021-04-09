using SEE.DataModel.DG;
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace SEE.GO
{
    /// <summary>
    /// A reference to a graph edge that can be attached to a game object as a component.
    /// </summary>
    public class EdgeRef : GraphElementRef
    {
        [NonSerialized] private static Dictionary<Edge, EdgeRef> edgeToEdgeRefDict = new Dictionary<Edge, EdgeRef>();

        /// <summary>
		/// The graph edge this edge reference is referring to. It will be set either
		/// by a graph renderer while in editor mode or at runtime by way of an
		/// AbstractSEECity object.
		/// It will not be serialized to prevent duplicating and endless serialization
		/// by both Unity and Odin.
        /// </summary>
        public Edge Value
        {
            get => (Edge)elem;
            set
            {
                if (elem != value)
                {
                    if (elem != null)
                    {
                        edgeToEdgeRefDict.Remove((Edge)elem);
                    }
                    elem = value;
                    if (elem != null)
                    {
                        edgeToEdgeRefDict[(Edge)elem] = this;
                    }
                }
            }
        }

        /// <summary>
        /// The unique ID of the source node of the edge referenced.
        /// </summary>
        public string SourceNodeID { get; internal set; }

        /// <summary>
        /// The unique ID of the target node of the edge referenced.
        /// </summary>
        public string TargetNodeID { get; internal set; }

        /// <summary>
        /// Returns the EdgeRef referring to <paramref name="edge"/>.
        /// </summary>
        /// <param name="edge">edge whose EdgeRef is requested</param>
        /// <returns>the EdgeRef referring to <paramref name="edge"/> or null if there is none</returns>
        public static EdgeRef Get(Edge edge)
        {
            Assert.IsNotNull(edge);
            return edgeToEdgeRefDict[edge];
        }

        public static bool TryGet(Edge edge, out EdgeRef edgeRef)
        {
            bool result = false;
            edgeRef = null;
            if (edgeToEdgeRefDict.TryGetValue(edge, out EdgeRef v))
            {
                result = true;
                edgeRef = v;
            }
            return result;
        }
    }
}
