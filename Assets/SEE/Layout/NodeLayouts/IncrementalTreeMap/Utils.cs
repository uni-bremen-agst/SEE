using System;
using System.Collections.Generic;
using System.Linq;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    public static class Utils
    {
        public static T ArgMaxJ<T>(ICollection<T> collection, Func<T, IComparable> eval)
        {
            var bestVal = collection.Max(eval);
            return collection.First(x => eval(x).CompareTo(bestVal) == 0);
        }
        
        public static T ArgMinJ<T>(ICollection<T> collection, Func<T, IComparable> eval)
        {
            var bestVal = collection.Min(eval);
            return collection.First(x => eval(x).CompareTo(bestVal) == 0);
        }

        public static TRectangle CreateParentRectangle(IList<TNode> nodes)
        {
            double x = nodes.Min(node => node.Rectangle.x);
            double z = nodes.Min(node => node.Rectangle.z);
            double width = nodes.Max(node => node.Rectangle.x + node.Rectangle.width) - x;
            double depth = nodes.Max(node => node.Rectangle.z + node.Rectangle.depth) - z;
            return new TRectangle(x, z, width, depth);
        }
        
        
    }
}