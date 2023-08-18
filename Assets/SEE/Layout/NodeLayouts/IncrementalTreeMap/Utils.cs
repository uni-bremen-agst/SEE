using System;
using System.Collections.Generic;
using System.Linq;
using static SEE.Layout.NodeLayouts.IncrementalTreeMap.Direction;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    /// <summary>
    /// Provides helper functions for incremental tree map layout.
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// arg max function, returns a item of a collection, that maximizes a function.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="eval">function to be maximized</param>
        /// <returns></returns>
        public static T ArgMax<T>(ICollection<T> collection, Func<T, IComparable> eval)
        {
            var bestVal = collection.Max(eval);
            return collection.First(x => eval(x).CompareTo(bestVal) == 0);
        }

        /// <summary>
        /// arg min function, returns a item of a collection, that minimizes a function.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="eval">function to be minimized</param>
        /// <returns></returns>
        public static T ArgMin<T>(ICollection<T> collection, Func<T, IComparable> eval)
        {
            var bestVal = collection.Min(eval);
            return collection.First(x => eval(x).CompareTo(bestVal) == 0);
        }

        /// <summary>
        /// Creates a new rectangle that includes all rectangles of <paramref name="nodes"/>.
        /// </summary>
        /// <param name="nodes">nodes with set rectangles</param>
        /// <returns>new parent rectangle</returns>
        public static Rectangle CreateParentRectangle(IList<Node> nodes)
        {
            double x = nodes.Min(node => node.Rectangle.X);
            double z = nodes.Min(node => node.Rectangle.Z);
            double width = nodes.Max(node => node.Rectangle.X + node.Rectangle.Width) - x;
            double depth = nodes.Max(node => node.Rectangle.Z + node.Rectangle.Depth) - z;
            return new Rectangle(x, z, width, depth);
        }

        /// <summary>
        /// A list of <paramref name="nodes"/> with rectangles that are laid out
        /// in a <paramref name="oldRectangle"/> will be transformed (linear), so that they fit
        /// in <paramref name="newRectangle"/>
        /// </summary>
        /// <param name="nodes">nodes with rectangles that should be transformed</param>
        /// <param name="newRectangle">new parent rectangle</param>
        /// <param name="oldRectangle">old parent rectangle</param>
        public static void TransformRectangles(IList<Node> nodes, Rectangle newRectangle, Rectangle oldRectangle)
        {
            double scaleX = newRectangle.Width / oldRectangle.Width;
            double scaleZ = newRectangle.Depth / oldRectangle.Depth;

            foreach (var node in nodes)
            {
                node.Rectangle.X = (node.Rectangle.X - oldRectangle.X) * scaleX + newRectangle.X;
                node.Rectangle.Z = (node.Rectangle.Z - oldRectangle.Z) * scaleZ + newRectangle.Z;
                node.Rectangle.Width *= scaleX;
                node.Rectangle.Depth *= scaleZ;
            }
        }

        /// <summary>
        /// Clones a list of nodes and preserves the layout including segments and rectangles.
        /// Therefor the <see cref="Segment"/>s and <see cref="Rectangle"/>s of the nodes are cloned to.
        /// </summary>
        /// <param name="nodes">siblings to be cloned</param>
        /// <returns>dictionary that maps ID to new clone node</returns>
        public static IDictionary<string, Node> CloneGraph(IList<Node> nodes)
        {
            // clones the nodes only partially: does NOT clone the segment
            IDictionary<string, Node> clonesMap = nodes.ToDictionary(node => node.ID,
                node => new Node(node.ID)
                {
                    Rectangle = node.Rectangle.Clone(), Size = node.Size
                }
            );
            CloneSegments(nodes, clonesMap);
            return clonesMap;
        }

        /// <summary>
        /// Clones all <see cref="Segment"/>s of <paramref name="from"/>
        /// and register the segment clones to the nodes of <paramref name="to"/>.
        /// The nodes of <paramref name="from"/> and <paramref name="to"/> needs to have the same IDs.
        /// </summary>
        /// <param name="from">nodes with segment structure, that should be copied</param>
        /// <param name="to">dictionary that maps ID to nodes, that should get the segment structure </param>
        public static void CloneSegments(IEnumerable<Node> from, IDictionary<string, Node> to)
        {
            var segments = from.SelectMany(n => n.SegmentsDictionary().Values).ToHashSet();
            foreach (var segment in segments)
            {
                var segmentClone = new Segment(segment.IsConst, segment.IsVertical);
                foreach (var node in segment.Side1Nodes.ToList())
                {
                    to[node.ID].RegisterSegment(segmentClone,
                        segmentClone.IsVertical ? Right : Upper);
                }

                foreach (var node in segment.Side2Nodes.ToList())
                {
                    to[node.ID].RegisterSegment(segmentClone,
                        segmentClone.IsVertical ? Left : Lower);
                }
            }
        }
    }
}
