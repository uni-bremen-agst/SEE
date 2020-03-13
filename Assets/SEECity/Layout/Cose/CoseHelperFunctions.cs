using SEE.DataModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public class CoseHelperFunctions
    {
        public static void CalculateListSublayouts()
        {

        }

        public static SublayoutNode CheckIfNodeIsSublayouRoot(List<SublayoutNode> sublayoutNodes, Node node)
        {
            foreach (SublayoutNode subLayoutNode in sublayoutNodes)
            {
                if (subLayoutNode.Node == node)
                {
                    return subLayoutNode;
                }
            }
            return null;
        }
    }
}


