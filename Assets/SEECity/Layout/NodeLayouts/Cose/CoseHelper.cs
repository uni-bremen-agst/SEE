using SEE.DataModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public class CoseHelper
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

        public static Rect NewRect(Vector3 scale, Vector3 center)
        {
            return new Rect
            {
                x = center.x - scale.x / 2,
                y = center.z - scale.z / 2,
                width = scale.x,
                height = scale.z
            }; 
        }
    }
}


