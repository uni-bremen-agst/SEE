using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public class CoseCoarsenEdge : CoseEdge
    {
        /// <summary>
        /// constructor for an edge of a coarsen graph
        /// </summary>
        /// <param name="source">the source node</param>
        /// <param name="target">the target node</param>
        public CoseCoarsenEdge(CoseNode source, CoseNode target) : base(source, target)
        {
        }

        /// <summary>
        /// constructor
        /// </summary>
        public CoseCoarsenEdge() : base(null, null)
        {

        }
    }
}

