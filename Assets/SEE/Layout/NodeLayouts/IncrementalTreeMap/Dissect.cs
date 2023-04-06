using System.Collections.Generic;
//using System.Collections.Generic;
using System.Linq;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    static class Dissect{
        static public void dissect(TRectangle rectangle, IEnumerable<TNode> nodes)
        {
            List<TNode> nodesList =  nodes.ToList();
            nodesList.Sort((x,y) => (x.Size.CompareTo(y.Size)));
            dissect(rectangle,
                    nodesList,
                    new TSegment(true, true),
                    new TSegment(true, true), 
                    new TSegment(true, false),
                    new TSegment(true, false));
        }

        static private void dissect( TRectangle rectangle, 
                                IList<TNode> nodes,
                                TSegment leftBound,
                                TSegment rightBound,
                                TSegment upperBound, 
                                TSegment lowerBound)
        {
            if(nodes.Count == 1)
            {
                TNode node = nodes[0];
                node.rectangle = rectangle;
                node.registerSegment(leftBound, Direction.Left);
                node.registerSegment(rightBound,Direction.Right);
                node.registerSegment(lowerBound,Direction.Lower);
                node.registerSegment(upperBound,Direction.Upper);
                return;
            }
            int splitIndex;
            if(nodes.Sum( x => x.Size)  <=  nodes.Last().Size )
            {
                splitIndex = nodes.Count -1;
            }
            else
            {

            }


        }

    }
}

//   Dissect in Python
//   -----------------
//
//    def __dissect(rectangle : Rectangle,
//                  areas : list[tuple[str,float]]) -> Node:
//        if len(areas) == 1:
//            return Node(name = areas[0][0],
//                        rectangle=rectangle, 
//                        split=None,
//                        child_1= None,
//                        child_2= None)
//
//        if sum(map(lambda x: x[1], areas)) <= areas[-1][1] * 3.0:
//            k = -1
//        else:
//            for k in range(1,len(areas)+1):
//                if sum(map(lambda x: x[1], areas[:k]))  * 3.0 >= sum(map(lambda x: x[1], areas)):
//                    break
//        areas_1 = areas[:k]
//        areas_2 = areas[k:]
//        ratio = sum(map(lambda x: x[1],areas_1))/sum(map(lambda x: x[1],areas))
//
//        if rectangle.width >= rectangle.height:
//            split = "w"
//            rectangle_1 = rectangle.copy()
//            rectangle_1.width *= ratio
//            rectangle_2 = rectangle.copy()
//            rectangle_2.x += ratio * rectangle.width
//            rectangle_2.width *= (1-ratio)
//        else:
//            split = "h"
//            rectangle_1 = rectangle.copy()
//            rectangle_1.height *= ratio
//            rectangle_2 = rectangle.copy()
//            rectangle_2.y += ratio * rectangle.height
//            rectangle_2.height *= (1-ratio)
//
//        child_1 = Layout.__dissect( rectangle = rectangle_1,
//                                       areas = areas_1)
//        child_2 = Layout.__dissect( rectangle = rectangle_2,
//                                    areas = areas_2)
//        return Node(name="INNER_NODE",
//                    rectangle=rectangle,
//                    child_1=child_1,
//                    child_2=child_2,
//                    split = split)
