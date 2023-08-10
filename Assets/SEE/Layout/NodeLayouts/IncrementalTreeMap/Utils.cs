using System;
using System.Collections.Generic;
using System.Linq;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    public static class Utils
    {
        public static T ArgMax<T>(ICollection<T> collection, Func<T, IComparable> eval)
        {
            var bestVal = collection.Max(eval);
            return collection.First(x => eval(x).CompareTo(bestVal) == 0);
        }
        
        public static T ArgMin<T>(ICollection<T> collection, Func<T, IComparable> eval)
        {
            var bestVal = collection.Min(eval);
            return collection.First(x => eval(x).CompareTo(bestVal) == 0);
        }

        public static Rectangle CreateParentRectangle(IList<Node> nodes)
        {
            double x = nodes.Min(node => node.Rectangle.x);
            double z = nodes.Min(node => node.Rectangle.z);
            double width = nodes.Max(node => node.Rectangle.x + node.Rectangle.width) - x;
            double depth = nodes.Max(node => node.Rectangle.z + node.Rectangle.depth) - z;
            return new Rectangle(x, z, width, depth);
        }
        
        public static void TransformRectangles(IList<Node> nodes, Rectangle newRectangle ,Rectangle oldRectangle)
        {

            // linear transform line   x1<---->x2
            //               to line       y1<------->y2
            // f  : [x1,x2] -> [y1,y2]
            // f  : x   maps to (x - x1) * ((y2-y1)/(x2-x1)) + y1

            double scaleX = newRectangle.width / oldRectangle.width;
            double scaleZ = newRectangle.depth / oldRectangle.depth;

            foreach( var node in nodes)
            {
                node.Rectangle.x = (node.Rectangle.x - oldRectangle.x) * scaleX + newRectangle.x;
                node.Rectangle.z = (node.Rectangle.z - oldRectangle.z) * scaleZ + newRectangle.z;
                node.Rectangle.width *= scaleX;
                node.Rectangle.depth *= scaleZ;
            }
        }
    }
}