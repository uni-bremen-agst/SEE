using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public class CoseCoarsenEdge : CoseEdge
    {
        public CoseCoarsenEdge(CoseNode source, CoseNode target) : base(source, target)
        {
        }

        public CoseCoarsenEdge() : base(null, null)
        {

        }
    }
}

