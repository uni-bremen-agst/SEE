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

        public static SublayoutNode CheckIfNodeIsSublayouRoot(ICollection<SublayoutNode> sublayoutNodes, string linkname)
        {
            foreach (SublayoutNode subLayoutNode in sublayoutNodes)
            {
                if (subLayoutNode.Node.LinkName == linkname)
                {
                    return subLayoutNode;
                }
            }
            return null;
        }

        public static SublayoutLayoutNode CheckIfNodeIsSublayouRoot(ICollection<SublayoutLayoutNode> sublayoutNodes, string linkname)
        {
            foreach (SublayoutLayoutNode subLayoutNode in sublayoutNodes)
            {
                if (subLayoutNode.Node.LinkName == linkname)
                {
                    return subLayoutNode;
                }
            }
            return null;
        }
    }
}


